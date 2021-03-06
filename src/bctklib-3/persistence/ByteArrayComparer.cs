﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Neo.BlockchainToolkit.Persistence
{
    internal class ByteArrayComparer : IEqualityComparer<byte[]>, IComparer<byte[]>
    {
        public static ByteArrayComparer Default { get; } = new ByteArrayComparer(false);
        public static ByteArrayComparer Reverse { get; } = new ByteArrayComparer(true);

        private readonly bool reverse;

        private ByteArrayComparer(bool reverse = false)
        {
            this.reverse = reverse;
        }

        public int Compare([AllowNull] byte[] x, [AllowNull] byte[] y)
        {
            return reverse ? -Compare() : Compare();

            int Compare()
            {
                if (x == null && y == null)
                    return 0;
                if (x == null)
                    return -1;
                if (y == null)
                    return 1;
                return x.AsSpan().SequenceCompareTo(y.AsSpan());
            }
        }

        public bool Equals([AllowNull] byte[] x, [AllowNull] byte[] y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.AsSpan().SequenceEqual(y.AsSpan());
        }

        public int GetHashCode([DisallowNull] byte[] obj)
        {
            int hash = 0;
            for (int i = 0; i < obj.Length; i++)
            {
                hash = HashCode.Combine(hash, i, obj[i]);
            }
            return hash;
        }
    }
}
