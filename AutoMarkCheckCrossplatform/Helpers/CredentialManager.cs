using System;
using System.Text;

namespace AutoMarkCheck.Helpers
{
    /**
     * <summary>Manages credentials used by the AutoMarkCheck bot.</summary>
     */
    public class CredentialManager
    {
        private const string VuwUsernameEnvironmentVariable = "AutoMarkCheckVuwUsername";
        private const string VuwPasswordEnvironmentVariable = "AutoMarkCheckVuwPassword";
        private const string ApiKeyEnvironmentVariable = "AutoMarkCheckApiKey";

        /**
         * <summary>Credential class to store credentials for MyVUW and the bot API</summary>
         * <seealso cref="CredentialManager"/>
         */
        public class MarkCredentials
        {
            public string Username;
            public string Password;
            public string ApiKey;
            public static Encoding CredentialEncoding = Encoding.UTF8;

            public MarkCredentials(string username, string password, string apiKey)
            {
                Username = username;
                Password = password;
                ApiKey = apiKey;
            }
        }

        /**
         * <summary>Returns all credentials that are used by AutoMarkCheck. Returns MyVUW account and API key.</summary>
         * <seealso cref="MarkCredentials"/>
         */
        public static MarkCredentials GetCredentials(bool suppressLogs = false)
        {
            try
            {
                string F5VuwUsername = Environment.GetEnvironmentVariable(VuwUsernameEnvironmentVariable, EnvironmentVariableTarget.User);
                string F5VuwPassword = Environment.GetEnvironmentVariable(VuwPasswordEnvironmentVariable, EnvironmentVariableTarget.User);
                string F5ApiKey = Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable, EnvironmentVariableTarget.User);

                bool missingCreds = false;

                // Ensures username is present
                if (string.IsNullOrEmpty(F5VuwUsername))
                {
                    if (!suppressLogs)
                        Logging.Log(Logging.LogLevel.WARNING, $"{nameof(AutoMarkCheck)}.{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(GetCredentials)}", $"VUW username not set. Please set the environment variable {VuwUsernameEnvironmentVariable}");
                    missingCreds = true;
                }

                // Ensures password is present
                if (string.IsNullOrEmpty(F5VuwPassword))
                {
                    if (!suppressLogs)
                        Logging.Log(Logging.LogLevel.WARNING, $"{nameof(AutoMarkCheck)}.{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(GetCredentials)}", $"VUW password not set. Please set the environment variable {VuwPasswordEnvironmentVariable}");
                    missingCreds = true;
                }

                // Ensures API key is present
                if (string.IsNullOrEmpty(F5ApiKey))
                {
                    if (!suppressLogs)
                        Logging.Log(Logging.LogLevel.WARNING, $"{nameof(AutoMarkCheck)}.{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(GetCredentials)}", $"F5 API key not set. Please set the environment variable {ApiKeyEnvironmentVariable}");
                    missingCreds = true;
                }

                if (missingCreds)
                    return null;

                //Load credentials into MarkCredentials object
                var creds = new MarkCredentials(
                    F5VuwUsername,
                    F5VuwPassword,
                    F5ApiKey);

                if (!suppressLogs)
                    Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(AutoMarkCheck)}.{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(GetCredentials)}", "Successfully got credentials from the credential store.");

                return creds;
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(GetCredentials)}", "Failed to get credentials from the credential store.", ex);

                return null;
            }
        }

        /**
         * <summary>Set credentials used by AutoMarkCheck. Existing credentials will be overwritten.</summary>
         * <seealso cref="MarkCredentials"/>
         * <param name="credentials">Credentials to save in the environment variables.</param>
         */
        public static void SetCredentials(MarkCredentials credentials)
        {
            try
            {
                Environment.SetEnvironmentVariable(VuwUsernameEnvironmentVariable, credentials.Username, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable(VuwPasswordEnvironmentVariable, credentials.Password, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable(ApiKeyEnvironmentVariable, credentials.ApiKey, EnvironmentVariableTarget.User);

                Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(SetCredentials)}", "New credentials saved.");
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(SetCredentials)}", "Failed to save new credentials.", ex);
            }
        }

        /**
         * <summary>Removes AutoMarkCheck credentials from the windows store.</summary>
         */
        public static void DeleteCredentials()
        {
            try
            {
                Environment.SetEnvironmentVariable(VuwUsernameEnvironmentVariable, "", EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable(VuwPasswordEnvironmentVariable, "", EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable(ApiKeyEnvironmentVariable, "", EnvironmentVariableTarget.User);

                Logging.Log(Logging.LogLevel.INFO, $"{nameof(AutoMarkCheck)}.{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(DeleteCredentials)}", "Credentials successfully delete from the credential store.");
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(AutoMarkCheck)}.{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(DeleteCredentials)}", "Failed to delete credentials from the credential store.", ex);
            }
        }
    }
}
