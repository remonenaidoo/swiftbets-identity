namespace SwiftBets.Identity.Application.Ports;

/// <summary>Which permissions each role grants, and which roles each user holds.</summary>
public interface IRoleStore
{
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> PermissionsByRoleAsync(CancellationToken cancellationToken);

    Task GrantAsync(string role, string permission, CancellationToken cancellationToken);

    Task RevokeAsync(string role, string permission, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> UserRolesAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Replaces the user's staff roles; customer roles are left as they are.</summary>
    Task SetStaffRolesAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken cancellationToken);
}
