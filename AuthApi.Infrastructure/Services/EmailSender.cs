using System.Net;
using System.Net.Mail;
using AuthApi.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuthApi.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration configuration, IHostEnvironment environment, ILogger<EmailSender> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.LogInformation("SMTP chưa cấu hình. Email tới {To} chủ đề {Subject} được ghi log (không trả token ra API).", toEmail, subject);
            if (_environment.IsDevelopment())
            {
                _logger.LogDebug("Email body: {Body}", htmlBody);
            }

            return;
        }

        var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;
        var from = _configuration["Smtp:From"] ?? "noreply@company.com";
        var user = _configuration["Smtp:User"];
        var password = _configuration["Smtp:Password"];

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = string.IsNullOrWhiteSpace(user)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(user, password)
        };

        using var message = new MailMessage(from, toEmail, subject, htmlBody) { IsBodyHtml = true };
        await client.SendMailAsync(message, cancellationToken);
    }
}
