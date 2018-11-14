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
            await MyVUWAgent.GetGrades(GetCredentials());
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            
        }
    }
}
