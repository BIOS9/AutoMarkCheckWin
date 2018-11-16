using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AutoMarkCheck.Helpers.Logging;

namespace AutoMarkCheckAgent
{
    public class Logging
    {
        public static void Initialize()
        {
            SetLogCallback(new Action<LogLevel, string, string, Exception>(Log)); //Set log output for the AutoMarkCheck library to the logging function in thie class.
        }

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
                Debug.Write($"[{DateTime.Now.ToString()}] [{level.ToString()}] <{source}> \"{message}\"");
                if (exception != null)
                {
                    Debug.Write(exception.Message);
                }
                Debug.WriteLine("");
            }
            catch
            {

            }
        }
    }
}
