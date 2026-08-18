using System.Net;
using EventManager.Code;
using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Services.Booking;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Controllers;

[ApiController]
[Route("bookings")]
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
        var result = await _bookingService.GetBookingByIdAsync(id);
        if (result != null)
        {
            return new ApiResult<BookingModel>()
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = $"Бронь {result.Id}, статус {result.Status}",
                Data =  result
            };
        }
        {
            throw new NotFoundException("Бронь не найдена");
        }
    }
}