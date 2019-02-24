using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TestClient
{
    //Client to test and develop new StudentRecords grade source with federated login
    class Program
    {
        static void Main(string[] args)
        {
            var creds = AutoMarkCheck.Helpers.CredentialManager.GetCredentials();
            if (creds == null)
                return;
            var source = new AutoMarkCheck.Grades.StudentRecordGradeSource(creds);
            var courses = source.GetGrades();
            Console.WriteLine(JsonConvert.SerializeObject(courses, Formatting.Indented));
            Console.ReadLine();
        }
    }
}
