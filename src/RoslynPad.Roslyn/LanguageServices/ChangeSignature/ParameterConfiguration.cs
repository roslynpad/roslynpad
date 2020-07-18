using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ChangeSignature;

namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature
{
    internal sealed class ParameterConfiguration
    {
        private readonly Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration? _inner;

        public ParameterConfiguration(Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration inner)
        {
            _inner = inner;
            ThisParameter = inner.ThisParameter;
            ParametersWithoutDefaultValues = inner.ParametersWithoutDefaultValues;
            RemainingEditableParameters = inner.RemainingEditableParameters;
            ParamsParameter = inner.ParamsParameter;
            SelectedIndex = inner.SelectedIndex;
        }

        public ParameterConfiguration(ExistingParameter? thisParameter, ImmutableArray<Parameter> parametersWithoutDefaultValues, ImmutableArray<Parameter> remainingEditableParameters, ExistingParameter? paramsParameter, int selectedIndex)
        {
            ThisParameter = thisParameter;
            ParametersWithoutDefaultValues = parametersWithoutDefaultValues;
            RemainingEditableParameters = remainingEditableParameters;
            ParamsParameter = paramsParameter;
            SelectedIndex = selectedIndex;
        }

        public ExistingParameter? ThisParameter { get; }
        public ImmutableArray<Parameter> ParametersWithoutDefaultValues { get; }
        public ImmutableArray<Parameter> RemainingEditableParameters { get; }
        public ExistingParameter? ParamsParameter { get; }
        public int SelectedIndex { get; set; }

        internal Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration ToInternal()
        {
            return _inner ??
                   new Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration(ThisParameter,
                       ParametersWithoutDefaultValues, RemainingEditableParameters, ParamsParameter, SelectedIndex);
        }
    }
}
