using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Npgsql;

namespace DesignAutomation.AppCatalog.Data;

public sealed class PostgresFoundationAccessRepository(NpgsqlDataSource dataSource) : IFoundationAccessRepository
{
    public async Task<List<FoundationUserSummary>> ListUsers()
    {
        var result = new List<FoundationUserSummary>();
        await using var c = await dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("SELECT id,email,role,created,modified FROM product_health_users ORDER BY lower(email)", c);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) result.Add(new(r.GetGuid(0), r.GetString(1), RoleName((UserRoleLevel)r.GetInt32(2)), r.GetFieldValue<DateTimeOffset>(3), r.GetFieldValue<DateTimeOffset>(4)));
        return result;
    }

    public async Task<Guid> CreateUser(string email, UserRoleLevel role)
    {
        email = ValidateEmail(email);
        if (role is not (UserRoleLevel.User or UserRoleLevel.Admin)) throw new ArgumentException("Global role must be User or Admin.");
        var id = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        await using var c = await dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("INSERT INTO product_health_users(id,email,created,modified,role) VALUES(@id,@email,@now,@now,@role) ON CONFLICT DO NOTHING", c);
        cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("email", email); cmd.Parameters.AddWithValue("now", now); cmd.Parameters.AddWithValue("role", (int)role);
        if (await cmd.ExecuteNonQueryAsync() == 0) throw new ArgumentException("A user with this email already exists.");
        return id;
    }

    public async Task<bool> SetUserRole(Guid id, UserRoleLevel role)
    {
        if (role is not (UserRoleLevel.User or UserRoleLevel.Admin)) throw new ArgumentException("Global role must be User or Admin.");
        await using var c = await dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand("UPDATE product_health_users SET role=@role,modified=now() WHERE id=@id", c);
        cmd.Parameters.AddWithValue("role", (int)role); cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() == 1;
    }

    public Task<bool> SetPortfolioAccess(Guid portfolioId, Guid userId, UserRoleLevel role) =>
        SetAccess("portfolio_user_access", "portfolio_id", "portfolios", portfolioId, userId, role);

    public Task<bool> SetProductAccess(Guid productId, Guid userId, UserRoleLevel role) =>
        SetAccess("product_user_access", "product_id", "products", productId, userId, role);

    public async Task<bool> CanReadProduct(Guid productId, Guid userId) => await Check(productId, userId, false);
    public async Task<bool> CanManageProduct(Guid productId, Guid userId) => await Check(productId, userId, true);

    private async Task<bool> Check(Guid productId, Guid userId, bool write)
    {
        await using var c = await dataSource.OpenConnectionAsync();
        var minimum = write ? 20 : 1;
        await using var cmd = new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM product_user_access a WHERE a.product_id=p.id AND a.user_id=@user AND a.role>=@min) OR EXISTS(SELECT 1 FROM portfolio_user_access a WHERE a.portfolio_id=p.portfolio_id AND a.user_id=@user AND a.role>=@min) FROM products p WHERE p.id=@product", c);
        cmd.Parameters.AddWithValue("product", productId); cmd.Parameters.AddWithValue("user", userId); cmd.Parameters.AddWithValue("min", minimum);
        return (bool?)await cmd.ExecuteScalarAsync() ?? false;
    }

    private async Task<bool> SetAccess(string table, string key, string parentTable, Guid parent, Guid user, UserRoleLevel role)
    {
        if (parent == Guid.Empty || user == Guid.Empty) throw new ArgumentException("A valid resource and user are required.");
        if (role is not (UserRoleLevel.User or UserRoleLevel.ProductManager or UserRoleLevel.PortfolioManager))
            throw new ArgumentException("Scoped role must be User, ProductManager, or PortfolioManager.");
        await using var c = await dataSource.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        await using (var exists = new NpgsqlCommand($"SELECT EXISTS(SELECT 1 FROM {parentTable} WHERE id=@parent) AND EXISTS(SELECT 1 FROM product_health_users WHERE id=@user)", c, tx))
        {
            exists.Parameters.AddWithValue("parent", parent); exists.Parameters.AddWithValue("user", user);
            if (!(bool)(await exists.ExecuteScalarAsync() ?? false)) return false;
        }
        await using (var cmd = new NpgsqlCommand($"INSERT INTO {table}({key},user_id,role) VALUES(@parent,@user,@role) ON CONFLICT({key},user_id) DO UPDATE SET role=EXCLUDED.role", c, tx))
        {
            cmd.Parameters.AddWithValue("parent", parent); cmd.Parameters.AddWithValue("user", user); cmd.Parameters.AddWithValue("role", (int)role);
            await cmd.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync(); return true;
    }

    private static string ValidateEmail(string email)
    {
        var value = (email ?? string.Empty).Trim();
        if (value.Length > 320 || !System.Net.Mail.MailAddress.TryCreate(value, out var parsed) || !string.Equals(parsed.Address, value, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A valid email address is required.");
        return value;
    }

    private static string RoleName(UserRoleLevel role) => role switch
    {
        UserRoleLevel.Admin => "Admin", UserRoleLevel.PortfolioManager => "PortfolioManager",
        UserRoleLevel.ProductManager => "ProductManager", UserRoleLevel.User => "User", _ => "Guest"
    };
}
