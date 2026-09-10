using Npgsql;

public class UserRepository
{
    private readonly DbConnection _dbConnection;

    public UserRepository(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<Guid> CreateUserAsync(
        string fullName,
        string email,
        string passwordHash,
        string role)
    {
        const string sql = """
            INSERT INTO users (
                full_name,
                email,
                password_hash,
                role
            )
            VALUES (
                @fullName,
                @email,
                @passwordHash,
                @role
            )
            RETURNING id;
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("fullName", fullName);
        command.Parameters.AddWithValue("email", email);
        command.Parameters.AddWithValue("passwordHash", passwordHash);
        command.Parameters.AddWithValue("role", role);

        return (Guid)(await command.ExecuteScalarAsync())!;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        const string sql = """
            SELECT
                id,
                full_name,
                email,
                password_hash,
                role,
                created_at
            FROM users
            WHERE email = @email;
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("email", email);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new User
        {
            Id = reader.GetGuid(0),
            FullName = reader.GetString(1),
            Email = reader.GetString(2),
            PasswordHash = reader.GetString(3),
            Role = reader.GetString(4),
            CreatedAt = reader.GetDateTime(5)
        };
    }
}