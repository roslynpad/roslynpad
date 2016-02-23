using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;

namespace RoslynPad.Utilities
{
    internal static class CompositionContainerExtensions
    {
        private static readonly MethodInfo _getExportedValuesMethod = typeof(CompositionContainer).GetMethod(nameof(CompositionContainer.GetExportedValues), Type.EmptyTypes);

        public static IEnumerable<object> GetExportedValues(this CompositionContainer container, Type type)
        {
            var method = _getExportedValuesMethod.MakeGenericMethod(type);
            return (IEnumerable<object>)method.Invoke(container, null);
        }
    }
}