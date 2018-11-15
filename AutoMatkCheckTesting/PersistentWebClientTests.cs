using AutoMarkCheck.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading;

namespace AutoMatkCheckTesting
{
    [TestClass]
    public class PersistentWebClientTests
    {
        [TestMethod]
        public void GetRequest()
        {
            PersistentWebClient client = new PersistentWebClient();
            string data = client.Get("https://httpbin.org/get").Result;
            Assert.IsTrue(data.Contains("headers"), "Get request failed to return valid data.");
        }

        [TestMethod]
        public void PostRequest()
        {
            PersistentWebClient client = new PersistentWebClient();
            string data = client.Post("https://httpbin.org/post", new Dictionary<string, string> {
                { "testKey", "testValue" }
            }).Result;
            JObject joResponse = JObject.Parse(data);
            JValue jObject = (JValue)joResponse["form"]["testKey"];
            string val = jObject.Value.ToString();
            Assert.AreEqual("testValue", val, "Post request did not contain post data.");
        }

        [TestMethod]
        public void GetRequestCookies()
        {
            PersistentWebClient client = new PersistentWebClient();
            client.Get("https://www.cloudflare.com/").Wait();
            string cookie1 = client.Cookies["__cfduid"].Value;
            client.Get("https://www.cloudflare.com/").Wait();
            string cookie2 = client.Cookies["__cfduid"].Value;
            Assert.AreEqual(cookie1, cookie2, "Failed to persist cookies.");
        }

        [TestMethod]
        public void GetRequestClearCookies()
        {
            PersistentWebClient client = new PersistentWebClient();
            client.Get("https://www.cloudflare.com/").Wait();
            string cookie1 = client.Cookies["__cfduid"].Value;
            client.ClearCookies();
            Thread.Sleep(1000);
            client.Get("https://www.cloudflare.com/").Wait();
            string cookie2 = client.Cookies["__cfduid"].Value;
            Assert.AreNotEqual(cookie1, cookie2, "Cookies persisted after clear.");
        }

        [TestMethod]
        public void PostRequestCookies()
        {
            PersistentWebClient client = new PersistentWebClient();
            client.Post("https://jsonplaceholder.typicode.com/posts", new Dictionary<string, string>()).Wait();
            string cookie1 = client.Cookies["__cfduid"].Value;
            client.Post("https://jsonplaceholder.typicode.com/posts", new Dictionary<string, string>()).Wait();
            string cookie2 = client.Cookies["__cfduid"].Value;
            Assert.AreEqual(cookie1, cookie2, "Failed to persist cookies.");
        }

        [TestMethod]
        public void PostRequestClearCookies()
        {
            PersistentWebClient client = new PersistentWebClient();
            client.Post("https://jsonplaceholder.typicode.com/posts", new Dictionary<string, string>()).Wait();
            string cookie1 = client.Cookies["__cfduid"].Value;
            client.ClearCookies();
            Thread.Sleep(1000);
            client.Post("https://jsonplaceholder.typicode.com/posts", new Dictionary<string, string>()).Wait();
            string cookie2 = client.Cookies["__cfduid"].Value;
            Assert.AreNotEqual(cookie1, cookie2, "Cookies persisted after clear.");
        }
    }
}
