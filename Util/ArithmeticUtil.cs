using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SingleCycleMIPS.Util
{
    public static class ArithmeticUtil
    {
        public static int Clamp(this int number, int min, int max)
        {
            if (number <= min)
                return min;
            if (number >= max)
                return max;
            return number;
        }

        // bit hacks from the following site
        // https://graphics.stanford.edu/~seander/bithacks.html
        public static bool IsPowerOfTwo(this int number)
        {
            if (number == 0)
                return false;

            return (number & number - 1) == 0;
        }

        public static int ClosetPowerOfTwo(this int number)
        {
            number--;
            number |= number >> 1;
            number |= number >> 2;
            number |= number >> 4;
            number |= number >> 8;
            number |= number >> 16;
            number++;
            return number;
        }

        private static readonly int[] Lookup32 = new int[]
        {
            0,  9,  1, 10, 13, 21,  2, 29,
            11, 14, 16, 18, 22, 25,  3, 30,
            8, 12, 20, 28, 15, 17, 24,  7,
            19, 27, 23,  6, 26,  5,  4, 31
        };

        public static int Lg(this int number)
        {
            return (int)Math.Log2(number);
            /*
            number |= number >> 1;
            number |= number >> 2;
            number |= number >> 4;
            number |= number >> 8;
            number |= number >> 16;
            return Lookup32[(int)(number * 0x07C4ACDD) >> 27];
            */
        }

        public static int ChangeByteOrder(this int integer)
        {
            var ret = ((integer & 0xFF) << 24)
                    | ((integer & 0xFF00) << 8)
                    | ((integer >> 8) & 0xFF00)
                    | ((integer >> 24) & 0xFF);
            return ret;
        }

    }
}
