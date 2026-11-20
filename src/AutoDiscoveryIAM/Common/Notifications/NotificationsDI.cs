// -----------------------------------------------------------------------
// <copyright file="NotificationsDI.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Notifications.Mail;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Notifications;

/// <summary>
/// Registro de dependencias para los servicios comunes de notificaciones (Mail, y futuros canales
/// como WhatsApp/Telegram/SMS cuando se implementen como carpetas hermanas de Mail).
/// </summary>
public static class NotificationsDI
{
  public static IServiceCollection AddNotificationsDI(this IServiceCollection services)
  {
    services.AddScoped<IMailSender, MailSender>();

    return services;
  }
}
