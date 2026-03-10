using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTGDeckBuilder.Data;
using MTGDeckBuilder.Models;
using MTGDeckBuilder.Services;

namespace MTGDeckBuilder.Controllers
{
    public class DecksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ScryfallService _scryfall;

        public DecksController(ApplicationDbContext context, ScryfallService scryfall)
        {
            _context = context;
            _scryfall = scryfall;
        }

        // GET: /Decks 
        public async Task<IActionResult> Index()
        {
            var decks = await _context.Decks
                .OrderByDescending(d => d.Id)
                .ToListAsync();

            return View(decks);
        }

        // GET: /Decks/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: /Decks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Deck deck)
        {
            if (!ModelState.IsValid)
                return View(deck);
               
            _context.Decks.Add(deck);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = deck.Id });
        }

        // GET: /Decks/Details/5?q=bolt
        public async Task<IActionResult> Details(int id, string? q)
        {
            var deck = await _context.Decks
                .Include(d => d.DeckCards)
                    .ThenInclude(dc => dc.Card)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (deck == null) return NotFound();

            var curve = new ManaCurve();
            
            foreach (var dc in deck.DeckCards)
            {
                int cmc = dc.Card?.ManaValue ?? 0;

                switch (cmc)
                {
                    case 0: curve.Cmc0 += dc.Quantity; break;
                    case 1: curve.Cmc1 += dc.Quantity; break;
                    case 2: curve.Cmc2 += dc.Quantity; break;
                    case 3: curve.Cmc3 += dc.Quantity; break;
                    case 4: curve.Cmc4 += dc.Quantity; break;
                    case 5: curve.Cmc5 += dc.Quantity; break;
                    case 6: curve.Cmc6 += dc.Quantity; break;
                    default: curve.Cmc7Plus += dc.Quantity; break;
                }
            }

            var manaCurveData = new List<int>
            {
                curve.Cmc0,
                curve.Cmc1,
                curve.Cmc2,
                curve.Cmc3,
                curve.Cmc4,
                curve.Cmc5,
                curve.Cmc6,
                curve.Cmc7Plus
            };

            ViewBag.ManaCurve = curve;
            ViewBag.ManaCurveData = manaCurveData;

            var colorPieData = new Dictionary<string, int>
            {
                ["W"] = 0,
                ["U"] = 0,
                ["B"] = 0,
                ["R"] = 0,
                ["G"] = 0
            };

            foreach (var dc in deck.DeckCards)
            {
                var colorIdentity = dc.Card?.ColorIdentity;

                if (string.IsNullOrWhiteSpace(colorIdentity))
                    continue;

                var colors = colorIdentity
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var rawColor in colors)
                {
                    var trimmedColor = rawColor.Trim();

                    if (colorPieData.ContainsKey(trimmedColor))
                    {
                        colorPieData[trimmedColor] += dc.Quantity;
                    }
                }
            }

            ViewBag.ColorPieData = colorPieData;

            q = q?.Trim();

            // Card search results (optional)
            if (!string.IsNullOrWhiteSpace(q))
            {
                try
                {
                    Console.WriteLine($"SEARCH TERM = {q}");

                    var scryfallCard = await _scryfall.SearchCardAsync(q);

                    if (scryfallCard != null && !string.IsNullOrWhiteSpace(scryfallCard.Name))
                    {
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
                                ImageUrl = scryfallCard.ImageUris?.Normal
                            };

                            _context.Cards.Add(newCard);
                            await _context.SaveChangesAsync();

                            Console.WriteLine($"Added card from Scryfall: {newCard.Name}");
                        }
                        else
                        {
                            Console.WriteLine($"Card already exists: {existingCard.Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SCRYFALL ERROR:");
                    Console.WriteLine(ex.Message);
                }

                var results = await _context.Cards
                    .Where(c => c.Name.Contains(q!))
                    .OrderBy(c => c.Name)
                    .Take(40)
                    .ToListAsync();

                ViewBag.Query = q;
                ViewBag.SearchResults = results;
            }

            return View(deck);
        }

        // POST: /Decks/AddCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCard(int deckId, int cardId, int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;

            var deckExists = await _context.Decks.AnyAsync(d => d.Id == deckId);
            if (!deckExists) return NotFound();

            var card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == cardId);
            if (card == null) return NotFound();

            var deckCard = await _context.DeckCards
                .FirstOrDefaultAsync(dc => dc.DeckId == deckId && dc.CardId == cardId);

            var currentQuantity = deckCard?.Quantity ?? 0;
            var newQuantity = currentQuantity + quantity;

            var isBasicLand = !string.IsNullOrEmpty(card.TypeLine) && card.TypeLine.Contains("Basic Land", StringComparison.OrdinalIgnoreCase);

            if (!isBasicLand && newQuantity > 4)
            {
                TempData["Error"] = $"{card.Name} cannot have more than 4 copies in a deck";
                return RedirectToAction(nameof(Details), new { id = deckId });
            }

            if (deckCard == null)
            {
                deckCard = new DeckCard
                {
                    DeckId = deckId,
                    CardId = cardId,
                    Quantity = quantity
                };

                _context.DeckCards.Add(deckCard);
            }
            else
            {
                deckCard.Quantity = newQuantity;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = deckId });
        }

        // POST: /Decks/SetQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetQuantity(int deckId, int cardId, int quantity)
        {
            var deckCard = await _context.DeckCards
                .Include(dc => dc.Card)
                .FirstOrDefaultAsync(dc => dc.DeckId == deckId && dc.CardId == cardId);
            
            if (deckCard == null) return NotFound();

            if (quantity <= 0)
            {
                _context.DeckCards.Remove(deckCard);
            }
            else
            {
                var isBasicLand = deckCard.Card != null && !string.IsNullOrEmpty(deckCard.Card.TypeLine) && deckCard.Card.TypeLine.Contains("Basic Land", StringComparison.OrdinalIgnoreCase);

                if (!isBasicLand && quantity > 4)
                {
                    TempData["Error"] = $"{deckCard.Card?.Name} cannot have more than 4 copies in a deck";
                    return RedirectToAction(nameof(Details), new { id = deckId });
                }

                deckCard.Quantity = quantity;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = deckId });
        }

        // POST: /Decks/RemoveCard
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCard(int deckId, int cardId)
        {
            var deckCard = await _context.DeckCards
                .FirstOrDefaultAsync(dc => dc.DeckId == deckId && dc.CardId == cardId);
            
            if (deckCard == null) return NotFound();

            _context.DeckCards.Remove(deckCard);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = deckId });
        }    
    }
}
