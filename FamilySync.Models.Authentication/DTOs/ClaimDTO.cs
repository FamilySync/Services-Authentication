using System.ComponentModel.DataAnnotations;

namespace FamilySync.Models.Authentication.DTOs;

public class ClaimDTO
{
    [Required]
    public string AccessLevel { get; set; }

    [Required]
    public string Claim { get; set; }
}