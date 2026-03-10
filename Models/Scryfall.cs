using System;
using System.Text.Json.Serialization;

namespace MTGDeckBuilder.Models.Scryfall;

public class ScryfallCardDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("mana_cost")]
    public string? ManaCost { get; set; }

    [JsonPropertyName("cmc")]
    public decimal Cmc { get; set; }

    [JsonPropertyName("type_line")]
    public string? TypeLine { get; set; }

    [JsonPropertyName("color_identity")]
    public List<string>? ColorIdentity { get; set; }

    [JsonPropertyName("image_uris")]
    public ScryfallImageUrisDto? ImageUris { get; set; }

}

public class ScryfallImageUrisDto
{
    [JsonPropertyName("normal")]
    public string? Normal { get; set; }
}
