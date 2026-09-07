public class DriverOffer
{
    public Guid Id { get; set; }

    public Guid RideId { get; set; }

    public Guid DriverId { get; set; }

    public decimal OfferedFare { get; set; }

    public DateTime OfferedAt { get; set; }

    public string Status { get; set; } = "Pending";
}
