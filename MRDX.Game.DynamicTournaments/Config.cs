using System.ComponentModel;
using MRDX.Base.Mod.Interfaces;
using MRDX.Game.DynamicTournaments.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;

namespace MRDX.Game.DynamicTournaments.Configuration;

public class Config : Configurable<Config>
{
    public enum TechInt
    {
        Minimal,
        Average,
        Smart,
        Genius
    }

    public enum TournamentBreeds
    {
        PlayerOnly,
        Realistic,
        PlayerOnlyRealistic,
        WildWest
    }

    public enum ESpeciesAccuracyTraits {
        Strict,
        Loose,
        WildWest,
    }
    /*
        User Properties:
            - Please put all of your configurable properties here.

        By default, configuration saves as "Config.json" in mod user config folder.
        Need more config files/classes? See Configuration.cs

        Available Attributes:
        - Category
        - DisplayName
        - Description
        - DefaultValue

        // Technically Supported but not Useful
        - Browsable
        - Localizable

        The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
    */

    [Category("Tournament Opponents")]
    [DisplayName("Monster Species")]
    [Description("Determines the types of monsters that are present in tournaments.\n" +
                 "Player Only - Besides themed events, tournament monsters use the same unlocked pool of monsters as the player.\n" +
                 "Realistic - Monster species variety is loosely tied to the in game year.\n" +
                 "PO/Realistic - A combination of Player Only and Realistic criteria allowing for species to be present.\n" +
                 "Wild West - Any legal species is available from Year 1000.")]
    [DefaultValue(TournamentBreeds.PlayerOnlyRealistic)]
    public TournamentBreeds EnemyTournamentBreed { get; set; } = TournamentBreeds.PlayerOnlyRealistic;

    [Category("Tournament Opponents")]
    [DisplayName("Unique Species")]
    [Description("A multiplier for how often unique species are added to tournaments once available.\n" +
                 "Valid values are from 0.01 - 1.0, 1% as common to 100% as common.\n" +
                 "Note: Unique species make up a significant portion of the monsters in MR2.")]
    [DefaultValue(0.25)]
    [SliderControlParams(0.01, showTextField: true, isTextFieldEditable: true)]
    public double SpeciesUnique { get; set; } = 0.25;

    [Category( "Tournament Opponents - Game Accuracy" )]
    [DisplayName( "Immutable Traits" )]
    [Description( "Determines whether tournament monsters respect the following monster species traits.\n" +
             "Guts Regeneration, Arena Movespeed\n" +
             "Strict - Monsters will always use the base trait values.\n" +
             "Loose - Monsters will have base trait values with small offsets (AS-1, GUTS-2)\n" +
             "Wild West - Monsters will have random base trait values.")]
    [DefaultValue( ESpeciesAccuracyTraits.Loose )]
    public ESpeciesAccuracyTraits SpeciesAccuracyTraits { get; set; } = ESpeciesAccuracyTraits.Loose;


    [Category( "Tournament Opponents - Game Accuracy" )]
    [DisplayName( "Stat Growths" )]
    [Description( "Determines whether if a monster's base stat growths are applied to trainer formulas.\n" +
            "Note: This is a soft modifier. Trainers will still raise monsters in interesting ways.\n" +
            "For example, when enabled a Mock will always have higher priority towards Int, a Golem will have higher Power/Def, etc." )]
    [DefaultValue( true )]
    public bool SpeciesAccuracyStatGrowths { get; set; } = true;


    [Category("Tournament Ranks")]
    [DisplayName("Stat Cap - Major 4")]
    [Description("The Soft Stat Cap to promote from M4 to Legend Status.\n" +
                 "Note: This currently has no effect on the player.")]
    [DefaultValue(4200)]
    public int RankM4 { get; set; } = 4200;

    [Category("Tournament Ranks")]
    [DisplayName("Stat Cap - S Rank")]
    [Description("The Soft Stat Cap to promote from S into M4.\n" +
                 "Note: This currently has no effect on the player.")]
    [DefaultValue(3400)]
    public int RankS { get; set; } = 3400;

    [Category("Tournament Ranks")]
    [DisplayName("Stat Cap - A Rank")]
    [Description("The Soft Stat Cap to promote from A into S.\n" +
                 "Note: This currently has no effect on the player.")]
    [DefaultValue(2700)]
    public int RankA { get; set; } = 2700;

    [Category("Tournament Ranks")]
    [DisplayName("Stat Cap - B Rank")]
    [Description("The Soft Stat Cap to promote from B into A.\n" +
                 "Note: This currently has no effect on the player.")]
    [DefaultValue(2100)]
    public int RankB { get; set; } = 2100;

    [Category("Tournament Ranks")]
    [DisplayName("Stat Cap - C Rank")]
    [Description("The Soft Stat Cap to promote from C into B.\n" +
                 "Note: This currently has no effect on the player.")]
    [DefaultValue(1700)]
    public int RankC { get; set; } = 1700;

    [Category("Tournament Ranks")]
    [DisplayName("Stat Cap - D Rank")]
    [Description("The Soft Stat Cap to promote from D into C.\n" +
                 "Note: This currently has no effect on the player.")]
    [DefaultValue(1300)]
    public int RankD { get; set; } = 1300;

    [Category("Tournament Ranks")]
    [DisplayName("Stat Cap - E Rank")]
    [Description("The Soft Stat Cap to promote from E into D.\n" +
                 "Note: This currently has no effect on the player.")]
    [DefaultValue(1000)]
    public int RankE { get; set; } = 1000;

