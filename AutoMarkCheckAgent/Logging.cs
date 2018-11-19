using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AutoMarkCheck.Helpers.Logging;

namespace AutoMarkCheckAgent
{
    public class Logging
    {
        public const string LogDirectory = "Logs";
        public const string LogFilePrefix = "AMC_";
        public const string LogFileSuffix = ".log.txt";

        static DateTime lastLog = DateTime.Now;
        static FileStream _logFileStream = null;
        static StreamWriter _logWriter = null;
        
        public static void Initialize()
        {
            SetLogCallback(new Action<LogLevel, string, string, Exception>(Log)); // Set log output for the AutoMarkCheck library to the logging function in thie class.
        }

        public static int MaxLogDays = 3; // How many days to keep a log file for
        public static LogLevel LoggingLevel { get; private set; } = LogLevel.INFO; // Ignore all logs under the current level

        public static void SetLogLevel(LogLevel logLevel)
        {
            //LoggingLevel = LogLevel.INFO; // Overrride log level so the next log will always show
            //Log(LogLevel.INFO, $"{nameof(AutoMarkCheckAgent)}.{nameof(Logging)}.{nameof(SetLogLevel)}", $"Logging level set to: {logLevel}");
            LoggingLevel = logLevel;
        }

        /**
         * <summary>Log an event of some sort.</summary>
         * <param name="level">Severity of the event. If this is lower than the current logging level, the event will not be saved.</param>
         * <param name="source">Where the event came from.</param>
         * <param name="message">A message explaining the event.</param>
         * <param name="exception">If there is an exception to log, it can be passed in here to be shown in the log.</param>
         */
        public static async void Log(LogLevel level, string source, string message, Exception exception = null)
        {
            try
            {
                if(lastLog.Day != DateTime.Now.Day)
                {
                    await _logWriter.FlushAsync();
                    await _logFileStream.FlushAsync();

                    _logWriter.Close();

                    _logWriter.Dispose();
                    _logFileStream.Dispose();

                    _logWriter = null;
                    _logFileStream = null;
                }

                if (!Directory.Exists(LogDirectory)) Directory.CreateDirectory(LogDirectory);
                string logFile = Path.Combine(Directory.GetCurrentDirectory(), LogDirectory, LogFilePrefix + DateTime.Now.ToString("yyyy-M-dd") + LogFileSuffix);

                if (_logFileStream == null)
                    _logFileStream = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.Read);

                if (_logWriter == null)
                    _logWriter = new StreamWriter(_logFileStream);

                if (level < LoggingLevel) return;

                string logText = $"[{DateTime.Now.ToString()}] [{level.ToString()}] <{source}> \"{message}\"";

#if DEBUG
                Debug.Write(logText);
                if (exception != null)
                {
                    Debug.Write(exception.Message);
                }
                Debug.WriteLine("");
#endif

                await _logWriter.WriteLineAsync(logText);
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Logging error: " + ex.Message);
            }
        }

        /**
         * <summary>Flushes log stream, writing the data to the file and then closes the file.</summary>
         */
        public static void CloseLogging()
        {
            _logWriter.Flush();
            _logFileStream.Flush();

            _logWriter.Close();

            _logWriter.Dispose();
            _logFileStream.Dispose();
        }

        /**
         * <summary>Deletes log files older than the specified time.</summary>
         */
        public static void CleanOldLogs()
        {
            try
            {
                if (!Directory.Exists(LogDirectory)) return;
                string[] files = Directory.GetFiles(LogDirectory, "*.log.txt");
                foreach(string file in files)
                {
                    var info = new FileInfo(file);
                    if (info.CreationTime < DateTime.Now - TimeSpan.FromDays(MaxLogDays))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch { }
                    }
                }
            }
            catch(Exception ex)
            {
                Logging.Log(LogLevel.ERROR, $"{nameof(AutoMarkCheckAgent)}.{nameof(Logging)}.{nameof(CleanOldLogs)}", "Failed to clean old log files.", ex);
            }
        }
    }
}
