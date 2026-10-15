using DesignAutomation.AppCatalog.Model;
using DesignAutomation.AppCatalog.Security;
using Npgsql;

namespace DesignAutomation.AppCatalog.Data;

/// <summary>Persists the local development identity used to sign Product Health requests.</summary>
public sealed class PostgresUsersRepository(NpgsqlDataSource dataSource) : IUsersRepository
{
    public AppUser? GetByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        using var connection = dataSource.OpenConnection();
        using var command = new NpgsqlCommand("SELECT id,email,created,modified,role FROM product_health_users WHERE lower(email)=lower(@email)", connection);
        command.Parameters.AddWithValue("email", email.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadUser(reader) : null;
    }

    public async Task<Guid> Insert(AppUser user)
    {
        if (user is null || string.IsNullOrWhiteSpace(user.Email)) return Guid.Empty;
        if (user.ID == Guid.Empty) user.ID = Guid.NewGuid();
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("INSERT INTO product_health_users(id,email,created,modified,role) VALUES(@id,@email,@created,@modified,@role) ON CONFLICT DO NOTHING", connection);
        command.Parameters.AddWithValue("id", user.ID);
        command.Parameters.AddWithValue("email", user.Email.Trim());
        command.Parameters.AddWithValue("created", user.Created);
        command.Parameters.AddWithValue("modified", user.Modified);
        command.Parameters.AddWithValue("role", (int)user.Role);
        return await command.ExecuteNonQueryAsync() == 1 ? user.ID : Guid.Empty;
    }

    public async Task<bool> UpdateRole(Guid userId, UserRoleLevel role)
    {
        if (userId == Guid.Empty) return false;
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("UPDATE product_health_users SET role=@role,modified=@modified WHERE id=@id", connection);
        command.Parameters.AddWithValue("id", userId);
        command.Parameters.AddWithValue("role", (int)role);
        command.Parameters.AddWithValue("modified", DateTimeOffset.UtcNow);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    private static AppUser ReadUser(NpgsqlDataReader reader) => new()
    {
        ID = reader.GetGuid(0),
        Email = reader.GetString(1),
        Created = reader.GetFieldValue<DateTimeOffset>(2),
        Modified = reader.GetFieldValue<DateTimeOffset>(3),
        Role = Enum.IsDefined(typeof(UserRoleLevel), reader.GetInt32(4)) ? (UserRoleLevel)reader.GetInt32(4) : UserRoleLevel.User
    };
}
