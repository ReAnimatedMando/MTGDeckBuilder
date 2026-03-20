using System;
using System.ComponentModel.DataAnnotations;

namespace MTGDeckBuilder.Models.ViewModels;

public class BuildDeckRequestViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(100)]
    public string? Theme { get; set; }

    [Display(Name = "White")]
    public bool UseWhite { get; set; }

    [Display(Name = "Blue")]
    public bool UseBlue { get; set; }

    [Display(Name = "Black")]
    public bool UseBlack { get; set; }

    [Display(Name = "Red")]
    public bool UseRed { get; set; }

    [Display(Name = "Green")]
    public bool UseGreen { get; set; }

    [Range(60, 100, ErrorMessage = "Deck size must be between 60 and 100 cards.")]
    public int DeckSize { get; set; } = 60;

    public string GetColorIdentity()
    {
        var colors = new List<string>();

        if(UseWhite) colors.Add("W");
        if(UseBlue) colors.Add("U");
        if(UseBlack) colors.Add("B");
        if(UseRed) colors.Add("R");
        if(UseGreen) colors.Add("G");

        return string.Join(",", colors);
    }
}
