using System.ComponentModel;
using MRDX.Game.MoreMonsters.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;

namespace MRDX.Game.MoreMonsters.Configuration;

public class Config : Configurable<Config>
{

    public enum ScalingGenetics {
        StrongGenetics,
        WildWest
    }

    [Category("Monster Sizes")]
    [DisplayName( "Enable Random Monster Sizes" )]
    [Description(   "Not all monsters are born the same!\n" +
                    "Enabling this feature will have all player owned monsters be different sizes.\n" +
                    "The likiness of certain monster sizes is based on the Monster Size Genetics option.\n" +
                    "Note: Setting sizes too small or large will result in unpredictable monster appearances.")]
    [DefaultValue( true )]
    public bool MonsterSizesEnabled { get; set; } = true;

    [Category( "Monster Sizes" )]
    [DisplayName( "Monster Size Genetics" )]
    [Description( "Determines the behavior of monster size genetics.\n" +
                    "Strong Genetics - Monsters have base sizes (+mutations) that are propagated through combining.\n" +
                    "                  For example Zilla/Pixie will, on average, be smaller than Zilla/Zilla.\n" +
                    "Wild West - 'Bigness' is its own unique trait and passed down through combinations." )]
    [DefaultValue( ScalingGenetics.StrongGenetics )]
    public ScalingGenetics MonsterSizesGenetics { get; set; } = ScalingGenetics.StrongGenetics;

    [Category( "Monster Sizes" )]
    [DisplayName( "Monster Size Minimum" )]
    [Description( "The minimum multiplier for monster sizes." )]
    [DefaultValue( 0.65 )]
    [SliderControlParams( minimum: 0.5, maximum: 1.0, smallChange: 0.01, largeChange: 0.1, tickFrequency: 1,
        isSnapToTickEnabled: false, showTextField: true, isTextFieldEditable: true )]
    public double MonsterSizeMinimum { get; set; } = 0.65;

    [Category( "Monster Sizes" )]
    [DisplayName( "Monster Size Maximum>" )]
    [Description( "The maximum multiplier for monster sizes." )]
    [DefaultValue( 1.8 )]
    [SliderControlParams( minimum: 1.0, maximum: 2.5, smallChange: 0.01, largeChange: 0.1, tickFrequency: 1,
        isSnapToTickEnabled: false, showTextField: true, isTextFieldEditable: true )]
    public double MonsterSizeMaximum { get; set; } = 1.8;



    public enum CombinaitonSettings {
        NoChanges,
        Modified
    }
    [Category( "Combinations" )]
    [DisplayName( "Combination Percent Calculation" )]
    [Description( "Determines the percentage of potential combinations and its effects.\n" +
             "No Changes - Keeps the default behavior of combination to include the new species.\n" +
             "             Maintains the stat bonuses for combining 'rarer' breeds (lower percentages).\n" +
             "             Bug Fix: 2-3% Combination Chances apply the expected stat bonuses.\n" +
             "Modified -   Combination chances are flatter and not breed dependent.\n" +
             "             A flat 85% of parent stat bonuses are used (approximately 2-3%)." )]
    [DefaultValue( CombinaitonSettings.NoChanges )]
    public CombinaitonSettings CombinationChanceAdjustment { get; set; } = CombinaitonSettings.NoChanges;

    [Category( "Combinations" )]
    [DisplayName( "Combination Item Adjustments" )]
    [Description( "Determines the properties of combination items when applied to monsters.\n" +
             "No Changes - Keeps the default behavior of combination items.\n" +
             "Modified - Improves or adjusts the behavior of combination items to make unappealing items more useful.")]
    [DefaultValue( CombinaitonSettings.Modified )]
    public CombinaitonSettings CombinationItemAdjustment { get; set; } = CombinaitonSettings.Modified;

    [Category( "Combinations" )]
    [DisplayName( "Enable Combinations for Special Subspecies" )]
    [Description( "Enables the ability to combine monsters to obtain ??? subspecies.\n" +
         "Note: This enables the XX through YZ genes in combination. Some specials may still be\n" +
         "difficult to combine for, and potential outputs may be not what you expect." )]
    [DefaultValue( false )]
    public bool CombinationSpecialSubspecies { get; set; } = false;
    /*
    [DisplayName( "String" )]
    [Description( "This is a string." )]
    [DefaultValue( "Default Name" )]
    public string String { get; set; } = "Default Name";

    [DisplayName( "Int" )]
    [Description( "This is an int." )]
    [DefaultValue( 42 )]
    public int Integer { get; set; } = 42;

    [DisplayName( "Bool" )]
    [Description( "This is a bool." )]
    [DefaultValue( true )]
    public bool Boolean { get; set; } = true;

    [DisplayName( "Float" )]
    [Description( "This is a floating point number." )]
    [DefaultValue( 6.987654F )]
    public float Float { get; set; } = 6.987654F;
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
