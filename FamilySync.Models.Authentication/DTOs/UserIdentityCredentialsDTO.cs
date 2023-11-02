using System.ComponentModel.DataAnnotations;

namespace FamilySync.Models.Authentication.DTOs;

public class UserIdentityCredentialsDTO
{
    [Required(ErrorMessage = "The Email field is required.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "The Password field is required.")]
    public string Password { get; set; }
}