using System.Text.Json.Serialization;

namespace FamilySync.Models.Authentication.DTOs;

public class AuthenticationResponseDTO
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryDate { get; set; }
}