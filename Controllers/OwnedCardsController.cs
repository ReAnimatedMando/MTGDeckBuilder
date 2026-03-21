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

                query = query.Where(oc =>
                    oc.Card != null &&
                    oc.Card.Name != null &&
                    EF.Functions.Like(oc.Card.Name, $"%{q}%"));
            }

            var ownedCards = await query
                .OrderBy(oc => oc.Card != null ? oc.Card.Name : "")
                .ToListAsync();

            ViewBag.Search = q;
            ViewBag.TotalUniqueCards = ownedCards.Count;
            ViewBag.TotalOwnedCopies = ownedCards.Sum(x => x.Quantity);

            return View(ownedCards);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity, string? q)
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

            return RedirectToAction(nameof(Index), new { q });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Increment(int id, string? q)
        {
            var ownedCard = await _context.OwnedCards.FindAsync(id);

            if (ownedCard == null)
                return NotFound();

            ownedCard.Quantity += 1;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { q });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decrement(int id, string? q)
        {
            var ownedCard = await _context.OwnedCards.FindAsync(id);

            if (ownedCard == null)
                return NotFound();

            ownedCard.Quantity -= 1;

            if (ownedCard.Quantity <= 0)
            {
                _context.OwnedCards.Remove(ownedCard);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { q });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? q)
        {
            var ownedCard = await _context.OwnedCards.FindAsync(id);

            if (ownedCard == null)
                return NotFound();

            _context.OwnedCards.Remove(ownedCard);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { q });
        }
    }
}
