using System.ComponentModel.DataAnnotations;

namespace Core.DAL.SideModels;

public class EntryRequestModel
{
    [Required]
    public string University { get; set; } = string.Empty;
    
    [Required]
    public string Username { get; set; } = string.Empty;
}