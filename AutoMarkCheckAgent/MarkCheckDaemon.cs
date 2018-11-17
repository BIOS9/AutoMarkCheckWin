using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using static AutoMarkCheck.Helpers.Logging;

namespace AutoMarkCheckAgent
{
    public class MarkCheckDaemon
    {
        public bool? GradeCheckingEnabled { get; private set; } = null; //Bool will be null if still waiting on response from server.
        public TimeSpan GradeCheckingInterval { get; private set; } = TimeSpan.FromSeconds(DefaultGradeCheckingInterval);

        public const int MinGradeCheckingInterval = 60; //Value in seconds, 1 minute
        public const int DefaultGradeCheckingInterval = 1800; //Value in seconds, 30 minutes
        public const int MaxGradeCheckingInterval = 43200; //Value in seconds, 12 hours

        private Thread _runThread;
        private bool _run = true;
        private bool _guiShowing = false;
        private DateTime _lastGradeCheck = DateTime.MinValue;

        private const int RunThreadInterval = 1000;

        public MarkCheckDaemon()
        {
            _runThread = new Thread(Run);
            _runThread.Start();
        }

        /**
         * <summary>Sets interval between grade checks. Ensures interval is between <see cref="MinGradeCheckingInterval">Min</see> and <see cref="MaxGradeCheckingInterval">Max</see>.</summary
         */
        public bool SetGradeCheckingInterval(TimeSpan interval)
        {
            if (interval.TotalSeconds < MinGradeCheckingInterval)
                return false;
            if (interval.TotalSeconds > MaxGradeCheckingInterval)
                return false;

            Logging.Log(LogLevel.DEBUG, $"{nameof(AutoMarkCheckAgent)}.{nameof(MarkCheckDaemon)}.{nameof(SetGradeCheckingInterval)}", $"Grade checking interval has been set to {interval.TotalHours}{interval.ToString("':'mm':'ss")} (hh:mm:ss)");
            GradeCheckingInterval = interval;
            return true;
        }

        private void Exit()
        {
            Logging.Log(LogLevel.INFO, $"{nameof(AutoMarkCheckAgent)}.{nameof(MarkCheckDaemon)}.{nameof(Exit)}", "Application shutting down.");

            _run = false;
            Application.Exit();
        }

        private void Show()
        {
            try
            {
                Logging.Log(LogLevel.DEBUG, $"{nameof(AutoMarkCheckAgent)}.{nameof(MarkCheckDaemon)}.{nameof(Show)}", "Opening GUI.");
                _guiShowing = true;
                MainWindow mainWindow = new MainWindow();
                mainWindow.ShowDialog();
                _guiShowing = false;
                GC.Collect(); //Collect memory after window closes
                Logging.Log(LogLevel.DEBUG, $"{nameof(AutoMarkCheckAgent)}.{nameof(MarkCheckDaemon)}.{nameof(Show)}", "Closed GUI.");
            }
            catch(Exception ex)
            {
                Logging.Log(LogLevel.ERROR, $"{nameof(AutoMarkCheckAgent)}.{nameof(MarkCheckDaemon)}.{nameof(Show)}", "Error loading GUI.", ex);
                MessageBox.Show("There was an error loading the GUI: \n" + ex.Message + "\n\nCheck the log file for a more detailed error.", "Auto Mark Check Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Run()
        {
            while (_run)
            {
                Thread.Sleep(RunThreadInterval);
                if (_guiShowing) continue; //Dont perform checking while the GUI is open because it could conflict with the check button.

                try
                {
                    
                }
                catch (Exception ex)
                {
                    Logging.Log(LogLevel.ERROR, $"{nameof(AutoMarkCheckAgent)}.{nameof(MarkCheckDaemon)}.{nameof(Run)}", "Error in main thread.", ex);
                }
            }
        }
    }
}
