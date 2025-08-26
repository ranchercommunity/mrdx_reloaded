using System.ComponentModel;
using MRDX.Base.Mod.Interfaces;
using MRDX.Qol.InfiniteFreezer.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;

namespace MRDX.Qol.InfiniteFreezer.Configuration;

public class Config : Configurable<Config>
{

    [Category( "Advanced - Mod Debugging" )]
    [DisplayName( "Reloaded Message Verbosity" )]
    [Description(
        "Enables internal printouts to the Reloaded Log file to help debug issues or track mod performance.\n" +
        "Error - No debug messages printed except dire, urgent issues. For normal gameplay.\n" +
        "Warning - Prints messages for major events only.\n" +
        "Info - Prints lots messages. Useful if there is consistent crashing.\n" +
        "Debug - Considerably helpful for diagnoisng issues though.\n" +
        "Trace - Meant for diagnosing issues internal to the mod's performance itself." )]
    [DefaultValue( Logger.LogLevel.Error )]
    public Logger.LogLevel LogLevel { get; set; } = Logger.LogLevel.Error;

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
