using System.ComponentModel.DataAnnotations;

namespace FamilySync.Models.Authentication.DTOs;

public class GetUserIdentityDTO
{
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public string Username { get; set; }

    [Required]
    public string Email { get; set; }
}