    [Category("Tournament Ranks")]
    [DisplayName("Stat Cap - Minimum")]
    [Description("The lower bounds for the weakest of E Rank Mosters.\n" +
                 "Note: This currently has no effect on the player.")]
    [DefaultValue(800)]
    public int RankZ { get; set; } = 800;

    [Category("Gameplay Adjustments")]
    [DisplayName("Tournament Stat Growths")]
    [Description("The modifier adds up to the provided value for each stat when participating in a tournament.\n" +
                 "A value of 4 adds between 0-4 points to each stat upon completion of a tournament.\n" +
                 "Game Default: 0, Recommended: 4")]
    [DefaultValue(4)]
    public int StatGrowth { get; set; } = 4;

    [Category("Gameplay Adjustments")]
    [DisplayName("Tournament Lifespan Index")]
    [Description(
        "The minimum lifespan applied to a monster that enters a tournament. Still affected by other factors such as fatigue, etc.\n" +
        "Game Default: 3, Recommended: 1\n")]
    [SliderControlParams(
        0.0,
        24,
        1.0,
        4,
        1,
        true,
        SliderControlTickPlacement.BottomRight,
        true,
        true,
        "\\d{1-24}")]
    [DefaultValue(1)]
    public int LifespanReduction { get; set; } = 1;


    //[Category("Advanced - Tournament Ranks") ]
    //[DisplayName("")]
    [Category("Advanced - Monster Growths")]
    [DisplayName("Average Monthly Growth")]
    [Description("The average monthly growth rate for trainer monsters in their prime.\n" +
                 "This value can be impacted by multiple factors including breed, variance, and age.")]
    [DefaultValue(40)]
    public int GrowthMonthly { get; set; } = 40;

    [Category("Advanced - Monster Growths")]
    [DisplayName("Monthly Growth Variance")]
    [Description("The general variance for trainer monster growth rates in prime.\n" +
                 "Higher variance will result in some opponent tournament monsters being powerful for their class.\n" +
                 "Higher variance will result in the appearance of more stragglers (monsters stuck or lagging for their class.")]
    [DefaultValue(12)]
    public int GrowthVariance { get; set; } = 12;

    [Category("Advanced - Monster Growths")]
    [DisplayName("Wildcard Stats")]
    [Description("The odds of a stat being 'wildcard'.\n" +
                 "Lower values will result in significantly less 'cohesive' monsters.\n" +
                 "Higher values will reduce the odds of monsters having 'wild' stats.")]
    [SliderControlParams(1, 500, showTextField: true, isTextFieldEditable: true)]
    [DefaultValue(300)]
    public int Wildcard { get; set; } = 300;

    [Category("Advanced - Monster Growths")]
    [DisplayName("Technique Growth Intelligence")]
    [Description("Determines the behavior of how techniques are learned by tournament monsters.\n" +
                 "Minimal - Uses extremely simple decision making for choosing techs. Almost the wild west for making choices.\n" +
                 "Average - Will attempt to focus on the correct scaling type (Pow vs Int).\n" +
                 "Smart - Will both account for scaling type and relative tech strength.\n" +
                 "Genius - Will not only focus on powerful, relevant techniques, but will also occasionally cheat and prune bad techniques from their monsters.")]
    [DefaultValue(TechInt.Smart)]
    public TechInt TechIntelligence { get; set; } = TechInt.Smart;

    [Category("Advanced - Monster Lifespan")]
    [DisplayName("Monster Lifespan Minimum")]
    [Description("The minimum months a tournament monster will live.\n" +
                 "This is simulated months. Monsters may need time to prepare for their tournaments the same way you do!")]
    [DefaultValue(42)]
    public int LifespanMin { get; set; } = 42;

    [Category("Advanced - Monster Lifespan")]
    [DisplayName("Monster Lifespan Maximum")]
    [Description("The maximum months a tournament monster will live.")]
    [DefaultValue(90)]
    public int LifespanMax { get; set; } = 90;

    [Category("Advanced - Experimental Options")]
    [DisplayName("Enable Autosaves Integration")]
    [Description("Allows the mod to save tournament data for autosaves.\n" +
                 "Considerably increases the number of concurrent file writes and was disabled due to early mod stability concerns.\n" +
                 "Will be evaluated over time and integreated into the mod proper if old issues were resolved.")]
    [DefaultValue(false)]
    public bool Autosaves { get; set; } = false;

    [Category("Advanced - Mod Debugging")]
    [DisplayName("Reloaded Message Verbosity")]
    [Description(
        "Enables internal printouts to the Reloaded Log file to help debug issues or track mod performance.\n" +
        "Error - No debug messages printed except dire, urgent issues. For normal gameplay.\n" +
        "Warning - Prints messages for major events only.\n" +
        "Info - Prints lots messages. Useful if there is consistent crashing.\n" +
        "Debug - Prints so many that the the log may be a source of issue itself. Most helpful for diagnoisng issues though.\n" +
        "Trace - Meant for diagnosing issues internal to the mod's performance itself.")]
    [DefaultValue(Logger.LogLevel.Error)]
    public Logger.LogLevel LogLevel { get; set; } = Logger.LogLevel.Error;
}

/// <summary>
///     Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
///     Override elements in <see cref="ConfiguratorMixinBase" /> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}