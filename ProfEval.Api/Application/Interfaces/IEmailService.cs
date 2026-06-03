using System.Threading.Tasks;

namespace ProfEval.Api.Application.Interfaces;

public interface IEmailService
{
    Task SendVerificationCodeAsync(string email, string code);
}
