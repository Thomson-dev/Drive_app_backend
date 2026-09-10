public class CreateDriverProfileRequest
{
    public Guid UserId { get; set; }

    public string LicenseNumber { get; set; } = string.Empty;

    public string VehicleMake { get; set; } = string.Empty;

    public string VehicleModel { get; set; } = string.Empty;

    public string VehiclePlateNumber { get; set; } = string.Empty;
}

