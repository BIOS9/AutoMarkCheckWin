using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
                MessageBox.Show(loginParams.Item1);
                MessageBox.Show(loginParams.Item2.Count.ToString());

                string postdata = "pass=aaa&user=asdasdas&uuid=" + loginParams.Item1;
                var data = Encoding.ASCII.GetBytes(postdata);

                HttpWebRequest request = WebRequest.CreateHttp(BASE_URL + LOGIN_POST_PATH);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(loginParams.Item2); //Add the cookies from the login parameters to the request

                using (Stream stream = await request.GetRequestStreamAsync())
                {
                    await stream.WriteAsync(data, 0, data.Length);
                }

                using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    string respStr = await new StreamReader(response.GetResponseStream()).ReadToEndAsync();
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
