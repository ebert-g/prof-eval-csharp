using Microsoft.AspNetCore.Mvc;
using ProfEval.Api.Application.DTOs;
using ProfEval.Api.Application.Interfaces;

namespace ProfEval.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("request-code")]
    public async Task<ActionResult<AuthResponseDto>> RequestVerificationCode(RequestVerificationCodeDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = "Email e Nome são obrigatórios."
            });
        }

        var response = await _authService.RequestVerificationCodeAsync(request.Email, request.Name);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpPost("verify-code")]
    public async Task<ActionResult<AuthResponseDto>> VerifyCode(VerifyCodeDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = "Email e Código são obrigatórios."
            });
        }

        var response = await _authService.VerifyCodeAsync(request.Email, request.Code);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }

    [HttpGet("student/{email}")]
    public async Task<ActionResult<StudentResponseDto>> GetStudent(string email)
    {
        var student = await _authService.GetStudentByEmailAsync(email);

        if (student == null)
            return NotFound();

        return Ok(student);
    }

    [HttpPost("demo-code")]
    public async Task<ActionResult<AuthResponseDto>> DemoGetCode(RequestVerificationCodeDto request)
    {
        // Endpoint apenas para DESENVOLVIMENTO - mostra código no response
        if (!request.Email.EndsWith("@ucsal.edu.br", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = "Use um email @ucsal.edu.br para testar"
            });
        }

        var response = await _authService.RequestVerificationCodeAsync(request.Email, request.Name);

        if (response.Success)
        {
            // Em desenvolvimento, adicionar o código na mensagem (nunca fazer em produção!)
            response.Message += " | CÓDIGO DE TESTE (dev): Verifique o console do backend ou banco de dados.";
        }

        return Ok(response);
    }

    [HttpPost("login-passwordless")]
    public async Task<ActionResult<AuthResponseDto>> LoginPasswordless(RequestVerificationCodeDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new AuthResponseDto
            {
                Success = false,
                Message = "Email é obrigatório."
            });
        }

        var response = await _authService.PasswordlessLoginAsync(request.Email);

        if (!response.Success)
            return BadRequest(response);

        return Ok(response);
    }
}
