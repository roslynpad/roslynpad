// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace RoslynPad.Roslyn.BraceMatching
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportBraceMatcherAttribute : ExportAttribute
    {
        public string Language { get; }

        public ExportBraceMatcherAttribute(string language)
            : base(typeof(IBraceMatcher))
        {
            Language = language ?? throw new ArgumentNullException(nameof(language));
        }
    }
}
