using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using MTGDeckBuilder.Data;
using MTGDeckBuilder.Models;
using static System.Formats.Asn1.AsnWriter;
using Microsoft.Extensions.DependencyInjection;
using MTGDeckBuilder.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<ScryfallService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var cardsToSeed = new List<Card>
    {
        new Card { Name = "Casey Jones, Jury-Rig Justiciar", ManaCost = "{R}{1}", ManaValue = 2, TypeLine = "Legendary Creature — Human Berserker", ColorIdentity = "R", ImageUrl = null },
        new Card { Name = "Donatello, Gadget Master", ManaCost = "{2}{U}", ManaValue = 3, TypeLine = "Legendary Creature — Mutant Ninja Turtle", ColorIdentity = "U", ImageUrl = null },
        new Card { Name = "Shredder's Revenge", ManaCost = "{2}{B}", ManaValue = 3, TypeLine = "Sorcery", ColorIdentity = "B", ImageUrl = null }
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
            if (string.IsNullOrWhiteSpace(existingCard.ManaCost))
                existingCard.ManaCost = seedCard.ManaCost;

            if (existingCard.ManaValue == 0)
                existingCard.ManaValue = seedCard.ManaValue;

            if (string.IsNullOrWhiteSpace(existingCard.TypeLine))
                existingCard.TypeLine = seedCard.TypeLine;

            if (string.IsNullOrWhiteSpace(existingCard.ColorIdentity))
                existingCard.ColorIdentity = seedCard.ColorIdentity;

            if (string.IsNullOrWhiteSpace(existingCard.ImageUrl))
                existingCard.ImageUrl = seedCard.ImageUrl;
        }
    }

    var placeholderCards = db.Cards
        .Where(c => c.ImageUrl != null && c.ImageUrl.Contains("via.placeholder.com"))
        .ToList();
    
    foreach (var card in placeholderCards)
    {
        card.ImageUrl = null;
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
