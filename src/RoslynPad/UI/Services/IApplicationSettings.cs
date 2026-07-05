namespace RoslynPad.UI;

public interface IApplicationSettings
{
    void LoadDefault();
    void LoadFrom(string path);
    string GetDefaultDocumentPath();

    IApplicationSettingsValues Values { get; }
}
