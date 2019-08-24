using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using RoslynPad.Annotations;

namespace RoslynPad.UI
{
    public abstract class NotificationObject : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        [NotifyPropertyChangedInvocator]
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

#pragma warning disable CS8602 // Possible dereference of a null reference.
            var errors = _propertyErrors.GetOrAdd(propertyName, _ => new List<ErrorInfo>());
#pragma warning restore CS8602 // Possible dereference of a null reference.
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

        public IEnumerable GetErrors(string propertyName)
        {
            List<ErrorInfo>? errors = null;
            _propertyErrors?.TryGetValue(propertyName, out errors);
            return errors?.AsEnumerable() ?? Array.Empty<ErrorInfo>();
        }

        public bool HasErrors => _propertyErrors?.Any(c => c.Value.Any()) == true;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        protected class ErrorInfo
        {
            public ErrorInfo(string id, string message)
            {
                Id = id;
                Message = message;
            }

            public string Id { get; }

            public string  Message { get; }

            public override string ToString() => Message;
        }
    }
}