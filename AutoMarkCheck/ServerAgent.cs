using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private const string TOKEN_PLACEHOLDER = "|[tokenplaceholder]|";
        private string USER_AGENT = $"Auto Mark Check {Environment.OSVersion.Platform} {Environment.OSVersion.VersionString}/1.0";

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
                await Upload(jsonData, credentials);

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

            

            Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(ServerAgent)}.{nameof(ReportGrades)}", "Report upload finished.");
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
            jsonObject.uptime = uptime.ToString("d' days, 'hh':'mm':'ss");

            jsonObject.hostname = hostanme;

            //Using a place holder for the token so it can be injected into the upload stream to prevent storing the unencrypted token in memory.
            jsonObject.token = TOKEN_PLACEHOLDER; 

            return JsonConvert.SerializeObject(jsonObject); //Convert object into JSON string.
        }
    }
}
