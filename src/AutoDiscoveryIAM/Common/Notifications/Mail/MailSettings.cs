// -----------------------------------------------------------------------
// <copyright file="MailSettings.cs" company="FRUGO Corp.">
// Copyright (c) FRUGO Corp.
// </copyright>
// -----------------------------------------------------------------------

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Notifications.Mail;

public sealed class MailSettings
{
  public string Provider { get; set; } = "Smtp";
  public MailFromSettings From { get; set; } = new();
  public SmtpSettings Smtp { get; set; } = new();
}

public sealed class MailFromSettings
{
  public string Email { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
}

public sealed class SmtpSettings
{
  public string Host { get; set; } = string.Empty;
  public string Port { get; set; } = "587";
  public string User { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public bool UseSsl { get; set; } = true;
}
