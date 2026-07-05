//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored helper for the System.Composition metadata view classes that
//  replaced the MEF v1 interface views (PLAN §5.2 conversion rule 2). Metadata values
//  arrive as raw dictionary entries: a repeated metadata attribute yields an array,
//  a single occurrence yields a scalar, and optional keys may be absent. MEF v1
//  interface views hid this behind proxy generation; these helpers do the same
//  normalization explicitly. Missing keys yield defaults (v1's [DefaultValue] cases
//  in the converted views all defaulted to null).
//
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Utilities
{
    public static class MetadataValue
    {
        public static T Get<T>(IDictionary<string, object> data, string name)
        {
            return data.TryGetValue(name, out object value) && value is T typed ? typed : default(T);
        }

        public static IEnumerable<T> GetMany<T>(IDictionary<string, object> data, string name)
        {
            if (!data.TryGetValue(name, out object value) || value == null)
            {
                return null;
            }
            if (value is T single)
            {
                return new[] { single };
            }
            if (value is IEnumerable<T> typed)
            {
                return typed;
            }
            if (value is IEnumerable untyped)
            {
                return untyped.Cast<T>().ToArray();
            }
            return null;
        }
    }
}
