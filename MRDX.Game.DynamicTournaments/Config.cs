using System.ComponentModel;

using MRDX.Game.DynamicTournaments.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;

namespace MRDX.Game.DynamicTournaments.Configuration;

public class Config : Configurable<Config>
{
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
    [DisplayName( "Monster Species" )]
    [Description( "Determines the types of monsters that are present in tournaments.\n" +
        "Player Only - Besides themed events, tournament monsters use the same unlocked pool of monsters as the player.\n" +
        "Realistic - Monster species variety is loosely tied to the in game year.\n" +
        "PO/Realistic - A combination of Player Only and Realistic criteria allowing for species to be present.\n" +
        "Wild West - Any legal species is available from Year 1000." )]
    [DefaultValue( E_ConfABD_TournamentBreeds.PlayerOnlyRealistic )]
    public E_ConfABD_TournamentBreeds _confABD_tournamentBreeds { get; set; } = E_ConfABD_TournamentBreeds.PlayerOnlyRealistic;
    public enum E_ConfABD_TournamentBreeds { PlayerOnly, Realistic, PlayerOnlyRealistic, WildWest }


    [Category( "Tournament Ranks" )]
    [DisplayName( "Stat Cap - Major 4" )]
    [Description( "The Soft Stat Cap to promote from M4 to Legend Status.\n" +
        "Note: This currently has no effect on the player." )]
    [DefaultValue( 4500 )]
    public int _confABD_tournament_rank_m4 { get; set; } = 4500;

    [Category( "Tournament Ranks" )]
    [DisplayName( "Stat Cap - S Rank" )]
    [Description( "The Soft Stat Cap to promote from S into M4.\n" +
        "Note: This currently has no effect on the player." )]
    [DefaultValue( 3500 )]
    public int _confABD_tournament_rank_s { get; set; } = 3500;

    [Category( "Tournament Ranks" )]
    [DisplayName( "Stat Cap - A Rank" )]
    [Description( "The Soft Stat Cap to promote from A into S.\n" +
    "Note: This currently has no effect on the player." )]
    [DefaultValue( 2700 )]
    public int _confABD_tournament_rank_a { get; set; } = 2700;

    [Category( "Tournament Ranks" )]
    [DisplayName( "Stat Cap - B Rank" )]
    [Description( "The Soft Stat Cap to promote from B into A.\n" +
    "Note: This currently has no effect on the player." )]
    [DefaultValue( 2100 )]
    public int _confABD_tournament_rank_b { get; set; } = 2100;

    [Category( "Tournament Ranks" )]
    [DisplayName( "Stat Cap - C Rank" )]
    [Description( "The Soft Stat Cap to promote from C into B.\n" +
    "Note: This currently has no effect on the player." )]
    [DefaultValue( 1700 )]
    public int _confABD_tournament_rank_c { get; set; } = 1700;

    [Category( "Tournament Ranks" )]
    [DisplayName( "Stat Cap - D Rank" )]
    [Description( "The Soft Stat Cap to promote from D into C.\n" +
    "Note: This currently has no effect on the player." )]
    [DefaultValue( 1300 )]
    public int _confABD_tournament_rank_d { get; set; } = 1300;

    [Category( "Tournament Ranks" )]
    [DisplayName( "Stat Cap - E Rank" )]
    [Description( "The Soft Stat Cap to promote from E into D.\n" +
    "Note: This currently has no effect on the player." )]
    [DefaultValue( 1000 )]
    public int _confABD_tournament_rank_e { get; set; } = 1000;

    [Category( "Tournament Ranks" )]
    [DisplayName( "Stat Cap - Minimum" )]
    [Description( "The lower bounds for the weakest of E Rank Mosters.\n" +
    "Note: This currently has no effect on the player." )]
    [DefaultValue( 800 )]
    public int _confABD_tournament_rank_z { get; set; } = 800;

