using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common;
using FrugoCorp.AspNetCore.AutoDiscoveryIAM.Common.Notifications.Mail;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.UnitTests.Common.Notifications.Mail;

/// <summary>
/// Pruebas unitarias para MailSender, servicio único de envío de correo (issue #066).
/// </summary>
public class MailSenderTests
{
  private readonly ILogger<MailSender> _logger = Substitute.For<ILogger<MailSender>>();

  private MailSender CreateSut(MailSettings settings)
  {
    var options = Substitute.For<IOptions<MailSettings>>();
    options.Value.Returns(settings);
    return new MailSender(options, _logger);
  }

  private static MailSettings ValidSettings() => new()
  {
    From = new MailFromSettings { Email = "dev@autodiscoveryiam.com", Name = "AutoDiscoveryIAM Dev" },
    Smtp = new SmtpSettings { Host = "smtp.test.local", Port = "587", User = "dev@autodiscoveryiam.com", Password = "secret", UseSsl = true },
  };

  [Fact(DisplayName = "SendAsync con fakeMail=true no intenta enviar y retorna Ok")]
  public async Task SendAsync_FakeMail_ReturnsOkWithoutSending()
  {
    var sut = CreateSut(ValidSettings());

    var result = await sut.SendAsync("to@example.com", "Asunto", "<p>Cuerpo</p>", CancellationToken.None, fakeMail: true);

    result.Success.Should().BeTrue();
    result.Data.Should().BeTrue();
  }

  [Fact(DisplayName = "SendAsync falla con Status400Validation cuando falta Mail:From:Email")]
  public async Task SendAsync_MissingFromEmail_ReturnsValidationError()
  {
    var settings = ValidSettings();
    settings.From.Email = string.Empty;
    var sut = CreateSut(settings);

    var result = await sut.SendAsync("to@example.com", "Asunto", "<p>Cuerpo</p>", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status400Validation);
    result.Error.Should().Be("Mail:From:Email es requerido.");
  }

  [Fact(DisplayName = "SendAsync falla con Status400Validation cuando falta Mail:Smtp:Host")]
  public async Task SendAsync_MissingSmtpHost_ReturnsValidationError()
  {
    var settings = ValidSettings();
    settings.Smtp.Host = string.Empty;
    var sut = CreateSut(settings);

    var result = await sut.SendAsync("to@example.com", "Asunto", "<p>Cuerpo</p>", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status400Validation);
    result.Error.Should().Be("Mail:Smtp:Host es requerido.");
  }

  [Fact(DisplayName = "SendAsync falla con Status400Validation cuando Mail:Smtp:Port es invalido")]
  public async Task SendAsync_InvalidSmtpPort_ReturnsValidationError()
  {
    var settings = ValidSettings();
    settings.Smtp.Port = "not-a-port";
    var sut = CreateSut(settings);

    var result = await sut.SendAsync("to@example.com", "Asunto", "<p>Cuerpo</p>", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status400Validation);
    result.Error.Should().Be("Mail:Smtp:Port es invalido.");
  }

  [Fact(DisplayName = "SendAsync retorna Status424Dependency cuando el envio SMTP real falla")]
  public async Task SendAsync_SmtpSendFails_ReturnsDependencyError()
  {
    // Host valido en formato pero sin servidor real escuchando: SmtpClient.SendMailAsync debe fallar.
    var settings = ValidSettings();
    settings.Smtp.Host = "127.0.0.1";
    settings.Smtp.Port = "1";
    var sut = CreateSut(settings);

    var result = await sut.SendAsync("to@example.com", "Asunto", "<p>Cuerpo</p>", CancellationToken.None);

    result.Success.Should().BeFalse();
    result.ErrorInfo.Should().Be(ErrorInfo.Status424Dependency);
  }
}
