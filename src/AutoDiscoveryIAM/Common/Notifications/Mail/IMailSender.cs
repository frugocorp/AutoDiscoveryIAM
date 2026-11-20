// -----------------------------------------------------------------------
// <copyright file="IMailSender.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Notifications.Mail;

/// <summary>
/// Servicio común de envío de correo, reutilizable por cualquier feature del proyecto.
/// </summary>
public interface IMailSender
{
  Task<OperationResult<bool>> SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken, bool fakeMail = false);
}
