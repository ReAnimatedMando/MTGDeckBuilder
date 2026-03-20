using Microsoft.EntityFrameworkCore;
using MTGDeckBuilder.Controllers;
using MTGDeckBuilder.Data;
using MTGDeckBuilder.Models;
using MTGDeckBuilder.Models.ViewModels;

namespace MTGDeckBuilder.Services
{
    public class DeckBuilderService
    {
        private readonly ApplicationDbContext _context;

        public DeckBuilderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Deck> BuildDeckAsync(BuildDeckRequestViewModel request)
        {
            if (request.DeckSize < 60 || request.DeckSize > 100)
                throw new ArgumentException("Deck size must be between 60 and 100 cards.");

            var ownedCards = await _context.OwnedCards
                .Include(oc => oc.Card)
                .ToListAsync();

            var selectedColors = request.GetColorIdentity()
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();

            var usableOwned = ownedCards
                .Where(oc => oc.Card != null)
                .Where(oc => oc.Quantity > 0)
                .Where(oc => CardMatchesColors(oc.Card!, selectedColors))
                .ToList();

            var lands = usableOwned
                .Where(oc => IsLand(oc.Card!))
                .OrderByDescending(oc => ScoreCard(oc.Card!, request.Theme, "land"))
                .ThenBy(oc => oc.Card!.ManaValue)
                .ThenBy(oc => oc.Card!.Name)
                .ToList();

            var creatures = usableOwned
                .Where(oc => IsCreature(oc.Card!))
                .OrderByDescending(oc => ScoreCard(oc.Card!, request.Theme, "creature"))
                .ThenBy(oc => oc.Card!.ManaValue)
                .ThenBy(oc => oc.Card!.Name)
                .ToList();

            var instantsSorceries = usableOwned
                .Where(oc => IsInstantOrSorcery(oc.Card!))
                .OrderByDescending(oc => ScoreCard(oc.Card!, request.Theme, "spell"))
                .ThenBy(oc => oc.Card!.ManaValue)
                .ThenBy(oc => oc.Card!.Name)
                .ToList();

            var enchantments = usableOwned
                .Where(oc => IsEnchantment(oc.Card!))
                .OrderByDescending(oc => ScoreCard(oc.Card!, request.Theme, "enchantment"))
                .ThenBy(oc => oc.Card!.ManaValue)
                .ThenBy(oc => oc.Card!.Name)
                .ToList();

            var others = usableOwned
                .Where(oc => !IsLand(oc.Card!) && !IsCreature(oc.Card!) && !IsInstantOrSorcery(oc.Card!) && !IsEnchantment(oc.Card!))
                .OrderByDescending(oc => ScoreCard(oc.Card!, request.Theme, "other"))
                .ThenBy(oc => oc.Card!.ManaValue)
                .ThenBy(oc => oc.Card!.Name)
                .ToList();

            var targets = GetScaledRoleTargets(request.DeckSize, request.Theme);

            var deck = new Deck
            {
                Name = request.Name,
                Theme = request.Theme,
                ColorIdentity = request.GetColorIdentity(),
                Description = $"Auto-built from owned cards on {DateTime.Now:d}. Target mix: {targets.Lands} lands / {targets.Creatures} creatures / {targets.Spells} instants-sorceries / {targets.Enchantments} enchantments / {targets.Other} other."
            };

            var chosen = new List<DeckCard>();

            AddCards(chosen, lands, targets.Lands, false);
            AddCards(chosen, creatures, targets.Creatures, false);
            AddCards(chosen, instantsSorceries, targets.Spells, false);
            AddCards(chosen, enchantments, targets.Enchantments, false);
            AddCards(chosen, others, targets.Other, false);

            var currentTotal = chosen.Sum(dc => dc.Quantity);

            if (currentTotal < request.DeckSize)
            {
                var fillerPool = usableOwned
                    .Select(oc => new
                    {
                        OwnedCard = oc,
                        Score = ScoreFillerCard(
                            oc.Card!,
                            request.Theme,
                            chosen.Where(dc => dc.CardId == oc.CardId).Sum(dc => dc.Quantity))
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.OwnedCard.Card!.ManaValue)
                    .ThenBy(x => x.OwnedCard.Card!.Name)
                    .Select(x => x.OwnedCard)
                    .ToList();

                AddCards(chosen, fillerPool, request.DeckSize - currentTotal, false);
            }

            if (chosen.Sum(dc => dc.Quantity) > request.DeckSize)
            {
                TrimToDeckSize(chosen, request.DeckSize);
            }

            deck.DeckCards = chosen;
            return deck;
        }

        private static (int Lands, int Creatures, int Spells, int Enchantments, int Other) GetScaledRoleTargets(int deckSize, string? theme)
        {
            var lands = (int)Math.Round(deckSize * 24.0 / 60.0);
            var creatures = (int)Math.Round(deckSize * 20.0 / 60.0);
            var spells = (int)Math.Round(deckSize * 12.0 / 60.0);
            var enchantments = (int)Math.Round(deckSize * 2.0 / 60.0);
            var other = deckSize - (lands + creatures + spells + enchantments);

            var t = theme?.Trim().ToLowerInvariant() ?? "";

            if (!string.IsNullOrWhiteSpace(t))
            {
                if (ContainsAny(t, "aggro"))
                {
                    lands -= 2;
                    creatures += 1;
                    spells += 1;
                }

                if (ContainsAny(t, "control"))
                {
                    lands += 2;
                    spells += 2;
                    creatures -= 2;
                }

                if (ContainsAny(t, "ramp"))
                {
                    lands += 3;
                    creatures += 1;
                    spells -= 2;
                    other -= 2;
                }

                if (ContainsAny(t, "midrange"))
                {
                    creatures += 2;
                    spells -= 1;
                    other -= 1;
                }

                if (ContainsAny(t, "burn"))
                {
                    lands -= 2;
                    spells += 3;
                    creatures -= 1;
                }

                if (ContainsAny(t, "tokens", "token"))
                {
                    creatures += 2;
                    enchantments +=1;
                    spells -= 2;
                    lands -= 1;
                }

                if (ContainsAny(t, "lifegain", "life gain"))
                {
                    creatures += 1;
                    enchantments += 1;
                    spells -= 1;
                    lands -= 1;
                }

                if (ContainsAny(t, "artifacts", "artifact"))
                {
                    other += 2;
                    enchantments -= 1;
                    creatures -= 1;
                }

                if (ContainsAny(t, "enchantment", "enchantments", "enchantress", "auras"))
                {
                    enchantments += 4;
                    creatures -= 1;
                    spells -= 1;
                    other -= 2;
                }
            }

            NormalizeRoleTargets(deckSize, ref lands, ref creatures, ref spells, ref enchantments, ref other);

            return (lands, creatures, spells, enchantments, other);
        }

        private static void NormalizeRoleTargets(int deckSize, ref int lands, ref int creatures, ref int spells, ref int enchantments, ref int other)
        {
            lands = Math.Max(0, lands);
            creatures = Math.Max(0, creatures);
            spells = Math.Max(0, spells);
            enchantments = Math.Max(0, enchantments);
            other = Math.Max(0, other);

            var total = lands + creatures + spells + enchantments + other;

            while (total > deckSize)
            {
                if (other > 0) other--;
                else if (enchantments > 0) enchantments--;
                else if (spells > 0) spells--;
                else if (creatures > 0) creatures--;
                else if (lands > 0) lands--;
                total = lands + creatures + spells + enchantments + other;
            }

            while (total < deckSize)
            {
                if (lands <= creatures && lands <= spells && lands <= enchantments)
                    lands++;
                else if (creatures <= spells && creatures <= enchantments)
                    creatures++;
                else if (spells <= enchantments)
                    spells++;
                else
                    enchantments++;

                total = lands + creatures + spells + enchantments + other;
            }
        }

        private static void AddCards(List<DeckCard> chosen, List<OwnedCard> pool, int targetCount, bool isSideboard)
        {
            var added = 0;

            foreach (var owned in pool)
            {
                if (added >= targetCount)
                    break;

                var card = owned.Card!;
                var maxCopies = GetMaxAllowedCopies(card, owned.Quantity);

                var existing = chosen.FirstOrDefault(dc => dc.CardId == owned.CardId && dc.IsSideboard == isSideboard);
                var alreadyChosen = existing?.Quantity ?? 0;
                var remainingAllowed = maxCopies - alreadyChosen;

                if (remainingAllowed <= 0)
                    continue;

                var copiesToAdd = Math.Min(remainingAllowed, targetCount - added);

                if (copiesToAdd <= 0)
                    continue;

                if (existing == null)
                {
                    chosen.Add(new DeckCard
                    {
                        CardId = owned.CardId,
                        Card = owned.Card,
                        Quantity = copiesToAdd,
                        IsSideboard = isSideboard
                    });
                }
                else
                {
                    existing.Quantity += copiesToAdd;
                }

                added += copiesToAdd;
            }
        }

        private static void TrimToDeckSize(List<DeckCard> chosen, int deckSize)
        {
            while (chosen.Sum(dc => dc.Quantity) > deckSize)
            {
                var removable = chosen
                    .Where(dc => dc.Quantity > 0)
                    .OrderBy(dc => IsLand(dc.Card!) ? 1 : 0)
                    .ThenByDescending(dc => dc.Card?.ManaValue ?? 0)
                    .FirstOrDefault();

                if (removable == null)
                    break;

                removable.Quantity--;

                if (removable.Quantity <= 0)
                {
                    chosen.Remove(removable);
                }
            }
        }

        private static int ScoreCard(Card card, string? theme, string role)
        {
            var score = 0;

            var name = card.Name ?? "";
            var typeLine = card.TypeLine ?? "";
            var manaValue = card.ManaValue;

            // Base mana curve preference
            if (role == "creature")
            {
                if (manaValue <= 3) score += 8;
                else if (manaValue == 4) score += 5;
                else if (manaValue == 5) score += 2;
                else score -= 2;
            }
            else if (role == "spell")
            {
                if (manaValue <= 2) score += 8;
                else if (manaValue == 3) score += 5;
                else if (manaValue == 4) score += 2;
                else score -= 2;
            }
            else if (role == "enchantment")
            {
                if (manaValue <= 3) score += 6;
                else if (manaValue == 4) score += 3;
            }
            else if (role == "other")
            {
                if (manaValue <= 3) score += 5;
                else if (manaValue == 4) score += 2;
            }
            else if (role == "land")
            {
                score += 5;
            }

            if (string.IsNullOrWhiteSpace(theme))
                return score;

            var t = theme.Trim().ToLowerInvariant();

            // Tribal themes
            if (ContainsAny(t, "zombie", "zombies"))
            {
                if (ContainsAny(typeLine, "Zombie")) score += 30;
            }

            if (ContainsAny(t, "angel", "angels"))
            {
                if (ContainsAny(typeLine, "Angel")) score += 30;
            }

            if (ContainsAny(t, "dragon", "dragons"))
            {
                if (ContainsAny(typeLine, "Dragon")) score += 30;
            }

            if (ContainsAny(t, "goblin", "goblins"))
            {
                if (ContainsAny(typeLine, "Goblin")) score += 30;
            }

            if (ContainsAny(t, "elf", "elves"))
            {
                if (ContainsAny(typeLine, "Elf")) score += 30;
            }

            if (ContainsAny(t, "vampire", "vampires"))
            {
                if (ContainsAny(typeLine, "Vampire")) score += 30;
            }

            if (ContainsAny(t, "soldier", "soldiers"))
            {
                if (ContainsAny(typeLine, "Soldier")) score += 30;
            }

            if (ContainsAny(t, "wizard", "wizards"))
            {
                if (ContainsAny(typeLine, "Wizard")) score += 30;
            }

            // Strategy / mechanical themes
            if (ContainsAny(t, "artifact", "artifacts"))
            {
                if (ContainsAny(typeLine, "Artifact")) score += 25;
            }

            if (ContainsAny(t, "enchantment", "enchantments", "enchantress"))
            {
                if (ContainsAny(typeLine, "Enchantment")) score += 25;
            }

            if (ContainsAny(t, "burn"))
            {
                if (ContainsAny(typeLine, "Instant", "Sorcery")) score += 18;
                if (manaValue <= 2) score += 8;
                if (ContainsAny(name, "Bolt", "Shock", "Skewer", "Burst", "Lava", "Flame", "Fire")) score += 10;
            }

            if (ContainsAny(t, "control"))
            {
                if (ContainsAny(typeLine, "Instant", "Sorcery")) score += 16;
                if (ContainsAny(typeLine, "Enchantment")) score += 4;
                if (manaValue >= 2 && manaValue <= 4) score += 5;
                if (ContainsAny(name, "Counter", "Negate", "Cancel", "Memory", "Wrath")) score += 10;
            }

            if (ContainsAny(t, "tokens", "token"))
            {
                if (ContainsAny(name, "Call", "Raise", "March", "Join")) score += 8;
                if (ContainsAny(typeLine, "Creature", "Enchantment")) score += 8;
                if (ContainsAny(typeLine, "Enchantment")) score += 6;
                if (ContainsAny(name, "Anointed", "Virtue", "Procession")) score += 8;
            }

            if (ContainsAny(t, "lifegain", "life gain"))
            {
                if (ContainsAny(name, "Life", "Soul", "Cleric", "Ajani", "Healer")) score += 10;
                if (ContainsAny(typeLine, "Creature")) score += 8;
                if (ContainsAny(typeLine, "Enchantment")) score += 5;
            }

            if (ContainsAny(t, "sacrifice", "aristocrats"))
            {
                if (ContainsAny(typeLine, "Creature")) score += 10;
                if (ContainsAny(name, "Blood", "Village", "Dead", "Devil", "Priest")) score += 8;
            }

            if (ContainsAny(t, "ramp"))
            {
                if (IsLand(card)) score += 10;
                if (ContainsAny(typeLine, "Creature", "Artifact")) score += 8;
                if (manaValue <= 3) score += 4;
            }

            if (ContainsAny(t, "midrange"))
            {
                if (ContainsAny(typeLine, "Creature")) score += 10;
                if (manaValue >= 2 && manaValue <= 4) score += 8;
            }

            if (ContainsAny(t, "aggro"))
            {
                if (ContainsAny(typeLine, "Creature", "Instant", "Sorcery")) score += 10;
                if (manaValue <= 2) score += 10;
                if (manaValue >= 5) score -= 6;
            }

            return score;
        }

        private static int ScoreFillerCard(Card card, string? theme, int alreadyChosenCopies)
        {
            var score = ScoreCard(card, theme, "filler");

            if (alreadyChosenCopies == 0) score += 4;
            else if (alreadyChosenCopies == 1) score += 2;
            else if (alreadyChosenCopies >= 3) score -= 3;

            if (IsLand(card)) score -= 2;

            return score;
        }

        private static bool ContainsAny(string text, params string[] terms)
        {
            return terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        private static int GetMaxAllowedCopies(Card card, int ownedQuantity)
        {
            if (IsBasicLand(card))
                return ownedQuantity;

            return Math.Min(4, ownedQuantity);
        }

        private static bool CardMatchesColors(Card card, HashSet<string> selectedColors)
        {
            if (selectedColors.Count == 0)
                return true;

            if (string.IsNullOrWhiteSpace(card.ColorIdentity))
                return true;

            var cardColors = card.ColorIdentity
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim());

            return cardColors.All(c => selectedColors.Contains(c));
        }

        private static bool IsLand(Card card) =>
            !string.IsNullOrWhiteSpace(card.TypeLine) &&
            card.TypeLine.Contains("Land", StringComparison.OrdinalIgnoreCase);

        private static bool IsBasicLand(Card card) =>
            !string.IsNullOrWhiteSpace(card.TypeLine) &&
            card.TypeLine.Contains("Basic Land", StringComparison.OrdinalIgnoreCase);

        private static bool IsCreature(Card card) =>
            !string.IsNullOrWhiteSpace(card.TypeLine) &&
            card.TypeLine.Contains("Creature", StringComparison.OrdinalIgnoreCase);

        private static bool IsInstantOrSorcery(Card card) =>
            !string.IsNullOrWhiteSpace(card.TypeLine) &&
            (card.TypeLine.Contains("Instant", StringComparison.OrdinalIgnoreCase) ||
             card.TypeLine.Contains("Sorcery", StringComparison.OrdinalIgnoreCase));

        private static bool IsEnchantment(Card card) => !string.IsNullOrWhiteSpace(card.TypeLine) && card.TypeLine.Contains("Enchantment", StringComparison.OrdinalIgnoreCase);
    }
}