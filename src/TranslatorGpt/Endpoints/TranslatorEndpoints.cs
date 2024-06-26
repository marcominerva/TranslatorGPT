﻿using OperationResults.AspNetCore.Http;
using TranslatorGpt.BusinessLayer.Services.Interfaces;
using TranslatorGpt.Shared.Models;

namespace TranslatorGpt.Endpoints;

public class ChatEndpoints : IEndpointRouteHandlerBuilder
{
    public static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var translatorApiGroup = endpoints.MapGroup("/api/translate").RequireAuthorization();

        translatorApiGroup.MapPost(string.Empty, TranslateAsync)
            .WithName("Translate")
            .Produces<IEnumerable<TranslationResponse>>()
            .ProducesValidationProblem()
            .WithOpenApi();
    }

    public static async Task<IResult> TranslateAsync(TranslationRequest request, ITranslatorService translatorService, HttpContext httpContext)
    {
        var result = await translatorService.TranslateAsync(request);

        var response = httpContext.CreateResponse(result);
        return response;
    }
}