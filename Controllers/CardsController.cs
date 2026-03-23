using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTGDeckBuilder.Data;
using MTGDeckBuilder.Models;
using MTGDeckBuilder.Services;

namespace MTGDeckBuilder.Controllers
{
    public class CardsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ScryfallService _scryfall;

        public CardsController(ApplicationDbContext context, ScryfallService scryfall)
        {
            _context = context;
            _scryfall = scryfall;
        }

        // GET: Cards
        public async Task<IActionResult> Index(string? q)
        {
            List<Card> cards;

            if (!string.IsNullOrWhiteSpace(q))
            {
                cards = await _scryfall.SearchAndSyncCardsAsync(q, 40);
            }
            else
            {
                cards = await _context.Cards
                    .OrderBy(c => c.Name)
                    .Take(200)
                    .ToListAsync();
            }

            ViewBag.Query = q;
            return View(cards);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RefreshPrices()
        {
            var cards = await _context.Cards.ToListAsync();

            int updatedCount = 0;

            foreach (var card in cards)
            {
                if (string.IsNullOrWhiteSpace(card.Name))
                    continue;

                try
                {
                    var scryfallCard = await _scryfall.SearchCardAsync(card.Name);

                    if (scryfallCard == null)
                        continue;

                    var parsedPrice = decimal.TryParse(scryfallCard.Prices?.Usd, out var latestPrice) ? latestPrice : 0;

                    if (parsedPrice > 0 && card.PriceUsd != latestPrice)
                    {
                        card.PriceUsd = latestPrice;
                        updatedCount++;
                    }

                    if (string.IsNullOrWhiteSpace(card.ImageUrl) && !string.IsNullOrWhiteSpace(scryfallCard.ImageUris?.Normal))
                    {
                        card.ImageUrl = scryfallCard.ImageUris.Normal;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to refresh price for {card.Name}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Checked {cards.Count} cards and refreshed prices for {updatedCount} cards.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var card = await _context.Cards
                .FirstOrDefaultAsync(m => m.Id == id);
            if (card == null)
            {
                return NotFound();
            }

            return View(card);
        }
    }
}
