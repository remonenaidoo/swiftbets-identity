using SwiftBets.Contracts.Errors;
using SwiftBets.Contracts.Results;
using SwiftBets.Identity.Application.Ports;
using SwiftBets.Identity.Domain;

namespace SwiftBets.Identity.Application.Roles;

/// <summary>
/// Changes to roles and permissions. Admin always keeps the right to manage roles, and nobody can take Admin away from
/// themselves, so the console can never lock every administrator out. Changes apply at each holder's next token.
/// </summary>
public sealed class RoleHandler(IRoleStore roles)
{
    public async Task<Result<bool>> SetPermissionAsync(string role, string permission, bool granted, CancellationToken cancellationToken)
    {
        if (!RoleNames.StaffRoles.Contains(role) || !Permissions.IsKnown(permission))
        {
            return Error.Validation("unknown_role_or_permission", "Choose a staff role and a listed permission.");
        }

        if (role == RoleNames.Admin && permission == Permissions.RolesWrite && !granted)
        {
            return Error.Validation("would_lock_out", "Admin always keeps the right to manage roles.");
        }

        await (granted ? roles.GrantAsync(role, permission, cancellationToken) : roles.RevokeAsync(role, permission, cancellationToken));
        return Result.Success(true);
    }

    public async Task<Result<bool>> SetUserRolesAsync(Guid userId, IReadOnlyList<string> staffRoles, Guid changedBy, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(staffRoles);
        if (staffRoles.Any(r => !RoleNames.StaffRoles.Contains(r)))
        {
            return Error.Validation("unknown_role", "Staff roles are Trader, Ops and Admin.");
        }

        if (userId == changedBy && !staffRoles.Contains(RoleNames.Admin))
        {
            return Error.Validation("would_lock_out", "You cannot remove your own Admin role.");
        }

        await roles.SetStaffRolesAsync(userId, [.. staffRoles.Distinct()], cancellationToken);
        return Result.Success(true);
    }
}
