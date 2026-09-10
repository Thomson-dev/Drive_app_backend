public class DriverProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string LicenseNumber { get; set; } = string.Empty;

    public string VehicleMake { get; set; } = string.Empty;

    public string VehicleModel { get; set; } = string.Empty;

    public string VehiclePlateNumber { get; set; } = string.Empty;

    public string Status { get; set; } = "Offline";

    public double? CurrentLatitude { get; set; }

    public double? CurrentLongitude { get; set; }
}

