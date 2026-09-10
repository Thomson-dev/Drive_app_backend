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

    [HttpPost]
    public async Task<IActionResult> CreateDriverProfile(
        CreateDriverProfileRequest request)
    {
        var driverId =
            await _driverProfileService.CreateDriverProfileAsync(
                request.UserId,
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDriverProfile(Guid id)
    {
        var driver =
            await _driverProfileService.GetDriverProfileByIdAsync(id);

        if (driver is null)
            return NotFound();

        return Ok(driver);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateDriverStatus(
        Guid id,
        [FromBody] string status)
    {
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

    [HttpPatch("{id:guid}/location")]
    public async Task<IActionResult> UpdateDriverLocation(
        Guid id,
        UpdateDriverLocationRequest request)
    {
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
}

