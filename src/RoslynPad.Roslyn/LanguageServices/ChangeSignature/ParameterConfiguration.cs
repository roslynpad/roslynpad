using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature
{
    internal sealed class ParameterConfiguration
    {
        private readonly Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration _inner;

        public ParameterConfiguration(Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration inner)
        {
            _inner = inner;
            ThisParameter = inner.ThisParameter;
            ParametersWithoutDefaultValues = inner.ParametersWithoutDefaultValues;
            RemainingEditableParameters = inner.RemainingEditableParameters;
            ParamsParameter = inner.ParamsParameter;
        }

        public ParameterConfiguration(IParameterSymbol thisParameter, List<IParameterSymbol> parametersWithoutDefaultValues, List<IParameterSymbol> remainingEditableParameters, IParameterSymbol paramsParameter)
        {
            ThisParameter = thisParameter;
            ParametersWithoutDefaultValues = parametersWithoutDefaultValues;
            RemainingEditableParameters = remainingEditableParameters;
            ParamsParameter = paramsParameter;
        }

        public IParameterSymbol ThisParameter { get; }
        public List<IParameterSymbol> ParametersWithoutDefaultValues { get; }
        public List<IParameterSymbol> RemainingEditableParameters { get; }
        public IParameterSymbol ParamsParameter { get; }

        internal Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration ToInternal()
        {
            return _inner ??
                   new Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration(ThisParameter,
                       ParametersWithoutDefaultValues, RemainingEditableParameters, ParamsParameter);
        }
    }
}