using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilySync.Models.Authentication.DTOs;

public class CreateUserIdentityDTO
{
    [Required]
    public string Username { get; set; }

    [Required]
    public string Email { get; set; }
    
    [DefaultValue("")]
    public string? Password { get; set; }
}