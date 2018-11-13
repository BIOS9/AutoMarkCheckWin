using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace AutoMarkCheck
{
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
            public string Username;
            public SecureString Password = new SecureString();
            public SecureString BotToken = new SecureString();

            #region Constructors

            public MarkCredentials() { }

            public MarkCredentials(string username, string password, string botToken)
            {
                Username = username;
                password.ToList().ForEach(x => Password.AppendChar(x));
                botToken.ToList().ForEach(x => BotToken.AppendChar(x));
            }

            public MarkCredentials(string username, SecureString password, SecureString botToken)
            {
                Username = username;
                Password = password;
                BotToken = botToken;
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
            var myVuwCredentials = new Credential {
                Target = VUW_CREDENTIAL_STORE_TARGET,
                PersistanceType = PersistanceType.LocalComputer,
                Username = credentials.Username,
                SecurePassword = credentials.Password,
                Type = CredentialType.Generic,
                Description = "MyVictoria user credentials for AutoMarkCheck bot."
            }.Save();

            //Create and save Discord bot credential object
            var discordCredentials = new Credential {
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
            var myVuwCredentials = new Credential { Target = VUW_CREDENTIAL_STORE_TARGET };
            var discordCredentials = new Credential { Target = DISCORD_CREDENTIAL_STORE_TARGET };
            return myVuwCredentials.Delete() && discordCredentials.Delete();
        }
    }
}
