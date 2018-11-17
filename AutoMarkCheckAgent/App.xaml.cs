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
        [STAThread]
        public static void Main(string[] args)
        {
            Logging.Initialize();
            if(args.Contains("-gui"))
            {
                Show();
            }
            else
            {

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
    }
}
