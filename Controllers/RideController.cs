
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("rides")]
public class RideController : ControllerBase
{
    private readonly RideService _rideService;

    public RideController(RideService rideService)
    {
        _rideService = rideService;
    }


    [HttpPost]
    public async Task<IActionResult> CreateRide(CreateRideRequest request)
    {
        try
        {
            var rideId = await _rideService.CreateRideAsync(
                request.PassengerId,
                request.PickupLatitude,
                request.PickupLongitude,
                request.DestinationLatitude,
                request.DestinationLongitude,
                request.ProposedFare,
                request.RideType
            );

            var ride = await _rideService.GetRideByIdAsync(rideId);
            if (ride == null)
            {
                return NotFound();
            }

            return CreatedAtAction(
                nameof(GetRide),
                new { id = rideId },
                ride
            );
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRide(Guid id)
    {
        var ride = await _rideService.GetRideByIdAsync(id);
        if (ride == null)
        {
            return NotFound();
        }
        return Ok(ride);
    }

    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelRide(Guid id)
    {
        var isCancelled = await _rideService.CancelRideAsync(id);
        if (!isCancelled)
        {
            return NotFound();
        }
        return Ok(new
        {
            message = "Ride cancelled successfully"
        });
    }
}


