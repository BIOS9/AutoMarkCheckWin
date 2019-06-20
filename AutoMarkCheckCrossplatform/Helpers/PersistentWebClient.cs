using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AutoMarkCheck.Helpers
{
    /**
     * <summary>Web client that supports cookie persistence to allow logins and other session sensitive actions.</summary>
     */
    public class PersistentWebClient
    {
        public class PersistentWebClientResponse
        {
            public string HTML;
            public WebHeaderCollection Headers;
        }

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
        public async Task<PersistentWebClientResponse> GetWithHeaders(string url, string referer = null)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.UserAgent = UserAgent;
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(Cookies); //Add existing cookies to this request
            request.Method = "GET";
            request.Accept = "*/*";
            request.AllowAutoRedirect = false;
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "identity");
            request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");
            if (referer != null)
                request.Referer = referer;

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

            //Get response
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                Cookies.Add(response.Cookies); //Persist cookies
                return new PersistentWebClientResponse
                {
                    HTML = await reader.ReadToEndAsync(),
                    Headers = response.Headers
                };
            }
        }

        /**
         * <summary>Shortcut for a GET request that ignores headers.</summary>
         */
        public async Task<string> Get(string url)
        {
            return (await GetWithHeaders(url)).HTML;
        }

        /**
         * <summary>Performs a POST request at a URL and sends post data then returns the HTML string.</summary>
         */
        public async Task<PersistentWebClientResponse> PostWithHeaders(string url, Dictionary<string, string> postData, string referer = null)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.UserAgent = UserAgent;
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(Cookies); //Add existing cookies to this request
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.AllowAutoRedirect = false;
            if (referer != null)
                request.Referer = referer;

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

            //Get response
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                Cookies.Add(response.Cookies); //Persist cookies from this request
                return new PersistentWebClientResponse
                {
                    HTML = await reader.ReadToEndAsync(),
                    Headers = response.Headers
                };
            }
        }

        /**
         * <summary>Shortcut for a POST request that ignores headers.</summary>
         */
        public async Task<string> Post(string url, Dictionary<string, string> postData)
        {
            return (await PostWithHeaders(url, postData)).HTML;
        }

//#if DEBUG
//        public void DisplayHTML(string html)
//        {
//            Form frm = new Form();
//            frm.WindowState = FormWindowState.Maximized;
//            WebBrowser wb = new WebBrowser();
//            wb.Dock = DockStyle.Fill;
//            wb.AllowNavigation = true;
//            wb.ScriptErrorsSuppressed = true;
//            frm.Controls.Add(wb);
//            wb.DocumentText = html;
//            frm.ShowDialog();
//        }

//        public void DisplayCookies(string msg = "")
//        {
//            string cookieStr = "";
//            foreach(Cookie c in Cookies)
//            {
//                cookieStr += c.Domain + ": " + c.Path + ": " + c.Name + " = " + c.Value + "\n";
//            }
//            if (cookieStr.Length > 0)
//            {
//                //cookieStr = cookieStr.Remove(0, 1);
//                MessageBox.Show(msg + " " + cookieStr);
//            }
//            else
//            {
//                MessageBox.Show(msg + " No cookies.");
//            }
//        }

//        public void PrintCookies()
//        {
//            Console.WriteLine();
//            Console.WriteLine();
//            Console.WriteLine();
//            Console.ForegroundColor = ConsoleColor.Magenta;
//            Console.WriteLine("Web Client Cookies:");
//            foreach (Cookie c in Cookies)
//            {
//                Console.ForegroundColor = ConsoleColor.Yellow;
//                Console.WriteLine(c.Domain + " - " + c.Path);
//                Console.ForegroundColor = ConsoleColor.Green;
//                Console.Write(c.Name);
//                Console.ForegroundColor = ConsoleColor.White;
//                Console.Write("=");
//                Console.ForegroundColor = ConsoleColor.Cyan;
//                Console.WriteLine(c.Value);
//                Console.WriteLine();
//            }
//            Console.ForegroundColor = ConsoleColor.Magenta;
//            Console.WriteLine("Finished cookie print");
//            Console.ResetColor();
//            Console.WriteLine();
//            Console.WriteLine();
//            Console.WriteLine();
//        }
//#endif

    }
}
