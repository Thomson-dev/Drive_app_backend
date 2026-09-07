using Npgsql;

public class DriverOfferRepository
{
    private readonly DbConnection _dbConnection;

    public DriverOfferRepository(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

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

        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();

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



    public async Task<bool> AcceptOfferAsync(
        Guid rideId,
        Guid offerId)
    {
        await using var connection = _dbConnection.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
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

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
