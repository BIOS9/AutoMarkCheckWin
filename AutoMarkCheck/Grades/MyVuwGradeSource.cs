using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Html = HtmlAgilityPack;
using static AutoMarkCheck.Helpers.CredentialManager;
using AutoMarkCheck.Helpers;

namespace AutoMarkCheck.Grades
{
    /**
     * <summary>Agent to interface with the MyVictoria website.</summary>
     */
    public class MyVuwGradeSource : IGradeSource
    {
        private const string BaseUrl = "https://my.vuw.ac.nz";
        private const string LoginPagePath = "/cp/home/displaylogin";
        private const string LoginPostPath = "/cp/home/login";
        private const string LoginOkPath = "/cps/welcome/loginok.html";
        private const string LoginNextPath = "/cp/home/next";

        //The following URLs are a bit funky, ther are alternate URLs for some of these, but they all seem to work. I wish they had an easier site structure.
        private const string HomePath = "/render.userLayoutRootNode.uP?uP_root=root";
        private const string MyStudyPath = "/tag.c56f3aaeaf27f1c8.render.userLayoutRootNode.uP?uP_root=root&uP_sparam=activeTab&activeTab=u12l1s8&uP_tparam=frm&frm=";
        private const string GradePath = "/tag.c56f3aaeaf27f1c8.render.userLayoutRootNode.uP?uP_root=u12l1n642"; //Alternative => /tag.e346fb87a7e9ef60.render.userLayoutRootNode.uP?uP_root=u12l1n642
        private const string TermUpdatePath = "/tag.c56f3aaeaf27f1c8.render.userLayoutRootNode.uP?uP_edit_target=u12l1n642";
        private const string TermUpdatePostPath = "/tag.c56f3aaeaf27f1c8.render.userLayoutRootNode.target.u12l1n642.uP";

        private const string LoginUuidPattern = "(?:document.cplogin.uuid.value=\")([\\da-zA-Z-]+)(?:\";)";

        private readonly static TimeSpan YearSetInterval = TimeSpan.FromHours(6); //Sets the year to the current year, just in case the user has an old year selected and the old results are coming up.

        private bool _setYearOnNext = false; //Whether to set the default grade year on the next request
        private DateTime _lastYearSet = DateTime.MinValue;
        private MarkCredentials _credentials;

        public MyVuwGradeSource(MarkCredentials credentials)
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
         * <summary>Gets course grades from the MyVictoria website.</summary>
         * <returns>A list of <see cref="CourseInfo">CourseGrade</see> objects containing the grades for each course.</returns>
         * <exception cref="AuthenticationException">Thrown if the credentials are incorrect or login fails for another reason.</exception>
         */
        public async Task<List<CourseInfo>> GetGrades()
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(GetGrades)}", "Grade grab started.");

                PersistentWebClient client = await Login(); //Create logged in session

                if (_setYearOnNext || DateTime.Now - _lastYearSet > YearSetInterval)
                {
                    _setYearOnNext = false;
                    _lastYearSet = DateTime.Now;
                    await SetGradeYear(client); //Set year for displayed grades to current year
                }

                string result = await client.Get(BaseUrl + GradePath); //Download grade page

                List<CourseInfo> grades = ParseGradeHtml(result);

