﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClient
{
    //Client to test and develop new StudentRecords grade source with federated login
    class Program
    {
        static void Main(string[] args)
        {
            var creds = AutoMarkCheck.Helpers.CredentialManager.GetCredentials();
            var source = new AutoMarkCheck.Grades.StudentRecordGradeSource(creds);
            Console.ReadLine();
        }
    }
}
