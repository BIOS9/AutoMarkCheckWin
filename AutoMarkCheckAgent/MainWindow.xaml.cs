using AutoMarkCheck;
using AutoMarkCheck.Grades;
using AutoMarkCheck.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
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
        bool _passwordChanged = false;
        bool _apiKeyChanged = false;
        SecureString _password;
        SecureString _apiKey;

        public MainWindow()
        {
            InitializeComponent();
            animator = new AnimationHelper(this);
            animator.Opacity(0, EnableCheckBox, UsernameLabel, UsernameTextBox, PasswordLabel, PasswordTextBox, ApiKeyLabel, ApiKeyTextBox, PublicCheckBox, SaveButton, CancelButton, TestButton); //Hide GUI controls before animation.
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCredentials();
            BlurHelper.BlurWindow(this); //Adds blurred glass effect
            animator.Animate("Show0.25", ExitButton, TitleLabel, TradeMarkLabel);

            //Animate the control display
            animator.DelayAnimate(25, "Show1Fast", EnableCheckBox);
            animator.DelayAct(25, () => animator.Opacity(1, EnableCheckBox));
            animator.DelayAnimate(50, "Show1Fast", UsernameLabel);
            animator.DelayAnimate(75, "Show1Fast", UsernameTextBox);
            animator.DelayAct(75, () => animator.Opacity(1, UsernameLabel, UsernameTextBox));
            animator.DelayAnimate(100, "Show1Fast", PasswordLabel);
            animator.DelayAnimate(125, "Show1Fast", PasswordTextBox);
            animator.DelayAct(125, () => animator.Opacity(1, PasswordLabel, PasswordTextBox));
            animator.DelayAnimate(150, "Show1Fast", ApiKeyLabel);
            animator.DelayAnimate(175, "Show1Fast", ApiKeyTextBox);
            animator.DelayAct(175, () => animator.Opacity(1, ApiKeyLabel, ApiKeyTextBox));
            animator.DelayAnimate(200, "Show1Fast", PublicCheckBox);
            animator.DelayAct(200, () => animator.Opacity(1, PublicCheckBox));
            animator.DelayAnimate(225, "Show1Fast", TestButton);
            animator.DelayAct(225, () => animator.Opacity(1, TestButton));
            animator.DelayAnimate(250, "Show1Fast", SaveButton, CancelButton);
            animator.DelayAct(250, () => animator.Opacity(1, SaveButton, CancelButton));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LoadCredentials()
        {
            var credentials = CredentialManager.GetCredentials();
            if(credentials != null)
            {
                UsernameTextBox.Text = credentials.Username;

                //Add placeholder password/key to text boxes to prevent password being insecure in memory
                //The values will be the correct length but only contain *
                for (int i = 0; i < credentials.Password.Length; ++i)
                    PasswordTextBox.Password += "*";
                for (int i = 0; i < credentials.ApiKey.Length; ++i)
                    ApiKeyTextBox.Password += "*";

                _password = credentials.Password;
                _apiKey = credentials.ApiKey;
            }

            _passwordChanged = false;
            _apiKeyChanged = false;
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

        private void PasswordTextBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _passwordChanged = true;
        }

        private void PasswordTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!_passwordChanged)
                PasswordTextBox.Clear();
        }

        private void ApiKeyTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!_apiKeyChanged)
                ApiKeyTextBox.Clear();
        }

        private void ApiKeyTextBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _apiKeyChanged = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsernameTextBox.Text.Length == 0 && PasswordTextBox.SecurePassword.Length == 0 && ApiKeyTextBox.SecurePassword.Length == 0)
                CredentialManager.DeleteCredentials();
            else
            {
                if (_passwordChanged)
                    _password = PasswordTextBox.SecurePassword;

                if (_apiKeyChanged)
                    _apiKey = ApiKeyTextBox.SecurePassword;

                var credentials = new CredentialManager.MarkCredentials(UsernameTextBox.Text, _password, _apiKey);

                CredentialManager.SetCredentials(credentials);
            }
        }
    }
}
