﻿using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ChatGptNet;
using OperationResults;
using TinyHelpers.Extensions;
using TranslatorGpt.BusinessLayer.Services.Interfaces;
using TranslatorGpt.Shared.Models;

namespace TranslatorGpt.BusinessLayer.Services;

public class TranslatorService : ITranslatorService
{
    private readonly IChatGptClient chatGptClient;

    private static readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    public TranslatorService(IChatGptClient chatGptClient)
    {
        this.chatGptClient = chatGptClient;
    }

    public async Task<Result<IEnumerable<TranslationResponse>>> TranslateAsync(TranslationRequest request)
    {
        var setupMessage = """
            You are an assistant that translates sentences from a language to another. Give the response in JSON format using the following schema:

            [{
                "text": null,
                "description": null
            }]

            Do not return anything besides the JSON. If you think there is only one acceptable translation, returns a JSON array with a single element. If you think there are multiple acceptable translations, provides each translation in the array.
            If the destination language is a neutral culture, try to provide the translation also for specific culture. For example, if the user requests the translation to English, provide alternatives for English (US) and English (UK) and all other English variants.
            You can use the "description" property to provide comments on the corresponding translation. Always provide a description for each translation when there are more than one translation. If you're unable to determine the language to use for the "description" property, use the English language.
            """;

        /*
            You can use the "description" property to provide comments on the corresponding translation. The description must be provided in the same language of the text to translate. For example, if the text to translate is in Italian, the description must be in Italian.
            You can use the "description" property to provide comments on the corresponding translation. The description must be provided in the same language of the user's message.
            You can use the "description" property to provide comments on the corresponding translation, using the original language of the user's message. 
         */

        var translationRequestMessage = $"""
            Translate "{request.Text}" to {CultureInfo.GetCultureInfo(request.Language).EnglishName}. The description must be in the same language of "{request.Text}".
            """;

        if (request.Context.HasValue())
        {
            translationRequestMessage = $"""
                {translationRequestMessage}

                Use the following context information to instruct the translator: 
                {request.Context}
                """;
        }

        var conversationId = await chatGptClient.SetupAsync(setupMessage);
        var response = await chatGptClient.AskAsync(conversationId, translationRequestMessage);

        var translations = JsonSerializer.Deserialize<List<TranslationResponse>>(response.GetMessage(), jsonSerializerOptions);
        return translations;
    }

    public Task<Result<IEnumerable<Language>>> GetLanguagesAsync()
    {
        var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
        var languages = cultures.Where(c => c.IsNeutralCulture).Select(c => new Language(c.IetfLanguageTag, c.EnglishName))
            .OrderBy(l => l.Name);

        var result = Result<IEnumerable<Language>>.Ok(languages);
        return Task.FromResult(result);
    }
}
