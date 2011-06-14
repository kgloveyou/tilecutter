using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TileCutter
{
    public static class Util
    {
        public static Func<B, R> Partial<A, B, R>(this Func<A, B, R> f, A a)
        {
            return b => f(a, b);
        }
    }
}
