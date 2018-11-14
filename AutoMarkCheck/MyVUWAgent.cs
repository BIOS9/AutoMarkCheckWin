using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AutoMarkCheck.CredentialManager;

namespace AutoMarkCheck
{
    class MyVUWAgent
    {
        public class CourseGrade
        {
            public string CRN;
            public string Subject;
            public string Course;
            public string CourseTitle;
            public string Grade;
        }

        private const string BASE_URL = "https://my.vuw.ac.nz";
        private const string LOGIN_PAGE_PATH = "/cp/home/displaylogin";
        private const string LOGIN_POST_PATH = "/cp/home/login";
        private const string LOGIN_UUID_PATTERN = "(?:document.cplogin.uuid.value=\")([\\da-zA-Z-]+)(?:\";)";

        public static async Task<IList<CourseGrade>> GetGrades(MarkCredentials credentials)
        {
            CookieCollection cookies = await Login(credentials);
            return new List<CourseGrade>();
        }

        private static async Task<CookieCollection> Login(MarkCredentials credentials)
        {
            try
            {
                Tuple<string, CookieCollection> loginParams = await GetLoginParams();

                byte[] uuidData = MarkCredentials.CredentialEncoding.GetBytes("uuid=" + loginParams.Item1);
                byte[] userData = MarkCredentials.CredentialEncoding.GetBytes("&user=" + credentials.Username);
                byte[] passData = MarkCredentials.CredentialEncoding.GetBytes("&pass=");
                int dataLength = uuidData.Length + userData.Length + passData.Length + credentials.EscapedPasswordSize;

                HttpWebRequest request = WebRequest.CreateHttp(BASE_URL + LOGIN_POST_PATH);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = dataLength;
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(loginParams.Item2); //Add the cookies from the login parameters to the request

                using (Stream stream = await request.GetRequestStreamAsync())
                {
                    //Write UUID, Username and the start of the password
                    await stream.WriteAsync(uuidData, 0, uuidData.Length);
                    await stream.WriteAsync(userData, 0, userData.Length);
                    await stream.WriteAsync(passData, 0, passData.Length);

                    //Write password to stream character by charactre
                    IntPtr passwordPtr = Marshal.SecureStringToBSTR(credentials.Password); //Convert SecureString password to BSTR and get the pointer
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
                
                using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    string respStr = await new StreamReader(response.GetResponseStream()).ReadToEndAsync();
                    MessageBox.Show(respStr);
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw new AuthenticationException("Failed to login to MyVictoria: " + ex.Message, ex); //Throw login failure exception with the inner exception
            }
        }

        private static async Task<Tuple<string, CookieCollection>> GetLoginParams()
        {
            HttpClient hc = new HttpClient();
            HttpWebRequest request = WebRequest.CreateHttp(BASE_URL + LOGIN_PAGE_PATH);
            request.Method = "GET";
            request.CookieContainer = new CookieContainer(); //Create somewhere for the cookies to go

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync()) //Get page data
            using (Stream stream = response.GetResponseStream()) //Get the data stream from the response
            using (StreamReader reader = new StreamReader(stream)) //Create reader to read HTML from the data stream
            {
                string pageText = await reader.ReadToEndAsync();
                string uuid = Regex.Match(pageText, LOGIN_UUID_PATTERN).Groups[1].Value; //Find the UUID inside the HTML/JS

                return new Tuple<string, CookieCollection>(uuid, response.Cookies); //Return UUID and cookies
            }
        }
    }
}
