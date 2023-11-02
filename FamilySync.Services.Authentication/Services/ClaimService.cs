using System.Security.Claims;
using FamilySync.Core.Authentication.Enums;
using FamilySync.Core.Authentication.Identity;
using FamilySync.Core.Authentication.Models;
using FamilySync.Core.Helpers.Exceptions;
using FamilySync.Models.Authentication.DTOs;
using FamilySync.Models.Authentication.Entities;
using Microsoft.AspNetCore.Identity;

namespace FamilySync.Services.Authentication.Services;

public interface IClaimService
{
    public Task<List<ClaimAccess>> Get(Guid id);
    public Task<List<ClaimDefinition>> GetAll();
    public Task<ClaimDTO> GetByType(Guid id, string type);
    public Task<ClaimDTO> Upsert(Guid id, ClaimDTO dto);
}

public class ClaimService : IClaimService
{
    readonly UserManager<UserIdentity> _userIdentityManager;

    public ClaimService(UserManager<UserIdentity> userIdentityManager)
    {
        _userIdentityManager = userIdentityManager;
    }


    #region Interface Methods

    public async Task<List<ClaimAccess>> Get(Guid id)
    {
        return await GetUserClaims(id);
    }

    public Task<List<ClaimDefinition>> GetAll()
    {
        return Task.FromResult(GetAllAvailableClaims());
    }

    public async Task<ClaimDTO> GetByType(Guid id, string type)
    {
        var identity = await _userIdentityManager.FindByIdAsync(id.ToString());
        
        if(identity is null)
        {
            throw new NotFoundException(typeof(UserIdentity), id.ToString());
        }

        var claim = await _userIdentityManager.GetClaimsAsync(identity)
            .ContinueWith(x => x.Result.FirstOrDefault(y => y.Type == type));

        if(claim is null)
        {
            throw new NotFoundException(typeof(ClaimDTO), $"{type}");
        }
        
        return new ClaimDTO
        {
            Claim = claim.Value,
            AccessLevel = claim.Value,
        };
    }

    public async Task<ClaimDTO> Upsert(Guid id, ClaimDTO dto)
    {
        var identity = await _userIdentityManager.FindByIdAsync(id.ToString());
        
        if(identity is null)
        {
            throw new NotFoundException(typeof(UserIdentity), id.ToString());
        }

        var claims = await _userIdentityManager.GetClaimsAsync(identity);

        var existingClaim = claims.FirstOrDefault(x => x.Type == dto.Claim);
        if (existingClaim is not null)
        {
            await _userIdentityManager.RemoveClaimAsync(identity, existingClaim);
        }

        var result = await _userIdentityManager.AddClaimAsync(identity, new(dto.Claim, dto.AccessLevel));
        
        if(!result.Succeeded)
        {
            throw new BadRequestException($"Error occured when adding claim: {result.Errors}");
        }

        return dto;
    }

    #endregion

    #region Class Methods

    async Task<List<ClaimAccess>> GetUserClaims(Guid id)
    {
        var identity = await _userIdentityManager.FindByIdAsync(id.ToString());
        
        if(identity == default)
        {
            throw new NotFoundException(typeof(UserIdentity), id.ToString());
        }
        
        var allClaims = ClaimDefinition.Definitions;
        var userClaims = await _userIdentityManager.GetClaimsAsync(identity);

        return allClaims
            .Select(x => new ClaimAccess
            {
                Name = x.Name,
                Claim = x.Claim,
                Description = x.Description,
                Policy = x.Policy,
                AccessLevel = GetAccessLevel(userClaims, x.Claim)
            })
            .Where(x => x.AccessLevel != null)
            .ToList();
    }
    List<ClaimDefinition> GetAllAvailableClaims()
    {
        return ClaimDefinition.Definitions
            .Where(x => x.Claim != "fam")
            .ToList();
    }
    static AccessLevel? GetAccessLevel(IEnumerable<Claim> claims, string claimType)
    {
        var claim = claims.FirstOrDefault(o => o.Type == claimType);

        if (claim is not null && Enum.TryParse(claim.Value, out AccessLevel accessLevel))
        {
            return accessLevel;
        }

        return null;
    }

    #endregion
}