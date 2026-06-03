using ProfEval.Api.Infrastructure.Persistence;
using ProfEval.Api.Application.DTOs;
using ProfEval.Api.Domain.Entities;
using ProfEval.Api.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ProfEval.Api.Application.Services;

public class AuthService : IAuthService
{
    private readonly ProfEvalDbContext _context;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;

    private const string UCSAL_DOMAIN = "@ucsal.edu.br";
    private const int CODE_EXPIRATION_MINUTES = 15;

    public AuthService(ProfEvalDbContext context, ILogger<AuthService> logger, IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<AuthResponseDto> RequestVerificationCodeAsync(string email, string name)
    {
        // Validar email UCSAL
        if (!email.EndsWith(UCSAL_DOMAIN, StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Apenas emails com domínio {UCSAL_DOMAIN} são permitidos."
            };
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "Nome é obrigatório."
            };
        }

        try
        {
            // Gerar código de 6 dígitos
            var code = GenerateVerificationCode();

            // Remover códigos antigos não usados do mesmo email
            var oldCodes = await _context.Set<VerificationCode>()
                .Where(v => v.Email == email && !v.IsUsed)
                .ToListAsync();

            _context.Set<VerificationCode>().RemoveRange(oldCodes);

            // Criar novo código
            var verificationCode = new VerificationCode
            {
                Email = email,
                Code = code,
                ExpiresAt = DateTime.UtcNow.AddMinutes(CODE_EXPIRATION_MINUTES),
                IsUsed = false
            };

            _context.Set<VerificationCode>().Add(verificationCode);
            await _context.SaveChangesAsync();

            // Envia o e-mail real ou mockado
            await _emailService.SendVerificationCodeAsync(email, code);

            return new AuthResponseDto
            {
                Success = true,
                Message = $"Código de verificação enviado para {email}. Válido por {CODE_EXPIRATION_MINUTES} minutos. [CÓDIGO: {code}]"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao solicitar código de verificação para {email}");
            return new AuthResponseDto
            {
                Success = false,
                Message = "Erro ao gerar código de verificação. Tente novamente."
            };
        }
    }

    public async Task<AuthResponseDto> VerifyCodeAsync(string email, string code)
    {
        if (!email.EndsWith(UCSAL_DOMAIN, StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Email deve ser do domínio {UCSAL_DOMAIN}."
            };
        }

        try
        {
            // Buscar código válido
            var verificationCode = await _context.Set<VerificationCode>()
                .FirstOrDefaultAsync(v =>
                    v.Email == email &&
                    v.Code == code &&
                    !v.IsUsed &&
                    v.ExpiresAt > DateTime.UtcNow);

            if (verificationCode == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Código inválido ou expirado."
                };
            }

            // Marcar como usado
            verificationCode.IsUsed = true;
            verificationCode.VerifiedAt = DateTime.UtcNow;

            // Buscar ou criar estudante
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Email == email);

            if (student == null)
            {
                // Criar novo estudante
                student = new Student
                {
                    Email = email,
                    Name = "", // Será preenchido no frontend
                    EmailVerified = true,
                    VerifiedAt = DateTime.UtcNow,
                    Registration = $"EST{DateTime.UtcNow.Ticks}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Students.Add(student);
            }
            else
            {
                // Atualizar status de verificação
                student.EmailVerified = true;
                student.VerifiedAt = DateTime.UtcNow;
                _context.Students.Update(student);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Email verificado com sucesso: {email}");

            return new AuthResponseDto
            {
                Success = true,
                Message = "Email verificado com sucesso!",
                Student = MapToStudentDto(student)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao verificar código para {email}");
            return new AuthResponseDto
            {
                Success = false,
                Message = "Erro ao verificar código. Tente novamente."
            };
        }
    }

    public async Task<StudentResponseDto?> GetStudentByEmailAsync(string email)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Email == email && s.EmailVerified);

        return student != null ? MapToStudentDto(student) : null;
    }

    public async Task<AuthResponseDto> PasswordlessLoginAsync(string email)
    {
        if (!email.EndsWith(UCSAL_DOMAIN, StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = $"Apenas emails com domínio {UCSAL_DOMAIN} são permitidos."
            };
        }

        try
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.Email == email);
            if (student == null)
            {
                // Derive a name from the email prefix (e.g. joao.silva@ucsal.edu.br -> Joao Silva)
                var prefix = email.Split('@')[0];
                var parts = prefix.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
                var name = string.Join(" ", parts.Select(p => char.ToUpper(p[0]) + p.Substring(1).ToLower()));

                student = new Student
                {
                    Email = email,
                    Name = string.IsNullOrWhiteSpace(name) ? "Aluno UCSAL" : name,
                    EmailVerified = true,
                    VerifiedAt = DateTime.UtcNow,
                    Registration = $"EST{DateTime.UtcNow.Ticks}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Students.Add(student);
            }
            else
            {
                student.EmailVerified = true;
                student.VerifiedAt = DateTime.UtcNow;
                _context.Students.Update(student);
            }

            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login realizado com sucesso!",
                Student = MapToStudentDto(student)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro no login sem senha para {email}");
            return new AuthResponseDto
            {
                Success = false,
                Message = "Erro ao processar o login. Tente novamente."
            };
        }
    }

    private string GenerateVerificationCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private StudentResponseDto MapToStudentDto(Student student)
    {
        return new StudentResponseDto
        {
            Id = student.Id,
            Name = student.Name,
            Email = student.Email,
            Registration = student.Registration,
            EmailVerified = student.EmailVerified,
            CreatedAt = student.CreatedAt
        };
    }
}
