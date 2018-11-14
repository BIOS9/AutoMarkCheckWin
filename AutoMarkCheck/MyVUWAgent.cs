using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        public async Task<IList<CourseGrade>> GetGrades(MarkCredentials credentials)
        {

            return new List<CourseGrade>();
        }

        private async Task<CookieCollection> Login(MarkCredentials credentials)
        {
            try
            {
                WebClient wc = new WebClient();
                string pageText = await wc.DownloadStringTaskAsync(BASE_URL + LOGIN_PAGE_PATH);
                string uuid = Regex.Match(pageText, LOGIN_UUID_PATTERN).Groups[1].Value;

                HttpWebRequest request = WebRequest.CreateHttp(BASE_URL + LOGIN_POST_PATH);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                using (var stream = await request.GetRequestStreamAsync())
                {

                }

                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
            }
            catch(Exception ex)
            {
                throw new AuthenticationException("Failed to login to MyVictoria: " + ex.Message, ex); //Throw login failure exception with the inner exception
            }
        }
    }
}
