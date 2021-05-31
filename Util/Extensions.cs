using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AiAndGamesJam {
    public static class ExtensionMethods {
        public static void Deconstruct<T>(this IList<T> list, out T first, out IList<T> rest) {
            first = list.Count > 0 ? list[0] : default; // or throw
            rest = list.Skip(1).ToList();
        }

        public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out IList<T> rest) {
            first = list.Count > 0 ? list[0] : default; // or throw
            second = list.Count > 1 ? list[1] : default; // or throw
            rest = list.Skip(2).ToList();
        }

        public static int GetCardinality(this BitArray bitArray) {

            int[] counts = new int[(bitArray.Count >> 5) + 1];

            bitArray.CopyTo(counts, 0);

            int count = 0;

            // fix for not truncated bits in last integer that may have been set to true with SetAll()
            counts[^1] &= ~(-1 << (bitArray.Count % 32));

            for (int i = 0; i < counts.Length; i++) {

                int c = counts[i];

                // magic (http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel)
                unchecked {
                    c -= (c >> 1) & 0x55555555;
                    c = (c & 0x33333333) + ((c >> 2) & 0x33333333);
                    c = ((c + (c >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
                }

                count += c;

            }

            return count;

        }
    }
}