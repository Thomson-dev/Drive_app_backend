using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("drivers")]
public class DriverProfileController : ControllerBase
{
    private readonly DriverProfileService _driverProfileService;

    public DriverProfileController(
        DriverProfileService driverProfileService)
    {
        _driverProfileService = driverProfileService;
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
        var driverId = await GetAuthenticatedDriverIdAsync();
        if (driverId is null)
        {
            return Unauthorized();
        }

        if (driverId.Value != id)
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

    [Authorize(Roles = "Driver")]
    [HttpPatch("{id:guid}/location")]
    public async Task<IActionResult> UpdateDriverLocation(
        Guid id,
        UpdateDriverLocationRequest request)
    {
        var driverId = await GetAuthenticatedDriverIdAsync();
        if (driverId is null)
        {
            return Unauthorized();
        }

        if (driverId.Value != id)
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

    private async Task<Guid?> GetAuthenticatedDriverIdAsync()
    {
        var userIdClaim =
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return null;
        }

        var driver =
            await _driverProfileService
                .GetDriverProfileByUserIdAsync(userId);

        return driver?.Id;
    }
}

