using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AutoMarkCheck.Helpers.Logging;

namespace AutoMarkCheckAgent
{
    public class MarkCheckDaemon
    {
        private NotifyIcon notifyIcon;

        public MarkCheckDaemon()
        {
            LoadNotifyIcon();
        }

        private void LoadNotifyIcon()
        {
            try
            {

            }
            catch(Exception ex)
            {
                Logging.Log(LogLevel.ERROR, $"{nameof(AutoMarkCheckAgent)}.{nameof(MarkCheckDaemon)}.{nameof(LoadNotifyIcon)}", "Failed to load tray icon.", ex);
            }
        }
    }
}
