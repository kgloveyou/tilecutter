using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace TileCutter
{
    public static class Util
    {
        public static Func<B, R> Partial<A, B, R>(this Func<A, B, R> f, A a)
        {
            return b => f(a, b);
        }

        public static string ToQueryString(this NameValueCollection nvc)
        {
            return string.Join("&", Array.ConvertAll(nvc.AllKeys, key => string.Format("{0}={1}", System.Uri.EscapeUriString(key), System.Uri.EscapeUriString(nvc[key]))));
        }
    }
}
