using System;
using System.Threading.Tasks;

namespace VisualPi3
{
    public static class HexPiMath
    {
        public static char ToHex(byte dec)
        {
            if (dec > 9)
                return (char)(dec + 55);
            return (char)(dec + 48);
        }

        private static long ModPow(long b, long e, long m)
        {
            long res = 1;
            while (e != 0)
            {
                if ((e & 1) != 0)
                    res = (res * b) % m;
                e >>= 1;
                b = b * b % m;
            }
            return res;
        }

        private static double FP(double a)
        {
            return a - ((long)(a + 10000) - 10000);
        }

        private static double SP(long n, long j)
        {
            double sum = 0;
            long denomtop = j;
            double denom = j;

            while (n >= 0)
            {
                sum = FP(sum + (ModPow(16, n, denomtop) / denom));
                denomtop += 8;
                denom += 8.0;
                --n;
            }

            double num = 0.0625;
            num /= denom;

            while (num > double.Epsilon)
            {
                sum += num;
                num *= 0.0078125;
            }

            return sum;
        }

        private static byte PiDec(long n)
        {
            double p1 = 4.0 * SP(n, 1);
            double p2 = 2.0 * SP(n, 4);
            double p3 = SP(n, 5);
            double p4 = SP(n, 6);
            double sum = p1 - p2 - p3 - p4;
            byte res = (byte)(16.0 * FP(sum));
            return res;
        }

        public static char PiHex(long n)
        {
            return ToHex(PiDec(n));
        }

        private static char[] PiThread(long start, int amount, int thread, int threadct)
        {
            long iteration = start + thread;
            char[] result = new char[amount];
            for (int i = 0; i < amount; ++i)
            {
                result[i] = PiHex(iteration);
                iteration += threadct;
            }
            return result;
        }

        public static string PiGroup(long start, int length, int threads)
        {
            int amount = length / threads;
            if (length - (amount * threads) != 0)
                throw new ArgumentException("length has to be divisible by threads.");

            Task<char[]>[] tasks = new Task<char[]>[threads];
            for (int i = 0; i < threads; ++i)
            {
                int localI = i;
                tasks[i] = new Task<char[]>(() => PiThread(start, amount, localI, threads));
                tasks[i].Start();
            }
            Task.WaitAll(tasks);

            char[] result = new char[length];
            int currAmount = 0;
            for (int i = 0; i < amount; ++i)
            {
                for (int j = 0; j < threads; ++j)
                {
                    result[currAmount] = tasks[j].Result[i];
                    ++currAmount;
                }
            }

            return new string(result);
        }
    }
}