    //[Category("Advanced - Tournament Ranks") ]
    //[DisplayName("")]
    [ Category( "Advanced - Monster Growths" )]
    [DisplayName( "Average Monthly Growth" )]
    [Description( "The average monthly growth rate for trainer monsters in their prime.\n" +
        "This value can be impacted by multiple factors including breed, variance, and age." )]
    [DefaultValue( 36 )]
    public int _confABD_growth_monthly { get; set; } = 36;

    [Category( "Advanced - Monster Growths" )]
    [DisplayName( "Monthly Growth Variance" )]
    [Description( "The general variance for trainer monster growth rates in prime.\n" +
        "Higher variance will result in some opponent tournament monsters being powerful for their class.\n" +
        "Higher variance will result in the appearance of more stragglers (monsters stuck or lagging for their class.")]
    [DefaultValue( 12 )]
    public int _confABD_growth_monthlyvariance { get; set; } = 12;

    [Category("Advanced - Mod Debugging")]
    [DisplayName("Reloaded Message Verbosity")]
    [Description("Enables internal printouts to the Reloaded Log file to help debug issues.\n" +
        "Off - No debug messages printed. For normal gameplay.\n" +
        "Minimal - Prints messages for major events only.\n" +
        "Medium - Prints lots messages. Useful if there is consistent crashing.\n" +
        "Verbose - Prints so many that the the log may be a source of issue itself. Most helpful for diagnoisng issues though.")]
    [DefaultValue(E_ConfABD_Debugging.Off )]
    public E_ConfABD_Debugging _confABD_debugging { get; set; } = E_ConfABD_Debugging.Off;
    public enum E_ConfABD_Debugging { Off, Minimal, Medium, Verbose }

    /*
    [DisplayName("String")]
    [Description("This is a string.")]
    [DefaultValue("Default Name")]
    public string String { get; set; } = "Default Name";

    [DisplayName("Int")]
    [Description("This is an int.")]
    [DefaultValue(42)]
    public int Integer { get; set; } = 42;

    [DisplayName("Bool")]
    [Description("This is a bool.")]
    [DefaultValue(true)]
    public bool Boolean { get; set; } = true;

    [DisplayName("Float")]
    [Description("This is a floating point number.")]
    [DefaultValue(6.987654F)]
    public float Float { get; set; } = 6.987654F;

    [DisplayName("Enum")]
    [Description("This is an enumerable.")]
    [DefaultValue(SampleEnum.ILoveIt)]
    public SampleEnum Reloaded { get; set; } = SampleEnum.ILoveIt;

    public enum SampleEnum
    {
        NoOpinion,
        Sucks,
        IsMediocre,
        IsOk,
        IsCool,
        ILoveIt
    }
    
    [DisplayName("Int Slider")]
    [Description("This is a int that uses a slider control similar to a volume control slider.")]
    [DefaultValue(100)]
    [SliderControlParams(
        minimum: 0.0,
        maximum: 100.0,
        smallChange: 1.0,
        largeChange: 10.0,
        tickFrequency: 10,
        isSnapToTickEnabled: false,
        tickPlacement:SliderControlTickPlacement.BottomRight,
        showTextField: true,
        isTextFieldEditable: true,
        textValidationRegex: "\\d{1-3}")]
    public int IntSlider { get; set; } = 100;

    [DisplayName("Double Slider")]
    [Description("This is a double that uses a slider control without any frills.")]
    [DefaultValue(0.5)]
    [SliderControlParams(minimum: 0.0, maximum: 1.0)]
    public double DoubleSlider { get; set; } = 0.5;

    [DisplayName("File Picker")]
    [Description("This is a sample file picker.")]
    [DefaultValue("")]
    [FilePickerParams(title:"Choose a File to load from")]
    public string File { get; set; } = "";

    [DisplayName("Folder Picker")]
    [Description("Opens a file picker but locked to only allow folder selections.")]
    [DefaultValue("")]
    [FolderPickerParams(
        initialFolderPath: Environment.SpecialFolder.Desktop,
        userCanEditPathText: false,
        title: "Custom Folder Select",
        okButtonLabel: "Choose Folder",
        fileNameLabel: "ModFolder",
        multiSelect: true,
        forceFileSystem: true)]
    public string Folder { get; set; } = "";*/
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}
