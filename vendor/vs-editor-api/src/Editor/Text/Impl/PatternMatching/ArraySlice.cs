// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using TextSpan = Microsoft.VisualStudio.Text.Span;

namespace Microsoft.VisualStudio.Text.PatternMatching.Implementation
{
    internal struct ArraySlice<T>
    {
        private readonly T[] _array;
        private int _start;
        private int _length;

        public int Length => _length;

        public ArraySlice(T[] array) : this(array, 0, array.Length)
        {
        }

        public ArraySlice(T[] array, TextSpan span) : this(array, span.Start, span.Length)
        {
        }

        public ArraySlice(T[] array, int start, int length) : this()
        {
            _array = array;
            SetStartAndLength(start, length);
        }

        public T this[int i]
        {
            get
            {
                Debug.Assert(i < _length);
                return _array[i + _start];
            }
        }

        private void SetStartAndLength(int start, int length)
        {
            if (start < 0)
            {
                throw new ArgumentException($"{start} < {0}", nameof(start));
            }

            if (start > _array.Length)
            {
                throw new ArgumentException($"{start} > {_array.Length}", nameof(start));
            }

            CheckLength(start, length);

            _start = start;
            _length = length;
        }

        private void CheckLength(int start, int length)
        {
            if (length < 0)
            {
                throw new ArgumentException($"{length} < {0}", nameof(length));
            }

            if (start + length > _array.Length)
            {
                throw new ArgumentException($"{start} + {length} > {_array.Length}", nameof(start));
            }
        }

        public void MoveStartForward(int amount)
        {
            SetStartAndLength(_start + amount, _length - amount);
        }

        public void SetLength(int length)
        {
            CheckLength(_start, length);
            _length = length;
        }
    }
}

