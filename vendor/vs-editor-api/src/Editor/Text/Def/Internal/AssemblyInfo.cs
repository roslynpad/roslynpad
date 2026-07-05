//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Avalonia;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

[assembly: InternalsVisibleTo("Morgania.Language.Intellisense.Implementation")]
[assembly: InternalsVisibleTo("Morgania.Platform.VSEditor")]
[assembly: InternalsVisibleTo("Morgania.Editor.Implementation")]
[assembly: InternalsVisibleTo("Morgania.Language.Intellisense.UnitTestHelper")]
[assembly: InternalsVisibleTo("Microsoft.Test.Apex.VisualStudio")]

//
// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//




// Peek types forwarded to Morgania.Language.Intellisense.dll
[assembly: TypeForwardedTo(typeof(IFocusableIntellisensePresenter))]
[assembly: TypeForwardedTo(typeof(IAccurateClassifier))]
[assembly: TypeForwardedTo(typeof(IAccurateTagger<>))]
[assembly: TypeForwardedTo(typeof(IAccurateTagAggregator<>))]
[assembly: TypeForwardedTo(typeof(ITelemetryDiagnosticID<>))]
