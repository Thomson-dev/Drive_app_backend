using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("rides/{rideId:guid}/offers")]
[Authorize]
public class DriverOfferController : ControllerBase
{
    private readonly DriverOfferService _driverOfferService;
    private readonly DriverProfileService _driverProfileService;
    private readonly RideService _rideService;

    public DriverOfferController(
        DriverOfferService driverOfferService,
        DriverProfileService driverProfileService,
        RideService rideService)
    {
        _driverOfferService = driverOfferService;
        _driverProfileService = driverProfileService;
        _rideService = rideService;
    }

    [Authorize(Roles = "Driver")]
    [HttpPost]
    public async Task<IActionResult> CreateOffer(
        Guid rideId,
        CreateDriverOfferRequest request)
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

        var offerId = await _driverOfferService.CreateOfferAsync(
            rideId,
            driver.Id,
            request.OfferedFare
        );

        return Ok(new
        {
            id = offerId
        });
    }

    [Authorize(Roles = "Passenger")]
    [HttpGet]
    public async Task<IActionResult> GetOffers(Guid rideId)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var passengerId))
        {
            return Unauthorized();
        }

        var ownsRide =
            await _rideService.IsRideOwnedByPassengerAsync(
                rideId,
                passengerId);

        if (!ownsRide)
        {
            return Forbid();
        }

        var offers =
            await _driverOfferService.GetOffersByRideIdAsync(rideId);

        return Ok(offers);
    }

    [Authorize(Roles = "Passenger")]
    [HttpPatch("{offerId:guid}/accept")]
    public async Task<IActionResult> AcceptOffer(
        Guid rideId,
        Guid offerId)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var passengerId))
        {
            return Unauthorized();
        }

        var ownsRide =
            await _rideService.IsRideOwnedByPassengerAsync(
                rideId,
                passengerId);

        if (!ownsRide)
        {
            return Forbid();
        }

        var accepted =
            await _driverOfferService.AcceptOfferAsync(
                rideId,
                offerId);

        if (!accepted)
        {
            return BadRequest(new
            {
                message = "Offer cannot be accepted."
            });
        }

        return Ok(new
        {
            message = "Offer accepted successfully"
        });
    }
}
