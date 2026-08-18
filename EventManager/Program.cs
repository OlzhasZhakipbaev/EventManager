using EventManager.Middlewares;
using EventManager.Services;
using EventManager.Services.Booking;
using EventManager.Services.Event;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
services.AddHostedService<BookingProcessor>();
services.AddControllers();
services.AddSingleton<IEventService, EventService>();
services.AddSingleton<IBookingService, BookingService>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.UseHttpsRedirection();

app.Run();
