namespace RoslynPad.UI;

public interface IErrorReporter
{
    void Initialize(string version, IApplicationSettings settings);
    Exception? LastError { get; }
    event Action LastErrorChanged;
    void ClearLastError();
    void ReportError(Exception exception);
}