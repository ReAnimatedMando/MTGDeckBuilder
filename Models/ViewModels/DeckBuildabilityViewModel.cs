using System;

namespace MTGDeckBuilder.Models.ViewModels
{
    public class DeckBuildabilityViewModel
    {
        public int DeckId { get; set; }

        public string DeckName { get; set; } = "";
        public string? Theme { get; set; }
        public string? ColorIdentity { get; set; }

        public int TotalRequired { get; set; }
        public int TotalOwned { get; set; }
        public int MissingCopies { get; set; }

        public decimal TotalDeckValue { get; set; }
        public decimal MissingValue { get; set; }

        public int CompletionPercent =>
            TotalRequired == 0 ? 0 : (int)Math.Round((double)TotalOwned / TotalRequired * 100);

        public string Status =>
            CompletionPercent >= 100 ? "Buildable" :
            CompletionPercent >= 80 ? "Nearly Complete" :
            "Needs Work";
    }
}
