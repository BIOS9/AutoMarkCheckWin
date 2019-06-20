﻿using AutoMarkCheck.Grades;
using AutoMarkCheck.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using static AutoMarkCheck.Helpers.CredentialManager;

namespace AutoMarkCheck
{
    /**
     * <summary>Agent to interface with the home bot server.</summary>
     */
    public class ServerAgent
    {
        public MarkCredentials Credentials;
        public string Hostname;
        public bool MakeCoursesPublic;

        private const string ApiUrl = "http://automarkcheck.kwiius.com:4567/yeet";
        private string _userAgent = $"Auto Mark Check {Environment.OSVersion.Platform} {Environment.OSVersion.VersionString}/1.0"; //User agent will contain OS name and version
       

        public ServerAgent(MarkCredentials credentials, string hostname, bool makeCoursesPublic = false)
        {
            Credentials = credentials;
            Hostname = hostname;
            MakeCoursesPublic = makeCoursesPublic;
        }

        //public async Task<bool> CheckCredentials()
        //{
        //    throw new NotImplementedException();
        //}

        /**
         * <summary>Report courses and grades to the bot server.</summary>
         * <param name="courses">List of <see cref="CourseInfo">CourseInfo</see> objects containing information about each course.</param>
         * <param name="hostname">Hostname to appear in messages from the Discord bot.</param>
         * <param name="credentials">Credentials containing the Bot Token to be used to authenticate the report.</param>
         * <returns>A boolean indicating if the report was successful.</returns>
         */
        public async Task<bool> ReportGrades(List<CourseInfo> courses,MarkCredentials credentials)
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(ReportGrades)}", "Grade report started.");

                string jsonData = SerializeData(courses, credentials);
                await Upload(jsonData);

                Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(ReportGrades)}", "Successfully reported grades to bot server.");
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(ReportGrades)}", "Failed to report grades to bot server.", ex);
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
        public async Task<bool> ReportError(string error, MarkCredentials credentials)
        {
            try
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(ReportError)}", "Error report started.");

                string jsonData = SerializeData(null, credentials);
                await Upload(jsonData);

                Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(ReportError)}", "Successfully reported error to bot server.");
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(ReportError)}", "Failed to report error to bot server.", ex);
                return false;
            }
        }

        /**
         * <summary>Upload JSON serialized report to the bot server.</summary>
         * <param name="jsonData">Json data containing the report.</param>
         * <param name="credentials">Credentials containing the Bot Token to be used to authenticate the report.</param>
         */
        private async Task Upload(string jsonData)
        {
            Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(Upload)}", "Report upload started.");

            byte[] requestData = CredentialManager.MarkCredentials.CredentialEncoding.GetBytes(jsonData);

            //Create request
            HttpWebRequest request = WebRequest.CreateHttp(ApiUrl);

            //Set HTTP data such as headers
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = requestData.Length;
            request.UserAgent = _userAgent;
            request.Accept = "*/*";
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            request.Headers.Add(HttpRequestHeader.CacheControl, "no-cache");

            Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(Upload)}", "Starting reuqest.");
            using (Stream stream = await request.GetRequestStreamAsync())
            {
                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(Upload)}", "Writing login credentials.");
                //Write the JSON
                await stream.WriteAsync(requestData, 0, requestData.Length);
            }

            Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(Upload)}", "Getting report response.");

            //Get the report response
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                    Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(Upload)}", "Successfully uploaded report to server.");
                else
                {
                    string responseStr;
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                        responseStr = await reader.ReadToEndAsync();
                    Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(Upload)}", $"Failed to upload report to bot server. Server returned: {response.StatusCode} {response.StatusDescription} : {responseStr}");
                    throw new Exception("Report upload failed.");
                }
            }

            Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(Upload)}", "Report upload finished.");
        }

        /**
         * <summary>Serializes report information into a single report string.</summary>
         * <param name="courses">Courses to serialize into a string. Grades will be replaced with TRUE/FALSE for privacy. Can be null if error is set.</param>
         * <param name="hostanme">Hostname to appear in messages from the Discord Bot.</param>
         * <param name="error">If error is set, courses will be ignored and the report will be serialized as an error report.</param>
         * <returns>A JSON string containing the serialized report.</returns>
         */
        private string SerializeData(List<CourseInfo> courses, MarkCredentials credentials, string error = null)
        {
            try
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
                string dayStr = uptime.Days == 1 ? "day" : "days"; //If day count is 1, use "day" instead of "days"
                jsonObject.uptime = uptime.ToString($"d' {dayStr}, 'hh':'mm':'ss"); //Format "1 day, 06:22:33" or "2 days, 20:37:09"

                jsonObject.hostname = Hostname;
                jsonObject.coursesPublic = MakeCoursesPublic;

                //Using a place holder for the token so it can be injected into the upload stream to prevent storing the unencrypted token in memory.
                jsonObject.token = credentials.ApiKey;

                return JsonConvert.SerializeObject(jsonObject); //Convert object into JSON string.
            }
            catch(Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(ServerAgent)}.{nameof(SerializeData)}", "Failed to serialize report data.", ex);
                throw new JsonSerializationException("JSON serialization failed.");
            }
        }
    }
}
