using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace FrugoCorp.AspNetCore.AutoDiscoveryIAM.Features.Users.Commands.Activation;

public interface IActivationTokenService
{
    string GenerateToken();
}

public sealed class ActivationTokenService(IOptions<ActivationSettings> options) : IActivationTokenService
{
    private readonly ActivationSettings _settings = options.Value;

    public string GenerateToken()
    {
        var targetLength = Math.Max(_settings.EmailVerificationTokenLength, 32);
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
