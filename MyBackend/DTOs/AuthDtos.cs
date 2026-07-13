using System;
using System.ComponentModel.DataAnnotations;

namespace MyBackend.DTOs
{
    public record SignupDto(
        [Required, MinLength(3)] string Username,
        [Required, EmailAddress] string Email,
        [Required, MinLength(6)] string Password
    );

    public record LoginDto(
        [Required, EmailAddress] string Email,
        [Required] string Password
    );

    public record UserDto(
        int Id,
        string Username,
        string Email,
        DateTime CreatedAt
    );

    public record AuthResponseDto(
        string Token,
        UserDto User
    );
}
