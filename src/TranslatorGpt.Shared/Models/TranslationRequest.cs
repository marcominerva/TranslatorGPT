namespace TranslatorGpt.Shared.Models;

public record class TranslationRequest(string Text, string Language, string Context);
