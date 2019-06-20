using AutoMarkCheck.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static AutoMarkCheck.Helpers.CredentialManager;
using Html = HtmlAgilityPack;

namespace AutoMarkCheck.Grades
{
    /**
     * <summary>Agent to interface with the Student Records website.</summary>
     */
    public class StudentRecordGradeSource : IGradeSource
    {
        private const string BaseUrl = "https://studentrecords.vuw.ac.nz";
        private const string SamlInitiatePath = "/ssomanager/saml/login?relayState=";
        private const string SamlCallbackPath = "/ssomanager/saml/SSO";
        private const string FinalSamlCallbackPath = "/ssomanager/c/auth/SSB";
        private const string AcademicHistoryUrl = "https://student-records.vuw.ac.nz/pls/webprod/bwsxacdh.P_FacStuInfo";
        private const string HomePageUrl = "https://student-records.vuw.ac.nz/pls/webprod/twbkwbis.P_GenMenu?name=bmenu.P_MainMnu";
        private const string RelayState = "/c/auth/SSB";
        private const string SsoUrl = "https://auth-eis.vuw.ac.nz/samlsso";
        private const string SsoCallback = "https://auth-eis.vuw.ac.nz/commonauth";
        private const string FederationUrl = "https://federation.vuw.ac.nz/adfs/ls";
        private MarkCredentials _credentials;


        public StudentRecordGradeSource(MarkCredentials credentials)
        {
            _credentials = credentials;
        }

        /**
         * <summary>Sets credentials used for authentication with MyVUW.</summary>
         */
        public void SetCredentials(MarkCredentials credentials)
        {
            _credentials = credentials;
        }

        /**
         * <summary>Checks if credentials are valid for this GradeSource</summary>
         */
        public async Task<bool> CheckCredentials()
        {
            try
            {
                await login();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /**
         * <summary>Gets course grades from the MyVictoria website using the specified credentials.</summary>
         * <returns>A list of <see cref="CourseInfo">CourseGrade</see> objects containing the grades for each course.</returns>
         * <exception cref="AuthenticationException">Thrown if the credentials are incorrect or login fails for another reason.</exception>
         */
        public async Task<List<CourseInfo>> GetGrades()
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(GetGrades)}", "Grade grab started.");

                PersistentWebClient client = await login(); //Create logged in session
                // Have to set cookie path to root because by default this cookie only gets set for the home page
                Cookie sessionCookie = client.Cookies["SESSID"];
                sessionCookie.Path = "/";

                string result = (await client.GetWithHeaders(AcademicHistoryUrl, HomePageUrl)).HTML; //Download grade page, the grade page REQUIRES the referer to be the home page, otherwise a 403 forbidden error is returned

                List<CourseInfo> grades = parseGradeHtml(result);

                if (grades.Count == 0) //If no courses were found
                {
                    Logging.Log(Logging.LogLevel.WARNING, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(GetGrades)}", "No courses were found, Term/Year wil be set to current year on next request.");
                }
                else
                    Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(GetGrades)}", $"Successfully got {grades.Count} grades from Student Records.");

