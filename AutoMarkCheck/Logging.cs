using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoMarkCheck
{
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

        public static LogLevel LoggingLevel = LogLevel.DEBUG;

        public static void Log(LogLevel level, string source, string message, Exception exception = null)
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

            tempForm.richTextBox1.AppendText($"[{DateTime.Now.ToString()}] [{level.ToString()}] <{source}> \"{message}\"");
            if (exception != null)
            {
                tempForm.richTextBox1.AppendText(" Exception: " + exception.Message + " - " + exception.StackTrace);
            }
            tempForm.richTextBox1.AppendText(Environment.NewLine);
        }
    }
}
