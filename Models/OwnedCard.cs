using System;

namespace MTGDeckBuilder.Models;

public class OwnedCard
{
    public int Id { get; set; }

    public int CardId { get; set; }
    public Card Card { get; set; }

    public int Quantity { get; set; }
}
