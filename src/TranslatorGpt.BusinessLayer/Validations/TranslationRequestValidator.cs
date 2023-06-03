using FluentValidation;
using TranslatorGpt.Shared.Models;

namespace TranslatorGpt.BusinessLayer.Validations;

public class TranslationRequestValidator : AbstractValidator<TranslationRequest>
{
    public TranslationRequestValidator()
    {
        RuleFor(r => r.Text).NotEmpty();
        RuleFor(r => r.Language).NotEmpty().MaximumLength(7);
    }
}
