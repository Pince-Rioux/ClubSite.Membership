using VereinMember.Data;
using VereinMember.Extensions;
using VereinMember.Models;
using VereinMember.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace VereinMember.Areas.Membership.Controllers;

[ApiController]
[Authorize]
[Area("Membership")]
[Route("membership/api/family-groups")]
public class FamilyGroupsController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly MembershipDbContext _context;

    public FamilyGroupsController(IMemberService memberService, MembershipDbContext context)
    {
        _memberService = memberService;
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetAll()
    {
        var groups = await _memberService.GetAllFamilyGroupsAsync();
        return Ok(groups);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetById(int id)
    {
        var group = await _memberService.GetFamilyGroupByIdAsync(id);
        if (group == null)
            return NotFound();

        return Ok(group);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create([FromBody] FamilyGroup group)
    {
        var created = await _memberService.CreateFamilyGroupAsync(
            group.Name, User.Identity?.Name ?? "system");

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{familyGroupId}/members/{memberId}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> AddMember(int familyGroupId, int memberId)
    {
        try
        {
            var group = await _memberService.AddMemberToFamilyGroupAsync(memberId, familyGroupId);
            return Ok(group);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not found",
                Status = 404,
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Validation error",
                Status = 400,
                Detail = ex.Message
            });
        }
    }

    [HttpDelete("{familyGroupId}/members/{memberId}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> RemoveMember(int familyGroupId, int memberId)
    {
        try
        {
            var group = await _memberService.RemoveMemberFromFamilyGroupAsync(memberId);
            return Ok(group);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not found",
                Status = 404,
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Remove blocked",
                Status = 409,
                Detail = ex.Message
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _memberService.DeleteFamilyGroupAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Delete blocked",
                Status = 409,
                Detail = ex.Message
            });
        }
    }
}