                if (grades.Count == 0) //If no courses were found
                {
                    _setYearOnNext = true;
                    Logging.Log(Logging.LogLevel.WARNING, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(GetGrades)}", "No courses were found, Term/Year wil be set to current year on next request.");
                }
                else
                    Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(GetGrades)}", $"Successfully got {grades.Count} grades from MyVUW.");

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(GetGrades)}", "Grade grab finished.");

                return grades;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(GetGrades)}", "Failed to get grades.", ex);
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

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(ParseGradeHtml)}", "Successfully parsed grades HTML.");

                return grades;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(ParseGradeHtml)}", "Failed to parsed grades HTML.", ex);
                return new List<CourseInfo>();
            }
        }

        /**
         * <summary>Uses the supplied credentials to login to the MyVictoria website and returns the session.</summary>
         * <returns>A <see cref="CookieCollection">CookieCollection</see> containing the cookies for the new logged in session.</returns>
         * <exception cref="AuthenticationException">Thrown when credentials are incorrect or the login has failed for another reason.</exception>
         */
        private async Task<PersistentWebClient> Login()
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(Login)}", "Login started");
                Tuple<string, PersistentWebClient> loginParams = await GetLoginParams(); //Get login parameters such as session cookies and UUID
                PersistentWebClient client = loginParams.Item2;

                //Put post data into byte arrays for easy upload through the request stream
                byte[] uuidData = MarkCredentials.CredentialEncoding.GetBytes("uuid=" + loginParams.Item1);
                byte[] userData = MarkCredentials.CredentialEncoding.GetBytes("&user=" + _credentials.Username);
                byte[] passData = MarkCredentials.CredentialEncoding.GetBytes("&pass=");
                int dataLength = uuidData.Length + userData.Length + passData.Length + _credentials.EscapedPasswordSize; //Calculate length of bytes

                //Create request
                HttpWebRequest request = WebRequest.CreateHttp(BaseUrl + LoginPostPath);

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

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(Login)}", "Login request opened.");

                using (Stream stream = await request.GetRequestStreamAsync())
                {
                    Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(Login)}", "Writing login credentials.");
                    //Write UUID, Username and the start of the password
                    await stream.WriteAsync(uuidData, 0, uuidData.Length);
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
                            if (b == 0) break; //If terminator character '\0' is hit exit loop

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

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(Login)}", "Getting login response.");

                //Get the login response
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    string respStr = await new StreamReader(response.GetResponseStream()).ReadToEndAsync(); //Get HTML page

                    if (respStr.Contains("Failed")) //Check if page contains "Fail"
                    {
                        Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(Login)}", "Login was rejected by MyVictoria for an unkown reason. Credentials may be incorrect.");
                        throw new AuthenticationException("Login failure returned from MyVictoria, credentials may be incorrect.");
                    }

                    client.Cookies.Add(response.Cookies); //Save the session in the client
                }

                //Browse to these URLs, the site doesnt work properly unless you visit these
                await client.Get(BaseUrl + LoginOkPath);
                await client.Get(BaseUrl + LoginNextPath);

                Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(Login)}", "Successfully logged into MyVuw");

                return client;
            }
            catch (Exception ex)
            {
                throw new AuthenticationException("Unable to login to MyVictoria: " + ex.Message, ex); //Throw login failure exception with the inner exception
            }
        }

        /**
         * <summary>Gets session cookies and UUID created by MyVictoria website using a GET request, simulating someone loading the login page.</summary>
         * <returns>A tuple value containing the UUID as Item1 and the session cookies as Item2</returns>
         */
        private async Task<Tuple<string, PersistentWebClient>> GetLoginParams()
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(GetLoginParams)}", "Getting login parameters.");

                PersistentWebClient client = new PersistentWebClient();

                string pageText = await client.Get(BaseUrl + LoginPagePath); //Download page text
                string uuid = Regex.Match(pageText, LoginUuidPattern).Groups[1].Value; //Find the UUID inside the HTML/JS

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(GetLoginParams)}", "Finished getting login parameters");

                return new Tuple<string, PersistentWebClient>(uuid, client); //Return UUID and client
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(GetLoginParams)}", "Error getting login parameters.", ex);

                throw new WebException("Failed to load or parse MyVictoria login page.", ex);
            }
        }

        /**
         * <summary>Sets the term/year on MyVuw to the current year so the grades that show up are for this year.</summary>
         * <param name="client">Authenticated client to use to update the setting.</param>
         */
        private async Task SetGradeYear(PersistentWebClient client)
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(SetGradeYear)}", $"Started setting grade year to {DateTime.Now.Year}01.");
                //The site wont change the setting until you browse to these urls
                await client.Get(BaseUrl + HomePath);
                await client.Get(BaseUrl + MyStudyPath);
                await client.Get(BaseUrl + TermUpdatePath);

                //Switch into edit mode, then update the setting
                await client.Post(BaseUrl + TermUpdatePostPath, new Dictionary<string, string> {
                    { "MODE", "EDIT" },
                    { "VIEW", "EDUPDATE" },
                    { "TEXTDATA", "999" }, //Display up to 999 grades on the Courses and Grades page
                    { "TERMLIST", DateTime.Now.Year + "01" }, //Set year to current year with a suffix of 01 because the website requires that
                });

                //Switch out of edit mode so grades can be viewed
                await client.Post(BaseUrl + TermUpdatePostPath, new Dictionary<string, string> {
                    { "MODE", "DEFAULT" },
                    { "VIEW", "DEFAULT" },
                    { "TEXTDATA", "999" },
                    { "TERMLIST", DateTime.Now.Year + "01" },
                });

                Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(SetGradeYear)}", $"Grade year has been successfully set to {DateTime.Now.Year}01.");
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(Grades)}.{nameof(MyVuwGradeSource)}.{nameof(SetGradeYear)}", $"Failed to set grade year to {DateTime.Now.Year}01.", ex);
            }
        }
    }
}
