using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AutoMarkCheck.CredentialManager;

namespace AutoMarkCheck
{
    /**
     * <summary>Web client that supports cookie persistence to allow logins and other session sensitive actions.</summary>
     */
    public class PersistentWebClient
    {
        public CookieCollection Cookies = new CookieCollection();
        public string UserAgent = "AutoMarkCheckBot/1.0";

        /**
         * <summary>Resets cookies for this client.</summary>
         */
        public void ClearCookies()
        {
            Cookies = new CookieCollection();
        }

        /**
         * <summary>Performs a GET request at the  URL and returns the HTML string.</summary>
         */
        public async Task<string> Get(string url)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.UserAgent = UserAgent;
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(Cookies); //Add existing cookies to this request
            request.Method = "GET";
            request.Accept = "*/*";
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");

            //Get response
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                Cookies.Add(response.Cookies); //Persist cookies
                return await reader.ReadToEndAsync();
            }
        }

        /**
         * <summary>Performs a POST request at a URL and sends post data then returns the HTML string.</summary>
         */
        public async Task<string> Post(string url, Dictionary<string, string> postData)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.UserAgent = UserAgent;
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(Cookies); //Add existing cookies to this request
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";


            //Encode the post data as a post tring
            string postStr = "";
            foreach (KeyValuePair<string, string> postItem in postData)
                postStr += $"&{postItem.Key}={postItem.Value}";
            if(postStr.Length > 0)
                postStr = postStr.Remove(0, 1); //Remove first &

            byte[] postBytes = Encoding.ASCII.GetBytes(postStr);
            request.ContentLength = postBytes.Length;

            //Upload post data
            using (Stream stream = await request.GetRequestStreamAsync())
                await stream.WriteAsync(postBytes, 0, postBytes.Length);

            //Get response
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                Cookies.Add(response.Cookies); //Persist cookies from this request
                return await reader.ReadToEndAsync();
            }
        }

#if DEBUG
        public void DisplayHTML(string html)
        {
            Form frm = new Form();
            frm.WindowState = FormWindowState.Maximized;
            WebBrowser wb = new WebBrowser();
            wb.Dock = DockStyle.Fill;
            wb.AllowNavigation = true;
            wb.ScriptErrorsSuppressed = true;
            frm.Controls.Add(wb);
            wb.DocumentText = html;
            frm.ShowDialog();
        }

        public void DisplayCookies(string msg = "")
        {
            string cookieStr = "";
            foreach(Cookie c in Cookies)
            {
                cookieStr += c.Name + " = " + c.Value + "\n";
            }
            if (cookieStr.Length > 0)
            {
                cookieStr = cookieStr.Remove(0, 1);
                MessageBox.Show(msg + " " + cookieStr);
            }
            else
            {
                MessageBox.Show(msg + " No cookies.");
            }
        }
#endif

    }
}
