using BeselerNet.Shared.Contracts.Roles;
using Dapper;
using Microsoft.Extensions.Caching.Hybrid;
using Npgsql;

namespace BeselerNet.Api.Accounts.OAuth;

internal sealed record RoleDetails
{
    public int RoleId { get; init; }
    public required string Name { get; init; }
    public bool Protected { get; init; }
    public bool LockedGrants { get; init; }
}

internal sealed class RoleDataSource(NpgsqlDataSource dataSource, HybridCache cache)
{
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly HybridCache _cache = cache;

    public async ValueTask<Role?> WithName(string name, CancellationToken cancellationToken)
    {
        var roles = await GetCollection(cancellationToken);
        return roles.TryGetValue(name, out var role) ? role : null;
    }

    public async Task<bool> IsAssignedToAnyone(string name, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(
            """
            SELECT EXISTS (
                SELECT 1
                FROM account_role ar
                INNER JOIN role r ON r.role_id = ar.role_id
                WHERE r.name = @name
            )
            """, new { name });
    }

    public async ValueTask<Dictionary<string, Role>> GetCollection(CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync("Roles", async token =>
        {
            using var connection = await _dataSource.OpenConnectionAsync(token);
            var roles = await connection.QueryAsync<Role>("SELECT role_id, name FROM role");
            return roles.ToDictionary(r => r.Name, StringComparer.Ordinal);
        }, new()
        {
            LocalCacheExpiration = TimeSpan.FromHours(1),
            Expiration = TimeSpan.FromHours(4)
        }, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<RoleResponse>> List(CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<RolePermissionRow>(
            """
            SELECT r.role_id, r.name, r.protected, r.locked_grants,
                   p.permission_id, p.resource, p.action
            FROM role r
            LEFT JOIN role_permission rp ON rp.role_id = r.role_id
            LEFT JOIN permission p ON p.permission_id = rp.permission_id
            ORDER BY r.protected DESC, r.name, p.resource, p.action
            """);
        return MapRoles(rows);
    }

    public async Task<RoleResponse?> WithId(int roleId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<RolePermissionRow>(
            """
            SELECT r.role_id, r.name, r.protected, r.locked_grants,
                   p.permission_id, p.resource, p.action
            FROM role r
            LEFT JOIN role_permission rp ON rp.role_id = r.role_id
            LEFT JOIN permission p ON p.permission_id = rp.permission_id
            WHERE r.role_id = @roleId
            ORDER BY p.resource, p.action
            """, new { roleId });
        return MapRoles(rows).FirstOrDefault();
    }

    public async Task<RoleDetails?> Details(int roleId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<RoleDetails>(
            "SELECT role_id, name, protected, locked_grants FROM role WHERE role_id = @roleId",
            new { roleId });
    }

    public async Task<RoleResponse> Create(string name, IReadOnlyList<int> permissionIds, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var role = await connection.QuerySingleAsync<RoleDetails>(
            """
            INSERT INTO role (name, protected, locked_grants)
            VALUES (@name, false, false)
            RETURNING role_id, name, protected, locked_grants
            """, new { name }, transaction);
        await ReplacePermissions(connection, transaction, role.RoleId, permissionIds);
        await transaction.CommitAsync(cancellationToken);
        await Invalidate(cancellationToken);
        return (await WithId(role.RoleId, cancellationToken))!;
    }

    public async Task Rename(int roleId, string name, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync("UPDATE role SET name = @name WHERE role_id = @roleId", new { name, roleId });
        await Invalidate(cancellationToken);
    }

    public async Task Delete(int roleId, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync("DELETE FROM role WHERE role_id = @roleId", new { roleId });
        await Invalidate(cancellationToken);
    }

    public async Task SetPermissions(int roleId, IReadOnlyList<int> permissionIds, CancellationToken cancellationToken)
    {
        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await ReplacePermissions(connection, transaction, roleId, permissionIds);
        await transaction.CommitAsync(cancellationToken);
        await Invalidate(cancellationToken);
    }

    public async Task<bool> PermissionsExist(IReadOnlyList<int> permissionIds, CancellationToken cancellationToken)
    {
        if (permissionIds.Count == 0)
            return true;

        using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        var found = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM permission WHERE permission_id = ANY(@permissionIds)",
            new { permissionIds = permissionIds.Distinct().ToArray() });
        return found == permissionIds.Distinct().Count();
    }

    private static async Task ReplacePermissions(NpgsqlConnection connection, NpgsqlTransaction transaction, int roleId, IReadOnlyList<int> permissionIds)
    {
        await connection.ExecuteAsync("DELETE FROM role_permission WHERE role_id = @roleId", new { roleId }, transaction);
        foreach (var permissionId in permissionIds.Distinct())
        {
            await connection.ExecuteAsync(
                "INSERT INTO role_permission (role_id, permission_id) VALUES (@roleId, @permissionId)",
                new { roleId, permissionId },
                transaction);
        }
    }

    private async Task Invalidate(CancellationToken cancellationToken) =>
        await _cache.RemoveAsync("Roles", cancellationToken);

    private static List<RoleResponse> MapRoles(IEnumerable<RolePermissionRow> rows)
    {
        return rows
            .GroupBy(row => row.RoleId)
            .Select(group =>
            {
                var first = group.First();
                return new RoleResponse
                {
                    RoleId = first.RoleId,
                    Name = first.Name,
                    Protected = first.Protected,
                    LockedGrants = first.LockedGrants,
                    Permissions = group
                        .Where(row => row.PermissionId is not null)
                        .Select(row => new PermissionResponse
                        {
                            PermissionId = row.PermissionId!.Value,
                            Resource = row.Resource!,
                            Action = row.Action!
                        })
                        .ToArray()
                };
            })
            .ToList();
    }

    private sealed class RolePermissionRow
    {
        public int RoleId { get; init; }
        public required string Name { get; init; }
        public bool Protected { get; init; }
        public bool LockedGrants { get; init; }
        public int? PermissionId { get; init; }
        public string? Resource { get; init; }
        public string? Action { get; init; }
    }
}
