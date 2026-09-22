using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("drivers")]
public class DriverProfileController : ControllerBase
{
    private readonly DriverProfileService _driverProfileService;
    private readonly DriverLocationService _driverLocationService;

    public DriverProfileController(
        DriverProfileService driverProfileService,
        DriverLocationService driverLocationService)
    {
        _driverProfileService = driverProfileService;
        _driverLocationService = driverLocationService;
    }

    [Authorize(Roles = "Driver")]
    [HttpPost]
    public async Task<IActionResult> CreateDriverProfile(
        CreateDriverProfileRequest request)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var driverId =
            await _driverProfileService.CreateDriverProfileAsync(
                userId,
                request.LicenseNumber,
                request.VehicleMake,
                request.VehicleModel,
                request.VehiclePlateNumber
            );

        var driver =
            await _driverProfileService.GetDriverProfileByIdAsync(
                driverId);

        return CreatedAtAction(
            nameof(GetDriverProfile),
            new { id = driverId },
            driver
        );
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDriverProfile(Guid id)
    {
        var driver =
            await _driverProfileService.GetDriverProfileByIdAsync(id);

        if (driver is null)
            return NotFound();

        return Ok(driver);
    }

    [Authorize(Roles = "Driver")]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateDriverStatus(
        Guid id,
        [FromBody] string status)
    {
        if (!await IsDriverProfileOwnerAsync(id))
        {
            return Forbid();
        }

        try
        {
            var updated =
                await _driverProfileService.UpdateDriverStatusAsync(
                    id,
                    status);

            if (!updated)
                return NotFound();

            return Ok(new
            {
                message = $"Driver is now {status}"
            });
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
    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyDrivers(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusKm = 5)
    {
        try
        {
            var drivers = await _driverLocationService.FindNearbyDriversAsync(
                latitude,
                longitude,
                radiusKm);

            return Ok(drivers);
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
    [HttpPatch("{id:guid}/location")]
    public async Task<IActionResult> UpdateDriverLocation(
        Guid id,
        UpdateDriverLocationRequest request)
    {
        if (!await IsDriverProfileOwnerAsync(id))
        {
            return Forbid();
        }

        try
        {
            var updated =
                await _driverProfileService.UpdateDriverLocationAsync(
                    id,
                    request.Latitude,
                    request.Longitude);

            if (!updated)
                return NotFound();

            await _driverLocationService.SetDriverLocationAsync(
                id,
                request.Latitude,
                request.Longitude);

            return Ok(new
            {
                message = "Driver location updated successfully"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    private async Task<bool> IsDriverProfileOwnerAsync(Guid driverProfileId)
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return false;
        }

        var driverProfile =
            await _driverProfileService
                .GetDriverProfileByIdAsync(driverProfileId);

        return driverProfile?.UserId == userId;
    }
}

