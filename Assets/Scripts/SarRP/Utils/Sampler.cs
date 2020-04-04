using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SarRP
{
    public static class Sampler
    {
        public static double RadicalInverse(int baseNumber, long n)
        {
            if (n == 0)
                return 0;
            long inversed = 0;
            double inverseBase = 1.0 / baseNumber;
            double inverseExponent = 1;
            while (n != 0)
            {
                inversed = inversed * baseNumber + (n % baseNumber);
                n /= baseNumber;
                inverseExponent *= inverseBase;
            }
            return inversed * inverseExponent;
        }

        public static IEnumerable<double> VanDerCorputSequence(int baseNumber)
        {
            for (long n = 0; ; n++)
                yield return RadicalInverse(baseNumber, n);
        }

        public static IEnumerable<Vector2> HaltonSequence2(int baseX, int baseY)
        {
            for (long n = 0; ; n++)
                yield return new Vector2((float)RadicalInverse(baseX, n), (float)RadicalInverse(baseY, n));
        }
    }
}
