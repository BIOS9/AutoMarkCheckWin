﻿using Newtonsoft.Json.Linq;
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
            var creds = CredentialManager.GetCredentials();
            if (creds == null)
            {
                MessageBox.Show("No creds");
                return;
            }
            try
            {
                var courses = await MyVUWAgent.GetGrades(creds);
                await ServerAgent.ReportGrades(courses, "coolHost", creds);
                MessageBox.Show("grade count: " + courses.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            
            //List<CourseInfo> courses = new List<CourseInfo>();
            //courses.Add(new CourseInfo { CRN = "123456", Subject = "COMP", Course = "112", CourseTitle = "TItle goes here", Grade = "A+" });
            //courses.Add(new CourseInfo { CRN = "123445", Subject = "CGRA", Course = "151", CourseTitle = "TItle goes here", Grade = "A+" });
            //courses.Add(new CourseInfo { CRN = "123445", Subject = "CYBR", Course = "171", CourseTitle = "TItle goes here", Grade = "" });
            //Clipboard.SetText(ServerAgent.serializeData(courses, "coolHost"));
        }
    }
}
