public class DriverProfileService
{
    private readonly DriverProfileRepository _driverProfileRepository;

    public DriverProfileService(DriverProfileRepository driverProfileRepository)
    {
        _driverProfileRepository = driverProfileRepository;
    }

    public async Task<Guid> CreateDriverProfileAsync(
        Guid userId,
        string licenseNumber,
        string vehicleMake,
        string vehicleModel,
        string vehiclePlateNumber)
    {
        return await _driverProfileRepository.CreateProfileAsync(
            userId,
            licenseNumber,
            vehicleMake,
            vehicleModel,
            vehiclePlateNumber);
    }

    public async Task<DriverProfile?> GetDriverProfileByIdAsync(Guid id)
    {
        return await _driverProfileRepository.GetProfileByIdAsync(id);
    }

    public async Task<DriverProfile?> GetDriverProfileByUserIdAsync(Guid userId)
    {
        return await _driverProfileRepository.GetDriverProfileByUserIdAsync(userId);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        return await _driverProfileRepository.UpdateStatusAsync(id, status);
    }

    public async Task<bool> UpdateDriverStatusAsync(
        Guid driverId,
        string status)
    {
        status = status?.Trim() ?? string.Empty;

        if (!string.Equals(status, "Online", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(status, "Offline", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Status must be Online or Offline.");
        }

        return await _driverProfileRepository.UpdateDriverStatusAsync(
            driverId,
            status.Equals("Online", StringComparison.OrdinalIgnoreCase)
                ? "Online"
                : "Offline");
    }

    public async Task<bool> UpdateDriverLocationAsync(
        Guid driverId,
        double latitude,
        double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentException("Invalid latitude.");

        if (longitude < -180 || longitude > 180)
            throw new ArgumentException("Invalid longitude.");

        return await _driverProfileRepository.UpdateDriverLocationAsync(
            driverId,
            latitude,
            longitude);
    }
}

