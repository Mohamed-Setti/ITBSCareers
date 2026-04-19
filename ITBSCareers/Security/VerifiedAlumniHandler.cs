using System.Security.Claims;
using ITBSCareers.Models.Carriere;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ITBSCareers.Security;

public sealed class VerifiedAlumniHandler : AuthorizationHandler<VerifiedAlumniRequirement>
{
    private readonly CarriereDbContext _context;

    public VerifiedAlumniHandler(CarriereDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, VerifiedAlumniRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var hasAlumniRole = await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.Role != null && ur.Role.Name == "Alumni");

        if (!hasAlumniRole)
        {
            return;
        }

        var hasAlumniRecord = await _context.Alumnis.AnyAsync(a => a.AlumniId == userId);

        bool hasApprovedRequest;
        try
        {
            hasApprovedRequest = await _context.AlumniRequests
                .AnyAsync(r => r.UserId == userId && r.Status == "Approved");
        }
        catch (SqlException ex) when (ex.Message.Contains("Invalid object name 'AlumniRequests'"))
        {
            hasApprovedRequest = false;
        }

        if (hasAlumniRecord && hasApprovedRequest)
        {
            context.Succeed(requirement);
        }
    }
}
