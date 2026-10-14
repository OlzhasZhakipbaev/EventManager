using System.Net;
using System.Security.Claims;
using Bookings.Api.Code;
using Bookings.Application.DTOs;
using Bookings.Application.Services;
using Bookings.Domain.Exceptions;
using Bookings.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Api.Controllers;

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

    [HttpPost]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ApiResult<BookingModel>> Create([FromBody] CreateBookingDto dto)
    {
        var result = await _bookingService.CreateBookingAsync(dto.EventId, ResolveUserId());
        Response.Headers.Location = $"/bookings/{result.Id}";

        return new ApiResult<BookingModel>
        {
            Success = true,
            StatusCode = HttpStatusCode.Accepted,
            Message = $"Номер брони {result.Id}. Событие c id {result.EventId} забронировано со статусом {result.Status}",
            Data = result
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<ApiResult<BookingModel>> GetBooking([FromRoute] Guid id)
    {
        var result = await _bookingService.GetBookingByIdAsync(id, ResolveUserId(), User.IsInRole("Admin"));
        if (result is null)
            throw new NotFoundException("Бронь не найдена");

        return new ApiResult<BookingModel>
        {
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = $"Бронь {result.Id}, статус {result.Status}",
            Data = result
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> CancelBooking([FromRoute] Guid id)
    {
        await _bookingService.CancelBookingAsync(id, ResolveUserId(), User.IsInRole("Admin"));
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
