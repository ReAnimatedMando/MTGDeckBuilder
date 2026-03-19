using System.Collections.Generic;

namespace MTGDeckBuilder.Models.ViewModels
{
    public class CollectionStatsViewModel
    {
        public int TotalCardsOwned { get; set; }
        public int UniqueCardsOwned { get; set; }
        public decimal TotalCollectionValue { get; set; }
        public decimal AverageCardValue { get; set; }

        public List<TopOwnedCardViewModel> TopOwnedCards { get; set; } = new();
        public List<StatItemViewModel> ColorDistribution { get; set; } = new();
        public List<StatItemViewModel> TypeDistribution { get; set; } = new();
    }

    public class TopOwnedCardViewModel
    {
        public string CardName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal PriceUsd { get; set; }
        public decimal TotalValue { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class StatItemViewModel
    {
        public string Label { get; set; } = "";
        public int Count { get; set; }
    }
}