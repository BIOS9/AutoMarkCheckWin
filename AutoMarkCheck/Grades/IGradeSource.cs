﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AutoMarkCheck.Helpers.CredentialManager;

namespace AutoMarkCheck.Grades
{
    /**
     * <summary>Interface for grade sources. Adds standardization to grade sources through enforced methods.</summary>
     */
    interface IGradeSource
    {
        Task<List<CourseInfo>> GetGrades();
        void SetCredentials(MarkCredentials credentials);
    }
}
