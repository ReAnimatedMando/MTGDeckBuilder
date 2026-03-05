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
            if (string.IsNullOrWhiteSpace(q))
            {
                return View(new List<Card>());
            }

            q = q.Trim();

            var term = $"%{q}%"; // for SQL LIKE queries, but EF Core will translate Contains to LIKE automatically
            
            var cards = await _context.Cards
                .Where(c =>
                    EF.Functions.Like(c.Name, term) ||
                    (c.TypeLine != null && EF.Functions.Like(c.TypeLine, term)) ||
                    (c.ManaCost != null && EF.Functions.Like(c.ManaCost, term)) ||
                    (c.ColorIdentity != null && EF.Functions.Like(c.ColorIdentity, term)))
                .OrderBy(c => c.Name)
                .Take(200)
                .ToListAsync();

            return View(cards);
        }

    }
}
