using CredentialManagement;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace AutoMarkCheck
{
    /**
     * <summary>Manages credentials used by the AutoMarkCheck bot.</summary>
     */
    class CredentialManager
    {
        private const string VUW_CREDENTIAL_STORE_TARGET = "AutoMarkCheckVUW";
        private const string DISCORD_CREDENTIAL_STORE_TARGET = "AutoMarkCheckDiscord";

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

                IntPtr passwordPtr = Marshal.SecureStringToBSTR(password); //Convert SecureString password to BSTR and get the pointer


                foreach(char c in password)
                {

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
            //Get credentials from Windows credential store
            var myVuwCredentials = new Credential { Target = VUW_CREDENTIAL_STORE_TARGET };
            var discordCredentials = new Credential { Target = DISCORD_CREDENTIAL_STORE_TARGET };

            if (!myVuwCredentials.Load() || !discordCredentials.Load()) return null; //If loading fails

            //Load credentials into MarkCredentials object
            var creds = new MarkCredentials(
                myVuwCredentials.Username,
                myVuwCredentials.SecurePassword,
                discordCredentials.SecurePassword);

            return creds;
        }

        /**
         * <summary>Set credentials used by AutoMarkCheck. Existing credentials will be overwritten.</summary>
         * <seealso cref="MarkCredentials"/>
         * <param name="credentials">Credentials to save in the Windows credential store.</param>
         */
        public static void SetCredentials(MarkCredentials credentials)
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
        }

        /**
         * <summary>Removes AutoMarkCheck credentials from the windows store.</summary>
         */
        public static bool DeleteCredentials()
        {
            Credential myVuwCredentials = new Credential { Target = VUW_CREDENTIAL_STORE_TARGET };
            Credential discordCredentials = new Credential { Target = DISCORD_CREDENTIAL_STORE_TARGET };
            return myVuwCredentials.Delete() && discordCredentials.Delete();
        }
    }
}
