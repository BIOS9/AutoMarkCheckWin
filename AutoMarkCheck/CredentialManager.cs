using CredentialManagement;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Windows.Forms;

namespace AutoMarkCheck
{
    /**
     * <summary>Manages credentials used by the AutoMarkCheck bot.</summary>
     */
    public class CredentialManager
    {
#if DEBUG
        private const string VUW_CREDENTIAL_STORE_TARGET = "AutoMarkCheckVUWDebug";
        private const string DISCORD_CREDENTIAL_STORE_TARGET = "AutoMarkCheckDiscordDebug";
#else
        private const string VUW_CREDENTIAL_STORE_TARGET = "AutoMarkCheckVUW";
        private const string DISCORD_CREDENTIAL_STORE_TARGET = "AutoMarkCheckDiscord";
#endif
        /**
         * <summary>Credential class to store credentials for MyVUW and the Discord bot</summary>
         * <seealso cref="CredentialManager"/>
         */
        public class MarkCredentials
        {
            public readonly static Encoding CredentialEncoding = Encoding.ASCII;
            public string Username;
            public SecureString Password = new SecureString();
            public SecureString BotToken = new SecureString();
            public int EscapedPasswordSize;
            public int EscapedBotTokenSize;

            #region Constructors

            public MarkCredentials() { }

            public MarkCredentials(string username, string password, string botToken)
            {
                Username = username;
                password.ToList().ForEach(x => Password.AppendChar(x));
                botToken.ToList().ForEach(x => BotToken.AppendChar(x));

                //Calculate the size in bytes of the escaped password and token so they can be used later for an HTTP request without persisting in memory as a string
                string escapedPassword = Uri.EscapeDataString(password);
                EscapedPasswordSize = CredentialEncoding.GetByteCount(escapedPassword);

                string escapedToken = Uri.EscapeDataString(botToken);
                EscapedBotTokenSize = CredentialEncoding.GetByteCount(escapedToken);
            }

            public MarkCredentials(string username, SecureString password, SecureString botToken)
            {
                Username = username;
                Password = password;
                BotToken = botToken;

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

                //Calculate token length
                IntPtr tokenPtr = Marshal.SecureStringToBSTR(BotToken); //Convert SecureString token to BSTR and get the pointer
                try
                {
                    byte b = 1;
                    int i = 0;

                    while (true) //Loop over characters in the BSTR
                    {
                        b = Marshal.ReadByte(passwordPtr, i);
                        if (b == 0) break; //If terminator character '\0' is hit exit loop

                        string escapedChar = Uri.EscapeDataString(((char)b).ToString()); //Must be a string because the escaped character can be more than 1 character long eg %7
                        EscapedBotTokenSize += CredentialEncoding.GetByteCount(escapedChar); //Add the length to the credential length

                        i = i + 2;  // BSTR is unicode and occupies 2 bytes
                    }
                }
                finally
                {
                    Marshal.ZeroFreeBSTR(tokenPtr); //Securely clear password BSTR from memory
                }
            }

            #endregion
        }

        /**
         * <summary>Returns all credentials that are used by AutoMarkCheck. Returns MyVUW account and Discord bot token.</summary>
         * <seealso cref="MarkCredentials"/>
         */
        public static MarkCredentials GetCredentials()
        {
            try
            {
                //Get credentials from Windows credential store
                var myVuwCredentials = new Credential { Target = VUW_CREDENTIAL_STORE_TARGET };
                var discordCredentials = new Credential { Target = DISCORD_CREDENTIAL_STORE_TARGET };

                if (!myVuwCredentials.Load() || !discordCredentials.Load())
                {
                    Logging.Log(Logging.LogLevel.WARNING, "CredentialManager.GetCredentials", "There are no credentials saved for AutoMarkCheck.");
                    return null; //If loading fails
                }

                //Load credentials into MarkCredentials object
                var creds = new MarkCredentials(
                    myVuwCredentials.Username,
                    myVuwCredentials.SecurePassword,
                    discordCredentials.SecurePassword);

                Logging.Log(Logging.LogLevel.DEBUG, "CredentialManager.GetCredentials", "Successfully got credentials from the credential store.");

                return creds;
            }
            catch(Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, "CredentialManager.GetCredentials", "Failed to get credentials from the credential store.", ex);

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
                var myVuwCredentials = new Credential
                {
                    Target = VUW_CREDENTIAL_STORE_TARGET,
                    PersistanceType = PersistanceType.LocalComputer,
                    Username = credentials.Username,
                    SecurePassword = credentials.Password,
                    Type = CredentialType.Generic,
                    Description = "MyVictoria user credentials for AutoMarkCheck bot."
                }.Save();

                //Create and save Discord bot credential object
                var discordCredentials = new Credential
                {
                    Target = DISCORD_CREDENTIAL_STORE_TARGET,
                    PersistanceType = PersistanceType.LocalComputer,
                    SecurePassword = credentials.BotToken,
                    Type = CredentialType.Generic,
                    Description = "Discord token for AutoMarkCheck bot."
                }.Save();

                Logging.Log(Logging.LogLevel.INFO, "CredentialManager.SetCredentials", "New credentials saved to the credential store.");
            }
            catch(Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, "CredentialManager.SetCredentials", "Failed to save new credentials to the credential store.", ex);
            }
        }

        /**
         * <summary>Removes AutoMarkCheck credentials from the windows store.</summary>
         */
        public static void DeleteCredentials()
        {
            try
            {
                Credential myVuwCredentials = new Credential { Target = VUW_CREDENTIAL_STORE_TARGET };
                Credential discordCredentials = new Credential { Target = DISCORD_CREDENTIAL_STORE_TARGET };
                bool deleted = myVuwCredentials.Delete() && discordCredentials.Delete();

                if(deleted)
                    Logging.Log(Logging.LogLevel.WARNING, "CredentialManager.DeleteCredentials", "One of the credentials may have not been deleted.");
                else
                    Logging.Log(Logging.LogLevel.INFO, "CredentialManager.DeleteCredentials", "Credentials successfully delete from the credential store.");
            }
            catch(Exception ex)
            {
                Logging.Log(Logging.LogLevel.ERROR, "CredentialManager.DeleteCredentials", "Failed to delete credentials from the credential store.");
            }
        }
    }
}
