
using Npgsql;

public class RideRepository
{
    private readonly DbConnection _dbConnection;

    public RideRepository(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

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
            //Create the database connection
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




    public async Task<bool> CancelRideAsync(Guid id)
{
    //only a pending ride can be cancelled.
    const string sql = """
        UPDATE rides
        SET status = @cancelledStatus
        WHERE id = @id
          AND status = 'Pending';
        """;

    await using var connection = _dbConnection.CreateConnection();
    await connection.OpenAsync();

    await using var command = new NpgsqlCommand(sql, connection);
    command.Parameters.AddWithValue("id", id);
    command.Parameters.AddWithValue(
        "cancelledStatus",
        RideStatus.Cancelled.ToString()
    );

    var rowsAffected = await command.ExecuteNonQueryAsync();

    return rowsAffected > 0;
}
}