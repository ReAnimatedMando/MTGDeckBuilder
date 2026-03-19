using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTGDeckBuilder.Data;
using MTGDeckBuilder.Models.ViewModels;

namespace MTGDeckBuilder.Controllers
{
    public class CollectionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CollectionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Stats()
        {
            var ownedCards = await _context.OwnedCards
                .Include(oc => oc.Card)
                .Where(oc => oc.Card != null)
                .ToListAsync();

            var totalCardsOwned = ownedCards.Sum(oc => oc.Quantity);
            var uniqueCardsOwned = ownedCards.Count;

            var totalCollectionValue = ownedCards.Sum(oc =>
                oc.Quantity * (oc.Card!.PriceUsd ?? 0m));

            var averageCardValue = totalCardsOwned == 0
                ? 0m
                : totalCollectionValue / totalCardsOwned;

            var topOwnedCards = ownedCards
                .GroupBy(oc => new
                {
                    oc.CardId,
                    Name = oc.Card!.Name,
                    ImageUrl = oc.Card.ImageUrl,
                    PriceUsd = oc.Card.PriceUsd ?? 0m
                })
                .Select(g => new TopOwnedCardViewModel
                {
                    CardName = g.Key.Name,
                    Quantity = g.Sum(x => x.Quantity),
                    TotalValue = g.Sum(x => x.Quantity) * g.Key.PriceUsd,
                    ImageUrl = g.Key.ImageUrl
                })
                .OrderByDescending(x => x.PriceUsd)
                .ThenByDescending(x => x.TotalValue)
                .ThenBy(x => x.CardName)
                .Take(10)
                .ToList();

            var colorCounts = new Dictionary<string, int>
            {
                ["W"] = 0,
                ["U"] = 0,
                ["B"] = 0,
                ["R"] = 0,
                ["G"] = 0,
                ["Colorless"] = 0
            };

            var colorLabelMap = new Dictionary<string, string>
            {
                ["W"] = "White",
                ["U"] = "Blue",
                ["B"] = "Black",
                ["R"] = "Red",
                ["G"] = "Green",
                ["Colorless"] = "Colorless"
            };

            foreach (var oc in ownedCards)
            {
                var qty = oc.Quantity;
                var colorIdentity = oc.Card?.ColorIdentity;

                if (string.IsNullOrWhiteSpace(colorIdentity))
                {
                    colorCounts["Colorless"] += qty;
                    continue;
                }

                var colors = colorIdentity
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (colors.Length == 0)
                {
                    colorCounts["Colorless"] += qty;
                    continue;
                }

                foreach (var color in colors)
                {
                    if (colorCounts.ContainsKey(color))
                    {
                        colorCounts[color] += qty;
                    }
                }
            }

            var colorDistribution = colorCounts
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => new StatItemViewModel
                {
                    Label = colorLabelMap.ContainsKey(kvp.Key) ? colorLabelMap[kvp.Key] : kvp.Key,
                    Count = kvp.Value
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var typeCounts = new Dictionary<string, int>
            {
                ["Creature"] = 0,
                ["Instant"] = 0,
                ["Sorcery"] = 0,
                ["Artifact"] = 0,
                ["Enchantment"] = 0,
                ["Planeswalker"] = 0,
                ["Land"] = 0,
                ["Battle"] = 0,
                ["Other"] = 0
            };

            foreach (var oc in ownedCards)
            {
                var qty = oc.Quantity;
                var typeLine = oc.Card?.TypeLine ?? "";

                bool matched = false;

                foreach (var key in typeCounts.Keys.Where(k => k != "Other").ToList())
                {
                    if (typeLine.Contains(key, StringComparison.OrdinalIgnoreCase))
                    {
                        typeCounts[key] += qty;
                        matched = true;
                    }
                }

                if (!matched)
                {
                    typeCounts["Other"] += qty;
                }
            }

            var typeDistribution = typeCounts
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => new StatItemViewModel
                {
                    Label = kvp.Key,
                    Count = kvp.Value
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var model = new CollectionStatsViewModel
            {
                TotalCardsOwned = totalCardsOwned,
                UniqueCardsOwned = uniqueCardsOwned,
                TotalCollectionValue = totalCollectionValue,
                AverageCardValue = averageCardValue,
                TopOwnedCards = topOwnedCards,
                ColorDistribution = colorDistribution,
                TypeDistribution = typeDistribution
            };

            return View(model);
        }
    }
}
