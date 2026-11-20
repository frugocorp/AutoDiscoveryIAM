using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.PasswordManagement;

public interface IPasswordResetTokenService
{
    string GenerateToken();
}

public sealed class PasswordResetTokenService(IOptions<PasswordResetSettings> options) : IPasswordResetTokenService
{
    private readonly PasswordResetSettings _settings = options.Value;

    public string GenerateToken()
    {
        var targetLength = Math.Max(_settings.TokenLength, 32);
        var byteLength = (int)Math.Ceiling(targetLength * 0.75);
        var buffer = RandomNumberGenerator.GetBytes(byteLength);
        var token = Convert.ToBase64String(buffer)
          .Replace("+", "-")
          .Replace("/", "_")
          .Replace("=", string.Empty);

        if (token.Length < targetLength)
        {
            return token.PadRight(targetLength, 'x');
        }

        return token[..targetLength];
    }
}
