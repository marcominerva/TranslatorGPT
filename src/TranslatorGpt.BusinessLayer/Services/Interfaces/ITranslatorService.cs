using OperationResults;
using TranslatorGpt.Shared.Models;

namespace TranslatorGpt.BusinessLayer.Services.Interfaces;

public interface ITranslatorService
{
    Task<Result<IEnumerable<TranslationResponse>>> TranslateAsync(TranslationRequest request);

    Task<Result<IEnumerable<Language>>> GetLanguagesAsync();
}