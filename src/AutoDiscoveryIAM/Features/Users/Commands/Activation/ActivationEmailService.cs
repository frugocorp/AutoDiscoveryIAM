using System.Net;
using System.Text;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Notifications.Mail;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.Extensions.Configuration;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;

public interface IActivationEmailService
{
  Task<OperationResult<bool>> SendActivationEmailAsync(User user, string token, CancellationToken cancellationToken, bool fakeMail = false);
  Task<OperationResult<bool>> SendAccountActivatedEmailAsync(User user, CancellationToken cancellationToken, bool fakeMail = false);
  Task<OperationResult<bool>> SendTestEmailAsync(string? subject, string? body, CancellationToken cancellationToken, bool fakeMail = false);

  /// <summary>
  /// Aviso al dueño de un correo ya registrado de que alguien intentó crear otra cuenta con él. El
  /// registro responde igual que si el correo fuera nuevo (no delata qué correos existen), así que
  /// este aviso es lo único que distingue los dos casos, y solo lo ve el dueño del correo.
  /// </summary>
  Task<OperationResult<bool>> SendRegistrationAttemptNoticeAsync(string email, CancellationToken cancellationToken, bool fakeMail = false);
}

public sealed class ActivationEmailService(
  IMailSender mailSender,
  IConfiguration configuration) : IActivationEmailService
{
  private readonly IMailSender _mailSender = mailSender;
  private readonly IConfiguration _configuration = configuration;

  public Task<OperationResult<bool>> SendActivationEmailAsync(User user, string token, CancellationToken cancellationToken, bool fakeMail = false)
  {
    var activationLink = BuildActivationLink(token);
    const string subject = "Activa tu cuenta";
    var body = BuildActivationBody(user, activationLink);
    return _mailSender.SendAsync(user.Email, subject, body, cancellationToken, fakeMail);
  }

  public Task<OperationResult<bool>> SendAccountActivatedEmailAsync(User user, CancellationToken cancellationToken, bool fakeMail = false)
  {
    const string subject = "Tu cuenta ha sido activada";
    var body = BuildAccountActivatedBody(user);
    return _mailSender.SendAsync(user.Email, subject, body, cancellationToken, fakeMail);
  }

  public Task<OperationResult<bool>> SendTestEmailAsync(string? subject, string? body, CancellationToken cancellationToken, bool fakeMail = false)
  {
    var toEmail = _configuration["Mail:From:Email"]?.Trim() ?? string.Empty;

    var effectiveSubject = string.IsNullOrWhiteSpace(subject)
      ? "Prueba SMTP activacion"
      : subject.Trim();

    var effectiveBody = string.IsNullOrWhiteSpace(body)
      ? "Correo de prueba SMTP para validar conectividad de activacion de cuentas."
      : body;

    return _mailSender.SendAsync(toEmail, effectiveSubject, effectiveBody, cancellationToken, fakeMail);
  }

  public Task<OperationResult<bool>> SendRegistrationAttemptNoticeAsync(string email, CancellationToken cancellationToken, bool fakeMail = false)
  {
    const string subject = "Intento de registro con tu correo";
    var sb = new StringBuilder();
    sb.Append("<p>Hola,</p>");
    sb.Append("<p>Alguien intento crear una cuenta nueva con este correo, que ya esta registrado.</p>");
    sb.Append("<p>Si fuiste tu, ya tienes cuenta: inicia sesion o, si olvidaste tu contrasena, restablecela.</p>");
    sb.Append("<p>Si no fuiste tu, ignora este correo: no se hizo ningun cambio en tu cuenta.</p>");
    return _mailSender.SendAsync(email, subject, sb.ToString(), cancellationToken, fakeMail);
  }

  private string BuildActivationLink(string token)
  {
    var baseUrl = (_configuration["BaseUrl"] ?? string.Empty).Trim().TrimEnd('/');
    var encodedToken = Uri.EscapeDataString(token);

    if (string.IsNullOrWhiteSpace(baseUrl))
    {
      return $"/users/activation/confirm?token={encodedToken}";
    }

    return $"{baseUrl}/users/activation/confirm?token={encodedToken}";
  }

  private static string BuildAccountActivatedBody(User user)
  {
    var sb = new StringBuilder();
    sb.Append("<p>Hola ").Append(WebUtility.HtmlEncode(user.Username)).Append(",</p>");
    sb.Append("<p>Tu cuenta fue activada correctamente. Ya puedes iniciar sesion con tu correo y contrasena.</p>");
    sb.Append("<p>Si no reconoces esta actividad, contacta a soporte de inmediato.</p>");
    return sb.ToString();
  }

  private static string BuildActivationBody(User user, string activationLink)
  {
    var sb = new StringBuilder();
    sb.Append("<p>Hola ").Append(WebUtility.HtmlEncode(user.Username)).Append(",</p>");
    sb.Append("<p>Tu cuenta fue creada correctamente. Activa tu acceso con el siguiente enlace:</p>");
    sb.Append("<p><a href=\"").Append(WebUtility.HtmlEncode(activationLink)).Append("\">Activar cuenta</a></p>");
    sb.Append("<p>Si no solicitaste esta cuenta, ignora este correo.</p>");
    return sb.ToString();
  }
}
