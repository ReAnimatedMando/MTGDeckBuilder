using System;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MTGDeckBuilder.Models;

public class MissingCardInfo
{
    public int CardId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public int RequiredQuantity { get; set; }
    public int OwnedQuantity { get; set; }
    public int MissingQuantity { get; set; }
}
