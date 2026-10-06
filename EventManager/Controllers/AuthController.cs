using System.Net;
using Application.DTOs;
using Application.Services.User;
using Domain.Models.Enums;
using EventManager.Code;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Controllers;

[ApiController]
[Route("auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;

    public AuthController(IUserService users)
    {
        _users = users;
    }

    [HttpPost("register")]
    public async Task<ApiResult<Guid>> Register([FromBody] RegisterUserDto dto)
    {
        var role = Roles.User;
        if (!string.IsNullOrWhiteSpace(dto.Role) && Enum.TryParse<Roles>(dto.Role, true, out var parsed))
            role = parsed;

        var user = await _users.RegisterAsync(dto.Login, dto.Password, role);
        return new ApiResult<Guid>
        {
            Success = true,
            StatusCode = HttpStatusCode.Created,
            Message = "Пользователь зарегистрирован",
            Data = user.Id
        };
    }

    [HttpPost("login")]
    public async Task<ApiResult<string>> Login([FromBody] LoginUserDto dto)
    {
        var token = await _users.LoginAsync(dto.Login, dto.Password);
        return new ApiResult<string>
        {
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Вход выполнен",
            Data = token
        };
    }
}
