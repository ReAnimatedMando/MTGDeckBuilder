using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTGDeckBuilder.Data;
using MTGDeckBuilder.Models;

namespace MTGDeckBuilder.Controllers
{
    public class DecksController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DecksController(ApplicationDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Create(Deck deck)
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

            // Card search results (optional)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var results = await _context.Cards
                    .Where(c => c.Name.Contains(q))
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
            if (quantity < 1) quantity = 1;

            if (!await _context.Decks.AnyAsync(d => d.Id == deckId)) return NotFound();

            if (!await _context.Cards.AnyAsync(c => c.Id == cardId)) return NotFound();

            var deckCard = await _context.DeckCards
                .FirstOrDefaultAsync(dc => dc.DeckId == deckId && dc.CardId == cardId);

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
                deckCard.Quantity += quantity;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = deckId });
        }

        // POST: /Decks/SetQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetQuantity(int deckId, int cardId, int quantity)
        {
            var deckCard = _context.DeckCards
                .FirstOrDefault(dc => dc.DeckId == deckId && dc.CardId == cardId);
            
            if (deckCard == null) return NotFound();

            if (quantity <= 0)
            {
                _context.DeckCards.Remove(deckCard);
            }
            else
            {
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
            var deckCard = _context.DeckCards
                .FirstOrDefault(dc => dc.DeckId == deckId && dc.CardId == cardId);
            
            if (deckCard == null) return NotFound();

            _context.DeckCards.Remove(deckCard);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = deckId });
        }    
    }
}
