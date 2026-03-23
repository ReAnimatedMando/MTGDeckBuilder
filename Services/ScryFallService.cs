using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MTGDeckBuilder.Data;
using MTGDeckBuilder.Models;
using MTGDeckBuilder.Models.Scryfall;

namespace MTGDeckBuilder.Services;

public class ScryfallService
{
    private readonly HttpClient _http;
    private readonly ApplicationDbContext _context;

    public ScryfallService(HttpClient http, ApplicationDbContext context)
    {
        _http = http;
        _context = context;

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

    public async Task<List<Card>> SearchAndSyncCardsAsync(string query, int maxResults = 40)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<Card>();
        }

        query = query.Trim();

        try
        {
            var scryfallCards = await SearchCardsAsync(query);

            foreach (var scryfallCard in scryfallCards.Take(maxResults))
            {
                if (string.IsNullOrWhiteSpace(scryfallCard.Name))
                {
                    continue;
                }

                var existingCard = await _context.Cards
                    .FirstOrDefaultAsync(c => c.Name == scryfallCard.Name);

                if (existingCard == null)
                {
                    var newCard = new Card
                    {
                        Name = scryfallCard.Name,
                        ManaCost = scryfallCard.ManaCost,
                        ManaValue = (int)scryfallCard.Cmc,
                        TypeLine = scryfallCard.TypeLine,
                        ColorIdentity = scryfallCard.ColorIdentity != null
                            ? string.Join(",", scryfallCard.ColorIdentity)
                            : "",
                        ImageUrl = scryfallCard.ImageUris?.Normal,
                        PriceUsd = decimal.TryParse(scryfallCard.Prices?.Usd, out var price)
                            ? price
                            : 0
                    };

                    _context.Cards.Add(newCard);
                }
                else
                {
                    var updated = false;

                    if (string.IsNullOrWhiteSpace(existingCard.ImageUrl) &&
                        !string.IsNullOrWhiteSpace(scryfallCard.ImageUris?.Normal))
                    {
                        existingCard.ImageUrl = scryfallCard.ImageUris.Normal;
                        updated = true;
                    }

                    if (string.IsNullOrWhiteSpace(existingCard.ManaCost) &&
                        !string.IsNullOrWhiteSpace(scryfallCard.ManaCost))
                    {
                        existingCard.ManaCost = scryfallCard.ManaCost;
                        updated = true;
                    }

                    if (string.IsNullOrWhiteSpace(existingCard.TypeLine) &&
                        !string.IsNullOrWhiteSpace(scryfallCard.TypeLine))
                    {
                        existingCard.TypeLine = scryfallCard.TypeLine;
                        updated = true;
                    }

                    if (string.IsNullOrWhiteSpace(existingCard.ColorIdentity) &&
                        scryfallCard.ColorIdentity != null)
                    {
                        existingCard.ColorIdentity = string.Join(",", scryfallCard.ColorIdentity);
                        updated = true;
                    }

                    if (existingCard.ManaValue == 0 && scryfallCard.Cmc > 0)
                    {
                        existingCard.ManaValue = (int)scryfallCard.Cmc;
                        updated = true;
                    }

                    var parsedPrice = decimal.TryParse(scryfallCard.Prices?.Usd, out var latestPrice)
                        ? latestPrice
                        : 0;

                    if (parsedPrice > 0 && existingCard.PriceUsd != latestPrice)
                    {
                        existingCard.PriceUsd = latestPrice;
                        updated = true;
                    }

                    if (updated)
                    {
                        Console.WriteLine($"Updated existing card: {existingCard.Name}");
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("SCRYFALL ERROR:");
            Console.WriteLine(ex.Message);
        }

        var term = $"%{query}%";

        return await _context.Cards
            .Where(c =>
                EF.Functions.Like(c.Name, term) ||
                (c.TypeLine != null && EF.Functions.Like(c.TypeLine, term)) ||
                (c.ManaCost != null && EF.Functions.Like(c.ManaCost, term)) ||
                (c.ColorIdentity != null && EF.Functions.Like(c.ColorIdentity, term)))
            .OrderBy(c => c.Name)
            .Take(maxResults)
            .ToListAsync();
    }
}