﻿namespace TranslatorGpt.Shared.Models;

public record class TranslationResponse(string Text, string Description)
{
    public string Description { get; set; } = Description;
}
