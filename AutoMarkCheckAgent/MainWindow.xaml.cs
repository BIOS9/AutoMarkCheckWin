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
        AnimationHelper animator;

        public MainWindow()
        {
            InitializeComponent();
            animator = new AnimationHelper(this);
            animator.Opacity(0, UsernameLabel, UsernameTextBox, PasswordLabel, PasswordTextBox, ApiKeyLabel, ApiKeyTextBox, PublicCheckBox, SubmitButton);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BlurHelper.BlurWindow(this);
            animator.Animate("Show0.25", ExitButton, TitleLabel, TradeMarkLabel);

            animator.DelayAnimate(25, "Show1Fast", UsernameLabel);
            animator.DelayAnimate(50, "Show1Fast", UsernameTextBox);
            animator.DelayAct(50, () => animator.Opacity(1, UsernameLabel, UsernameTextBox));
            animator.DelayAnimate(75, "Show1Fast", PasswordLabel);
            animator.DelayAnimate(100, "Show1Fast", PasswordTextBox);
            animator.DelayAct(100, () => animator.Opacity(1, PasswordLabel, PasswordTextBox));
            animator.DelayAnimate(125, "Show1Fast", ApiKeyLabel);
            animator.DelayAnimate(150, "Show1Fast", ApiKeyTextBox);
            animator.DelayAct(150, () => animator.Opacity(1, ApiKeyLabel, ApiKeyTextBox));
            animator.DelayAnimate(175, "Show1Fast", PublicCheckBox);
            animator.DelayAct(175, () => animator.Opacity(1, PublicCheckBox));
            animator.DelayAnimate(200, "Show1Fast", SubmitButton);
            animator.DelayAct(200, () => animator.Opacity(1, SubmitButton, ApiKeyTextBox));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
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
