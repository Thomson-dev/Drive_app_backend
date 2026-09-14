using Npgsql;

/// <summary>
/// Repository for managing ride database operations.
/// </summary>
public class RideRepository
{
    private readonly DbConnection _dbConnection;

    public RideRepository(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    /// <summary>
    /// Creates a new ride request in the database.
    /// </summary>
    /// <param name="passengerId">The unique ID of the passenger requesting the ride.</param>
    /// <param name="pickupLatitude">Latitude coordinate of pickup location.</param>
    /// <param name="pickupLongitude">Longitude coordinate of pickup location.</param>
    /// <param name="destinationLatitude">Latitude coordinate of destination location.</param>
    /// <param name="destinationLongitude">Longitude coordinate of destination location.</param>
    /// <param name="proposedFare">The fare proposed by the passenger.</param>
    /// <param name="rideType">The type/category of ride requested.</param>
    /// <returns>The generated ride ID.</returns>
    public async Task<Guid> CreateRideAsync(
        Guid passengerId,
        double pickupLatitude,
        double pickupLongitude,
        double destinationLatitude,
        double destinationLongitude,
        decimal proposedFare,
        string rideType)
    {
        const string sql = """
            INSERT INTO rides (
                passenger_id,
                pickup_latitude,
                pickup_longitude,
                destination_latitude,
                destination_longitude,
                proposed_fare,
                ride_type
            )
            VALUES (
                @passengerId,
                @pickupLatitude,
                @pickupLongitude,
                @destinationLatitude,
                @destinationLongitude,
                @proposedFare,
                @rideType
            )
            RETURNING *;
            """;

        // Create and open the database connection
        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@passengerId", passengerId);
        command.Parameters.AddWithValue("@pickupLatitude", pickupLatitude);
        command.Parameters.AddWithValue("@pickupLongitude", pickupLongitude);
        command.Parameters.AddWithValue("@destinationLatitude", destinationLatitude);
        command.Parameters.AddWithValue("@destinationLongitude", destinationLongitude);
        command.Parameters.AddWithValue("@proposedFare", proposedFare);
        command.Parameters.AddWithValue("@rideType", rideType);

        var result = await command.ExecuteScalarAsync();
        if (result is not Guid rideId)
        {
            throw new InvalidOperationException("The database did not return a ride ID.");
        }

        return rideId;
    }

    /// <summary>
    /// Retrieves a ride by its unique ID.
    /// </summary>
    /// <param name="id">The unique ID of the ride.</param>
    /// <returns>The Ride entity if found; null otherwise.</returns>
    public async Task<Ride?> GetRideByIdAsync(Guid id)
    {
        const string sql = """
            SELECT
                id,
                passenger_id,
                pickup_latitude,
                pickup_longitude,
                destination_latitude,
                destination_longitude,
                proposed_fare,
                ride_type,
                requested_at,
                status
            FROM rides
            WHERE id = @id;
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new Ride
        {
            Id = reader.GetGuid(0),
            PassengerId = reader.GetGuid(1),
            PickupLatitude = reader.GetDouble(2),
            PickupLongitude = reader.GetDouble(3),
            DestinationLatitude = reader.GetDouble(4),
            DestinationLongitude = reader.GetDouble(5),
            ProposedFare = reader.GetDecimal(6),
            RideType = reader.GetString(7),
            RequestedAt = reader.GetDateTime(8),
            Status = Enum.Parse<RideStatus>(reader.GetString(9))
        };
    }

    public async Task<bool> IsRideOwnedByPassengerAsync(
        Guid rideId,
        Guid passengerId)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM rides
                WHERE id = @rideId
                  AND passenger_id = @passengerId
            );
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.AddWithValue("rideId", rideId);
        command.Parameters.AddWithValue("passengerId", passengerId);

        return (bool)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>
    /// Cancels a ride if it is currently pending or accepted.
    /// </summary>
    /// <param name="rideId">The unique ID of the ride to cancel.</param>
    /// <returns>True if the ride was successfully cancelled; false otherwise.</returns>
    public async Task<bool> CancelRideAsync(Guid rideId)
    {
        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string cancelRideSql = """
                UPDATE rides
                SET status = 'Cancelled'
                WHERE id = @rideId
                  AND status IN ('Pending', 'Accepted');
                """;

            await using var cancelRideCommand =
                new NpgsqlCommand(cancelRideSql, connection, transaction);

            cancelRideCommand.Parameters.AddWithValue("rideId", rideId);

            var rideRowsAffected =
                await cancelRideCommand.ExecuteNonQueryAsync();

            if (rideRowsAffected == 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            const string cancelOfferSql = """
                UPDATE driver_offers
                SET status = 'Cancelled'
                WHERE ride_id = @rideId
                  AND status = 'Accepted';
                """;

            await using var cancelOfferCommand =
                new NpgsqlCommand(cancelOfferSql, connection, transaction);

            cancelOfferCommand.Parameters.AddWithValue("rideId", rideId);
            await cancelOfferCommand.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Starts an accepted ride for the driver with the accepted offer.
    /// </summary>
    /// <param name="rideId">The unique ID of the ride.</param>
    /// <param name="driverId">The unique ID of the driver.</param>
    /// <returns>True if the ride was successfully started; false otherwise.</returns>
    public async Task<bool> StartRideAsync(
        Guid rideId,
        Guid driverId)
    {
        const string sql = """
            UPDATE rides
            SET status = 'InProgress'
            WHERE id = @rideId
              AND status = 'Accepted'
              AND EXISTS (
                  SELECT 1
                  FROM driver_offers
                  WHERE ride_id = @rideId
                    AND driver_id = @driverId
                    AND status = 'Accepted'
              );
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("rideId", rideId);
        command.Parameters.AddWithValue("driverId", driverId);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }

    /// <summary>
    /// Completes an in-progress ride for the driver with the accepted offer.
    /// </summary>
    /// <param name="rideId">The unique ID of the ride.</param>
    /// <param name="driverId">The unique ID of the driver.</param>
    /// <returns>True if the ride was successfully completed; false otherwise.</returns>
    public async Task<bool> CompleteRideAsync(
        Guid rideId,
        Guid driverId)
    {
        const string sql = """
            UPDATE rides
            SET status = 'Completed'
            WHERE id = @rideId
              AND status = 'InProgress'
              AND EXISTS (
                  SELECT 1
                  FROM driver_offers
                  WHERE ride_id = @rideId
                    AND driver_id = @driverId
                    AND status = 'Accepted'
              );
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("rideId", rideId);
        command.Parameters.AddWithValue("driverId", driverId);

        var rowsAffected = await command.ExecuteNonQueryAsync();

        return rowsAffected > 0;
    }

    /// <summary>
    /// Retrieves all rides currently in 'Pending' status, ordered by request time.
    /// </summary>
    /// <returns>A list of pending rides ordered by requested_at ascending.</returns>
    public async Task<List<Ride>> GetPendingRidesAsync()
    {
        const string sql = """
            SELECT
                id,
                passenger_id,
                pickup_latitude,
                pickup_longitude,
                destination_latitude,
                destination_longitude,
                proposed_fare,
                ride_type,
                requested_at,
                status
            FROM rides
            WHERE status = 'Pending'
            ORDER BY requested_at ASC;
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);

        await using var reader = await command.ExecuteReaderAsync();

        var rides = new List<Ride>();

        while (await reader.ReadAsync())
        {
            rides.Add(new Ride
            {
                Id = reader.GetGuid(0),
                PassengerId = reader.GetGuid(1),
                PickupLatitude = reader.GetDouble(2),
                PickupLongitude = reader.GetDouble(3),
                DestinationLatitude = reader.GetDouble(4),
                DestinationLongitude = reader.GetDouble(5),
                ProposedFare = reader.GetDecimal(6),
                RideType = reader.GetString(7),
                RequestedAt = reader.GetDateTime(8),
                Status = Enum.Parse<RideStatus>(reader.GetString(9))
            });
        }

        return rides;
    }

