using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using RoslynPad.Utilities;

namespace RoslynPad.Roslyn.LanguageServices.ChangeSignature
{
    internal sealed class ParameterConfiguration
    {
        internal static readonly Type Type = Type.GetType("Microsoft.CodeAnalysis.ChangeSignature.ParameterConfiguration, Microsoft.CodeAnalysis.Features", throwOnError: true);

        private readonly object _inner;

        public ParameterConfiguration(object inner)
        {
            _inner = inner;
            ThisParameter = inner.GetFieldValue<IParameterSymbol>(nameof(ThisParameter));
            ParametersWithoutDefaultValues = inner.GetFieldValue<List<IParameterSymbol>>(nameof(ParametersWithoutDefaultValues));
            RemainingEditableParameters = inner.GetFieldValue<List<IParameterSymbol>>(nameof(RemainingEditableParameters));
            ParamsParameter = inner.GetFieldValue<IParameterSymbol>(nameof(ParamsParameter));
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

        internal object ToInternal()
        {
            if (_inner != null) return _inner;

            var constructorInfo =
                Type.GetConstructor(new[]
                {
                    typeof (IParameterSymbol), typeof (List<IParameterSymbol>), typeof (List<IParameterSymbol>),
                    typeof (IParameterSymbol)
                });
            if (constructorInfo == null)
            {
                throw new MissingMemberException("Missing internal constructor");
            }
            return constructorInfo.Invoke(new object[]
            {
                ThisParameter,
                ParametersWithoutDefaultValues,
                RemainingEditableParameters,
                ParamsParameter
            });
        }
    }
}