using AutoMarkCheck.Grades;
using AutoMarkCheck.Helpers;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static AutoMarkCheck.Grades.MyVuwGradeSource;

namespace AutoMarkCheck.UI
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

            IGradeSource gradeSource = new MyVuwGradeSource(creds);
            ServerAgent serverAgent = new ServerAgent(creds, "CoolHost 😎");
            List<CourseInfo> courses = await gradeSource.GetGrades();

            if (courses.Count > 0)
            {
                var success = await serverAgent.ReportGrades(courses);
            }
        }
    }
}
