public class RideService
{
    private readonly RideRepository _rideRepository;

    public RideService(RideRepository rideRepository)
    {
        _rideRepository = rideRepository;
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


public async Task<bool> CancelRideAsync(Guid id)
{
    return await _rideRepository.CancelRideAsync(id);
}


}