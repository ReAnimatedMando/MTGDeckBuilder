using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using MTGDeckBuilder.Data;
using MTGDeckBuilder.Models;
using static System.Formats.Asn1.AsnWriter;
using Microsoft.Extensions.DependencyInjection; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var cardsToSeed = new List<Card>
    {
        new Card { Name = "Casey Jones, Jury-Rig Justiciar", ManaCost = "{R}{1}", ManaValue = 2, TypeLine = "Legendary Creature — Human Berserker", ColorIdentity = "R", ImageUrl = "https://via.placeholder.com/223x310?text=Casey+Jones" },
        new Card { Name = "Donatello, Gadget Master", ManaCost = "{2}{U}", ManaValue = 3, TypeLine = "Legendary Creature — Mutant Ninja Turtle", ColorIdentity = "U", ImageUrl = "https://via.placeholder.com/223x310?text=Donatello" },
        new Card { Name = "Shredder's Revenge", ManaCost = "{2}{B}", ManaValue = 3, TypeLine = "Sorcery", ColorIdentity = "B", ImageUrl = "https://via.placeholder.com/223x310?text=Shredder's+Revenge" },
        new Card { Name = "Mouser Foundry", ManaCost = "{1}{R}", ManaValue = 2, TypeLine = "Artifact", ColorIdentity = "R", ImageUrl = "https://via.placeholder.com/223x310?text=Mouser+Foundry" },
        new Card { Name = "Pain 101", ManaCost = "{1}{B}", ManaValue = 2, TypeLine = "Instant", ColorIdentity = "B", ImageUrl = "https://via.placeholder.com/223x310?text=Pain+101" },
        new Card { Name = "Plains", ManaCost = "", ManaValue = 0, TypeLine = "Basic Land — Plains", ColorIdentity = "W", ImageUrl = "https://via.placeholder.com/223x310?text=Plains" },
        new Card { Name = "Guac & Marshmallow Pizza", ManaCost = "{G}", ManaValue = 1, TypeLine = "Artifact-Food", ColorIdentity = "G", ImageUrl = "https://via.placeholder.com/223x310?text=Guac+%26+Marshmallow+Pizza" },
        new Card { Name = "Armaggon, Future Shark", ManaCost = "{6}{B}{B}", ManaValue = 8, TypeLine = "Legendary Creature - Shark Horror Mutant", ColorIdentity = "B", ImageUrl = "https://via.placeholder.com/223x310?text=Armaggon" },
        new Card { Name = "Bishop, Warthog Warrior", ManaCost = "{4}{B}", ManaValue = 5, TypeLine = "Legendary Creature - Boar Mutant Warrior", ColorIdentity = "B", ImageUrl = "https://via.placeholder.com/223x310?text=Bishop" },
        new Card { Name = "Rock Soldiers", ManaCost = "{3}{R}", ManaValue = 4, TypeLine = "Artifact Creature - Soldier", ColorIdentity = "R", ImageUrl = "https://via.placeholder.com/223x310?text=Rock+Soldiers" },
        new Card { Name = "Utrom Scientists", ManaCost = "{2}{U}", ManaValue = 3, TypeLine = "Artifact Creature - Utrom Robot Scientist", ColorIdentity = "U", ImageUrl = "https://via.placeholder.com/223x310?text=Utrom+Scientists" },
        new Card { Name = "Heroes in a Half Shell", ManaCost = "{W}{U}{B}{R}{G}", ManaValue = 5, TypeLine = "Legendary Creature - Mutant Ninja Turtle", ColorIdentity = "W,U,B,R,G", ImageUrl = "https://via.placeholder.com/223x310?text=Heroes+in+a+Half+Shell" }
    };

    foreach (var seedCard in cardsToSeed)
    {
        var existingCard = db.Cards.FirstOrDefault(c => c.Name == seedCard.Name);
        if (existingCard == null)
        {
            db.Cards.Add(seedCard);
        }
        else
        {
            existingCard.ManaCost = seedCard.ManaCost;
            existingCard.ManaValue = seedCard.ManaValue;
            existingCard.TypeLine = seedCard.TypeLine;
            existingCard.ColorIdentity = seedCard.ColorIdentity;
            existingCard.ImageUrl = seedCard.ImageUrl;
        }
    }

    db.SaveChanges();

    var deck = db.Decks.FirstOrDefault(d => d.Name == "TMNT Deck");

    if (deck == null)
    {
        deck = new Deck { Name = "TMNT Deck" };
        db.Decks.Add(deck);
        db.SaveChanges();
    }

    var caseyJones = db.Cards.FirstOrDefault(c => c.Name == "Casey Jones, Jury-Rig Justiciar");
    var shreddersRevenge = db.Cards.FirstOrDefault(c => c.Name == "Shredder's Revenge");
    var donatello = db.Cards.FirstOrDefault(c => c.Name == "Donatello, Gadget Master");

    if (caseyJones != null && !db.OwnedCards.Any(oc => oc.CardId == caseyJones.Id))
    {
        db.OwnedCards.Add(new OwnedCard { CardId = caseyJones.Id, Quantity = 4 });
    }

    if (donatello != null && !db.OwnedCards.Any(oc => oc.CardId == donatello.Id))
    {
        db.OwnedCards.Add(new OwnedCard { CardId = donatello.Id, Quantity = 2 });
    }

    if (shreddersRevenge != null && !db.OwnedCards.Any(oc => oc.CardId == shreddersRevenge.Id))
    {
        db.OwnedCards.Add(new OwnedCard { CardId = shreddersRevenge.Id, Quantity = 1 });
    }

    if (caseyJones != null && !db.DeckCards.Any(dc => dc.DeckId == deck.Id && dc.CardId == caseyJones.Id))
    {
        db.DeckCards.Add(new DeckCard { DeckId = deck.Id, CardId = caseyJones.Id, Quantity = 4 });
    }

    if (shreddersRevenge != null && !db.DeckCards.Any(dc => dc.DeckId == deck.Id && dc.CardId == shreddersRevenge.Id))
    {
        db.DeckCards.Add(new DeckCard { DeckId = deck.Id, CardId = shreddersRevenge.Id, Quantity = 1 });
    }

    db.SaveChanges();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Only redirect to HTTPS outside Development 
if (!app.Environment.IsDevelopment()){
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
