using AutoMarkCheck.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static AutoMarkCheck.Helpers.CredentialManager;
using static AutoMarkCheck.Helpers.Logging;

namespace AutoMarkCheck
{
    class Program
    {
        public static Settings Settings;

        static DateTime _lastGradeCheck = DateTime.MinValue;
        static MarkCredentials _credentials;
        static bool _missingCredentials = false;
        static bool _actionStarted = false;
        static CancellationTokenSource _actionDelayCancellation = new CancellationTokenSource();

        static void Main(string[] args)
        {
            Logging.Log(LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(Program)}.{nameof(Main)}", "Starting...");
            Console.WriteLine("AutoMarkCheck cross-platform agent V1.0.0");
            Logging.CleanOldLogs();

            Console.WriteLine("Loading settings...");
            Settings = Settings.Load().Result; // Load settings

            // If settings failed to load, exit
            if (Settings == null)
            {
                Logging.Log(LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(Program)}.{nameof(Main)}", "Failed to load settings! Exiting...");
                return;
            }

            Console.WriteLine("Log level set to: " + Settings.LogLevel.ToString());
            Logging.SetLogLevel(Settings.LogLevel);

            Console.WriteLine("Loading credentials...");
            _credentials = CredentialManager.GetCredentials(false);

            if (_credentials == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Credentials missing!");
                _missingCredentials = true;
                askCredentials();
            }

            startGradeCheckPolling();

            if(!_missingCredentials)
            {
                
                while (true)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("Do you want to change your credentials? (Y/N) (10 seconds): ");
                    Console.ResetColor();
                    char key = Console.ReadKey().KeyChar;
                    Console.WriteLine();
                    switch (key)
                    {
                        case 'n':
                        case 'N':
                            _actionDelayCancellation.Cancel();
                            goto fin;
                        case 'y':
                        case 'Y':
                            _actionStarted = true;
                            _actionDelayCancellation.Cancel();
                            askCredentials();
                            startGradeCheckPolling();
                            goto fin;
                    }
                }
            }

        fin:
            while (true)
            {
                DateTime nextCheck = _lastGradeCheck.AddSeconds(Settings.GradeCheckInterval).AddMinutes(1);
                if (nextCheck < DateTime.Now)
                    nextCheck = DateTime.Now;
                TimeSpan timeTillNext = nextCheck - DateTime.Now;
                Console.WriteLine($"Next grade check at: {nextCheck.ToShortTimeString()} (in apprx {timeTillNext.Hours} hours, {timeTillNext.Minutes} minutes)");
                Console.ReadLine();
            }
        }

        private static async void startGradeCheckPolling()
        {
            // Awaits user action at beginning of program, times out after 10s
            if (!_actionDelayCancellation.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(10000, _actionDelayCancellation.Token);
                }
                catch { }
                if (_actionStarted) // Cancel this async method when the user does something at the start
                    return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\nReady!");
            Console.ResetColor();

            while (true)
            {
                //Ensure that grades are not checked until the interval has been waited from the last check
                if ((DateTime.Now - _lastGradeCheck).TotalSeconds > Settings.GradeCheckInterval)
                {
                    await checkGrades(Settings.CustomHostname, Settings.CoursesPublic);

                    _lastGradeCheck = DateTime.Now;
                }
                else
                    Logging.Log(LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Program)}.{nameof(Main)}", "Aborting mark check because interval has not passed.");

                // check every 30s
                await Task.Delay(30000);
            }
        }

        /**
         * <summary>Starts the grade check process.</summary>
         * <param name="hostname">The hostname to appear on the F5 Discord bot messages.</param>
         * <param name="coursesPublic">Indicates if your username should be shown with the courses or if they should be anonymized.</param>
         */
        private static async Task checkGrades(string hostname, bool coursesPublic)
        {
            ServerAgent agent = null;
            Grades.IGradeSource gradeSource;

            try
            {
                _credentials = CredentialManager.GetCredentials();
                if (_credentials != null)
                {
                    agent = new ServerAgent(_credentials, hostname, coursesPublic);
                    gradeSource = new Grades.StudentRecordGradeSource(_credentials);

                    List<Grades.CourseInfo> grades = await gradeSource.GetGrades();

                    if (grades == null || grades.Count == 0)
                        throw new Exception("Grade list empty.");

                    await agent.ReportGrades(grades, _credentials);
                }
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(Program)}.{nameof(checkGrades)}", "Error checking grades.", ex);

                // Delay next grade check to avoid account lockouts for an incorrect password
                Logging.Log(LogLevel.WARNING, $"{nameof(AutoMarkCheck)}.{nameof(Program)}.{nameof(checkGrades)}", "Delaying next grade check for 4 hours.");
                _lastGradeCheck = DateTime.Now.AddHours(4);
            }
        }

        /**
         * <summary>Asks the user for their VUW and F5 bot credentials through the console.</summary>
         */
        private static void askCredentials()
        {
            Console.ResetColor();

            Console.Write("Please enter your VUW username: ");
            string username = Console.ReadLine();

            Console.Write("Please enter your VUW password: ");
            string password = "";
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        break;
                    }
                }
            } while (true);

            Console.Write("Please enter your F5 AutoMarkCheck API key: ");
            string token = Console.ReadLine();

            _credentials = new MarkCredentials(username, password, token);

            Console.WriteLine("Saving credentials... (this may take a while on Windows systems)");
            CredentialManager.SetCredentials(_credentials);
        }
    }
}
