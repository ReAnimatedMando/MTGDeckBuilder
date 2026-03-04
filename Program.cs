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

    if (!db.Cards.Any())
    {
        var cards = new List<Card>
        {
            new Card { Name = "Casey Jones, Jury-Rig Justiciar", ManaCost = "{R}{1}", TypeLine = "Legendary Creature — Human Berserker", ColorIdentity = "R", ImageUrl = "https://via.placeholder.com/223x310?text=Casey+Jones" },
            new Card { Name = "Donatello, Gadget Master", ManaCost = "{2}{U}", TypeLine = "Legendary Creature — Mutant Ninja Turtle", ColorIdentity = "U", ImageUrl = "https://via.placeholder.com/223x310?text=Donatello" },
            new Card { Name = "Shredder's Revenge", ManaCost = "{2}{B}", TypeLine = "Sorcery", ColorIdentity = "B", ImageUrl = "https://via.placeholder.com/223x310?text=Shredder's+Revenge" }
        };

        db.Cards.AddRange(cards);
        db.SaveChanges();

        var deck = new Deck { Name = "TMNT Deck" };
        db.Decks.Add(deck);
        db.SaveChanges();

        db.OwnedCards.AddRange(
            new OwnedCard { CardId = cards[0].Id, Quantity = 4 },
            new OwnedCard { CardId = cards[1].Id, Quantity = 2 },
            new OwnedCard { CardId = cards[2].Id, Quantity = 1 }
        );

        db.DeckCards.AddRange(
            new DeckCard { DeckId = deck.Id, CardId = cards[0].Id, Quantity = 4 },
            new DeckCard { DeckId = deck.Id, CardId = cards[2].Id, Quantity = 1 }
        );

        db.SaveChanges();
    }
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
