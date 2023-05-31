using System.Security.Claims;
using SimpleAuthentication.ApiKey;
using TinyHelpers.Extensions;

namespace TranslatorGpt.Authentication;

public class ApiKeyValidator : IApiKeyValidator
{
    public Task<ApiKeyValidationResult> ValidateAsync(string bearer)
    {
        var apiKey = bearer?.ReplaceIgnoreCase("Bearer", string.Empty).Trim().GetValueOrDefault(string.Empty);
        var apiKeyValidationResult = ApiKeyValidationResult.Success("ChatGPT User", new List<Claim> { new("ApiKey", apiKey) });

        return Task.FromResult(apiKeyValidationResult);
    }
}
