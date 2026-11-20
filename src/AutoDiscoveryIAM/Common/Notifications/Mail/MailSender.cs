// -----------------------------------------------------------------------
// <copyright file="MailSender.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Mail;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Notifications.Mail;

public sealed class MailSender(
  IOptions<MailSettings> mailOptions,
  ILogger<MailSender> logger) : IMailSender
{
  private static readonly Action<ILogger, Exception?> LogSmtpEmailSent =
    LoggerMessage.Define(LogLevel.Information, new EventId(1, nameof(LogSmtpEmailSent)), "Correo SMTP enviado correctamente.");

  private static readonly Action<ILogger, Exception?> LogSmtpEmailError =
    LoggerMessage.Define(LogLevel.Error, new EventId(2, nameof(LogSmtpEmailError)), "Error enviando correo SMTP.");

  private static readonly Action<ILogger, Exception?> LogSmtpEmailFaked =
    LoggerMessage.Define(LogLevel.Information, new EventId(3, nameof(LogSmtpEmailFaked)), "Envio SMTP omitido por bandera fakeMail=true.");

  private readonly MailSettings _mailSettings = mailOptions.Value;
  private readonly ILogger<MailSender> _logger = logger;

  public async Task<OperationResult<bool>> SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken, bool fakeMail = false)
  {
    try
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (fakeMail)
      {
        LogSmtpEmailFaked(_logger, null);
        return OperationResult.Ok(true);
      }

      if (!TryValidateSmtpSettings(out var fromEmail, out var host, out var smtpPort, out var validationError))
      {
        return OperationResult.Fail<bool>(ErrorInfo.Status400Validation, validationError!);
      }

      using var mailMessage = BuildMailMessage(recipient, fromEmail!, subject, htmlBody);
      using var smtpClient = BuildSmtpClient(host!, smtpPort);

      await smtpClient.SendMailAsync(mailMessage, cancellationToken);

      LogSmtpEmailSent(_logger, null);
      return OperationResult.Ok(true);
    }
    catch (Exception ex)
    {
      LogSmtpEmailError(_logger, ex);
      return OperationResult.Fail<bool>(ErrorInfo.Status424Dependency, "No fue posible enviar el correo.");
    }
  }

  private bool TryValidateSmtpSettings(out string? fromEmail, out string? host, out int smtpPort, out string? error)
  {
    fromEmail = _mailSettings.From?.Email?.Trim();
    if (string.IsNullOrWhiteSpace(fromEmail))
    {
      host = null;
      smtpPort = 0;
      error = "Mail:From:Email es requerido.";
      return false;
    }

    host = _mailSettings.Smtp?.Host?.Trim();
    if (string.IsNullOrWhiteSpace(host))
    {
      smtpPort = 0;
      error = "Mail:Smtp:Host es requerido.";
      return false;
    }

    var smtpPortRaw = _mailSettings.Smtp?.Port;
    if (!int.TryParse(smtpPortRaw, out smtpPort) || smtpPort <= 0)
    {
      error = "Mail:Smtp:Port es invalido.";
      return false;
    }

    error = null;
    return true;
  }

  private MailMessage BuildMailMessage(string to, string fromEmail, string subject, string htmlBody)
  {
    var message = new MailMessage
    {
      From = new MailAddress(fromEmail, _mailSettings.From?.Name ?? string.Empty),
      Subject = subject,
      Body = htmlBody,
      IsBodyHtml = true,
    };

    message.To.Add(to);

    return message;
  }

  private SmtpClient BuildSmtpClient(string host, int smtpPort)
    => new(host, smtpPort)
    {
      EnableSsl = _mailSettings.Smtp.UseSsl,
      DeliveryMethod = SmtpDeliveryMethod.Network,
      UseDefaultCredentials = false,
      Credentials = new NetworkCredential(
        _mailSettings.Smtp.User?.Trim() ?? string.Empty,
        _mailSettings.Smtp.Password ?? string.Empty),
    };
}
