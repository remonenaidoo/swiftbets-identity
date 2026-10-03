namespace SwiftBets.Identity.Domain;

/// <summary>Fine-grained staff permissions carried in the <c>perm</c> claim. Customer tokens carry none (D100).</summary>
public static class Permissions
{
    public const string UsersRead = "identity.users.read";
    public const string UsersStatusWrite = "identity.users.status.write";
    public const string RolesRead = "identity.roles.read";
    public const string RolesWrite = "identity.roles.write";

    /// <summary>Every permission a role can be given, with what it allows; the console lists them from here.</summary>
    public static IReadOnlyList<(string Name, string Allows)> Catalogue { get; } =
    [
        (UsersRead, "Find customers and see their accounts"),
        (UsersStatusWrite, "Suspend, close or reopen accounts"),
        (RolesRead, "See roles and who holds them"),
        (RolesWrite, "Change roles and their permissions"),
        ("compliance.read", "See limits, restrictions and KYC"),
        ("compliance.write", "Add restrictions and decide lift requests"),
        ("compliance.audit.read", "Read the audit trail"),
        ("payments.read", "See deposits and withdrawals"),
        ("payments.approve", "Approve or reject withdrawals"),
        ("config.read", "See operational settings"),
        ("config.write", "Change settings and the kill switch"),
        ("reports.read", "Read finance reports and export them"),
    ];

    public static bool IsKnown(string permission) => Catalogue.Any(p => p.Name == permission);
}
