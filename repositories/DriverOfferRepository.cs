using Npgsql;

/// <summary>
/// Repository for managing driver offer database operations.
/// </summary>
public class DriverOfferRepository
{
    private readonly DbConnection _dbConnection;

    public DriverOfferRepository(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    /// <summary>
    /// Creates a new driver offer for a ride after verifying the driver profile exists.
    /// </summary>
    /// <param name="rideId">The unique ID of the ride.</param>
    /// <param name="driverId">The unique ID of the driver.</param>
    /// <param name="offeredFare">The fare proposed by the driver.</param>
    /// <returns>The generated offer ID.</returns>
    public async Task<Guid> CreateOfferAsync(
        Guid rideId,
        Guid driverId,
        decimal offeredFare)
    {
        const string sql = """
            INSERT INTO driver_offers (
                ride_id,
                driver_id,
                offered_fare
            )
            VALUES (
                @rideId,
                @driverId,
                @offeredFare
            )
            RETURNING id;
            """;

        // Create and open the database connection
        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        // 1. Verify that the ride exists and is in 'Pending' status
        const string rideExistsSql = """
            SELECT EXISTS (
                SELECT 1
                FROM rides
                WHERE id = @rideId
                  AND status = 'Pending'
            );
            """;

        await using var rideCheckCommand =
            new NpgsqlCommand(rideExistsSql, connection);

        rideCheckCommand.Parameters.AddWithValue("rideId", rideId);

        var rideIsPending =
            (bool)(await rideCheckCommand.ExecuteScalarAsync())!;

        if (!rideIsPending)
        {
            throw new ArgumentException(
                "Ride does not exist or is no longer available.");
        }

        // 2. Verify that the driver profile exists and is Online before creating an offer
        const string driverStatusSql = """
            SELECT status
            FROM driver_profiles
            WHERE id = @driverId;
            """;

        await using var driverStatusCommand =
            new NpgsqlCommand(driverStatusSql, connection);

        driverStatusCommand.Parameters.AddWithValue("driverId", driverId);

        var driverStatus =
            await driverStatusCommand.ExecuteScalarAsync();

        if (driverStatus is null)
        {
            throw new ArgumentException("Driver does not exist.");
        }

        if ((string)driverStatus != "Online")
        {
            throw new ArgumentException(
                "Driver must be online to submit an offer.");
        }

        // 2. Verify that the driver has not already submitted an offer for this ride
        const string existingOfferSql = """
            SELECT EXISTS (
                SELECT 1
                FROM driver_offers
                WHERE ride_id = @rideId
                  AND driver_id = @driverId
            );
            """;

        await using var existingOfferCommand =
            new NpgsqlCommand(existingOfferSql, connection);

        existingOfferCommand.Parameters.AddWithValue("rideId", rideId);
        existingOfferCommand.Parameters.AddWithValue("driverId", driverId);

        var offerExists =
            (bool)(await existingOfferCommand.ExecuteScalarAsync())!;

        if (offerExists)
        {
            throw new ArgumentException(
                "Driver has already submitted an offer for this ride.");
        }

        // 3. Insert the new offer and return its generated ID
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("rideId", rideId);
        command.Parameters.AddWithValue("driverId", driverId);
        command.Parameters.AddWithValue("offeredFare", offeredFare);

        var result = await command.ExecuteScalarAsync();
        if (result is not Guid offerId)
        {
            throw new InvalidOperationException("The database did not return an offer ID.");
        }

        return offerId;
    }

    /// <summary>
    /// Retrieves all driver offers submitted for a specific ride, ordered by creation time.
    /// </summary>
    /// <param name="rideId">The unique ID of the ride.</param>
    /// <returns>A list of driver offers for the specified ride.</returns>
    public async Task<List<DriverOffer>> GetOffersByRideIdAsync(Guid rideId)
    {
        const string sql = """
            SELECT
                id,
                ride_id,
                driver_id,
                offered_fare,
                offered_at,
                status
            FROM driver_offers
            WHERE ride_id = @rideId
            ORDER BY offered_at ASC;
            """;

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("rideId", rideId);

        await using var reader = await command.ExecuteReaderAsync();
        var offers = new List<DriverOffer>();

        while (await reader.ReadAsync())
        {
            offers.Add(new DriverOffer
            {
                Id = reader.GetGuid(0),
                RideId = reader.GetGuid(1),
                DriverId = reader.GetGuid(2),
                OfferedFare = reader.GetDecimal(3),
                OfferedAt = reader.GetDateTime(4),
                Status = reader.GetString(5)
            });
        }

        return offers;
    }

    /// <summary>
    /// Accepts a driver offer for a ride within an atomic database transaction.
    /// Validates ride status, marks the chosen offer as 'Accepted', rejects all other pending offers for the ride, and marks the ride as 'Accepted'.
    /// </summary>
    /// <param name="rideId">The unique ID of the ride.</param>
    /// <param name="offerId">The unique ID of the offer being accepted.</param>
    /// <returns>True if the offer acceptance process succeeds; false otherwise.</returns>
    public async Task<bool> AcceptOfferAsync(
        Guid rideId,
        Guid offerId)
    {
        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

        // Begin an atomic transaction to ensure all status updates happen together or fail together
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            // 1. Check if the ride exists and is in 'Pending' status
            const string rideStatusSql = """
                SELECT status
                FROM rides
                WHERE id = @rideId;
                FOR UPDATE;
                """;

            await using var rideStatusCommand =
                new NpgsqlCommand(rideStatusSql, connection, transaction);

            rideStatusCommand.Parameters.AddWithValue("rideId", rideId);

            var rideStatus =
                await rideStatusCommand.ExecuteScalarAsync();

            if (rideStatus is null ||
                (string)rideStatus != "Pending")
            {
                await transaction.RollbackAsync();
                return false;
            }

            // 2. Validate that the specified offer exists, belongs to this ride, and is in 'Pending' status
            const string getOfferSql = """
                SELECT ride_id
                FROM driver_offers
                WHERE id = @offerId
                AND ride_id = @rideId
                  AND status = 'Pending';
                """;

            await using var getOfferCommand =
                new NpgsqlCommand(getOfferSql, connection, transaction);

            getOfferCommand.Parameters.AddWithValue("offerId", offerId);
            getOfferCommand.Parameters.AddWithValue("rideId", rideId);

            var result = await getOfferCommand.ExecuteScalarAsync();

            if (result is null)
            {
                await transaction.RollbackAsync();
                return false;
            }

            var offerRideId = (Guid)result;

            // 3. Mark the selected offer status as 'Accepted'
            const string acceptOfferSql = """
                UPDATE driver_offers
                SET status = 'Accepted'
                WHERE id = @offerId
                  AND status = 'Pending';
                """;

            await using var acceptOfferCommand =
                new NpgsqlCommand(acceptOfferSql, connection, transaction);

            acceptOfferCommand.Parameters.AddWithValue("offerId", offerId);
            await acceptOfferCommand.ExecuteNonQueryAsync();

            // 4. Mark all other pending offers for this ride as 'Rejected'
            const string rejectOtherOffersSql = """
                UPDATE driver_offers
                SET status = 'Rejected'
                WHERE ride_id = @rideId
                  AND id <> @offerId
                  AND status = 'Pending';
                """;

            await using var rejectCommand =
                new NpgsqlCommand(rejectOtherOffersSql, connection, transaction);

            rejectCommand.Parameters.AddWithValue("rideId", offerRideId);
            rejectCommand.Parameters.AddWithValue("offerId", offerId);
            await rejectCommand.ExecuteNonQueryAsync();

            // 5. Update the ride status to 'Accepted'
            const string acceptRideSql = """
                UPDATE rides
                SET status = 'Accepted'
                WHERE id = @rideId
                  AND status = 'Pending';
                """;

            await using var acceptRideCommand =
                new NpgsqlCommand(acceptRideSql, connection, transaction);

            acceptRideCommand.Parameters.AddWithValue("rideId", offerRideId);

            var rideRowsAffected =
                await acceptRideCommand.ExecuteNonQueryAsync();

            if (rideRowsAffected == 0)
            {
                await transaction.RollbackAsync();
                return false;
            }

            // Commit transaction if all steps succeeded
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            // Rollback transaction on any unhandled failure
            await transaction.RollbackAsync();
            throw;
        }
    }
}
