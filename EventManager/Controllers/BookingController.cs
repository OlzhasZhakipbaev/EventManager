using System.Net;
using System.Security.Claims;
using EventManager.Code;
using Domain.Exceptions;
using Domain.Models;
using Domain.Models.Enums;
using Application.Services.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Controllers;

[ApiController]
[Route("bookings")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("{id:guid}")]
    public async Task<ApiResult<BookingModel>> GetBooking([FromRoute] Guid id)
    {
        var result = await _bookingService.GetBookingByIdAsync(
            id,
            ResolveUserId(),
            User.IsInRole(nameof(Roles.Admin)));
        if (result != null)
        {
            return new ApiResult<BookingModel>
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = $"Бронь {result.Id}, статус {result.Status}",
                Data = result
            };
        }

        throw new NotFoundException("Бронь не найдена");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> CancelBooking([FromRoute] Guid id)
    {
        await _bookingService.CancelBookingAsync(
            id,
            ResolveUserId(),
            User.IsInRole(nameof(Roles.Admin)));

        return NoContent();
    }

    private Guid ResolveUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var userId))
            throw new AccessDeniedException(Guid.Empty, "bookings");
        return userId;
    }
}
