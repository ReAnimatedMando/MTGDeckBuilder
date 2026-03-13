using System.Net.Http.Headers;
using System.Text.Json;
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

                    var scryfallCards = await _scryfall.SearchCardsAsync(q);

                    foreach (var scryfallCard in scryfallCards.Take(20))
                    {
                        if (string.IsNullOrWhiteSpace(scryfallCard.Name))
                            continue;

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
                        }
                        else
                        {
                            var updated = false;

                            if (string.IsNullOrWhiteSpace(existingCard.ImageUrl) && !string.IsNullOrWhiteSpace(scryfallCard.ImageUris?.Normal))
                            {
                                existingCard.ImageUrl = scryfallCard.ImageUris.Normal;
                                updated = true;
                            }

                            if (string.IsNullOrWhiteSpace(existingCard.ManaCost) && !string.IsNullOrWhiteSpace(scryfallCard.ManaCost))
                            {
                                existingCard.ManaCost = scryfallCard.ManaCost;
                                updated = true;
                            }

                            if (string.IsNullOrWhiteSpace(existingCard.TypeLine) && !string.IsNullOrWhiteSpace(scryfallCard.TypeLine))
                            {
                                existingCard.TypeLine = scryfallCard.TypeLine;
                                updated = true;
                            }

                            if (string.IsNullOrWhiteSpace(existingCard.ColorIdentity) && scryfallCard.ColorIdentity != null)
                            {
                                existingCard.ColorIdentity = string.Join(",", scryfallCard.ColorIdentity);
                                updated = true;
                            }

                            if (existingCard.ManaValue == 0 && scryfallCard.Cmc > 0)
                            {
                                existingCard.ManaValue = (int)scryfallCard.Cmc;
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

                var term = $"%{q}%";

                var results = await _context.Cards
                    .Where(c => EF.Functions.Like(c.Name, term) || (c.TypeLine != null && EF.Functions.Like(c.TypeLine, term)) || (c.ManaCost != null && EF.Functions.Like(c.ManaCost, term)) || (c.ColorIdentity != null && EF.Functions.Like(c.ColorIdentity, term)))
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
        public async Task<IActionResult> AddCard(int deckId, int cardId, int quantity = 1, bool isSideboard = false)
        {
            if (quantity <= 0) quantity = 1;

            var deckExists = await _context.Decks.AnyAsync(d => d.Id == deckId);
            if (!deckExists) return NotFound();

            var card = await _context.Cards.FirstOrDefaultAsync(c => c.Id == cardId);
            if (card == null) return NotFound();

            var deckCard = await _context.DeckCards
                .FirstOrDefaultAsync(dc => dc.DeckId == deckId && dc.CardId == cardId && dc.IsSideboard == isSideboard);

            var totalCopiesAcrossDeck = await _context.DeckCards
                .Where(dc => dc.DeckId == deckId && dc.CardId == cardId)
                .SumAsync(dc => (int?)dc.Quantity) ?? 0;

            var currentSectionQuantity = deckCard?.Quantity ?? 0;
            var newSectionQuantity = currentSectionQuantity + quantity;
            var newTotalCopiesAcrossDeck = totalCopiesAcrossDeck + quantity;

            var isBasicLand = !string.IsNullOrEmpty(card.TypeLine) && card.TypeLine.Contains("Basic Land", StringComparison.OrdinalIgnoreCase);

            if (!isBasicLand && newTotalCopiesAcrossDeck > 4)
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
                    Quantity = quantity,
                    IsSideboard = isSideboard
                };

                _context.DeckCards.Add(deckCard);
            }
            else
            {
                deckCard.Quantity = newSectionQuantity;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = deckId });
        }

        // POST: /Decks/SetQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetQuantity(int deckId, int cardId, int quantity, bool isSideboard = false)
        {
            var deckCard = await _context.DeckCards
                .Include(dc => dc.Card)
                .FirstOrDefaultAsync(dc => dc.DeckId == deckId && dc.CardId == cardId && dc.IsSideboard == isSideboard);
            
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
        public async Task<IActionResult> RemoveCard(int deckId, int cardId, bool isSideboard = false)
        {
            var deckCard = await _context.DeckCards
                .FirstOrDefaultAsync(dc => dc.DeckId == deckId && dc.CardId == cardId && dc.IsSideboard == isSideboard);
            
            if (deckCard == null) return NotFound();

            _context.DeckCards.Remove(deckCard);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = deckId });
        }    

        // POST: /Decks/ImportDecklist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportDecklist(int deckId, string? decklistText)
        {
            var deck = await _context.Decks.FindAsync(deckId);
            if (deck == null ) return NotFound();

            if (string.IsNullOrWhiteSpace(decklistText))
            {
                TempData["Error"] = "Please paste a decklist first.";
                return RedirectToAction(nameof(Details), new { id = deckId });
            }

            var existingCards = await _context.DeckCards
                .Where(dc => dc.DeckId == deckId)
                .ToListAsync();
            
            _context.DeckCards.RemoveRange(existingCards);
            await _context.SaveChangesAsync();

            var lines = decklistText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(l => l.Trim())
                .ToList();

            var importedCount = 0;
            var failedLines = new List<string>();
            var isSideboard = false;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                
                var normalized = line.Trim();

                if (normalized.Equals("Sideboard", StringComparison.OrdinalIgnoreCase) || normalized.Equals("Sideboard:", StringComparison.OrdinalIgnoreCase))
                {
                    isSideboard = true;
                    continue;
                }

                if (!TryParseDecklistLine(normalized, out int quantity, out string cardName))
                {
                    failedLines.Add(line);
                    continue;
                }

                try
                {
                    var card = await GetOrCreateCardFromDecklistImport(cardName);

                    if (card == null)
                    {
                        failedLines.Add(line);
                        continue;
                    }

                    var existingDeckCard = await _context.DeckCards
                        .Include(dc => dc.Card)
                        .FirstOrDefaultAsync(dc => dc.DeckId == deckId && dc.CardId == card.Id && dc.IsSideboard == isSideboard);

                    var isBasicLand = !string.IsNullOrWhiteSpace(card.TypeLine) && card.TypeLine.Contains("Basic Land", StringComparison.OrdinalIgnoreCase);

                    var currentQuantity = existingDeckCard?.Quantity ?? 0;
                    var newQuantity = currentQuantity + quantity;

                    if (!isBasicLand && newQuantity > 4)
                    {
                        failedLines.Add($"{line} (would exceed 4-copy limit)");
                        continue;
                    }

                    if (existingDeckCard == null)
                    {
                        _context.DeckCards.Add(new DeckCard
                        {
                            DeckId = deckId,
                            CardId = card.Id,
                            Quantity = quantity,
                            IsSideboard = isSideboard
                        });
                    }
                    else
                    {
                        existingDeckCard.Quantity = newQuantity;
                    }

                    importedCount += quantity;
                }
                
                catch
                {
                    failedLines.Add(line);
                }
            }

            await _context.SaveChangesAsync();

            if (failedLines.Any())
            {
                TempData["Error"] = $"Imported {importedCount} cards, but some lines failed: {string.Join(" | ", failedLines.Take(5))}" + (failedLines.Count > 5 ? "..." : "");
            }
            else
            {
                TempData["Success"] = $"Imported {importedCount} cards successfully.";
            }
            return RedirectToAction(nameof(Details), new { id = deckId });
        }

        // POST: /Decks/ImportFromUrl
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportFromUrl(int deckId, string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                TempData["Error"] = "Please enter a deck URL.";
                return RedirectToAction(nameof(Details), new { id = deckId });
            }

            if (url.Contains("moxfield.com", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Moxfield URL import is currently blocked by Moxfield/Cloudflare. Please export the deck as text from Moxfield and paste it into Import Decklist below.";
                return RedirectToAction(nameof(Details), new { id = deckId });
            }

            if (url.Contains("archidekt.com", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Archidekt URL import is currently unavailable due to unstable public API responses. Please export the deck as text from Archidekt and paste it into Import Decklist below.";
                return RedirectToAction(nameof(Details), new { id = deckId });
            }

            TempData["Error"] = "Unsupported deck URL. Please paste a decklist instead.";
            return RedirectToAction(nameof(Details), new { id = deckId });
        }

        private bool TryParseDecklistLine(string line, out int quantity, out string cardName)
        {
            quantity = 0;
            cardName = string.Empty;

            if (string.IsNullOrWhiteSpace(line))
                return false;

            var firstSpace = line.IndexOf(' ');
            if (firstSpace <= 0)
                return false;

            var qtyPart = line.Substring(0, firstSpace).Trim();
            var namePart = line.Substring(firstSpace + 1).Trim();

            if (!int.TryParse(qtyPart, out quantity))
                return false;

            if (quantity <= 0 || string.IsNullOrWhiteSpace(namePart))
                return false;

            cardName = namePart;
            return true;
        }

        private async Task<Card> GetOrCreateCardFromDecklistImport(string cardName)
        {
            var existingCard = await _context.Cards
                .FirstOrDefaultAsync(c => c.Name == cardName);

            if (existingCard != null)
                return existingCard;

            var scryfallCards = await _scryfall.SearchCardsAsync(cardName);
            var scryfallCard = scryfallCards
                .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Name) &&
                                    c.Name.Equals(cardName, StringComparison.OrdinalIgnoreCase));

            if (scryfallCard == null)
            {
                scryfallCard = scryfallCards
                    .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Name));
            }

            if (scryfallCard == null || string.IsNullOrWhiteSpace(scryfallCard.Name))
                return null!;

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

            return newCard;
        }

        private async Task<string?> DownloadDecklistFromUrl(string url)
        {
            using var http = new HttpClient();

            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " + "(KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36");

            http.DefaultRequestHeaders.Accept.ParseAdd("application/json,text/plain,*/*");
            http.DefaultRequestHeaders.Referrer = new Uri("https://www.moxfield.com/");

            // MOXFIELD
            if (url.Contains("moxfield.com", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(url);
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                var deckIndex = Array.FindIndex(segments, s => s.Equals("decks", StringComparison.OrdinalIgnoreCase));

                if (deckIndex < 0 || deckIndex + 1 >= segments.Length)
                {
                    Console.WriteLine("MOXFIELD: could not extract deck id");
                    return null;
                }

                var deckId = segments[deckIndex + 1];

                var apiUrl = $"https://api.moxfield.com/v2/decks/all/{deckId}";

                Console.WriteLine($"MOXFIELD API URL = {apiUrl}");

                var response = await http.GetAsync(apiUrl);
                Console.WriteLine($"MOXFIELD STATUS = {(int)response.StatusCode} {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("MOXFIELD ERROR BODY:");  
                    Console.WriteLine(errorBody);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();

                var data = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json);

                var decklist = new List<string>();

                if (!data.TryGetProperty("mainboard", out JsonElement mainboard) || mainboard.ValueKind != JsonValueKind.Object)
                {
                    Console.WriteLine("MOXFIELD: mainboard missing");
                    return null;
                }

                foreach (var card in mainboard.EnumerateObject())
                {
                    var qty = card.Value.GetProperty("quantity").GetInt32();
                    var name = card.Value.GetProperty("card").GetProperty("name").GetString();

                    if (!string.IsNullOrWhiteSpace(name))
                        decklist.Add($"{qty} {name}");
                }

                if (data.TryGetProperty("sideboard", out JsonElement sideboard) && sideboard.ValueKind == JsonValueKind.Object && sideboard.EnumerateObject().Any())
                {
                    decklist.Add("");
                    decklist.Add("Sideboard");

                    foreach (var card in sideboard.EnumerateObject())
                    {
                        var qty = card.Value.GetProperty("quantity").GetInt32();
                        var name = card.Value.GetProperty("card").GetProperty("name").GetString();

                        if (!string.IsNullOrWhiteSpace(name))
                            decklist.Add($"{qty} {name}");
                    }
                }

                Console.WriteLine($"MOXFIELD IMPORTED LINES = {decklist.Count}");
                return string.Join("\n", decklist);
            }

            // ARCHIDEKT
            if (url.Contains("archidekt.com", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(url);
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                var deckIndex = Array.FindIndex(segments, s => s.Equals("decks", StringComparison.OrdinalIgnoreCase));

                if (deckIndex < 0 || deckIndex + 1 >= segments.Length)
                {
                    Console.WriteLine("ARCHIDEKT: could not extract deck id");
                    return null;
                }

                var deckId = segments[deckIndex + 1];

                var apiUrl = $"https://archidekt.com/api/decks/{deckId}/";

                Console.WriteLine($"ARCHIDEKT API URL = {apiUrl}");

                var response = await http.GetAsync(apiUrl);
                Console.WriteLine($"ARCHIDEKT STATUS = {(int)response.StatusCode} {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("ARCHIDEKT ERROR BODY:");
                    Console.WriteLine(errorBody);
                    return null;
                }

                var json = await http.GetStringAsync(apiUrl);

                var data = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json);

                var decklist = new List<string>();

                if (!data.TryGetProperty("cards", out JsonElement cards) || cards.ValueKind != JsonValueKind.Array)
                {
                    Console.WriteLine("ARCHIDEKT: cards array missing");
                    return null;
                }

                foreach (var card in cards.EnumerateArray())
                {
                    var qty = card.GetProperty("quantity").GetInt32();
                    var name = card.GetProperty("card").GetProperty("oracleCard").GetProperty("name").GetString();

                    bool isSideBoard = false;

                    if (card.TryGetProperty("categories", out JsonElement categories) && categories.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var category in categories.EnumerateArray())
                        {
                            var categoryName = category.GetString();
                            if (string.Equals(categoryName, "Sideboard", StringComparison.OrdinalIgnoreCase))
                            {
                                isSideBoard =true;
                                break;
                            }
                        }
                    }

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (isSideBoard)
                    {
                        if (!decklist.Contains("Sideboard"))
                        {
                            decklist.Add("");
                            decklist.Add("Sideboard");
                        }

                        decklist.Add($"{qty} {name}");
                    }
                    else
                    {
                        decklist.Add($"{qty} {name}");
                    }
                }

                Console.WriteLine($"ARCHIDEKT IMPORTED LINES = {decklist.Count}");
                return string.Join("\n", decklist);
            }

            Console.WriteLine("Unsupported URL");
            return null;
        }
    }
}
