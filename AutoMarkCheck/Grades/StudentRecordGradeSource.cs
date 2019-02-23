using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Threading.Tasks;
using Html = HtmlAgilityPack;
using static AutoMarkCheck.Helpers.CredentialManager;
using AutoMarkCheck.Helpers;

namespace AutoMarkCheck.Grades
{
    /**
     * <summary>Agent to interface with the Student Records website.</summary>
     */
    public class StudentRecordGradeSource : IGradeSource
    {
        private const string BaseUrl = "https://studentrecords.vuw.ac.nz";
        private const string SamlInitiatePath = "/ssomanager/saml/login?relayState=/c/auth/SSB";
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
            throw new NotImplementedException();
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
                    Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(GetGrades)}", $"Successfully got {grades.Count} grades from MyVUW.");

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(GetGrades)}", "Grade grab finished.");

                return grades;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(GetGrades)}", "Failed to get grades.", ex);
                return new List<CourseInfo>(); //Return empty list
            }
        }

        private List<CourseInfo> ParseGradeHtml(string html)
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

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(ParseGradeHtml)}", "Successfully parsed grades HTML.");

                return grades;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(ParseGradeHtml)}", "Failed to parsed grades HTML.", ex);
                return new List<CourseInfo>();
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
                PersistentWebClient client = new PersistentWebClient();

                string federationRedirect = await client.Get(SamlInitiatePath);

                Html.HtmlDocument doc = new Html.HtmlDocument();
                doc.LoadHtml(federationRedirect);

                var samlNode = doc.DocumentNode.SelectSingleNode("//input[name=SAMLRequest]"); //Selects SAML data using XPath query
                string samlData = samlNode.GetAttributeValue("value", "");

                //https://auth-eis.vuw.ac.nz:443/samlsso



                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(ParseGradeHtml)}", "Successfully parsed grades HTML.");

                ////Put post data into byte arrays for easy upload through the request stream
                //byte[] uuidData = MarkCredentials.CredentialEncoding.GetBytes("uuid=" + loginParams.Item1);
                //byte[] userData = MarkCredentials.CredentialEncoding.GetBytes("&user=" + credentials.Username);
                //byte[] passData = MarkCredentials.CredentialEncoding.GetBytes("&pass=");
                //int dataLength = uuidData.Length + userData.Length + passData.Length + credentials.EscapedPasswordSize; //Calculate length of bytes

                ////Create request
                //HttpWebRequest request = WebRequest.CreateHttp(BASE_URL + LOGIN_POST_PATH);

                ////Set HTTP data such as headers
                //request.Method = "POST";
                //request.ContentType = "application/x-www-form-urlencoded";
                //request.ContentLength = dataLength;
                //request.CookieContainer = new CookieContainer();
                //request.CookieContainer.Add(client.Cookies); //Add the cookies from the login parameters to the request
                //request.UserAgent = client.UserAgent;
                //request.Accept = "*/*";
                //request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                //request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");

                //Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(Login)}", "Login request opened.");

                //using (Stream stream = await request.GetRequestStreamAsync())
                //{
                //    Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(Login)}", "Writing login credentials.");
                //    //Write UUID, Username and the start of the password
                //    await stream.WriteAsync(uuidData, 0, uuidData.Length);
                //    await stream.WriteAsync(userData, 0, userData.Length);
                //    await stream.WriteAsync(passData, 0, passData.Length);

                //    //Write password to stream character by character
                //    IntPtr passwordPtr = Marshal.SecureStringToBSTR(credentials.Password); //Convert SecureString password to BSTR and get the pointer
                //    try
                //    {
                //        byte b = 1;
                //        int i = 0;

                //        while (true) //Loop over characters in the BSTR
                //        {
                //            b = Marshal.ReadByte(passwordPtr, i);
                //            if (b == 0) break; //If terminator character '\0' is hit exit loop

                //            string escapedChar = Uri.EscapeDataString(((char)b).ToString()); //Must be a string because the escaped character can be more than 1 character long eg %00
                //            byte[] escapedCharBytes = MarkCredentials.CredentialEncoding.GetBytes(escapedChar);
                //            await stream.WriteAsync(escapedCharBytes, 0, escapedCharBytes.Length);

                //            i = i + 2;  // BSTR is unicode and occupies 2 bytes
                //        }
                //    }
                //    finally
                //    {
                //        Marshal.ZeroFreeBSTR(passwordPtr); //Securely clear password BSTR from memory
                //    }
                //}

                //Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(Login)}", "Getting login response.");

                ////Get the login response
                //using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                //{
                //    string respStr = await new StreamReader(response.GetResponseStream()).ReadToEndAsync(); //Get HTML page

                //    if (respStr.Contains("Failed")) //Check if page contains "Fail"
                //    {
                //        Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(Login)}", "Login was rejected by MyVictoria for an unkown reason. Credentials may be incorrect.");
                //        throw new AuthenticationException("Login failure returned from MyVictoria, credentials may be incorrect.");
                //    }

                //    client.Cookies.Add(response.Cookies); //Save the session in the client
                //}

                ////Browse to these URLs, the site doesnt work properly unless you visit these
                //await client.Get(BASE_URL + LOGIN_OK_PATH);
                //await client.Get(BASE_URL + LOGIN_NEXT_PATH);

                //Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(StudentRecordGradeSource)}.{nameof(Login)}", "Successfully logged into MyVuw");

                return client;
            }
            catch (Exception ex)
            {
                throw new AuthenticationException("Unable to login to MyVictoria: " + ex.Message, ex); //Throw login failure exception with the inner exception
            }
        }
    }
}
