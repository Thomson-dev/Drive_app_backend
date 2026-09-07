using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("rides/{rideId:guid}/offers")]
public class DriverOfferController : ControllerBase
{
    private readonly DriverOfferService _driverOfferService;

    public DriverOfferController(DriverOfferService driverOfferService)
    {
        _driverOfferService = driverOfferService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOffer(
        Guid rideId,
        CreateDriverOfferRequest request)
    {
        var offerId = await _driverOfferService.CreateOfferAsync(
            rideId,
            request.DriverId,
            request.OfferedFare
        );

        return Ok(new
        {
            id = offerId
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetOffers(Guid rideId)
    {
        var offers = await _driverOfferService.GetOffersByRideIdAsync(rideId);

        return Ok(offers);
    }

    [HttpPatch("{offerId:guid}/accept")]
    public async Task<IActionResult> AcceptOffer(
        Guid rideId,
        Guid offerId)
    {
        var accepted = await _driverOfferService.AcceptOfferAsync(
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
