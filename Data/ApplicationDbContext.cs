using System;
using Microsoft.EntityFrameworkCore;
using MTGDeckBuilder.Models;

namespace MTGDeckBuilder.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Card> Cards { get; set; }
    public DbSet<Deck> Decks { get; set; }
    public DbSet<DeckCard> DeckCards { get; set; }
    public DbSet<OwnedCard> OwnedCards { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); 

        // Composite key for DeckCard
        modelBuilder.Entity<DeckCard>() 
            .HasKey(dc => new { dc.DeckId, dc.CardId, dc.IsSideboard });

        modelBuilder.Entity<DeckCard>()
            .HasOne(dc => dc.Deck)
            .WithMany(d => d.DeckCards)
            .HasForeignKey(dc => dc.DeckId);

        modelBuilder.Entity<DeckCard>()
            .HasOne(dc => dc.Card)
            .WithMany(c => c.DeckCards)
            .HasForeignKey(dc => dc.CardId);
    }
}
