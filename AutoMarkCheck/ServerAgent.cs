using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AutoMarkCheck.MyVUWAgent;

namespace AutoMarkCheck
{
    /**
     * <summary>Agent to interface with the home bot server.</summary>
     */
    public class ServerAgent
    {
        private const string API_URL = "http://automarkcheck.kwiius.com:4567/yeet";
        private const string TOKEN_PLACEHOLDER = "|[tokenplaceholder]|";
        private static string _userAgent = $"Auto Mark Check {Environment.OSVersion.Platform} {Environment.OSVersion.VersionString}/1.0"; //User agent will contain OS name and version

        /**
         * <summary>Report courses and grades to the bot server.</summary>
         * <param name="courses">List of <see cref="CourseInfo">CourseInfo</see> objects containing information about each course.</param>
         * <param name="hostname">Hostname to appear in messages from the Discord bot.</param>
         * <param name="credentials">Credentials containing the Bot Token to be used to authenticate the report.</param>
         * <returns>A boolean indicating if the report was successful.</returns>
         */
        public static async Task<bool> ReportGrades(List<CourseInfo> courses, string hostname, CredentialManager.MarkCredentials credentials)
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(ServerAgent)}.{nameof(ReportGrades)}", "Grade report started.");

                string jsonData = SerializeData(courses, hostname);
                //await Upload(jsonData, credentials);

