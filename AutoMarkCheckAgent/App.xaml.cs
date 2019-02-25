using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using static AutoMarkCheck.Helpers.Logging;

namespace AutoMarkCheckAgent
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static Settings Settings;

        [STAThread]
        public static void Main(string[] args)
        {
            Logging.Initialize();
            Logging.CleanOldLogs();

            Settings = Settings.Load().Result; // Load settings

            // If settings failed to load, exit
            if (Settings == null)
            {
                exit(1);
                return;
            }

            Logging.SetLogLevel(Settings.LogLevel);

            //If the app is launched with the -gui command line option, show the settings window
            if (args.Contains("-gui"))
            {
                show();
            }
            else
            {
                //Ensure checking is enabled
                if(!Settings.CheckingEnabled)
                {
                    Logging.Log(LogLevel.DEBUG, $"{nameof(AutoMarkCheckAgent)}.{nameof(App)}.{nameof(Main)}", "Aborting mark check because checking is disabled.");
                    exit(2);
                }

                //Ensure that grades are not checked until the interval has been waited from the last check
                if ((DateTime.Now - Settings.LastGradeCheck).TotalSeconds > Settings.GradeCheckInterval)
                {
                    checkGrades(Settings.CustomHostname, Settings.CoursesPublic).Wait();

                    try
                    {
                        Settings.LastGradeCheck = DateTime.Now;
                        Settings.Save(Settings).Wait();
                    }
                    catch { }
                }
                else
                {
                    Logging.Log(LogLevel.DEBUG, $"{nameof(AutoMarkCheckAgent)}.{nameof(App)}.{nameof(Main)}", "Aborting mark check because interval has not passed.");
                    exit(3);
                }
            }

            exit();
        }

        public static void exit(int errorCode = 0)
        {
            Logging.CloseLogging();
            Environment.Exit(errorCode);
        }

        private static void show()
        {
            try
            {
                Logging.Log(LogLevel.DEBUG, $"{nameof(AutoMarkCheckAgent)}.{nameof(App)}.{nameof(show)}", "Opening GUI.");
                MainWindow mainWindow = new MainWindow();
                mainWindow.ShowDialog();
                Logging.Log(LogLevel.DEBUG, $"{nameof(AutoMarkCheckAgent)}.{nameof(App)}.{nameof(show)}", "Closed GUI.");
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.ERROR, $"{nameof(AutoMarkCheckAgent)}.{nameof(App)}.{nameof(show)}", "Error loading GUI.", ex);
                System.Windows.Forms.MessageBox.Show("There was an error loading the GUI: \n" + ex.Message + "\n\nCheck the log file for a more detailed error.", "Auto Mark Check Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static async Task checkGrades(string hostname, bool coursesPublic)
        {
            AutoMarkCheck.ServerAgent agent = null;
            AutoMarkCheck.Helpers.CredentialManager.MarkCredentials credentials;
            AutoMarkCheck.Grades.IGradeSource gradeSource;

            try
            {
                credentials = AutoMarkCheck.Helpers.CredentialManager.GetCredentials();
                if (credentials != null)
                {
                    agent = new AutoMarkCheck.ServerAgent(credentials, hostname, coursesPublic);
                    gradeSource = new AutoMarkCheck.Grades.StudentRecordGradeSource(credentials);
                    List<AutoMarkCheck.Grades.CourseInfo> grades = await gradeSource.GetGrades();
                    if (grades == null || grades.Count == 0)
                        throw new Exception("Grade list empty.");
                    await agent.ReportGrades(grades);
                }
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.ERROR, $"{nameof(AutoMarkCheckAgent)}.{nameof(App)}.{nameof(checkGrades)}", "Error checking grades.", ex);

                // Attempt to report error to server
                try
                {
                    if(agent != null)
                        await agent.ReportError("Error reporting grades: " + ex.Message);
                }
                catch { }

                // Delay next grade check to avoid account lockouts for an incorrect password
                Logging.Log(LogLevel.WARNING, $"{nameof(AutoMarkCheckAgent)}.{nameof(App)}.{nameof(checkGrades)}", "Delaying next grade check for 4 hours.");
                Settings.LastGradeCheck = DateTime.Now.AddHours(4);
                await Settings.Save(Settings);
            }
        }
    }
}
