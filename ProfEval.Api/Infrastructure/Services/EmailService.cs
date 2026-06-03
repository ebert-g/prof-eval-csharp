using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProfEval.Api.Application.Interfaces;

namespace ProfEval.Api.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(string email, string code)
    {
        var smtpSection = _configuration.GetSection("SmtpSettings");
        var server = smtpSection["Server"];

        if (string.IsNullOrWhiteSpace(server))
        {
            // Se SMTP não estiver configurado, loga o código no console para ambiente local/desenvolvimento
            _logger.LogWarning($"[MOCK EMAIL] Para: {email} | Código OTP Gerado: {code}");
            return;
        }

        try
        {
            var port = int.Parse(smtpSection["Port"] ?? "587");
            var senderName = smtpSection["SenderName"] ?? "Avaliação UCSAL";
            var senderEmail = smtpSection["SenderEmail"] ?? "noreply@ucsal.edu.br";
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];
            var enableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true");

            using (var client = new SmtpClient(server, port))
            {
                client.EnableSsl = enableSsl;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(username, password);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = "Código de Acesso - Avaliação UCSAL",
                    Body = $@"
                        <div style='font-family: sans-serif; padding: 24px; max-width: 500px; border: 1px solid #e2e8f0; border-radius: 12px; margin: 0 auto;'>
                            <div style='text-align: center; margin-bottom: 20px;'>
                                <h1 style='color: #002d62; font-size: 24px; margin: 0;'>Avaliação UCSAL</h1>
                                <span style='font-size: 12px; color: #64748b;'>Universidade Católica do Salvador</span>
                            </div>
                            <p style='color: #1e293b; font-size: 15px;'>Olá,</p>
                            <p style='color: #1e293b; font-size: 15px;'>Use o código de acesso abaixo para confirmar o seu login no Sistema de Avaliação de Professores:</p>
                            <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; padding: 18px; border-radius: 8px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #002d62; margin: 24px 0;'>
                                {code}
                            </div>
                            <p style='color: #64748b; font-size: 13px; margin-top: 24px;'>Este código é válido por 15 minutos. Não compartilhe este código com ninguém.</p>
                        </div>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"E-mail de verificação OTP enviado com sucesso para {email}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[SMTP ERROR] Falha ao enviar e-mail OTP para {email}. Código temporário logado: {code}");
        }
    }
}
