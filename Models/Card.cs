using System;
using System.ComponentModel.DataAnnotations;

namespace MTGDeckBuilder.Models;

public class Card
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    public string? ManaCost { get; set; }

    public string? TypeLine { get; set; }

    public string? ColorIdentity { get; set; }

    public string? ImageUrl { get; set; }

    // Navigation
    public ICollection<DeckCard> DeckCards { get; set; } = new List<DeckCard>();
    public ICollection<OwnedCard> OwnedCards { get; set; } = new List<OwnedCard>();
}
