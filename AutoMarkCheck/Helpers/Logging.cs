using System;
using System.Diagnostics;

namespace AutoMarkCheck.Helpers
{
    /**
     * <summary>Logging helper that supports multi level logging with sources and exceptions.</summary>
     */
    public class Logging
    {
        public enum LogLevel
        {
            DEBUG,
            INFO,
            WARNING,
            ERROR
        }

        public static LogLevel LoggingLevel = LogLevel.DEBUG; //Ignore all logs under the current level

        private static Action<LogLevel, string, string, Exception> _loggingCallback = null;

        /**
         * <summary>Log an event of some sort.</summary>
         * <param name="level">Severity of the event. If this is lower than the current logging level, the event will not be saved.</param>
         * <param name="source">Where the event came from.</param>
         * <param name="message">A message explaining the event.</param>
         * <param name="exception">If there is an exception to log, it can be passed in here to be shown in the log.</param>
         */
        public static void Log(LogLevel level, string source, string message, Exception exception = null)
        {
            try
            {
                if (_loggingCallback == null)
                {
                    Console.Write($"[{DateTime.Now.ToString()}] [{level.ToString()}] <{source}> \"{message}\"");
                    if (exception != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(exception.Message);
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                }
                else
                    _loggingCallback(level, source, message, exception);
            }
            catch
            {

            }
        }

        public static void SetLogCallback(Action<LogLevel, string, string, Exception> callback)
        {
            _loggingCallback = callback;
        }
    }
}
