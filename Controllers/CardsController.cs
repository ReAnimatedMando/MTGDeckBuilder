using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTGDeckBuilder.Data;

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
            var cardsQuery = _context.Cards.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                
                cardsQuery = cardsQuery.Where(c => c.Name.Contains(q) || (c.TypeLine != null && c.TypeLine.Contains(q)) || (c.ManaCost != null && c.ManaCost.Contains(q)) || (c.ColorIdentity != null && c.ColorIdentity.Contains(q)));
            }

            var cards = await cardsQuery
                .OrderBy(c => c.Name)
                .Take(200)
                .ToListAsync();
                
            return View(cards);
        }

    }
}
