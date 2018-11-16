using AutoMarkCheck;
using AutoMarkCheck.Grades;
using AutoMarkCheck.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AutoMarkCheckAgent
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void test()
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
            //MessageBox.Show((await gradeSource.CheckCredentials()).ToString());
            List<CourseInfo> courses = await gradeSource.GetGrades();

            if (courses.Count > 0)
            {
                var success = await serverAgent.ReportGrades(courses);
            }
        }
    }
}
