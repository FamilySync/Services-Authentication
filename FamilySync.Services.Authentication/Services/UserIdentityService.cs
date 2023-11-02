using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FamilySync.Core.Authentication.Enums;
using FamilySync.Core.Helpers.Exceptions;
using FamilySync.Core.Helpers.Settings;
using FamilySync.Models.Authentication.DTOs;
using FamilySync.Models.Authentication.Entities;
using FamilySync.Services.Authentication.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;


namespace FamilySync.Services.Authentication.Services;

public interface IUserIdentityService
{
    public Task<AuthenticationResponseDTO> Login(UserIdentityCredentialsDTO dto);
    public Task Logout(HttpRequest request, HttpResponse response);
    Task<AuthenticationResponseDTO> Refresh(HttpRequest request);
    public Task<GetUserIdentityDTO> Create(CreateUserIdentityDTO dto);
    public Task<GetUserIdentityDTO> Get(Guid id);
    public Task<string> GetAuthenticationString();
}

public class UserIdentityService : IUserIdentityService
{
    readonly AuthContext _context;
    readonly AuthenticationSettings _authenticationSettings;
    readonly UserManager<UserIdentity> _userIdentityManager;
    readonly SignInManager<UserIdentity> _userSignInManager;
    JwtSecurityTokenHandler? _handler;

    public UserIdentityService(AuthContext context, IOptions<AuthenticationSettings> authenticationSettings,
        UserManager<UserIdentity> userIdentityManager, SignInManager<UserIdentity> userSignInManager)
    {
        _context = context;
        _userIdentityManager = userIdentityManager;
        _userSignInManager = userSignInManager;
        _authenticationSettings = authenticationSettings.Value;
    }

    JwtSecurityTokenHandler JwtTokenHandler
    {
        get { return _handler ??= new(); }
    }

    #region Interface Methods

    public async Task<AuthenticationResponseDTO> Login(UserIdentityCredentialsDTO dto)
    {
        var identity = await _userIdentityManager.FindByEmailAsync(dto.Email);

        if (identity == default)
        {
            throw new NotFoundException(typeof(UserIdentityCredentialsDTO), dto.Email);
        }

        var result = await _userSignInManager.CheckPasswordSignInAsync(identity, dto.Password, false);

        if (!result.Succeeded)
        {
            throw new ForbiddenException(typeof(UserIdentityCredentialsDTO),
                $"{dto.Email} Authentication failed : {result}");
        }

        return await GenerateAuthenticationResponse(identity);
    }

    public async Task Logout(HttpRequest request, HttpResponse response)
    {
        if (request.Cookies.TryGetValue("familysync.refresh", out string refreshToken))
        {
            var token = await ConsumeRefreshToken(refreshToken);
        }
        
        response.Cookies.Delete("familysync.refresh");
    }

    public async Task<AuthenticationResponseDTO> Refresh(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue("familysync.refresh", out string refreshToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new NotFoundException("Couldn't find refresh token!");
        }

        var token = await ConsumeRefreshToken(refreshToken);
        
        if(token is null)
        {
            throw new NotFoundException("Couldn't Consume refresh token!");
        }

        var identity = await _userIdentityManager.FindByIdAsync(token.UserId.ToString());

        if(identity is null)
        {
            throw new NotFoundException("Couldn't find Token UserId!");
        }
        
        return await GenerateAuthenticationResponse(identity);
    }

    public async Task<GetUserIdentityDTO> Create(CreateUserIdentityDTO dto)
    {
        var identity = new UserIdentity
        {
            UserName = dto.Username,
            Email = dto.Email,
        };

        IdentityResult result;

        if (string.IsNullOrEmpty(dto.Password))
        {
            result = await _userIdentityManager.CreateAsync(identity);
        }
        else
        {
            result = await _userIdentityManager.CreateAsync(identity, dto.Password);
        }

        if (!result.Succeeded)
        {
            return default;
        }

        result = await AddDefaultClaims(identity);

        if (!result.Succeeded)
        {
            await _userIdentityManager.DeleteAsync(identity);
        }

        return new GetUserIdentityDTO
        {
            Id = identity.Id,
            Username = identity.UserName,
            Email = identity.Email
        };
    }

    public async Task<GetUserIdentityDTO> Get(Guid id)
    {
        var identity = await _userIdentityManager.FindByIdAsync(id.ToString());

        if (identity == default)
        {
            throw new NotFoundException(typeof(UserIdentity), id.ToString());
        }

        return new GetUserIdentityDTO
        {
            Id = identity.Id,
            Username = identity.UserName!,
            Email = identity.Email!,
        };
    }

    public async Task<string> GetAuthenticationString()
    {
        return await Task.FromResult(_authenticationSettings.Secret);
    }

    #endregion

    #region Class Methods

    #region Authentication Methods

    public async Task<AuthenticationResponseDTO> GenerateAuthenticationResponse(UserIdentity identity)
    {
        var claims = await _userIdentityManager.GetClaimsAsync(identity);

        JwtSecurityToken accessToken = CreateToken(claims);
        RefreshToken refreshToken = await CreateRefreshToken(identity.Id);
        var expirationTime = (int)(accessToken.ValidTo - DateTime.UtcNow).TotalSeconds;

        return new AuthenticationResponseDTO
        {
            AccessToken = JwtTokenHandler.WriteToken(accessToken),
            RefreshToken = refreshToken.Token,
            ExpiresIn = expirationTime,
            TokenType = "Bearer",
            RefreshTokenExpiryDate = refreshToken.ExpirationDate,
        };
    }

    JwtSecurityToken CreateToken(IList<Claim> claims)
    {
        DateTime expiresAt = DateTime.UtcNow.Add(TimeSpan.FromMinutes(120));

        return new(
            issuer: "familysync.auth",
            audience: "api://familysync",
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authenticationSettings.Secret)), "HS256")
        );
    }

    async Task<RefreshToken> CreateRefreshToken(Guid userId)
    {
        float lifeTimeDays = 3;
        TimeSpan refreshLifeTime = TimeSpan.FromDays(lifeTimeDays);
        DateTime expirationDate = DateTime.UtcNow.Add(refreshLifeTime);

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            ExpirationDate = expirationDate,
        };

        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        return token;
    }
    
    async Task<RefreshToken?> ConsumeRefreshToken(string refreshToken)
    {
        var token = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
        
        if(token is null)
        {
            return null;
        }

        _context.RefreshTokens.Remove(token);
        await _context.SaveChangesAsync();

        return token;
    }

    #endregion

    async Task<IdentityResult> AddDefaultClaims(UserIdentity user)
    {
        var claims = new List<Claim>
        {
            new("iid", user.Id.ToString()),
            new("uid", user.UserName),
            new("email", user.Email),
            new("fam", Enum.GetName(AccessLevel.USER_BASIC))
        };

        return await _userIdentityManager.AddClaimsAsync(user, claims);
    }

    #endregion
}