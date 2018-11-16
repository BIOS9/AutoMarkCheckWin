using AutoMarkCheck.Grades;
using AutoMarkCheck.Helpers;
using CredentialManagement;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace AutoMarkCheck.Helpers
{
    /**
     * <summary>Manages credentials used by the AutoMarkCheck bot.</summary>
     */
    public class CredentialManager
    {
#if DEBUG
        private const string VuwCredentialStoreTarget = "AutoMarkCheckGradesDebug";
        private const string ApiCredentialStoreTarget = "AutoMarkCheckAPIDebug";
#else
        private const string VuwCredentialStoreTarget = "AutoMarkCheckGrades";
        private const string ApiCredentialStoreTarget = "AutoMarkCheckAPI";
#endif
        /**
         * <summary>Credential class to store credentials for MyVUW and the bot API</summary>
         * <seealso cref="CredentialManager"/>
         */
        public class MarkCredentials
        {
            public readonly static Encoding CredentialEncoding = Encoding.UTF8;
            public string Username;
            public SecureString Password = new SecureString();
            public SecureString ApiKey = new SecureString();
            public int EscapedPasswordSize;
            public int EscapedApiKeySize;

            #region Constructors

            public MarkCredentials(string username, string password, string apiKey)
            {
                Username = username;
                password.ToList().ForEach(x => Password.AppendChar(x));
                apiKey.ToList().ForEach(x => ApiKey.AppendChar(x));

                //Calculate the size in bytes of the escaped password and key so they can be used later for an HTTP request without persisting in memory as a string
                string escapedPassword = Uri.EscapeDataString(password);
                EscapedPasswordSize = CredentialEncoding.GetByteCount(escapedPassword);

                //API key doesn't need to be URI escaped because it isnt sent as a POST FORM its sent as POST JSON
                string jsonString = JsonConvert.ToString(apiKey); //Key is uploaded in JSON so it must be escaped in json format
                jsonString = jsonString.Remove(0, 1); //To remove the leading "
                jsonString = jsonString.Remove(jsonString.Length - 1, 1); //To remove the trailing "
                EscapedApiKeySize = CredentialEncoding.GetByteCount(jsonString);
                Password.MakeReadOnly();
                ApiKey.MakeReadOnly();
            }

            public MarkCredentials(string username, SecureString password, SecureString apiKey)
            {
                Username = username;
                Password = password;
                ApiKey = apiKey;

                //Calculate password length
                IntPtr passwordPtr = Marshal.SecureStringToBSTR(password); //Convert SecureString password to BSTR and get the pointer
                try
                {
                    byte b = 1;
                    int i = 0;

                    while (true) //Loop over characters in the BSTR
                    {
                        b = Marshal.ReadByte(passwordPtr, i);
                        if (b == 0) break; //If terminator character '\0' is hit exit loop

                        string escapedChar = Uri.EscapeDataString(((char)b).ToString()); //Must be a string because the escaped character can be more than 1 character long eg %7
                        EscapedPasswordSize += CredentialEncoding.GetByteCount(escapedChar); //Add the length to the credential length

                        i = i + 2;  // BSTR is unicode and occupies 2 bytes
                    }
                }
                finally
                {
                    Marshal.ZeroFreeBSTR(passwordPtr); //Securely clear password BSTR from memory
                }

                //Calculate key length
                IntPtr keyPtr = Marshal.SecureStringToBSTR(ApiKey); //Convert SecureString key to BSTR and get the pointer
                try
                {
                    byte b = 1;
                    int i = 0;

                    while (true) //Loop over characters in the BSTR
                    {
                        b = Marshal.ReadByte(keyPtr, i);
                        if (b == 0) break; //If terminator character '\0' is hit exit loop

                        string jsonString = JsonConvert.ToString((char)b); //Key is uploaded in JSON so it must be escaped in json format
                        jsonString = jsonString.Remove(0, 1); //To remove the leading "
                        jsonString = jsonString.Remove(jsonString.Length - 1, 1); //To remove the trailing "
                        EscapedApiKeySize += CredentialEncoding.GetByteCount(jsonString); //Add the length to the credential length

                        i = i + 2;  // BSTR is unicode and occupies 2 bytes
                    }
                }
                finally
                {
                    Marshal.ZeroFreeBSTR(keyPtr); //Securely clear key BSTR from memory
                }
                Password.MakeReadOnly();
                ApiKey.MakeReadOnly();
            }

            #endregion
        }

        /**
         * <summary>Returns all credentials that are used by AutoMarkCheck. Returns MyVUW account and API key.</summary>
         * <seealso cref="MarkCredentials"/>
         */
        public static MarkCredentials GetCredentials()
        {
            try
            {
                //Get credentials from Windows credential store
                var myVuwCredentials = new Credential { Target = VuwCredentialStoreTarget };
                var apiCredentials = new Credential { Target = ApiCredentialStoreTarget };

                if (!myVuwCredentials.Load() || !apiCredentials.Load())
                {
                    Logging.Log(Logging.LogLevel.WARNING, $"{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(GetCredentials)}", "There are no credentials saved for AutoMarkCheck.");
                    return null; //If loading fails
                }

                //Load credentials into MarkCredentials object
                var creds = new MarkCredentials(
                    myVuwCredentials.Username,
                    myVuwCredentials.SecurePassword,
                    apiCredentials.SecurePassword);

                Logging.Log(Logging.LogLevel.DEBUG, $"{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(GetCredentials)}", "Successfully got credentials from the credential store.");

                return creds;
            }
            catch(Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(GetCredentials)}", "Failed to get credentials from the credential store.", ex);

                return null;
            }
        }

        /**
         * <summary>Set credentials used by AutoMarkCheck. Existing credentials will be overwritten.</summary>
         * <seealso cref="MarkCredentials"/>
         * <param name="credentials">Credentials to save in the Windows credential store.</param>
         */
        public static void SetCredentials(MarkCredentials credentials)
        {
            try
            {
                //Create and save MyVictoria credential object
                new Credential
                {
                    Target = VuwCredentialStoreTarget,
                    PersistanceType = PersistanceType.LocalComputer,
                    Username = credentials.Username,
                    SecurePassword = credentials.Password,
                    Type = CredentialType.Generic,
                    Description = "MyVictoria user credentials for AutoMarkCheck bot."
                }.Save();

                //Create and save API credential object
                new Credential
                {
                    Target = ApiCredentialStoreTarget,
                    PersistanceType = PersistanceType.LocalComputer,
                    Username = "API",
                    SecurePassword = credentials.ApiKey,
                    Type = CredentialType.Generic,
                    Description = "API key for AutoMarkCheck bot."
                }.Save();

                Logging.Log(Logging.LogLevel.INFO, $"{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(SetCredentials)}", "New credentials saved to the credential store.");
            }
            catch(Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(SetCredentials)}", "Failed to save new credentials to the credential store.", ex);
            }
        }

        /**
         * <summary>Removes AutoMarkCheck credentials from the windows store.</summary>
         */
        public static void DeleteCredentials()
        {
            try
            {
                Credential myVuwCredentials = new Credential { Target = VuwCredentialStoreTarget };
                Credential apiCredentials = new Credential { Target = ApiCredentialStoreTarget };
                bool deleted = myVuwCredentials.Delete() && apiCredentials.Delete();

                if(deleted)
                    Logging.Log(Logging.LogLevel.WARNING, $"{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(DeleteCredentials)}", "One of the credentials may have not been deleted.");
                else
                    Logging.Log(Logging.LogLevel.INFO, $"{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(DeleteCredentials)}", "Credentials successfully delete from the credential store.");
            }
            catch(Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, $"{nameof(Helpers)}.{nameof(CredentialManager)}.{nameof(DeleteCredentials)}", "Failed to delete credentials from the credential store.", ex);
            }
        }
    }
}
