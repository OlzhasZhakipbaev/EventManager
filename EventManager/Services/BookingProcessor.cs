using EventManager.Services.Booking;

namespace EventManager.Services;

public class BookingProcessor : BackgroundService
{
    private readonly IBookingService _bookingService;
    
    public BookingProcessor(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _bookingService.ProcessPendingAsync(stoppingToken);
            await Task.Delay(2000, stoppingToken);
        }
    }
}

