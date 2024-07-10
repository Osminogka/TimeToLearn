using System.ComponentModel.DataAnnotations;

namespace Authentication.DAL.Models;

public class TeacherModel
{
    [Required]
    public string UniversityName { get; set; }
    
    [Required]
    public string Degree { get; set; }
}