using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AutoMarkCheck.Helpers.CredentialManager;

namespace AutoMarkCheck.Grades
{
    interface IGradeSource
    {
        Task<List<CourseInfo>> GetGrades();
        void SetCredentials(MarkCredentials credentials);
    }
}
