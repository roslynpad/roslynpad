#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using System.Collections;

/// <summary>
/// A case-insensitive set of text view roles.
/// </summary>
internal sealed class TextViewRoleSet : ITextViewRoleSet
{
    private readonly HashSet<string> _roles;

    public TextViewRoleSet(IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(roles);
        _roles = new HashSet<string>(roles.Where(role => role is not null), StringComparer.OrdinalIgnoreCase);
    }

    public bool Contains(string textViewRole)
    {
        ArgumentNullException.ThrowIfNull(textViewRole);
        return _roles.Contains(textViewRole);
    }

    public bool ContainsAll(IEnumerable<string> textViewRoles)
    {
        ArgumentNullException.ThrowIfNull(textViewRoles);
        return textViewRoles.All(role => role is null || _roles.Contains(role));
    }

    public bool ContainsAny(IEnumerable<string> textViewRoles)
    {
        ArgumentNullException.ThrowIfNull(textViewRoles);
        return textViewRoles.Any(role => role is not null && _roles.Contains(role));
    }

    public ITextViewRoleSet UnionWith(ITextViewRoleSet roleSet)
    {
        ArgumentNullException.ThrowIfNull(roleSet);
        return new TextViewRoleSet(_roles.Concat(roleSet));
    }

    public IEnumerator<string> GetEnumerator() => _roles.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => string.Join(",", _roles);
}
