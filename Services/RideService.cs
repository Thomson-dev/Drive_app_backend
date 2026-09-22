public class RideService
{
    private readonly RideRepository _rideRepository;
    private readonly DriverProfileRepository _driverProfileRepository;

    public RideService(
        RideRepository rideRepository,
        DriverProfileRepository driverProfileRepository)
    {
        _rideRepository = rideRepository;
        _driverProfileRepository = driverProfileRepository;
    }


    public async Task<Guid> CreateRideAsync( Guid passengerId,
    double pickupLatitude,
    double pickupLongitude,
    double destinationLatitude,
    double destinationLongitude,
    decimal proposedFare,
    string rideType)
{
    return await _rideRepository.CreateRideAsync(passengerId, pickupLatitude, pickupLongitude, destinationLatitude, destinationLongitude, proposedFare, rideType);
}



public async Task<Ride?> GetRideByIdAsync(Guid id)
{
    return await _rideRepository.GetRideByIdAsync(id);
}

public async Task<bool> IsRideOwnedByPassengerAsync(
    Guid rideId,
    Guid passengerId)
{
    return await _rideRepository.IsRideOwnedByPassengerAsync(
        rideId,
        passengerId);
}


public async Task<bool> CancelRideAsync(Guid id)
{
    return await _rideRepository.CancelRideAsync(id);
}

public async Task<List<Ride>> GetPendingRidesAsync()
{
    return await _rideRepository.GetPendingRidesAsync();
}

public async Task<List<Ride>> GetPendingRidesForDriverAsync(Guid driverId)
{
    var driver =
        await _driverProfileRepository.GetProfileByIdAsync(driverId);

    if (driver is null ||
        !string.Equals(
            driver.Status?.Trim(),
            "Online",
            StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException(
            "Driver must be online to view available rides.");
    }

    return await _rideRepository.GetPendingRidesAsync();
}

public async Task<List<Ride>> GetNearbyPendingRidesForDriverAsync(
    Guid driverId,
    double radiusKm)
{
    var driver =
        await _driverProfileRepository.GetProfileByIdAsync(driverId);

    if (driver is null)
    {
        throw new ArgumentException("Driver does not exist.");
    }

    if (driver.Status != "Online")
    {
        throw new ArgumentException(
            "Driver must be online to view nearby rides.");
    }

    if (driver.CurrentLatitude is null ||
        driver.CurrentLongitude is null)
    {
        throw new ArgumentException(
            "Driver location is not available.");
    }

    return await _rideRepository.GetNearbyPendingRidesAsync(
        driver.CurrentLatitude.Value,
        driver.CurrentLongitude.Value,
        radiusKm);
}

public async Task<bool> StartRideAsync(
    Guid rideId,
    Guid driverId)
{
    return await _rideRepository.StartRideAsync(rideId, driverId);
}

public async Task<bool> CompleteRideAsync(
    Guid rideId,
    Guid driverId)
{
    return await _rideRepository.CompleteRideAsync(rideId, driverId);
}


}