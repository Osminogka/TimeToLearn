using System.ComponentModel.DataAnnotations;

namespace Users.DAL.SideModels;

public class EntryRequestModel
{
    [Required]
    public string University { get; set; } = string.Empty;
    
    [Required]
    public string Username { get; set; } = string.Empty;
}