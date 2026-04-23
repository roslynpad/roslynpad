using System.Collections.ObjectModel;
using RoslynPad.Runtime;
using RoslynPad.Utilities;

namespace RoslynPad.UI;

public class SecretsViewModel : NotificationObject, IDocumentContent
{
    private readonly SecretManager _secretManager;
    private readonly IClipboardService _clipboardService;

    public SecretsViewModel(ICommandProvider commands, IClipboardService clipboardService)
    {
        _secretManager = SecretManager.Default;
        _clipboardService = clipboardService;
        Directory = _secretManager.Directory;
        Secrets = [];

        AddCommand = commands.Create(AddSecret);
        RemoveCommand = commands.CreateAsync<SecretItemViewModel>(RemoveSecretAsync);
        CopyCommand = commands.CreateAsync<SecretItemViewModel>(CopySecretAsync);
        SaveNewSecretCommand = commands.Create<SecretItemViewModel>(SaveNewSecret);
        CancelNewSecretCommand = commands.Create<SecretItemViewModel>(CancelNewSecret);
        SaveValueCommand = commands.Create<SecretItemViewModel>(SaveValue);

        Refresh();
    }

    public string Id => "secrets";

    public string Title => "Secrets";

    public bool IsDirty => false;

    public string Directory { get; }

    public ObservableCollection<SecretItemViewModel> Secrets { get; }

    public IDelegateCommand AddCommand { get; }

    public IDelegateCommand<SecretItemViewModel> RemoveCommand { get; }

    public IDelegateCommand<SecretItemViewModel> CopyCommand { get; }

    public IDelegateCommand<SecretItemViewModel> SaveNewSecretCommand { get; }

    public IDelegateCommand<SecretItemViewModel> CancelNewSecretCommand { get; }

    public IDelegateCommand<SecretItemViewModel> SaveValueCommand { get; }

    /// <summary>
    /// Platform callback to confirm removal of a secret.
    /// Returns true if the user confirms.
    /// </summary>
    public Func<string, ValueTask<bool>>? ConfirmRemoveSecret { get; set; }

    public string? Error { get; private set; }

    public void Refresh()
    {
        try
        {
            ClearError();
            Secrets.Clear();
            foreach (var (name, _) in _secretManager.GetAll())
            {
                Secrets.Add(new SecretItemViewModel(name, _secretManager, ReportError));
            }
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void ReportError(Exception ex)
    {
        Error = ex.Message;
        OnPropertyChanged(nameof(Error));
    }

    private void ClearError()
    {
        Error = null;
        OnPropertyChanged(nameof(Error));
    }

    private void AddSecret()
    {
        if (Secrets.Any(s => s.IsEditing))
        {
            return;
        }

        var item = new SecretItemViewModel(_secretManager, ReportError) { IsEditing = true };
        Secrets.Insert(0, item);
    }

    private void SaveNewSecret(SecretItemViewModel? item)
    {
        if (item is null || !item.IsEditing)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(item.EditName))
        {
            return;
        }

        try
        {
            ClearError();
            _secretManager.SetString(item.EditName, item.EditValue ?? string.Empty);
            Refresh();
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void CancelNewSecret(SecretItemViewModel? item)
    {
        if (item is not null)
        {
            Secrets.Remove(item);
        }
    }

    private async Task RemoveSecretAsync(SecretItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        if (ConfirmRemoveSecret is { } confirm &&
            !await confirm(item.Name).ConfigureAwait(true))
        {
            return;
        }

        try
        {
            ClearError();
            _secretManager.Remove(item.Name);
            Refresh();
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private async Task CopySecretAsync(SecretItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            ClearError();
            var value = _secretManager.GetString(item.Name);
            if (value is not null)
            {
                await _clipboardService.SetTextAsync(value).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void SaveValue(SecretItemViewModel? item)
    {
        if (item is null || !item.IsRevealed)
        {
            return;
        }

        try
        {
            ClearError();
            _secretManager.SetString(item.Name, item.EditableValue ?? string.Empty);
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }
}

public class SecretItemViewModel : NotificationObject
{
    private readonly SecretManager _secretManager;
    private readonly Action<Exception>? _onError;

    public SecretItemViewModel(string name, SecretManager secretManager, Action<Exception> onError)
    {
        Name = name;
        _secretManager = secretManager;
        _onError = onError;
    }

    public SecretItemViewModel(SecretManager secretManager, Action<Exception> onError)
    {
        Name = string.Empty;
        _secretManager = secretManager;
        _onError = onError;
    }

    public string Name { get; }

    public bool IsEditing { get; set; }

    public string? EditName { get; set; }

    public string? EditValue { get; set; }

    public string? EditableValue { get; set; }

    public bool IsRevealed
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                try
                {
                    EditableValue = value
                        ? _secretManager.GetString(Name) ?? string.Empty
                        : null;
                    OnPropertyChanged(nameof(EditableValue));
                }
                catch (Exception ex)
                {
                    field = !value;
                    OnPropertyChanged(nameof(IsRevealed));
                    _onError?.Invoke(ex);
                }
            }
        }
    }
}
