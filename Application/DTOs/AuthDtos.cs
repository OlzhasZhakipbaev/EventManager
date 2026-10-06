namespace Application.DTOs;

public class RegisterUserDto
{
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
    public string? Role { get; set; }
}

public class LoginUserDto
{
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
}
