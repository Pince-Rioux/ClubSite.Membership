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
[Route("membership/api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly MembershipDbContext _context;

    public DepartmentsController(MembershipDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();

        return Ok(departments);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> GetById(int id)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
            return NotFound();

        return Ok(department);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create([FromBody] Department department)
    {
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = department.Id }, department);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(int id, [FromBody] Department department)
    {
        var existing = await _context.Departments.FindAsync(id);
        if (existing == null)
            return NotFound();

        existing.Name = department.Name;
        existing.Description = department.Description;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _context.Departments
            .Include(d => d.Members)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null)
            return NotFound();

        if (department.Members.Any(m => m.Status == MemberStatus.Active))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Delete blocked",
                Status = 409,
                Detail = "Cannot delete department with active members."
            });
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}