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
        private AnimationHelper _animator;
        private bool _passwordChanged = false;
        private bool _apiKeyChanged = false;
        private SecureString _password;
        private SecureString _apiKey;

        public MainWindow()
        {
            InitializeComponent();
            _animator = new AnimationHelper(this);
            _animator.Opacity(0, EnableCheckBox, UsernameLabel, UsernameTextBox, PasswordLabel, PasswordTextBox, ApiKeyLabel, ApiKeyTextBox, PublicCheckBox, SaveButton, CancelButton, TestButton); //Hide GUI controls before animation.
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCredentials();

            EnableCheckBox.IsChecked = App.Settings.CheckingEnabled;
            PublicCheckBox.IsChecked = App.Settings.CoursesPublic;

            BlurHelper.BlurWindow(this); //Adds blurred glass effect
            _animator.Animate("Show0.25", ExitButton, TitleLabel, TradeMarkLabel);

            //Animate the control display
            _animator.DelayAnimate(25, "Show1Fast", EnableCheckBox);
            _animator.DelayAct(25, () => _animator.Opacity(1, EnableCheckBox));
            _animator.DelayAnimate(50, "Show1Fast", UsernameLabel);
            _animator.DelayAnimate(75, "Show1Fast", UsernameTextBox);
            _animator.DelayAct(75, () => _animator.Opacity(1, UsernameLabel, UsernameTextBox));
            _animator.DelayAnimate(100, "Show1Fast", PasswordLabel);
            _animator.DelayAnimate(125, "Show1Fast", PasswordTextBox);
            _animator.DelayAct(125, () => _animator.Opacity(1, PasswordLabel, PasswordTextBox));
            _animator.DelayAnimate(150, "Show1Fast", ApiKeyLabel);
            _animator.DelayAnimate(175, "Show1Fast", ApiKeyTextBox);
            _animator.DelayAct(175, () => _animator.Opacity(1, ApiKeyLabel, ApiKeyTextBox));
            _animator.DelayAnimate(200, "Show1Fast", PublicCheckBox);
            _animator.DelayAct(200, () => _animator.Opacity(1, PublicCheckBox));
            _animator.DelayAnimate(225, "Show1Fast", TestButton);
            _animator.DelayAct(225, () => _animator.Opacity(1, TestButton));
            _animator.DelayAnimate(250, "Show1Fast", SaveButton, CancelButton);
            _animator.DelayAct(250, () => _animator.Opacity(1, SaveButton, CancelButton));
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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await Settings.Save(App.Settings);

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

        private void PublicCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            App.Settings.CoursesPublic = false;
        }

        private void PublicCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            App.Settings.CoursesPublic = true;
        }

        private void EnableCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            App.Settings.CheckingEnabled = true;
        }

        private void EnableCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            App.Settings.CheckingEnabled = false;
        }
    }
}
