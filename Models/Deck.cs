using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MTGDeckBuilder.Models;

public class Deck
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    public string ColorIdentity { get; set; }

    // Navigation
    public ICollection<DeckCard> DeckCards { get; set; }
}