    /// <summary>
    /// Retrieves pending rides within the specified radius of a driver's location.
    /// </summary>
    /// <param name="driverLatitude">The driver's current latitude.</param>
    /// <param name="driverLongitude">The driver's current longitude.</param>
    /// <param name="radiusKm">The search radius in kilometers.</param>
    /// <returns>Pending rides within the radius, ordered by request time.</returns>
    public async Task<List<Ride>> GetNearbyPendingRidesAsync(
        double driverLatitude,
        double driverLongitude,
        double radiusKm)
    {
        const string sql = """
            SELECT
                id,
                passenger_id,
                pickup_latitude,
                pickup_longitude,
                destination_latitude,
                destination_longitude,
                proposed_fare,
                ride_type,
                requested_at,
                status
            FROM rides
            WHERE status = 'Pending'
              AND (
                  6371 * acos(
                      cos(radians(@driverLatitude))
                      * cos(radians(pickup_latitude))
                      * cos(radians(pickup_longitude) - radians(@driverLongitude))
                      + sin(radians(@driverLatitude))
                      * sin(radians(pickup_latitude))
                  )
              ) <= @radiusKm
            ORDER BY requested_at ASC;
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("driverLatitude", driverLatitude);
        command.Parameters.AddWithValue("driverLongitude", driverLongitude);
        command.Parameters.AddWithValue("radiusKm", radiusKm);

        await using var reader = await command.ExecuteReaderAsync();

        var rides = new List<Ride>();

        while (await reader.ReadAsync())
        {
            rides.Add(new Ride
            {
                Id = reader.GetGuid(0),
                PassengerId = reader.GetGuid(1),
                PickupLatitude = reader.GetDouble(2),
                PickupLongitude = reader.GetDouble(3),
                DestinationLatitude = reader.GetDouble(4),
                DestinationLongitude = reader.GetDouble(5),
                ProposedFare = reader.GetDecimal(6),
                RideType = reader.GetString(7),
                RequestedAt = reader.GetDateTime(8),
                Status = Enum.Parse<RideStatus>(reader.GetString(9))
            });
        }

        return rides;
    }
}