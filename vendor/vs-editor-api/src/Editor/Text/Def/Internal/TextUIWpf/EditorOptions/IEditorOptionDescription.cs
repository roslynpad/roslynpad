using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Text.OptionDescriptions
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <para>
    /// MEF export, exposed using attributes:
    /// <code>
    ///    [Export(typeof(IEditorOptionDescription))]
    ///    [Name(BrandNewOptionDefinitionAndDescription.OptionName)]        // Must either match the name of an <see cref="EditorOptionDefinition"/> or have the [IsOption(false)]
    ///    [Parent(StandardOptionPages.DynamicEditorOptions)]
    ///    [IsRoaming(true)]                                                // defaults to false
    ///    [SettingName("Line Numbers")]                                    // Named used to persist the option. If not provided then the Name attribute is used. If empty, then setting is not persisted.
    ///    [XmlName("Line Numbers")]                                        // Named used to persist the option to .vssettings files. If not provided then the Name attribute is used. If empty, then setting is not persisted.
    ///    [Order(After = ...)]
    /// </code>
    /// </para>
    /// <para>
    /// An <see cref="IEditorOptionDescription"/> export can also be an <see cref="EditorOptionDefinition"/> export. Just have your class derive from <see cref="EditorOptionDefinition"/> and add a
    /// <code>
    ///     [Export(typeof(EditorOptionDefinition))]
    /// </code>
    /// tag.
    /// </para>
    /// </remarks>
    public interface IEditorOptionDescription
    {
        /// <summary>
        /// The localized name of the option. If this property is null, then no control will be created for the option (but the option will be persisted appropriately).
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Keywords that can be used when searching for the option.
        /// </summary>
        string Keywords { get; }

        /// <summary>
        /// Return an <see cref="IOptionControl"/> to manage the option in the tools/options page.
        /// </summary>
        /// <remarks>
        /// <para>This method can return null (in which case, the a default control based on the option's data type will be used).</para>
        /// <para>Only <see cref="bool"/> options have a default control at the moment.</para>
        /// </remarks>
        IOptionControl CreateControl(string displayName, IOptionsPage optionpage, EditorOptionDefinition definition);
    }
}
