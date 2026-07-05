using System;

namespace Microsoft.VisualStudio.Utilities.BaseUtility
{
    /// <summary>
    /// Declares the default value of an editor option definition, allowing the
    /// option service to obtain the default without instantiating the definition.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DefaultEditorOptionValueAttribute : Attribute
    {
        public DefaultEditorOptionValueAttribute(object value)
        {
            Value = value;
        }

        public object Value { get; }
    }
}
