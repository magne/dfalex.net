using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CodeHive.DfaLex.tree
{
    public static class StringExtensions
    {
        public static string AsString(this object obj)
        {
            if (obj != null)
            {
                if (obj.GetType() == typeof(IDictionary<,>))
                {
                    return ((IDictionary<object, object>) obj).AsString();
                }

                if (obj is IEnumerable list)
                {
                    return list.AsString();
                }
            }

            return obj?.ToString();
        }

        public static string AsString(this IEnumerable list)
        {
            var sb = new StringBuilder();
            sb.Append('[');
            var first = true;
            foreach (var item in list)
            {
                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append(item);
                first = false;
            }

            sb.Append(']');
            return sb.ToString();
        }

        public static string AsString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            var first = true;
            foreach (var item in dictionary)
            {
                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append('<').Append(item.Key.AsString()).Append(", ").Append(item.Value.AsString()).Append('>');
                first = false;
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}
