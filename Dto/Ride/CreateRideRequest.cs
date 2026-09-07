using System.ComponentModel.DataAnnotations;

public class CreateRideRequest
{
    [Required]
    public Guid PassengerId { get; set; }

    [Range(-90, 90)]
    public double PickupLatitude { get; set; }

    [Range(-180, 180)]
    public double PickupLongitude { get; set; }

    [Range(-90, 90)]
    public double DestinationLatitude { get; set; }

    [Range(-180, 180)]
    public double DestinationLongitude { get; set; }

    [Range(1, double.MaxValue)]
    public decimal ProposedFare { get; set; }

    [Required]
    public string RideType { get; set; } = string.Empty;
}