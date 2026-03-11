using System;
using System.Text.Json.Serialization;

namespace MTGDeckBuilder.Models.Scryfall;

public class ScryfallSearchDto
{
    [JsonPropertyName("data")]
    public List<ScryfallCardDto> Data { get; set; } = new();
}
