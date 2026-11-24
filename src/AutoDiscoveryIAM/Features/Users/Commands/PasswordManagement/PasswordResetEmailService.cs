using System.Net;
using System.Text;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Notifications.Mail;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;
using Microsoft.Extensions.Configuration;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;

public interface IPasswordResetEmailService
{
  Task<OperationResult<bool>> SendPasswordResetEmailAsync(User user, string token, CancellationToken cancellationToken, bool fakeMail = false);

  /// <summary>
  /// Invitación a una cuenta creada por un administrador sin contraseña: el enlace (el mismo del
  /// restablecimiento) le permite al usuario definir la suya.
  /// </summary>
  Task<OperationResult<bool>> SendPasswordSetupEmailAsync(User user, string token, CancellationToken cancellationToken, bool fakeMail = false);
}

public sealed class PasswordResetEmailService(
  IMailSender mailSender,
  IConfiguration configuration) : IPasswordResetEmailService
{
  private readonly IMailSender _mailSender = mailSender;
  private readonly IConfiguration _configuration = configuration;

  public Task<OperationResult<bool>> SendPasswordResetEmailAsync(User user, string token, CancellationToken cancellationToken, bool fakeMail = false)
  {
    var resetLink = BuildResetLink(token);
    const string subject = "Restablece tu contrasena";
    var body = BuildResetBody(user, resetLink);
    return _mailSender.SendAsync(user.Email, subject, body, cancellationToken, fakeMail);
  }

  public Task<OperationResult<bool>> SendPasswordSetupEmailAsync(User user, string token, CancellationToken cancellationToken, bool fakeMail = false)
  {
    var setupLink = BuildResetLink(token);
    const string subject = "Define tu contrasena";
    var sb = new StringBuilder();
    sb.Append("<p>Hola ").Append(WebUtility.HtmlEncode(user.Username)).Append(",</p>");
    sb.Append("<p>Un administrador creo una cuenta para ti. Define tu contrasena con el siguiente enlace:</p>");
    sb.Append("<p><a href=\"").Append(WebUtility.HtmlEncode(setupLink)).Append("\">Definir contrasena</a></p>");
    sb.Append("<p>Si el enlace vencio, usa la opcion de restablecer contrasena con este mismo correo.</p>");
    return _mailSender.SendAsync(user.Email, subject, sb.ToString(), cancellationToken, fakeMail);
  }

  private string BuildResetLink(string token)
  {
    var baseUrl = (_configuration["BaseUrl"] ?? string.Empty).Trim().TrimEnd('/');
    var encodedToken = Uri.EscapeDataString(token);

    if (string.IsNullOrWhiteSpace(baseUrl))
    {
      return $"/users/password/reset/confirm?token={encodedToken}";
    }

    return $"{baseUrl}/users/password/reset/confirm?token={encodedToken}";
  }

  private static string BuildResetBody(User user, string resetLink)
  {
    var sb = new StringBuilder();
    sb.Append("<p>Hola ").Append(WebUtility.HtmlEncode(user.Username)).Append(",</p>");
    sb.Append("<p>Recibimos una solicitud para restablecer tu contrasena. Usa el siguiente enlace:</p>");
    sb.Append("<p><a href=\"").Append(WebUtility.HtmlEncode(resetLink)).Append("\">Restablecer contrasena</a></p>");
    sb.Append("<p>Si no solicitaste este cambio, ignora este correo.</p>");
    return sb.ToString();
  }
}
