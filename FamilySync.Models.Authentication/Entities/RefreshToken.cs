namespace FamilySync.Models.Authentication.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string Token { get; set; }
}