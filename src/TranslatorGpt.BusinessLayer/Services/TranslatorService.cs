﻿using System.Text.Json;
using System.Text.Json.Serialization;
using ChatGptNet;
using OperationResults;
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

            If you think there is only one acceptable translation, returns a JSON array with a single element. If you think there are multiple acceptable translations, provides each translation in the array.
            You can use the "description" property to provide comments on the corresponding translation.
            """;

        var translationRequestmessage = $"""
            Translate "{request.Text}" to "{request.Language}"
            """;

        var conversationId = await chatGptClient.SetupAsync(setupMessage);
        var response = await chatGptClient.AskAsync(conversationId, translationRequestmessage);

        var translations = JsonSerializer.Deserialize<List<TranslationResponse>>(response.GetMessage(), jsonSerializerOptions);
        return translations;
    }
}