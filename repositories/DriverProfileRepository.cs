using Npgsql;
using NpgsqlTypes;

/// <summary>
/// Repository for managing driver profile database operations.
/// </summary>
public class DriverProfileRepository
{
    private readonly DbConnection _dbConnection;

    public DriverProfileRepository(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    /// <summary>
    /// Creates a new driver profile record in the database.
    /// </summary>
    /// <param name="userId">The user ID associated with the driver.</param>
    /// <param name="licenseNumber">Driver's license number.</param>
    /// <param name="vehicleMake">Make of the vehicle.</param>
    /// <param name="vehicleModel">Model of the vehicle.</param>
    /// <param name="vehiclePlateNumber">License plate number of the vehicle.</param>
    /// <returns>The generated driver profile ID.</returns>
    public async Task<Guid> CreateProfileAsync(
        Guid userId,
        string licenseNumber,
        string vehicleMake,
        string vehicleModel,
        string vehiclePlateNumber)
    {
        const string sql = """
            INSERT INTO driver_profiles (
                user_id,
                license_number,
                vehicle_make,
                vehicle_model,
                vehicle_plate_number
            )
            VALUES (
                @userId,
                @licenseNumber,
                @vehicleMake,
                @vehicleModel,
                @vehiclePlateNumber
            )
            RETURNING id;
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("licenseNumber", licenseNumber);
        command.Parameters.AddWithValue("vehicleMake", vehicleMake);
        command.Parameters.AddWithValue("vehicleModel", vehicleModel);
        command.Parameters.AddWithValue("vehiclePlateNumber", vehiclePlateNumber);

        var result = await command.ExecuteScalarAsync();
        if (result is not Guid profileId)
        {
            throw new InvalidOperationException("The database did not return a profile ID.");
        }

        return profileId;
    }

    /// <summary>
    /// Retrieves a driver profile by associated user ID.
    /// </summary>
    /// <param name="userId">The user ID of the driver.</param>
    /// <returns>The DriverProfile entity if found; null otherwise.</returns>
    public async Task<DriverProfile?> GetProfileByUserIdAsync(Guid userId)
    {
        const string sql = """
            SELECT
                id,
                user_id,
                license_number,
                vehicle_make,
                vehicle_model,
                vehicle_plate_number,
                status
            FROM driver_profiles
            WHERE user_id = @userId;
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("userId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new DriverProfile
            {
                Id = reader.GetGuid(0),
                UserId = reader.GetGuid(1),
                LicenseNumber = reader.GetString(2),
                VehicleMake = reader.GetString(3),
                VehicleModel = reader.GetString(4),
                VehiclePlateNumber = reader.GetString(5),
                Status = reader.GetString(6)
            };
        }

        return null;
    }

    /// <summary>
    /// Retrieves a driver profile by its primary key ID.
    /// </summary>
    /// <param name="id">The unique profile ID.</param>
    /// <returns>The DriverProfile entity if found; null otherwise.</returns>
    public async Task<DriverProfile?> GetProfileByIdAsync(Guid id)
    {
        const string sql = """
            SELECT
                id,
                user_id,
                license_number,
                vehicle_make,
                vehicle_model,
                vehicle_plate_number,
                status,
                current_latitude,
                current_longitude
            FROM driver_profiles
            WHERE id = @id;
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new DriverProfile
            {
                Id = reader.GetGuid(0),
                UserId = reader.GetGuid(1),
                LicenseNumber = reader.GetString(2),
                VehicleMake = reader.GetString(3),
                VehicleModel = reader.GetString(4),
                VehiclePlateNumber = reader.GetString(5),
                Status = reader.GetString(6),
                CurrentLatitude = reader.IsDBNull(7)
                    ? null
                    : reader.GetDouble(7),
                CurrentLongitude = reader.IsDBNull(8)
                    ? null
                    : reader.GetDouble(8)
            };
        }

        return null;
    }

    /// <summary>
    /// Updates the status of a driver profile (e.g. 'Online', 'Offline').
    /// </summary>
    /// <param name="id">The unique profile ID.</param>
    /// <param name="status">The new status value.</param>
    /// <returns>True if the status update succeeded; false otherwise.</returns>
    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        const string sql = """
            UPDATE driver_profiles
            SET status = @status
            WHERE id = @id;
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("status", status);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    /// <summary>
    /// Updates the status of a driver profile (e.g. 'Online', 'Offline').
    /// </summary>
    /// <param name="driverId">The unique profile ID.</param>
    /// <param name="status">The new status value.</param>
    /// <returns>True if the status update succeeded; false otherwise.</returns>
    public async Task<bool> UpdateDriverStatusAsync(Guid driverId, string status)
    {
        const string sql = """
            UPDATE driver_profiles
            SET status = @status
            WHERE id = @driverId;
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("driverId", driverId);
        command.Parameters.AddWithValue("status", status);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateDriverLocationAsync(
        Guid driverId,
        double latitude,
        double longitude)
    {
        const string sql = """
            UPDATE driver_profiles
            SET
                current_latitude = @latitude,
                current_longitude = @longitude
            WHERE id = @driverId;
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("driverId", driverId);
        command.Parameters.AddWithValue("latitude", latitude);
        command.Parameters.AddWithValue("longitude", longitude);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }

    /// <summary>
    /// Checks whether a driver profile exists and is currently Online.
    /// </summary>
    /// <param name="driverId">The unique ID of the driver profile.</param>
    /// <returns>True if the driver exists and is Online; false otherwise.</returns>
    public async Task<bool> IsDriverOnlineAsync(Guid driverId)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM driver_profiles
                WHERE id = @driverId
                  AND status = 'Online'
            );
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("driverId", driverId);

        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
