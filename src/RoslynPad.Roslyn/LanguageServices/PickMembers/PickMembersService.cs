// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PickMembers;

namespace RoslynPad.Roslyn.LanguageServices.PickMembers;

[ExportWorkspaceService(typeof(IPickMembersService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
internal class PickMembersService(ExportFactory<IPickMembersDialog> dialogFactory) : IPickMembersService
{
    private readonly ExportFactory<IPickMembersDialog> _dialogFactory = dialogFactory;

    public PickMembersResult PickMembers(
        string title, ImmutableArray<ISymbol> members, ImmutableArray<PickMembersOption> options = default, bool selectAll = true)
    {
        options = options.NullToEmpty();

        var viewModel = new PickMembersDialogViewModel(members, options);
        var dialog = _dialogFactory.CreateExport().Value;
        dialog.Title = title;
        dialog.ViewModel = viewModel;
        if (dialog.Show() == true)
        {
            return new PickMembersResult(
                viewModel.MemberContainers.Where(c => c.IsChecked)
                                          .Select(c => c.MemberSymbol)
                                          .ToImmutableArray(), 
                options, selectAll);
        }
        else
        {
            return PickMembersResult.Canceled;
        }
    }
}

internal interface IPickMembersDialog : IRoslynDialog
{
    string? Title { get; set; }
}
