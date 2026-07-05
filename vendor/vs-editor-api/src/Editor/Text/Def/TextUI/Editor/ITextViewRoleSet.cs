//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    using System;
    using System.Collections.Generic;

#pragma warning disable CA1710 // Identifiers should have correct suffix
    /// <summary>
    /// Set of text view roles.
    /// </summary>
    public interface ITextViewRoleSet : IEnumerable<string>
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        /// <summary>
        /// Compute whether the given text view role is a member of the set.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="textViewRole"/> is null.</exception>
        bool Contains(string textViewRole);

        /// <summary>
        /// Compute whether the set contains all of the given text view roles.
        /// </summary>
        /// <exception cref="ArgumentNullException"> if <paramref name="textViewRoles"/> is null.</exception>
        /// <param name="textViewRoles">The list of roles to check for inclusion.</param>
        /// <remarks>
        /// Returns <b>true</b> if <paramref name="textViewRoles"/> contains no roles. Null values 
        /// in <paramref name="textViewRoles"/> are ignored.
        /// </remarks>
        bool ContainsAll(IEnumerable<string> textViewRoles);

        /// <summary>
        /// Compute whether the set contains at least one of the given text view roles. 
        /// </summary>
        /// <param name="textViewRoles">The list of roles to check for inclusion.</param>
        /// <exception cref="ArgumentNullException"> if <paramref name="textViewRoles"/> is null.</exception>
        /// <remarks>
        /// Returns <b>false</b> if <paramref name="textViewRoles"/> contains no roles. Null values 
        /// in <paramref name="textViewRoles"/> are ignored.
        /// </remarks>
        bool ContainsAny(IEnumerable<string> textViewRoles);

        /// <summary>
        /// Compute the union of the set and another text view role set.
        /// </summary>
        /// <param name="roleSet"></param>
        /// <exception cref="ArgumentNullException"> if <paramref name="roleSet"/> is null.</exception>
        ITextViewRoleSet UnionWith(ITextViewRoleSet roleSet);
    }
}
