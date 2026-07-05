namespace Microsoft.VisualStudio.Text.Editor
{
    internal class ConvertTabsToSpacesContext
    {
        public static readonly ConvertTabsToSpacesContext FromCodingConventions = new ConvertTabsToSpacesContext(true);
        public static readonly ConvertTabsToSpacesContext FromToolsOptions = new ConvertTabsToSpacesContext(false);

        public bool SettingFromCodingConventions { get; }

        private ConvertTabsToSpacesContext(bool settingFromCodingConventions)
        {
            this.SettingFromCodingConventions = settingFromCodingConventions;
        }
    }
}
