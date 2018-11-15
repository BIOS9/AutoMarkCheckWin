using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AutoMarkCheck.MyVUWAgent;

namespace AutoMarkCheck
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            Logging.tempForm = this;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var creds = CredentialManager.GetCredentials();
            if (creds == null)
            {
                MessageBox.Show("No creds");
                return;
            }
            try
            {
                //var courses = await MyVUWAgent.GetGrades(creds);
                //await ServerAgent.ReportGrades(courses, "coolHost", creds);
                //MessageBox.Show("grade count: " + courses.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            var creds = CredentialManager.GetCredentials();
            if (creds == null)
            {
                CredentialManager.SetCredentials(new CredentialManager.MarkCredentials("test", "test", "tokenInsertHere"));
                MessageBox.Show("No creds");
                return;
            }
            List<CourseInfo> courses = await MyVUWAgent.GetGrades(creds);
            if (courses.Count > 0)
            {
                var success = await ServerAgent.ReportGrades(courses, "CoolHost 😎", creds);
            }
        }
    }
}
