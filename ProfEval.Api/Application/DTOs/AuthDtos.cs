namespace ProfEval.Api.Application.DTOs;

public class RequestVerificationCodeDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class VerifyCodeDto
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class StudentResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Registration { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public StudentResponseDto? Student { get; set; }
    public string? Token { get; set; }
}
