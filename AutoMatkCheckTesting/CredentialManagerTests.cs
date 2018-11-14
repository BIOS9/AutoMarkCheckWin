using System;
using System.Linq;
using System.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoMarkCheck;
using static AutoMarkCheck.CredentialManager;
using System.Runtime.InteropServices;

namespace AutoMatkCheckTesting
{
    [TestClass]
    public class CredentialManagerTests
    {
        [TestMethod]
        public void CredentialCreation()
        {
            string username = "User1234";
            string str = "AAAAA";
            SecureString secureStr = new SecureString();
            str.ToList().ForEach(x => secureStr.AppendChar(x));

            MarkCredentials cred1 = new MarkCredentials(username, str, str);
            Assert.AreEqual(username, cred1.Username, "Username does not match input for string initialized MarkCredentials.");
            Assert.AreEqual(str, ToStr(cred1.Password), "Password does not match input for string initialized MarkCredentials.");
            Assert.AreEqual(str, ToStr(cred1.BotToken), "Bot token does not match input for string initialized MarkCredentials.");

            MarkCredentials cred2 = new MarkCredentials(username, secureStr, secureStr);
            Assert.AreEqual(username, cred2.Username, "Username does not match input for SecureString initialized MarkCredentials.");
            Assert.AreEqual(str, ToStr(cred1.Password), "Password does not match input for SecureString initialized MarkCredentials.");
            Assert.AreEqual(str, ToStr(cred1.BotToken), "Bot token does not match input for SecureString initialized MarkCredentials.");
        }

        [TestMethod]
        public void EscapedCredentialLength()
        {
            string str = "HelloTest123|\\@@#!@#5";
            SecureString secureStr = new SecureString();
            str.ToList().ForEach(x => secureStr.AppendChar(x));

            int escapedLength = MarkCredentials.CredentialEncoding.GetByteCount(Uri.EscapeDataString(str));

            MarkCredentials cred1 = new MarkCredentials("", str, str);
            Assert.AreEqual(escapedLength, cred1.EscapedPasswordSize, "Escaped password length incorrect for string initialized MarkCredentials.");
            Assert.AreEqual(escapedLength, cred1.EscapedBotTokenSize, "Escaped bot token length incorrect for string initialized MarkCredentials.");

            MarkCredentials cred2 = new MarkCredentials("", secureStr, secureStr);
            Assert.AreEqual(escapedLength, cred2.EscapedPasswordSize, "Escaped password length incorrect for SecureString initialized MarkCredentials.");
            Assert.AreEqual(escapedLength, cred2.EscapedBotTokenSize, "Escaped bot token length incorrect for SecureString initialized MarkCredentials.");
        }

        [TestMethod]
        public void CredentialSavingLoading()
        {
            string username = RandomString(10);
            string password = RandomString(10);
            string token = RandomString(10);
            MarkCredentials cred1 = new MarkCredentials(username, password, token);
            SetCredentials(cred1);
            MarkCredentials cred2 = GetCredentials();
            Assert.AreEqual(cred1.Username, cred2.Username, "Saved and loaded username does not match.");
            Assert.AreEqual(ToStr(cred1.Password), ToStr(cred2.Password), "Saved and loaded password does not match.");
            Assert.AreEqual(ToStr(cred1.BotToken), ToStr(cred2.BotToken), "Saved and loaded bot token does not match.");
        }

        [TestMethod]
        public void CredentialDeleting()
        {
            string username = RandomString(10);
            string password = RandomString(10);
            string token = RandomString(10);
            MarkCredentials cred1 = new MarkCredentials(username, password, token);
            SetCredentials(cred1);
            DeleteCredentials();
            MarkCredentials cred2 = GetCredentials();
            Assert.IsNull(cred2, "Credentials still remain in store after deletion.");
        }

        //Creates random string for testing
        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        //Converts secure string to insecure string for testing
        string ToStr(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}
