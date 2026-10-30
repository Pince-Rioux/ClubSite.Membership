using VereinMember.Data;
using VereinMember.Extensions;
using VereinMember.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace VereinMember.Areas.Membership.Controllers;

[ApiController]
[Authorize]
[Area("Membership")]
[Route("membership/api/fees")]
public class FeesController : ControllerBase
{
    private readonly MembershipDbContext _context;

    public FeesController(MembershipDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetAll()
    {
        var fees = await _context.MembershipFees
            .AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync();

        return Ok(fees);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetById(int id)
    {
        var fee = await _context.MembershipFees
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fee == null)
            return NotFound();

        return Ok(fee);
    }

    [HttpGet("{id}/category")]
    [Authorize(Policy = AuthorizationPolicies.MemberView)]
    public async Task<IActionResult> GetCategory(int id)
    {
        var fee = await _context.MembershipFees
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fee == null)
            return NotFound();

        return Ok(fee.Category.ToString());
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create([FromBody] MembershipFee fee)
    {
        _context.MembershipFees.Add(fee);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = fee.Id }, fee);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(int id, [FromBody] MembershipFee fee)
    {
        var existing = await _context.MembershipFees.FindAsync(id);
        if (existing == null)
            return NotFound();

        existing.Name = fee.Name;
        existing.Amount = fee.Amount;
        existing.Interval = fee.Interval;
        existing.Category = fee.Category;
        existing.IsDefault = fee.IsDefault;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var fee = await _context.MembershipFees
            .Include(f => f.Members)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (fee == null)
            return NotFound();

        if (fee.Members.Any(m => m.Status == MemberStatus.Active))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Delete blocked",
                Status = 409,
                Detail = "Cannot delete a fee that is assigned to active members."
            });
        }

        _context.MembershipFees.Remove(fee);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}