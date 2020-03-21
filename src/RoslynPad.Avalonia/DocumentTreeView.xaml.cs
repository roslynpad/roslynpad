using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using RoslynPad.UI;

namespace RoslynPad
{
    class DocumentTreeView : UserControl
    {
        private MainViewModel _viewModel;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public DocumentTreeView()
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            AvaloniaXamlLoader.Load(this);
            var treeView = this.Find<TreeView>("Tree");
            treeView.ItemContainerGenerator.Materialized += ItemContainerGenerator_Materialized;
            treeView.ItemContainerGenerator.Dematerialized += ItemContainerGenerator_Dematerialized;
        }

        private void ItemContainerGenerator_Materialized(object? sender, Avalonia.Controls.Generators.ItemContainerEventArgs e)
        {
            foreach (var item in e.Containers)
            {
                if (item.ContainerControl is TreeViewItem treeViewItem)
                {
                    treeViewItem.DoubleTapped += OnDocumentClick;
                    treeViewItem.KeyDown += OnDocumentKeyDown;
                }
            }
        }

        private void ItemContainerGenerator_Dematerialized(object? sender, Avalonia.Controls.Generators.ItemContainerEventArgs e)
        {
            foreach (var item in e.Containers)
            {
                if (item.ContainerControl is TreeViewItem treeViewItem)
                {
                    treeViewItem.DoubleTapped -= OnDocumentClick;
                    treeViewItem.KeyDown -= OnDocumentKeyDown;
                }
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            _viewModel = (MainViewModel)DataContext;
        }


        private void OnDocumentClick(object? sender, RoutedEventArgs e)
        {
            OpenDocument(e.Source);
        }

        private void OnDocumentKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OpenDocument(e.Source);
            }
        }

        private void OpenDocument(object source)
        {
            var documentViewModel = (DocumentViewModel)((Control)source).DataContext;
            _viewModel.OpenDocument(documentViewModel);
        }
    }
}
