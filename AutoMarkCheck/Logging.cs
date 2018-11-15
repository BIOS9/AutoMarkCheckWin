using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoMarkCheck
{
    /**
     * <summary>Logging helper that supports multi level logging with sources and exceptions.</summary>
     */
    public class Logging
    {
        public static MainForm tempForm;

        public enum LogLevel
        {
            DEBUG,
            INFO,
            WARNING,
            ERROR
        }

        public static LogLevel LoggingLevel = LogLevel.DEBUG; //Ignore all logs under the current level
        
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
                //Console.Write($"[{DateTime.Now.ToString()}] [{level.ToString()}] <{source}> \"{message}\"");
                //if(exception != null)
                //{
                //    Console.ForegroundColor = ConsoleColor.Red;
                //    Console.Write(exception.Message);
                //    Console.ResetColor();
                //}
                //Console.WriteLine();

                Debug.Write($"[{DateTime.Now.ToString()}] [{level.ToString()}] <{source}> \"{message}\"");
                if (exception != null)
                {
                    Debug.Write(exception.Message);
                }
                Debug.WriteLine("");

                if (tempForm != null)
                {
                    tempForm.richTextBox1.AppendText($"[{DateTime.Now.ToString()}] [{level.ToString()}] <{source}> \"{message}\"");
                    if (exception != null)
                    {
                        tempForm.richTextBox1.AppendText(" Exception: \"" + exception.Message + " - " + exception.StackTrace + "\"");
                    }
                    tempForm.richTextBox1.AppendText(Environment.NewLine);
                }
            }
            catch
            {

            }
        }
    }
}
