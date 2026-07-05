using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RoslynPad.UI;

public abstract class NotificationObject : INotifyPropertyChanged, INotifyDataErrorInfo
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>([NotNullIfNotNull(nameof(value))] ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        return false;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private ConcurrentDictionary<string, List<ErrorInfo>>? _propertyErrors;

    protected void SetError(string propertyName, string id, string message)
    {
        if (_propertyErrors == null)
        {
            LazyInitializer.EnsureInitialized(ref _propertyErrors, () => new ConcurrentDictionary<string, List<ErrorInfo>>());
        }

        var errors = _propertyErrors.GetOrAdd(propertyName, _ => []);
        errors.RemoveAll(e => e.Id == id);
        errors.Add(new ErrorInfo(id, message));

        OnErrorsChanged(propertyName);
    }

    protected void ClearError(string propertyName, string id)
    {
        if (_propertyErrors == null) return;

        _propertyErrors.TryGetValue(propertyName, out var errors);
        if (errors?.RemoveAll(e => e.Id == id) > 0)
        {
            OnErrorsChanged(propertyName);
        }
    }

    protected void ClearErrors(string propertyName)
    {
        if (_propertyErrors == null) return;

        _propertyErrors.TryGetValue(propertyName, out var errors);
        if (errors?.Count > 0)
        {
            errors.Clear();

            OnErrorsChanged(propertyName);
        }
    }

    public IEnumerable GetErrors(string? propertyName)
    {
        if (propertyName == null)
        {
            return Array.Empty<ErrorInfo>();
        }

        List<ErrorInfo>? errors = null;
        _propertyErrors?.TryGetValue(propertyName, out errors);
        return errors?.AsEnumerable() ?? [];
    }

    public bool HasErrors => _propertyErrors?.Any(c => c.Value.Count != 0) == true;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    protected virtual void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    protected class ErrorInfo(string id, string message)
    {
        public string Id { get; } = id;

        public string Message { get; } = message;

        public override string ToString() => Message;
    }
}
