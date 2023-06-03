using System.Web;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TranslatorGpt.BusinessLayer.Services.Interfaces;
using TranslatorGpt.Shared.Models;

namespace TranslatorGpt.Pages;

public class IndexModel : PageModel
{
    private readonly ITranslatorService translatorService;

    public IEnumerable<Language> Languages { get; set; }

    public IndexModel(ITranslatorService translatorService)
    {
        this.translatorService = translatorService;
    }

    public async Task OnGetAsync()
    {
        var text = HttpUtility.HtmlDecode(Resources.Pages.Index.InvalidSettingsErrorMessage);
        Console.WriteLine(text);
        var languagesResponse = await translatorService.GetLanguagesAsync();
        Languages = languagesResponse.Content;
    }
}
