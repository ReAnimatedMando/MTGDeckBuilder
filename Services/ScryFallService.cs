using System.Net.Http.Headers;
using System.Text.Json;
using MTGDeckBuilder.Models.Scryfall;

namespace MTGDeckBuilder.Services;

public class ScryfallService
{
    private readonly HttpClient _http;

    public ScryfallService(HttpClient http)
    {
        _http = http;

        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "MTGDeckBuilder/1.0 (learning project; contact: example@example.com)"
        );

        _http.DefaultRequestHeaders.Accept.Clear();
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json", 0.9)
        );
        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("*/*", 0.8)
        );
    }

    public async Task<ScryfallCardDto?> SearchCardAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var url = $"https://api.scryfall.com/cards/named?fuzzy={Uri.EscapeDataString(query)}";

        var json = await _http.GetStringAsync(url);

        return JsonSerializer.Deserialize<ScryfallCardDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<List<ScryfallCardDto>> SearchCardsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<ScryfallCardDto>();
        }

        var url = $"https://api.scryfall.com/cards/search?q={Uri.EscapeDataString(query)}";
        
        var json = await _http.GetStringAsync(url);

        var result = JsonSerializer.Deserialize<ScryfallSearchDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result?.Data ?? new List<ScryfallCardDto>();
    }
}
