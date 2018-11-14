using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoMarkCheck
{
    public class Logging
    {
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

        }
    }
}
