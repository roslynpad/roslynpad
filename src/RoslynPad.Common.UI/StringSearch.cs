using System.Buffers;

namespace RoslynPad.Converters;

public static class StringSearch
{
    public static IndicesEnumerator GetIndices(string text, SearchValues<char> searchValues) => new(text, searchValues);

    public struct IndicesEnumerator
    {
        private readonly string _text;
        private readonly SearchValues<char> _searchValues;

        public IndicesEnumerator(string text, SearchValues<char> searchValues)
        {
            _text = text;
            _searchValues = searchValues;
            Reset();
        }

        public int Current { get; private set; }

        public bool MoveNext()
        {
            var current = Current + 1;
            var index = _text.AsSpan(current).IndexOfAny(_searchValues);
            if (index >= 0)
            {
                Current = current + index;
                return true;
            }

            Reset();
            return false;
        }

        public void Reset() => Current = -1;

        public readonly IndicesEnumerator GetEnumerator() => this;
    }
}
