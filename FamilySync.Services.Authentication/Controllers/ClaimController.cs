using FamilySync.Core.Authentication.Identity;
using FamilySync.Core.Authentication.Models;
using FamilySync.Models.Authentication.DTOs;
using FamilySync.Services.Authentication.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FamilySync.Services.Authentication.Controllers;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
public class ClaimController : ControllerBase
{
    readonly IClaimService _service;

    public ClaimController(IClaimService service)
    {
        _service = service;
    }

    [Authorize("famsync:admin")]
    [HttpGet("User/{id}")]
    [ProducesResponseType(200, Type = typeof(List<ClaimAccess>))]
    public async Task<List<ClaimAccess>> Get(Guid id)
    {
        return await _service.Get(id);
    }
    
    [HttpGet]
    [ProducesResponseType(200, Type = typeof(List<ClaimDefinition>))]
    public Task<List<ClaimDefinition>> GetAll()
    {
        return _service.GetAll();
    }
    
    [Authorize("famsync:admin")]
    [HttpPut("User/{id}/claims")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> Upsert([FromRoute] Guid id, [BindRequired][FromBody] ClaimDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await _service.Upsert(id, dto);

        return CreatedAtAction(nameof(GetByType), new { id, type = dto.Claim },dto);
    }

    [Authorize("famsync:admin")]
    [HttpGet("{id}/claims/{type}")]
    [ProducesResponseType(200, Type = typeof(ClaimDTO))]
    [ProducesResponseType(404)]
    public async Task<ActionResult<ClaimDTO>> GetByType([FromRoute] Guid id, [FromRoute] string type)
    {
        var claim = await _service.GetByType(id, type);
        return Ok(claim);
    }
}