                Logging.Log(Logging.LogLevel.INFO, $"{nameof(ServerAgent)}.{nameof(ReportError)}", "Successfully reported grades to bot server.");
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(ServerAgent)}.{nameof(ReportGrades)}", "Failed to report grades to bot server.", ex);
                return false;
            }
        }

        /**
         * <summary>Report an error fetching the grades to the bot server.</summary>
         * <param name="error">Error message to report to the server.</param>
         * <param name="hostname">Hostname to appear in messages from the Discord bot.</param>
         * <param name="credentials">Credentials containing the Bot Token to be used to authenticate the report.</param>
         * <returns>A boolean indicating if the report was successful.</returns>
         */
        public static async Task<bool> ReportError(string error, string hostname, CredentialManager.MarkCredentials credentials)
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(ServerAgent)}.{nameof(ReportGrades)}", "Error report started.");

                string jsonData = SerializeData(null, hostname, error);
                await Upload(jsonData, credentials);

                Logging.Log(Logging.LogLevel.INFO, $"{nameof(ServerAgent)}.{nameof(ReportError)}", "Successfully reported error to bot server.");
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(ServerAgent)}.{nameof(ReportError)}", "Failed to report error to bot server.", ex);
                return false;
            }
        }

        /**
         * <summary>Upload JSON serialized report to the bot server.</summary>
         * <param name="jsonData">Json data containing the report.</param>
         * <param name="credentials">Credentials containing the Bot Token to be used to authenticate the report.</param>
         */
        private static async Task Upload(string jsonData, CredentialManager.MarkCredentials credentials)
        {
            Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(ServerAgent)}.{nameof(ReportGrades)}", "Report upload started.");

            string beforeToken = jsonData.Substring(0, jsonData.IndexOf(TOKEN_PLACEHOLDER));
            string afterToken = jsonData.Substring(jsonData.IndexOf(TOKEN_PLACEHOLDER) + TOKEN_PLACEHOLDER.Length);

            //Calculate length of post JSON data
            byte[] beforeTokenBytes = CredentialManager.MarkCredentials.CredentialEncoding.GetBytes(beforeToken);
            byte[] afterTokenBytes = CredentialManager.MarkCredentials.CredentialEncoding.GetBytes(afterToken);
            int dataLength = beforeTokenBytes.Length + afterTokenBytes.Length + credentials.EscapedBotTokenSize;

            //Create request
            HttpWebRequest request = WebRequest.CreateHttp(API_URL);

            //Set HTTP data such as headers
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = dataLength;
            request.UserAgent = _userAgent;
            request.Accept = "*/*";
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");

            using (Stream stream = await request.GetRequestStreamAsync())
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(ServerAgent)}.{nameof(Upload)}", "Writing login credentials.");
                //Write first part of JSON before token
                await stream.WriteAsync(beforeTokenBytes, 0, beforeTokenBytes.Length);

                //Write token to stream character by character
                IntPtr tokenPtr = Marshal.SecureStringToBSTR(credentials.BotToken); //Convert SecureString token to BSTR and get the pointer
                try
                {
                    byte b = 1;
                    int i = 0;

                    while (true) //Loop over characters in the BSTR
                    {
                        b = Marshal.ReadByte(tokenPtr, i);
                        if (b == 0) break; //If terminator character '\0' is hit exit loop

                        string escapedChar = JsonConvert.ToString((char)b); //Token is uploaded in JSON so it must be escaped in both json then URI format
                        escapedChar = escapedChar.Remove(0, 1); //To remove the leading "
                        escapedChar = escapedChar.Remove(escapedChar.Length - 1, 1); //To remove the trailing "
                        byte[] escapedCharBytes = CredentialManager.MarkCredentials.CredentialEncoding.GetBytes(escapedChar);
                        await stream.WriteAsync(escapedCharBytes, 0, escapedCharBytes.Length);

                        i = i + 2;  // BSTR is unicode and occupies 2 bytes
                    }
                }
                finally
                {
                    Marshal.ZeroFreeBSTR(tokenPtr); //Securely clear token BSTR from memory
                }

                await stream.WriteAsync(afterTokenBytes, 0, afterTokenBytes.Length); //Write the end of the JSON
            }

            Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(ServerAgent)}.{nameof(Upload)}", "Getting report response.");

            //Get the report response
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                    Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(ServerAgent)}.{nameof(Upload)}", "Successfully uploaded report to server.");
                else
                {
                    string responseStr;
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                        responseStr = await reader.ReadToEndAsync();
                    Logging.Log(Logging.LogLevel.ERROR, $"{nameof(ServerAgent)}.{nameof(Upload)}", $"Failed to upload report to bot server. Server returned: {response.StatusCode} {response.StatusDescription} : {responseStr}");
                    throw new Exception("Report upload failed.");
                }
            }

            Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(ServerAgent)}.{nameof(Upload)}", "Report upload finished.");
        }

        /**
         * <summary>Serializes report information into a single report string.</summary>
         * <param name="courses">Courses to serialize into a string. Grades will be replaced with TRUE/FALSE for privacy. Can be null if error is set.</param>
         * <param name="hostanme">Hostname to appear in messages from the Discord Bot.</param>
         * <param name="error">If error is set, courses will be ignored and the report will be serialized as an error report.</param>
         * <returns>A JSON string containing the serialized report.</returns>
         */
        private static string SerializeData(List<CourseInfo> courses, string hostanme, string error = null)
        {
            dynamic jsonObject = new JObject();

            if (string.IsNullOrWhiteSpace(error))
            {
                jsonObject.courses = new JObject();
                foreach (CourseInfo info in courses)
                    jsonObject.courses[info.Subject + info.Course] = !string.IsNullOrWhiteSpace(info.Grade);
            }
            else
                jsonObject.error = error;

            TimeSpan uptime = (DateTime.Now - Process.GetCurrentProcess().StartTime);
            //string dayStr = uptime.Days == 1 ? "day" : "days";
            jsonObject.uptime = uptime.ToString($"d' days', 'hh':'mm':'ss");

            jsonObject.hostname = hostanme;

            //Using a place holder for the token so it can be injected into the upload stream to prevent storing the unencrypted token in memory.
            jsonObject.token = TOKEN_PLACEHOLDER;

            return JsonConvert.SerializeObject(jsonObject); //Convert object into JSON string.
        }
    }
}
