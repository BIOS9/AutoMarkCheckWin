using AutoMarkCheck.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Authentication;
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
        private const string AcademicHistoryPath = "/pls/webprod/bwsxacdh.P_FacStuInfo";
        private const string RelayState = "/c/auth/SSB";
        private const string SSOUrl = "https://auth-eis.vuw.ac.nz/samlsso";
        private const string SSOCallback = "https://auth-eis.vuw.ac.nz/commonauth";
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
                await Login();
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

                PersistentWebClient client = await Login(); //Create logged in session

                //string result = await client.Get(BASE_URL + GRADE_PATH); //Download grade page

                List<CourseInfo> grades = new List<CourseInfo>();// ParseGradeHtml(result);

                if (grades.Count == 0) //If no courses were found
                {
                    Logging.Log(Logging.LogLevel.WARNING, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(GetGrades)}", "No courses were found, Term/Year wil be set to current year on next request.");
                }
                else
                    Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(GetGrades)}", $"Successfully got {grades.Count} grades from Student Records.");

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

                var nodes = doc.DocumentNode.SelectNodes("//table[@class='datadisplaytable']/form/tr[not(@class='uportal-background-light')]"); //Selects rows that have the grades using XPath query

                List<CourseInfo> grades = new List<CourseInfo>();
                foreach (var row in nodes)
                    if (row.ChildNodes.Count == 5) //Rows containing grades have 5 cells
                    {
                        grades.Add(new CourseInfo
                        {
                            //Set course data from the HTML cells
                            CRN = row.ChildNodes[0].InnerText,
                            Subject = row.ChildNodes[1].InnerText,
                            Course = row.ChildNodes[2].InnerText,
                            CourseTitle = row.ChildNodes[3].InnerText,
                            Grade = row.ChildNodes[4].InnerText,
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
                string federationRedirect = await client.Post(SSOUrl, new Dictionary<string, string> {
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
            catch(Exception ex)
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

                var samlNode = doc.DocumentNode.SelectSingleNode("//input[@name='SAMLResponse']"); //Select SAML response using an XPath query
                string samlData = samlNode.GetAttributeValue("value", ""); // Gets SAML data

                var relayStateNode = doc.DocumentNode.SelectSingleNode("//input[@name='RelayState']"); //Select relay state using an XPath query
                string sessionRelayState = relayStateNode.GetAttributeValue("value", ""); // Gets relay state

                var response = await client.PostWithHeaders(SSOCallback, new Dictionary<string, string> {
                    { "SAMLResponse", HttpUtility.UrlEncode(samlData) },
                    { "RelayState", HttpUtility.UrlEncode(sessionRelayState) }
                });

                string redirectUrl = response.Headers.Get("Location");

                string callbackResponse = await client.Get(redirectUrl);

                doc = new Html.HtmlDocument();
                doc.LoadHtml(callbackResponse);

                samlNode = doc.DocumentNode.SelectSingleNode("//input[@name='SAMLResponse']"); //Select SAML response using an XPath query
                samlData = samlNode.GetAttributeValue("value", ""); // Gets SAML data

                await client.Post(BaseUrl + SamlCallbackPath, new Dictionary<string, string> {
                    { "RelayState", HttpUtility.UrlEncode(RelayState) },
                    { "SAMLResponse", HttpUtility.UrlEncode(samlData) }
                });

                await client.Get(BaseUrl + FinalSamlCallbackPath);

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
        public async Task<PersistentWebClient> Login()
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(Login)}", "Login started");

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

                //Put post data into byte arrays for easy upload through the request stream
                byte[] authMethodData = MarkCredentials.CredentialEncoding.GetBytes("AuthMethod=FormsAuthentication");
                byte[] userData = MarkCredentials.CredentialEncoding.GetBytes("&UserName=student%5C" + _credentials.Username); // "student\username" uses the student login domain
                byte[] passData = MarkCredentials.CredentialEncoding.GetBytes("&Password=");
                int dataLength = authMethodData.Length + userData.Length + passData.Length + _credentials.EscapedPasswordSize; //Calculate length of bytes

                //Create request
                HttpWebRequest request = WebRequest.CreateHttp(FederationUrl);

                //Set HTTP data such as headers
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = dataLength;
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(client.Cookies); //Add the cookies from the login parameters to the request
                request.UserAgent = client.UserAgent;
                request.Accept = "*/*";
                request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");
                request.AllowAutoRedirect = false;

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(Login)}", "Login request opened.");

                using (Stream stream = await request.GetRequestStreamAsync())
                {
                    Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(Login)}", "Writing login credentials.");
                    //Write UUID, Username and the start of the password
                    await stream.WriteAsync(authMethodData, 0, authMethodData.Length);
                    await stream.WriteAsync(userData, 0, userData.Length);
                    await stream.WriteAsync(passData, 0, passData.Length);

                    //Write password to stream character by character
                    IntPtr passwordPtr = Marshal.SecureStringToBSTR(_credentials.Password); //Convert SecureString password to BSTR and get the pointer
                    try
                    {
                        byte b = 1;
                        int i = 0;

                        while (true) //Loop over characters in the BSTR
                        {
                            b = Marshal.ReadByte(passwordPtr, i);
                            if (b == 0)
                                break; //If terminator character '\0' is hit exit loop

                            string escapedChar = Uri.EscapeDataString(((char)b).ToString()); //Must be a string because the escaped character can be more than 1 character long eg %00
                            byte[] escapedCharBytes = MarkCredentials.CredentialEncoding.GetBytes(escapedChar);
                            await stream.WriteAsync(escapedCharBytes, 0, escapedCharBytes.Length);

                            i = i + 2;  // BSTR is unicode and occupies 2 bytes
                        }
                    }
                    finally
                    {
                        Marshal.ZeroFreeBSTR(passwordPtr); //Securely clear password BSTR from memory
                    }
                }

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(Login)}", "Getting login response.");

                //Get the login response
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    string respStr = await new StreamReader(response.GetResponseStream()).ReadToEndAsync(); //Get HTML page

                    if (respStr.Contains("Incorrect user ID or password"))
                    {
                        Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(Login)}", "Login was rejected by the server because the user ID or password is incorrect.");
                        throw new AuthenticationException("Login failure returned from server: invalid credentials.");
                    }
                    else if(respStr.Contains("An error occurred"))
                    {
                        Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(Login)}", "Login was rejected by the server because an error occured.");
                        throw new Exception("Login failure returned from server: an error occured");
                    }

                    client.Cookies.Add(response.Cookies); //Save the session in the client
                }

                await finalizeLogin(client);

                Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(Login)}", "Successfully logged into Student Records.");

                return client;
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to login to Student Records: " + ex.Message, ex); //Throw login failure exception with the inner exception
            }
        }
    }
}
