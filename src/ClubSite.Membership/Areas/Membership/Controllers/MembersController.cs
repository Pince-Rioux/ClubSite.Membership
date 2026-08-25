using ClubSite.Membership.Data;
using ClubSite.Membership.Extensions;
using ClubSite.Membership.Models;
using ClubSite.Membership.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClubSite.Membership.Areas.Membership.Controllers;

[ApiController]
[Authorize]
[Area("Membership")]
[Route("membership/api/members")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly MembershipDbContext _context;

    public MembersController(IMemberService memberService, MembershipDbContext context)
    {
        _memberService = memberService;
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.MemberView)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchTerm,
        [FromQuery] MemberStatus? status,
        [FromQuery] int? departmentId,
        [FromQuery] FeeCategory? feeCategory,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount) = await _memberService.SearchAsync(
            searchTerm, status, departmentId, feeCategory, page, pageSize);

        return Ok(new { items, totalCount, page, pageSize });
    }

    [HttpGet("{id}")]
    [Authorize(Policy = AuthorizationPolicies.MemberView)]
    public async Task<IActionResult> GetById(int id)
    {
        var member = await _memberService.GetByIdAsync(id);
        if (member == null)
            return NotFound();

        return Ok(member);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MemberWrite)]
    public async Task<IActionResult> Create([FromBody] Member member)
    {
        try
        {
            var created = await _memberService.CreateAsync(member, User.Identity?.Name ?? "system");
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
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

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.MemberWrite)]
    public async Task<IActionResult> Update(int id, [FromBody] Member member)
    {
        member.Id = id;

        try
        {
            var updated = await _memberService.UpdateAsync(member);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
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

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.MemberDelete)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _memberService.SoftDeleteAsync(id, User.Identity?.Name ?? "system");
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

    [HttpGet("{id}/export")]
    [Authorize(Policy = AuthorizationPolicies.MemberView)]
    public async Task<IActionResult> Export(int id)
    {
        var member = await _memberService.GetByIdAsync(id);
        if (member == null)
            return NotFound();

        var export = new
        {
            exportDate = DateTime.UtcNow,
            requestedById = id,
            personalData = new
            {
                name = $"{member.FirstName} {member.LastName}",
                email = member.Email,
                phone = member.Phone,
                birthDate = member.BirthDate,
                address = new
                {
                    street = member.Street,
                    city = member.City,
                    zipCode = member.ZipCode,
                    country = member.Country
                },
                bankInfo = member.BankIban != null
                    ? $"DE••••••••••••{member.BankIban[^4..]}"
                    : null,
                membership = new
                {
                    status = member.Status.ToString(),
                    joinedAt = member.JoinedAt,
                    department = member.Department?.Name,
                    fee = member.MembershipFee != null
                        ? new
                        {
                            name = member.MembershipFee.Name,
                            amount = member.MembershipFee.Amount,
                            interval = member.MembershipFee.Interval.ToString()
                        }
                        : null,
                    guardian = member.GuardianMember != null
                        ? $"{member.GuardianMember.FirstName} {member.GuardianMember.LastName}"
                        : null,
                    familyGroup = member.FamilyGroup?.Name
                },
                notes = member.Notes,
                createdAt = member.CreatedAt,
                updatedAt = member.UpdatedAt
            },
            dataRetentionPolicy = "Member data retained during membership + 3 years. See DSGVO_SECURITY.md"
        };

        return Ok(export);
    }

    [HttpDelete("{id}/erasure")]
    [Authorize(Policy = AuthorizationPolicies.MemberDelete)]
    public async Task<IActionResult> Erasure(int id)
    {
        try
        {
            await _memberService.ErasureAsync(id, User.Identity?.Name ?? "system");
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Erasure blocked",
                Status = 409,
                Detail = ex.Message
            });
        }
    }

    [HttpGet("guardian-candidates")]
    [Authorize(Policy = AuthorizationPolicies.MemberWrite)]
    public async Task<IActionResult> GetGuardianCandidates()
    {
        var candidates = await _memberService.GetGuardianCandidatesAsync();
        return Ok(candidates);
    }

    [HttpGet("{id}/dependents")]
    [Authorize(Policy = AuthorizationPolicies.MemberView)]
    public async Task<IActionResult> GetDependents(int id)
    {
        var member = await _memberService.GetByIdAsync(id);
        if (member == null)
            return NotFound();

        var dependents = await _memberService.GetDependentsAsync(id);
        return Ok(dependents);
    }
}