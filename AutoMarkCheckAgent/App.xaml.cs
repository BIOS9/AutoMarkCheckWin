using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AutoMarkCheckAgent
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static MarkCheckDaemon Daemon;

        [STAThread]
        public static void Main()
        {
            Daemon = new MarkCheckDaemon();
        }
    }
}
