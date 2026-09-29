using Application;
using Application.Repositories;
using Infrastructure.DataAccess;
using EventManager.Middlewares;
using Infrastructure.Repositories;
using Application.Services;
using Application.Services.Booking;
using Application.Services.Event;
using Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
services.AddHostedService<BookingProcessor>();
services.AddControllers();
services.AddInfrastructure(builder.Configuration);
services.AddApplication();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
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
