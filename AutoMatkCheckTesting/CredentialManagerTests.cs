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
