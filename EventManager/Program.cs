using EventManager.DataAccess;
using EventManager.Middlewares;
using EventManager.Services;
using EventManager.Services.Booking;
using EventManager.Services.Event;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
services.AddHostedService<BookingProcessor>();
services.AddControllers();
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
services.AddScoped<IEventService, EventService>();
services.AddScoped<IBookingService, BookingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.UseHttpsRedirection();

app.Run();
