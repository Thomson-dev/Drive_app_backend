using StackExchange.Redis;

public class DriverLocationService
{
    private const string DriverLocationsKey = "drivers:locations";
    private readonly IDatabase _database;

    public DriverLocationService(IConnectionMultiplexer connection)
    {
        _database = connection.GetDatabase();
    }

    public async Task SetDriverLocationAsync(
        Guid driverId,
        double latitude,
        double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Invalid latitude.");

        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Invalid longitude.");

        await _database.GeoAddAsync(
            DriverLocationsKey,
            longitude,
            latitude,
            driverId.ToString());
    }

    public async Task<IReadOnlyList<NearbyDriver>> FindNearbyDriversAsync(
        double latitude,
        double longitude,
        double radiusKm)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Invalid latitude.");

        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Invalid longitude.");

        if (radiusKm <= 0)
            throw new ArgumentException("Radius must be greater than zero.");

        var rawResults = await _database.ExecuteAsync(
            "GEOSEARCH",
            DriverLocationsKey,
            "FROMLONLAT",
            longitude,
            latitude,
            "BYRADIUS",
            radiusKm,
            "km",
            "ASC",
            "WITHDIST");
        var results = (RedisResult[]?)rawResults ?? Array.Empty<RedisResult>();

        return Enumerable.Range(0, results.Length / 2)
            .Select(index => new NearbyDriver
            {
                DriverId = Guid.Parse(results[index * 2].ToString()),
                DistanceKm = double.Parse(
                    results[index * 2 + 1].ToString(),
                    System.Globalization.CultureInfo.InvariantCulture)
            })
            .ToArray();
    }
}