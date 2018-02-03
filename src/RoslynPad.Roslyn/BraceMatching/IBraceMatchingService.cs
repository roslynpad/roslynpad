// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace RoslynPad.Roslyn.BraceMatching
{
    public interface IBraceMatchingService
    {
        Task<BraceMatchingResult?> GetMatchingBracesAsync(Document document, int position, CancellationToken cancellationToken = default);
    }

    public struct BraceMatchingResult : IEquatable<BraceMatchingResult>
    {
        public TextSpan LeftSpan { get; }
        public TextSpan RightSpan { get; }

        public BraceMatchingResult(TextSpan leftSpan, TextSpan rightSpan)
            : this()
        {
            LeftSpan = leftSpan;
            RightSpan = rightSpan;
        }

        public bool Equals(BraceMatchingResult other)
        {
            return LeftSpan.Equals(other.LeftSpan) && RightSpan.Equals(other.RightSpan);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is BraceMatchingResult result && Equals(result);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (LeftSpan.GetHashCode() * 397) ^ RightSpan.GetHashCode();
            }
        }

        public static bool operator ==(BraceMatchingResult left, BraceMatchingResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BraceMatchingResult left, BraceMatchingResult right)
        {
            return !left.Equals(right);
        }
    }
}
