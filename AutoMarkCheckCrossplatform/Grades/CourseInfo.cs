namespace AutoMarkCheck.Grades
{
    /**
    * <summary>Contains information about a single VUW course. Includes CRN, Subject, Course, Course Title and Grade.</summary>
    */
    public class CourseInfo
    {
        public string CRN;
        public string Subject;
        public string Course;
        public string CourseTitle;
        public string Grade;

        public override string ToString()
        {
            string grade = string.IsNullOrWhiteSpace(Grade) ? "Empty" : Grade;
            return $"{Subject}{Course} {grade}";
        }
    }
}
