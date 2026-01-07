using System;
using System.Collections.Generic;

namespace TestAPI.Models;

public partial class Student
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Group { get; set; } = null!;

    public int Course { get; set; }

    public string StudyForm { get; set; } = null!;

    public DateTime AdmissionDate { get; set; }

    public bool IsActive { get; set; }
}
