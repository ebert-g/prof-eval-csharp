using ProfEval.Api.Application.DTOs;

namespace ProfEval.Api.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RequestVerificationCodeAsync(string email, string name);
    Task<AuthResponseDto> VerifyCodeAsync(string email, string code);
    Task<StudentResponseDto?> GetStudentByEmailAsync(string email);
    Task<AuthResponseDto> PasswordlessLoginAsync(string email);
}
