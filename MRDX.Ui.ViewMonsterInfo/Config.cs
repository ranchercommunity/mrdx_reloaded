using System.ComponentModel;
using MRDX.Ui.ViewMonsterInfo.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;

namespace MRDX.Ui.ViewMonsterInfo.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Banana Type")]
    [Description("Magic Bananas will always apply the following set of effects.\n" +
    "Stress Banana: +10 Fear/Spoil, -1 Form, -10 Stress\n" +
    "Mixed Banana: -10 Fear, +10 Spoil, -15 Fatigue, -5 Stress\n" +
    "Fatigue Banana: -10 Fear/Spoil, +1 Form, -30 Fatigue")]
    [DefaultValue(EConfBananaType.Stress)]

    public EConfBananaType _config_bananaType { get; set; } = EConfBananaType.Stress;

    public enum EConfBananaType { Stress, Mixed, Fatigue }

    [DisplayName( "Banana Coloration" )]
    [Description( "Enables gentle coloration reminders of the selected banana type from this mod.\n" +
    "Stress Banana: These bananas are not looking so ripe.\n" +
    "Mixed Banana: Your regular yellow bananas. Just the way you remember them.\n" +
    "Fatigue Banana: These banans may be past their ripeness." )]
    [DefaultValue( true )]
    public bool _conifg_bananaColors { get; set; } = true;

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
