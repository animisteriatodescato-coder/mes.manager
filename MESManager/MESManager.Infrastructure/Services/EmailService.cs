using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MESManager.Application.Interfaces;

namespace MESManager.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public bool IsConfigured
    {
        get
        {
            var host = _config["SmtpSettings:Host"];
            var port = _config["SmtpSettings:Port"];
            var user = _config["SmtpSettings:Username"];
            var pass = _config["SmtpSettings:Password"];
            return !string.IsNullOrWhiteSpace(host)
                && !string.IsNullOrWhiteSpace(port)
                && !string.IsNullOrWhiteSpace(user)
                && !string.IsNullOrWhiteSpace(pass);
        }
    }

    public async Task<bool> InviaPreventivoPdfAsync(
        string destinatario,
        string htmlContent,
        string clienteName,
        int numeroPreventivo,
        CancellationToken ct = default)
    {
        var host = _config["SmtpSettings:Host"] ?? throw new InvalidOperationException("SmtpSettings:Host non configurato.");
        var port = int.TryParse(_config["SmtpSettings:Port"], out var p) ? p : 587;
        var user = _config["SmtpSettings:Username"] ?? "";
        var pass = _config["SmtpSettings:Password"] ?? "";
        var from = _config["SmtpSettings:FromAddress"] ?? user;
        var fromName = _config["SmtpSettings:FromName"] ?? "Animisteria Todescato";

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        var subject = numeroPreventivo > 0
            ? $"Preventivo N° {numeroPreventivo} – {clienteName}"
            : $"Preventivo – {clienteName}";

        using var msg = new MailMessage
        {
            From = new MailAddress(from, fromName),
            Subject = subject,
            Body = htmlContent,
            IsBodyHtml = true
        };
        msg.To.Add(destinatario);

        await client.SendMailAsync(msg, ct);
        _logger.LogInformation("[EMAIL] Preventivo N°{Num} inviato a {Dest}", numeroPreventivo, destinatario);
        return true;
    }
}