                grades.ForEach(grade =>
                {
                    Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(GetGrades)}", $"Grade: {grade.ToString()}");
                });

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(GetGrades)}", "Grade grab finished.");

                return grades;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(GetGrades)}", "Failed to get grades.", ex);
                return new List<CourseInfo>(); //Return empty list
            }
        }

        private List<CourseInfo> parseGradeHtml(string html)
        {
            try
            {
                Html.HtmlDocument doc = new Html.HtmlDocument();
                doc.LoadHtml(html);

                var nodes = doc.DocumentNode.SelectNodes("(/html/body/div[@class='pagebodydiv']/table[@summary='This table displays the student course history information.'])[1]/tr[position()>1]"); //Selects the courses from latest term

                List<CourseInfo> grades = new List<CourseInfo>();
                foreach (var node in nodes)
                {
                    var dataNodes = node.SelectNodes("td"); // Get the table cells for the current row
                    string fullCourseName = dataNodes[0].InnerText; // Grab the full name of the course eg COMP102
                    grades.Add(new CourseInfo
                    {
                        Subject = Regex.Match(fullCourseName, @"(^[A-Z]+)").Value, // Get the first part of the full name eg COMP
                        Course = Regex.Match(fullCourseName, @"([0-9]+$)").Value, // Get the last part of the full name eg 102
                        CourseTitle = dataNodes[1].InnerText,
                        CRN = null,
                        Grade = dataNodes[6].InnerText.Replace("&nbsp;", "") // If there is no grade, the cell contains &nbsp; replace it with nothing
                    });
                }

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(parseGradeHtml)}", "Successfully parsed grades HTML.");

                return grades;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(parseGradeHtml)}", "Failed to parsed grades HTML.", ex);
                return new List<CourseInfo>();
            }
        }

        /**
         * <summary>Initiates a login request and follows the login flow until user credentials are required.</summary>
         * <returns>A <see cref="PersistentWebClient">PersistentWebClient</see> for the login session.</returns>
         */
        private async Task<PersistentWebClient> initiateLogin()
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(initiateLogin)}", "Initiating login.");

                PersistentWebClient client = new PersistentWebClient(); // Cookie persistence is required for this login process

                // Get the server to initate a login using SAML
                string ssoRedirect = await client.Get(BaseUrl + SamlInitiatePath + RelayState);

                Html.HtmlDocument doc = new Html.HtmlDocument();
                doc.LoadHtml(ssoRedirect);

                var samlNode = doc.DocumentNode.SelectSingleNode("//input[@name='SAMLRequest']"); // Selects SAML data element using XPath query
                string samlData = samlNode.GetAttributeValue("value", ""); // Gets SAML data


                // Use previous SAML data to forward to another SAML endpoint that will then forward again to the last login endpoint
                string federationRedirect = await client.Post(SsoUrl, new Dictionary<string, string> {
                    { "RelayState", HttpUtility.UrlEncode(RelayState) }, // URL encode post data
                    { "SAMLRequest", HttpUtility.UrlEncode(samlData) }
                });

                doc = new Html.HtmlDocument();
                doc.LoadHtml(federationRedirect);

                samlNode = doc.DocumentNode.SelectSingleNode("//input[@name='SAMLRequest']"); // Selects SAML data element using XPath query
                samlData = samlNode.GetAttributeValue("value", ""); // Gets SAML data

                var relayStateNode = doc.DocumentNode.SelectSingleNode("//input[@name='RelayState']"); //Selects relay state element using XPath query
                string sessionRelayState = relayStateNode.GetAttributeValue("value", ""); //Gets relay state for this login session

                // Send final SAML data to the login endpoint so it can set the SAML session cookie
                await client.Post(FederationUrl, new Dictionary<string, string> {
                    { "RelayState", HttpUtility.UrlEncode(sessionRelayState) },
                    { "SAMLRequest", HttpUtility.UrlEncode(samlData) }
                });

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(initiateLogin)}", "Finished login initiation.");

                return client;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(initiateLogin)}", "Error initiating login.", ex);
                throw ex;
            }
        }

        /**
         * <summary>Finalizes the login process by completing the SAML authentication flow.</summary>
         * <param name="client">A <see cref="PersistentWebClient">PersistentWebClient</see> for the authenticated session to finalize.</param>
         */
        private async Task finalizeLogin(PersistentWebClient client)
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(finalizeLogin)}", "Finalizing login.");

                string ssoRedirect = await client.Get(FederationUrl);

                Html.HtmlDocument doc = new Html.HtmlDocument();
                doc.LoadHtml(ssoRedirect);

                var samlNode = doc.DocumentNode.SelectSingleNode("//input[@name='SAMLResponse']"); // Select SAML response using an XPath query
                string samlData = samlNode.GetAttributeValue("value", ""); // Gets SAML data

                var relayStateNode = doc.DocumentNode.SelectSingleNode("//input[@name='RelayState']"); // Select relay state using an XPath query
                string sessionRelayState = relayStateNode.GetAttributeValue("value", ""); // Gets relay state

                var response = await client.PostWithHeaders(SsoCallback, new Dictionary<string, string> {
                    { "SAMLResponse", HttpUtility.UrlEncode(samlData) },
                    { "RelayState", HttpUtility.UrlEncode(sessionRelayState) }
                });

                string redirectUrl = response.Headers.Get("Location");

                string callbackResponse = await client.Get(redirectUrl);

                doc = new Html.HtmlDocument();
                doc.LoadHtml(callbackResponse);

                samlNode = doc.DocumentNode.SelectSingleNode("//input[@name='SAMLResponse']"); // Select SAML response using an XPath query
                samlData = samlNode.GetAttributeValue("value", ""); // Gets SAML data

                await client.Post(BaseUrl + SamlCallbackPath, new Dictionary<string, string> {
                    { "RelayState", HttpUtility.UrlEncode(RelayState) },
                    { "SAMLResponse", HttpUtility.UrlEncode(samlData) }
                });

                await client.Get(BaseUrl + FinalSamlCallbackPath);

                await client.Get(HomePageUrl);

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(finalizeLogin)}", "Finished finalizing login.");
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(finalizeLogin)}", "Failed to finalize login.", ex);
                throw ex;
            }
        }

        /**
         * <summary>Uses the supplied credentials to login to the MyVictoria website and returns the session.</summary>
         * <returns>A <see cref="CookieCollection">CookieCollection</see> containing the cookies for the new logged in session.</returns>
         * <exception cref="AuthenticationException">Thrown when credentials are incorrect or the login has failed for another reason.</exception>
         */
        private async Task<PersistentWebClient> login()
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(login)}", "Login started");

                // Simplified flow of SAML SSO login process
                //
                //        The User (Not logged in)
                //           || (GET secure page)
                //           \/
                // studentrecords.vuw.ac.nz
                //           || (POST SAML Request)
                //           \/
                // auth-eis.vuw.ac.nz/samlsso
                //           || (POST SAML Request)
                //           \/
                // federation.vuw.ac.nz (This is the login page that the user sees)
                //
                //
                //        The User
                //           || (POST user credentials)
                //           \/
                // federation.vuw.ac.nz
                //           || (POST SAML Response)
                //           \/
                // auth-eis.vuw.ac.nz
                //           || (POST SAML Response)
                //           \/
                // studentrecords.vuw.ac.nz (Logged in!)

                PersistentWebClient client = await initiateLogin();

                // Put post data into byte arrays for easy upload through the request stream
                byte[] authMethodData = MarkCredentials.CredentialEncoding.GetBytes("AuthMethod=FormsAuthentication");
                byte[] userData = MarkCredentials.CredentialEncoding.GetBytes("&UserName=student%5C" + WebUtility.UrlEncode(_credentials.Username)); // "student\username" uses the student login domain
                byte[] passData = MarkCredentials.CredentialEncoding.GetBytes("&Password=" + WebUtility.UrlEncode(_credentials.Password));
                int dataLength = authMethodData.Length + userData.Length + passData.Length; // Calculate length of bytes

                // Create request
                HttpWebRequest request = WebRequest.CreateHttp(FederationUrl);

                // Set HTTP data such as 
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = dataLength;
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(client.Cookies); // Add the cookies from the login parameters to the request
                request.UserAgent = client.UserAgent;
                request.Accept = "*/*";
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");
                request.AllowAutoRedirect = false;

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(login)}", "Login request opened.");

                using (Stream stream = await request.GetRequestStreamAsync())
                {
                    Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(login)}", "Writing login credentials.");
                    // Write UUID, Username and the start of the password
                    await stream.WriteAsync(authMethodData, 0, authMethodData.Length);
                    await stream.WriteAsync(userData, 0, userData.Length);
                    await stream.WriteAsync(passData, 0, passData.Length);
                }

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(login)}", "Getting login response.");

                HttpWebResponse response = null;

                // .net core fix, it errors on 302 response code, ignore it
                try
                {
                    response = (HttpWebResponse)await request.GetResponseAsync();
                }
                catch (WebException e)
                {
                    if (e.Message.Contains("302"))
                        response = (HttpWebResponse)e.Response;
                }

                // Get the login response

                string respStr = await new StreamReader(response.GetResponseStream()).ReadToEndAsync(); //Get HTML page

                if (respStr.Contains("Incorrect user ID or password"))
                {
                    Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(login)}", "Login was rejected by the server because the user ID or password is incorrect.");
                    throw new AuthenticationException("Login failure returned from server: invalid credentials.");
                }
                else if (respStr.Contains("An error occurred"))
                {
                    Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(login)}", "Login was rejected by the server because an error occured.");
                    throw new Exception("Login failure returned from server: an error occured");
                }

                client.Cookies.Add(response.Cookies); //Save the session in the client

                response?.Dispose();

                await finalizeLogin(client);

                Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(login)}", "Successfully logged into Student Records.");

                return client;
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to login to Student Records: " + ex.Message, ex); //Throw login failure exception with the inner exception
            }
        }
    }
}
