using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AutoMarkCheck.CredentialManager;

namespace AutoMarkCheck
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var creds = GetCredentials();
            if (creds == null)
            {
                MessageBox.Show("No creds");
                return;
            }
            try
            {
                var grades = await MyVUWAgent.GetGrades(creds);
                MessageBox.Show("grade count: " + grades.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            PersistentWebClient client = new PersistentWebClient();
            await client.Get("https://cloudflare.com");
            client.DisplayCookies();
            await client.Get("https://cloudflare.com");
            client.DisplayCookies();
            client.ClearCookies();
            await client.Get("https://cloudflare.com");
            client.DisplayCookies();
            

            //PersistentWebClient client = new PersistentWebClient();
            //string html = await client.Get("https://my.vuw.ac.nz");
            //client.DisplayHTML(html);
        }
    }
}
