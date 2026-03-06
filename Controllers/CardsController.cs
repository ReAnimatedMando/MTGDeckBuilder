using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTGDeckBuilder.Data;
using MTGDeckBuilder.Models;

namespace MTGDeckBuilder.Controllers
{
    public class CardsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CardsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cards
        public async Task<IActionResult> Index(string? q)
        {
            IQueryable<Card> query = _context.Cards;

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                var term = $"%{q}%";

                query = query.Where(c =>
                    EF.Functions.Like(c.Name, term) ||
                    (c.TypeLine != null && EF.Functions.Like(c.TypeLine, term)) ||
                    (c.ManaCost != null && EF.Functions.Like(c.ManaCost, term)) ||
                    (c.ColorIdentity != null && EF.Functions.Like(c.ColorIdentity, term)));
            }

            var cards = await query
                .OrderBy(c => c.Name)
                .Take(200)
                .ToListAsync();

            return View(cards);
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
