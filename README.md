MTG Deck Builder
A full-stack web application for building and managing themed Magic: The Gathering decks using owned card inventory, deck analysis tools,
and live card data integration.

Built with C#, ASP.NET Core MVC, Entity Framework Core, MySQL, and the Scryfall API, this project combines deck construction, 
collection management, and deck completion tracking into a practical planning tool for MTG players.

Project Goal
The goal of MTG Deck Builder was to create a smarter way to build decks around cards I already own while helping identify:
  Which decks are realistically buildable
  Missing cards needed to complete a deck
  Estimated deck costs
  Mana curve and color balance issues
  Themed deck-building options within color identity constraints

Rather than simply storing decklists, the application acts as a deck planning and collection optimization tool.
  Core Features
  Card Database + Search
  Search cards using live integration with the Scryfall API
  Automatic card import into the local database
  Fuzzy search support for card discovery
  Card details view with:
  Mana cost
  Card type
  Color identity
  Pricing
  Card image previews
  Owned Card Inventory

Track owned copies of cards to determine deck completion.
Features include:
  Quantity tracking
  Duplicate management
  Inventory-aware deck validation
  Collection-based deck recommendations
  Deck Builder

Create custom decks with support for:
  Themed decks (Goblins, Control, Tokens, etc.)
  Color identity filtering
  Configurable deck sizes (60–100 cards)
  Main deck + sideboard support
  Deck import workflow

Deck rules include:
  Maximum 4 copies of non-basic cards
  Unlimited basic lands
  Deck validation warnings
  Deck Completion Tracking

A “buildability” system calculates:
  Completion percentage
  Missing cards
  Missing copies
  Owned value
  Missing value
  Total deck cost

Helping answer the question:
“What deck can I realistically build right now?”

Deck Analytics
Visual deck statistics include:
  Mana Curve
  Displays mana value distribution across cards to evaluate deck pacing and consistency.
  Color Identity Breakdown
  Visual color pie analysis showing mana/color composition.
  Card Type Breakdown

Categorized totals for:
  Creatures
  Instants / Sorceries
  Artifacts
  Enchantments
  Planeswalkers
  Lands
  Price Tracking

Integrated card pricing using Scryfall market data.
Features:
  Stored USD card values
  Refresh prices utility
  Deck value estimation
  Missing card cost calculations

Technical Highlights

Backend
  C#
  ASP.NET Core MVC
  Entity Framework Core
  LINQ
  Dependency Injection

Service-layer architecture
Database
  MySQL
  EF Core migrations
  Relational data modeling

Key models include:
  Card
  Deck
  DeckCard
  OwnedCard

Composite relationships support:
  Main deck vs sideboard
  Quantity tracking
  Inventory matching
  API Integration

Integrated with the public MTG API:
  Scryfall API

Implementation includes:
  Multi-card search
  Fuzzy card lookup
  Price syncing
  Image retrieval
  Automatic local database updates
  UI / UX Focus

The application emphasizes usability and fast deck evaluation.
Features include:
  Responsive Bootstrap layout
  Hover card previews
  Search improvements
  Reduced page jumping during workflows
  Clear validation messaging
  Quick deck analysis visibility
  Technical Challenges Solved
  API + Local Database Synchronization

Designed a hybrid workflow that:
  Searches Scryfall
  Imports missing cards automatically
  Updates local card information
  Preserves application performance using local queries

This allows fast repeat searches without relying entirely on external API calls.

Deck Completion Logic
Built a calculation system that compares:
  Required deck cards vs owned inventory
to determine:
  completion %
  missing cards
  owned value
  missing value
  Data Modeling

Created relational structures to support:
  many-to-many deck/card relationships
  quantity tracking
  sideboard separation
  ownership inventory calculations
  
This project strengthened experience in:
  Full-stack ASP.NET MVC architecture
  Entity Framework Core relationships
  API integration patterns
  Database migrations
  LINQ query optimization
  Refactoring shared service logic
  UI/UX polishing and workflow design

It also reinforced the importance of iterative development, testing, and refactoring for maintainability.


Future Improvements (Phase 2)
Potential roadmap items include:
  Deck archetype recommendations
  Smarter AI-assisted deck building
  Advanced filtering
  Wishlist / acquisition tracking
  Deck export improvements
  Authentication + cloud sync
  Performance optimization for larger collections
  
Why This Project Matters:
MTG Deck Builder demonstrates the ability to design and ship a complete full-stack application that combines:
  relational databases
  external APIs
  business logic
  analytics
  responsive UI/UX
  real-world problem solving while maintaining clean architecture and extensible code.
