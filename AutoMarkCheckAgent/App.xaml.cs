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

            Settings = Settings.Load().Result; // Load settings

            // If settings failed to load, exit
            if (Settings == null)
            {
                Environment.Exit(2);
                return;
            }

            if (args.Contains("-gui"))
            {
                Show();
            }
            else
            {
                System.Windows.MessageBox.Show("AYE");
                CheckGrades().Wait();
            }
        }

        private static void Show()
        {
            try
            {
                Logging.Log(LogLevel.DEBUG, $"{nameof(AutoMarkCheckAgent)}.{nameof(App)}.{nameof(Show)}", "Opening GUI.");
                MainWindow mainWindow = new MainWindow();
                mainWindow.ShowDialog();
                Logging.Log(LogLevel.DEBUG, $"{nameof(AutoMarkCheckAgent)}.{nameof(App)}.{nameof(Show)}", "Closed GUI.");
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.ERROR, $"{nameof(AutoMarkCheckAgent)}.{nameof(App)}.{nameof(Show)}", "Error loading GUI.", ex);
                System.Windows.Forms.MessageBox.Show("There was an error loading the GUI: \n" + ex.Message + "\n\nCheck the log file for a more detailed error.", "Auto Mark Check Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static async Task CheckGrades()
        {
            AutoMarkCheck.ServerAgent agent = null;
            AutoMarkCheck.Helpers.CredentialManager.MarkCredentials credentials;
            AutoMarkCheck.Grades.IGradeSource gradeSource;

            try
            {
                credentials = AutoMarkCheck.Helpers.CredentialManager.GetCredentials();
                if (credentials != null)
                {
                    agent = new AutoMarkCheck.ServerAgent(credentials, "CoolHost 😎", false);
                    gradeSource = new AutoMarkCheck.Grades.MyVuwGradeSource(credentials);
                    List<AutoMarkCheck.Grades.CourseInfo> grades = await gradeSource.GetGrades();
                    if (grades == null || grades.Count == 0)
                        throw new Exception("Grade list empty.");
                    await agent.ReportGrades(grades);
                }
            }
            catch (Exception ex)
            {
                Logging.Log(LogLevel.ERROR, $"{nameof(AutoMarkCheckAgent)}.{nameof(App)}.{nameof(CheckGrades)}", "Error checking grades.", ex);

                //Attempt to report error to server
                try
                {
                    if(agent != null)
                        await agent.ReportError("Error reporting grades: " + ex.Message);
                }
                catch { }
            }
        }
    }
}
