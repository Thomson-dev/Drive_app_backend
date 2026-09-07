public class Ride
{
    public Guid Id { get; set; }

    public Guid PassengerId { get; set; }

    public double PickupLatitude { get; set; }
    public double PickupLongitude { get; set; }

    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }

    public decimal ProposedFare { get; set; }

    public string RideType { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; }

    public RideStatus Status { get; set; } = RideStatus.Pending;
}



public enum RideStatus
{
    Pending,
    Accepted,
    InProgress,
    Completed,
    Cancelled

}