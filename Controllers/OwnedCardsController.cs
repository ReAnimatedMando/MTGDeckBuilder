using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTGDeckBuilder.Data;
using MTGDeckBuilder.Models;

namespace MTGDeckBuilder.Controllers
{
    public class OwnedCardsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public OwnedCardsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: OwnedCards
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.OwnedCards
                .Include(oc => oc.Card)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();

                query = query.Where(oc => oc.Card != null && oc.Card.Name != null && oc.Card.Name.Contains(q));
            }

            var ownedCards = await query
                .OrderBy(oc => oc.Card != null ? oc.Card.Name : "")
                .ToListAsync();

            ViewBag.Search = q;

            return View(ownedCards);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var ownedCard = await _context.OwnedCards.FindAsync(id);

            if (ownedCard == null)
                return NotFound();

            if (quantity <= 0)
            {
                _context.OwnedCards.Remove(ownedCard);
            }
            else
            {
                ownedCard.Quantity = quantity;
                _context.OwnedCards.Update(ownedCard);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ownedCard = await _context.OwnedCards.FindAsync(id);

            if (ownedCard == null)
                return NotFound();

            _context.OwnedCards.Remove(ownedCard);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
