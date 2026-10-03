using Dapper;
using Microsoft.Data.SqlClient;
using SwiftBets.BuildingBlocks.Persistence;
using SwiftBets.Identity.Application.Ports;

namespace SwiftBets.Identity.Infrastructure.Persistence;

public sealed class SqlRoleStore(ISqlConnectionFactory connections) : IRoleStore
{
    private static readonly SqlResources Sql = SqlResources.For<SqlRoleStore>();

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> PermissionsByRoleAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync<(string Role, string Permission)>(new CommandDefinition(Sql.Get("Roles.PermissionsByRole"), cancellationToken: cancellationToken));
        return rows.GroupBy(r => r.Role).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)[.. g.Select(r => r.Permission)]);
    }

    public Task GrantAsync(string role, string permission, CancellationToken cancellationToken) => ExecuteAsync("Roles.Grant", new { Role = role, Permission = permission }, cancellationToken);

    public Task RevokeAsync(string role, string permission, CancellationToken cancellationToken) => ExecuteAsync("Roles.Revoke", new { Role = role, Permission = permission }, cancellationToken);

    public async Task<IReadOnlyList<string>> UserRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        return [.. await connection.QueryAsync<string>(new CommandDefinition(Sql.Get("Roles.ForUser"), new { UserId = userId }, cancellationToken: cancellationToken))];
    }

    public async Task SetStaffRolesAsync(Guid userId, IReadOnlyList<string> roles, CancellationToken cancellationToken)
    {
        await using var connection = (SqlConnection)await connections.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction();
        await connection.ExecuteAsync(Sql.Get("Roles.ClearStaff"), new { UserId = userId }, transaction);
        foreach (var role in roles)
        {
            await connection.ExecuteAsync(Sql.Get("Users.InsertRole"), new { UserId = userId, Role = role }, transaction);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ExecuteAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(Sql.Get(sql), parameters, cancellationToken: cancellationToken));
    }
}
