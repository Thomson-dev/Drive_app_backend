
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("rides")]
public class RideController : ControllerBase
{
    private readonly RideService _rideService;
    private readonly DriverProfileService _driverProfileService;

    public RideController(
        RideService rideService,
        DriverProfileService driverProfileService)
    {
        _rideService = rideService;
        _driverProfileService = driverProfileService;
    }


    [Authorize(Roles = "Passenger")]
    [HttpPost]
    public async Task<IActionResult> CreateRide(CreateRideRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var passengerId))
        {
            return Unauthorized();
        }

        try
        {
            var rideId = await _rideService.CreateRideAsync(
                passengerId,
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

    [Authorize]
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

    [Authorize(Roles = "Passenger")]
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelRide(Guid id)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var passengerId))
        {
            return Unauthorized();
        }

        var ownsRide =
            await _rideService.IsRideOwnedByPassengerAsync(
                id,
                passengerId);

        if (!ownsRide)
        {
            return Forbid();
        }

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

    [Authorize(Roles = "Driver")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingRides()
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var driver =
            await _driverProfileService
                .GetDriverProfileByUserIdAsync(userId);

        if (driver is null)
        {
            return NotFound(new
            {
                message = "Driver profile not found."
            });
        }

        try
        {
            var rides =
                await _rideService.GetPendingRidesForDriverAsync(driver.Id);

            return Ok(rides);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize(Roles = "Driver")]
    [HttpPatch("{rideId:guid}/start")]
    public async Task<IActionResult> StartRide(Guid rideId)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var driver =
            await _driverProfileService
                .GetDriverProfileByUserIdAsync(userId);

        if (driver is null)
        {
            return NotFound(new
            {
                message = "Driver profile not found."
            });
        }

        var started = await _rideService.StartRideAsync(rideId, driver.Id);

        if (!started)
        {
            return BadRequest(new
            {
                message = "Ride cannot be started."
            });
        }

        return Ok(new
        {
            message = "Ride started successfully"
        });
    }

    [Authorize(Roles = "Driver")]
    [HttpPatch("{rideId:guid}/complete")]
    public async Task<IActionResult> CompleteRide(Guid rideId)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var driver =
            await _driverProfileService
                .GetDriverProfileByUserIdAsync(userId);

        if (driver is null)
        {
            return NotFound(new
            {
                message = "Driver profile not found."
            });
        }

        var completed =
            await _rideService.CompleteRideAsync(rideId, driver.Id);

        if (!completed)
        {
            return BadRequest(new
            {
                message = "Ride cannot be completed."
            });
        }

        return Ok(new
        {
            message = "Ride completed successfully"
        });
    }
}


