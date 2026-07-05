//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System;
using Avalonia;
using Avalonia.Media;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    public static class VisualTreeExtensions
    {
        /// <summary>
        /// This uses the visual tree first, then diverts to the logical tree if the visual tree ends.
        /// This is necessary for the TabControl, where using either the logical tree or visual tree
        /// by themselves is insufficient.
        /// </summary>
        /// <param name="sourceElement">The object to begin searching from.</param>
        /// <returns>The visual tree parent of the object, or the logical tree parent if
        /// the visual tree parent is null, or null if the logical tree parent is null.</returns>
        public static AvaloniaObject GetVisualOrLogicalParent(AvaloniaObject sourceElement)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Helper method that finds the first ancestor of a given Type in the
        /// logical or visual tree, or the object itself if it matches in type.
        /// </summary>
        /// <typeparam name="TAncestorType">The type of ancestor to find.</typeparam>
        /// <typeparam name="TElementType">The base type of intermediate elements in the ancestor tree.</typeparam>
        /// <param name="element">The object at which to begin searching.</param>
        /// <returns>The object itself, if it matches in type, else the first ancestor of type T in the parent chain of obj,
        /// or null if no ancestor is found.</returns>
        /// <remarks>
        /// The type of <paramref name="element"/> is Visual rather than AvaloniaObject in order to disambiguate this method 
        /// from Morgania.PlatformUI.Shell.ExtensionMethods.FindAncestor(ViewElement element).  If you need
        /// to find an ancestor of a non-Visual AvaloniaObject you should call 
        /// FindAncestor&lt;TAncestorType, AvaloniaObject&gt;(obj, GetVisualOrLogicalParent) directly.
        /// </remarks>
        public static TAncestorType FindAncestorOrSelf<TAncestorType, TElementType>(TElementType element, Func<TElementType, TElementType> parentEvaluator) where TAncestorType : AvaloniaObject
        {
            return element as TAncestorType ?? FindAncestor<TAncestorType, TElementType>(element, parentEvaluator);
        }

        /// <summary>
        /// Helper method that finds the first ancestor in the logical or visual tree, or the object itself, that is accepted by the ancestor selector function.
        /// </summary>
        /// <param name="element">The object at which to begin searching.</param>
        /// <param name="parentEvaluator">The method used to determine the parent of an element.</param>
        /// <param name="ancestorSelector">The method used to select an ancestor of interest.</param>
        /// <typeparam name="TElementType">The base type of intermediate elements in the ancestor tree.</typeparam>
        /// <returns>The first ancestor in the parent chain of obj accepted by the the ancestor selector function, or null if no ancestor is found.</returns>
        public static object FindAncestorOrSelf<TElementType>(TElementType element, Func<TElementType, TElementType> parentEvaluator, Func<TElementType, bool> ancestorSelector)
        {
            if (ancestorSelector(element))
                return element;

            return FindAncestor<TElementType>(element, parentEvaluator, ancestorSelector);
        }

        /// <summary>
        /// Helper method that finds the first ancestor of a given Type in the
        /// logical or visual tree.
        /// </summary>
        /// <typeparam name="TAncestorType">The type of ancestor to find.</typeparam>
        /// <typeparam name="TElementType">The base type of intermediate elements in the ancestor tree.</typeparam>
        /// <param name="element">The object at which to begin searching.</param>
        /// <param name="parentEvaluator">The method used to determine the parent of an element.</param>
        /// <returns>The first ancestor of type T in the parent chain of obj, or null
        /// if no ancestor is found.</returns>
        public static TAncestorType FindAncestor<TAncestorType, TElementType>(TElementType element, Func<TElementType, TElementType> parentEvaluator) where TAncestorType : class
        {
            return FindAncestor<TElementType>(element, parentEvaluator, ancestor => ancestor is TAncestorType) as TAncestorType;
        }

        /// <summary>
        /// Helper method that finds the first ancestor in the logical or visual tree that is accepted by the ancestor selector function.
        /// </summary>
        /// <param name="element">The object at which to begin searching.</param>
        /// <param name="parentEvaluator">The method used to determine the parent of an element.</param>
        /// <param name="ancestorSelector">The method used to select an ancestor of interest.</param>
        /// <typeparam name="TElementType">The base type of intermediate elements in the ancestor tree.</typeparam>
        /// <returns>The first ancestor in the parent chain of obj accepted by the the ancestor selector function.</returns>
        public static object FindAncestor<TElementType>(TElementType element, Func<TElementType, TElementType> parentEvaluator, Func<TElementType, bool> ancestorSelector)
        {
            TElementType parent = parentEvaluator(element);
            while (parent != null)
            {
                if (ancestorSelector(parent))
                {
                    return parent;
                }

                parent = parentEvaluator(parent);
            }

            return null;
        }
    }
}
