// -----------------------------------------------------------------------
// <copyright file="TwoFactorEmailService.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using System.Text;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Notifications.Mail;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Models.Entities;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Auth.Commands.TwoFactor;

public interface ITwoFactorEmailService
{
  Task<OperationResult<bool>> SendCodeAsync(User user, string code, CancellationToken cancellationToken, bool fakeMail = false);
}

public sealed class TwoFactorEmailService(IMailSender mailSender) : ITwoFactorEmailService
{
  private readonly IMailSender _mailSender = mailSender;

  public Task<OperationResult<bool>> SendCodeAsync(User user, string code, CancellationToken cancellationToken, bool fakeMail = false)
  {
    const string subject = "Tu codigo de verificacion";
    var body = BuildBody(user, code);
    return _mailSender.SendAsync(user.Email, subject, body, cancellationToken, fakeMail);
  }

  private static string BuildBody(User user, string code)
  {
    var sb = new StringBuilder();
    sb.Append("<p>Hola ").Append(WebUtility.HtmlEncode(user.Username)).Append(",</p>");
    sb.Append("<p>Tu codigo de verificacion es:</p>");
    sb.Append("<p style=\"font-size:24px;font-weight:bold;\">").Append(WebUtility.HtmlEncode(code)).Append("</p>");
    sb.Append("<p>Si no intentaste iniciar sesion, ignora este correo.</p>");
    return sb.ToString();
  }
}
