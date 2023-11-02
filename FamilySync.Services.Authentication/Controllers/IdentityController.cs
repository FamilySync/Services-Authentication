using FamilySync.Models.Authentication.DTOs;
using FamilySync.Models.Authentication.Entities;
using FamilySync.Services.Authentication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FamilySync.Services.Authentication.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class IdentityController : ControllerBase
{
    readonly IUserIdentityService _service;

    public IdentityController(IUserIdentityService service)
    {
        _service = service;
    }
    
    
    [HttpPost("Login")]
    [ProducesResponseType(200, Type = typeof(AuthTokenDTO))]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<AuthTokenDTO>> Login ([BindRequired][FromBody] UserIdentityCredentialsDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var token = await _service.Login(dto);

        Response.Cookies.Append("familysync.refresh", token.RefreshToken, new CookieOptions
        {
            SameSite = SameSiteMode.None,
            Secure = true,
            HttpOnly = false,
            Expires = token.RefreshTokenExpiryDate
        });
        
        return Ok(new AuthTokenDTO { Token = token.AccessToken });
    }
    
    [HttpPost("Logout")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> Logout()
    {
        await _service.Logout(Request, Response);

        return NoContent();
    }
    
    [Authorize("famsync:admin")]
    [HttpPost("Token/Refresh")]
    [ProducesResponseType(200, Type = typeof(AuthTokenDTO))]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<AuthTokenDTO>> Refresh()
    {
        var token = await _service.Refresh(Request);

        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            return Unauthorized();
        }

        Response.Cookies.Append("familysync.refresh", token.RefreshToken, new CookieOptions
        {
            SameSite = SameSiteMode.None,
            Secure = true,
            HttpOnly = false,
            Expires = token.RefreshTokenExpiryDate
        });
        
        return Ok(new AuthTokenDTO { Token = token.AccessToken });
    }
    
    [Authorize("famsync:admin")]
    [HttpGet("Secret")]
    [ProducesResponseType(200)]
    [ProducesResponseType(204)]
    public async Task<ActionResult<string>> GetAuthenticationString()
    {
        var result = await _service.GetAuthenticationString();
        return string.IsNullOrEmpty(result) ? NoContent() : Ok(await _service.GetAuthenticationString());
    }
    
    [HttpPost("Identity")]
    [ProducesResponseType(201, Type = typeof(CreateUserIdentityDTO))]
    [ProducesResponseType(400)]
    public async Task<ActionResult<CreateUserIdentityDTO>> Create([FromBody] CreateUserIdentityDTO dto)
    {
        if(!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var identity = await _service.Create(dto);

        return CreatedAtAction(nameof(GetById), new { id = identity.Id }, identity);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(200, Type = typeof(GetUserIdentityDTO))]
    [ProducesResponseType(404)]
    public async Task<ActionResult<GetUserIdentityDTO>> GetById([FromRoute] Guid id)
    {
        return Ok(await _service.Get(id));
    }

}