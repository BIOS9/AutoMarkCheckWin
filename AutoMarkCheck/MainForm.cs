using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoMarkCheck
{
    public partial class MainForm : Form
    {
        private const string BASE_URL = "https://my.vuw.ac.nz";
        private const string LOGIN_PAGE_PATH = "/cp/home/displaylogin";
        private const string LOGIN_POST_PATH = "/cp/home/login";
        private const string LOGIN_UUID_PATTERN = "(?:document.cplogin.uuid.value=\")([\\da-zA-Z-]+)(?:\";)";

        public MainForm()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await MyVUWAgent.GetGrades(null);
        }
    }
}
