public class DriverOfferService
{
    private readonly DriverOfferRepository _driverOfferRepository;

    public DriverOfferService(DriverOfferRepository driverOfferRepository)
    {
        _driverOfferRepository = driverOfferRepository;
    }

    public async Task<Guid> CreateOfferAsync(
        Guid rideId,
        Guid driverId,
        decimal offeredFare)
    {
        return await _driverOfferRepository.CreateOfferAsync(
            rideId,
            driverId,
            offeredFare);
    }

    public async Task<List<DriverOffer>> GetOffersByRideIdAsync(Guid rideId)
    {
        return await _driverOfferRepository.GetOffersByRideIdAsync(rideId);
    }

    public async Task<bool> AcceptOfferAsync(
        Guid rideId,
        Guid offerId)
    {
        return await _driverOfferRepository.AcceptOfferAsync(
            rideId,
            offerId);
    }
}
