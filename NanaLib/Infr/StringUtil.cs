using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Nana.Delegates;
using System.Text.RegularExpressions;

namespace Nana.Infr
{
    /// <summary>String Utility</summary>
    public static class Sty
    {
        public static string Escape(string v, char[] targets) { return Escape(v, '\\', targets); }
        public static string Escape(string v, char esc, char[] targets)
        {
            if (v == null) return null;
            StringBuilder b;
            List<char> targetlst;
            int i;
            char c;
            
            b = new StringBuilder();
            targetlst = new List<char>(targets);
            i = 0;
            while (i < v.Length)
            {
                c = v[i];
                if (targetlst.Contains(c)) b.Append(esc);
                //if (targets.Contains(c)) b.Append(esc);
                b.Append(c);
                i++;
            }
            return b.ToString();
        }

        public static string Unescape(string v) { return Unescape(v, '\\'); }
        public static string Unescape(string v, char esc)
        {
            if (v == null || v.IndexOf(esc) < 0) return v;

            StringBuilder b;
            int i;
            char c;
            
            b = new StringBuilder();
            i = 0;
            while (i < v.Length)
            {
                c = v[i];
                if (c != esc)
                {
                    b.Append(c);
                    i++;
                }
                else
                {
                    if (i < (v.Length - 1)) b.Append(v[i + 1]);
                    i += 2;
                }
            }
            return b.ToString();
        }

        public static string Wrap(string s, string l, string r) { return l + s + r; }
        public static string Parenthesize(string s) { return Wrap(s, "(", ")"); }
        public static string Curly(string s) { return Wrap(s, "{", "}"); }

        public static string Trim(string s)         /**/ { return s != null ? s.Trim() : null; }
        public static string TrimStart(string s)    /**/ { return s != null ? s.TrimStart() : null; }
        public static string TrimEnd(string s)      /**/ { return s != null ? s.TrimEnd() : null; }

        public static List<string> Clean(List<string> ls) { return ls.ConvertAll<string>(Trim).FindAll(NotNullOrEmpty); }

        public static bool Contains(string[] array, string value)
        {
            foreach (string s in array)
            {
                if (s == value) return true;
            }
            return false;
        }

        static public bool NotNullOrEmpty(string s) { return string.IsNullOrEmpty(s) == false; }

        static public List<string> ToStringList(string s)
        {
            List<string> ss = new List<string>();
            string ln;
            using (StringReader r = new StringReader(s))
                while ((ln = r.ReadLine()) != null) ss.Add(ln);
            return ss;
        }

        /// <summary>
        /// Create List&lt;string&gt; by giving string, then aply string.Trim() to each line(s) and remove empty string
        /// </summary>
        /// <param name="s">source string</param>
        /// <returns>created List&lt;string&gt;</returns>
        static public List<string> ToStringListAndClean(string s)
        {
            List<string> ss = new List<string>();
            string ln;
            using (StringReader r = new StringReader(s))
                while ((ln = r.ReadLine()) != null) ss.Add(ln);
            return Clean(ss);
        }

        static public FList<string> ToStringFList(string s)
        {
            return new FList<string>(ToStringList(s));
        }

        static public string ToLongestString(Type enumType, int v)
        {
            return ToLongestString(enumType, v.ToString());
        }

        static public string ToLongestString(Type enumType, string v)
        {
            Dictionary<int, string> d = new Dictionary<int, string>();

            foreach (string n in Enum.GetNames(enumType))
            {
                int i = (int)Enum.Parse(enumType, n);
                if (d.ContainsKey(i) == false)
                {
                    d[i] = n;
                }
                else
                {
                    if (d[i].Length > n.Length)
                    {
                        continue;
                    }
                    else if (d[i].Length == n.Length)
                    {
                        List<string> ss = new List<string>(new string[]{
                            d[i],n});
                        ss.Sort();
                        d[i] = ss[0];
                    }
                    else
                    {
                        d[i] = n;
                    }
                }
            }

            return d[(int)Enum.Parse(enumType, v)];
        }

        static public string Nv(string n, object v) { return n + "=" + v.ToString(); }

        static public string Csv(object v1, object v2)                                  /**/ { return v1 + "," + v2; }
        static public string Csv(object v1, object v2, object v3)                       /**/ { return v1 + "," + v2 + "," + v3; }
        static public string Csv(object v1, object v2, object v3, object v4)            /**/ { return v1 + "," + v2 + "," + v3 + "," + v4; }
        static public string Csv(object v1, object v2, object v3, object v4, object v5) /**/ { return v1 + "," + v2 + "," + v3 + "," + v4 + "," + v5; }

        static public List<string> ToStringList(object[] os)
        {
            return new List<object>(os).ConvertAll<string>(delegate(object o) { return o.ToString(); });
        }

        static public string Csv(object[] os)
        {
            return string.Join(",", ToStringList(os).ToArray());
        }

        static public string Sieve(string s)
        {
            return Regex.Replace(s, @"\s", "");
        }

        static public List<string> SplitBySpaces(string s)
        {
            return new List<string>(Regex.Split(s, @"\s+"));
        }

    }

    public class Bty
    {
        static public Bty New() { return new Bty(); }
        public StringBuilder B = new StringBuilder();
        public Bty Add(string s) { B.Append(s); return this; }
        public Bty Nv(string n, object v) { B.Append(n).Append("=").Append(v.ToString()); return this; }
        public Bty Csv(object v1, object v2)                                    /**/ { B.Append(v1).Append(",").Append(v2); return this; }
        public Bty Csv(object v1, object v2, object v3)                         /**/ { B.Append(v1).Append(",").Append(v2).Append(",").Append(v3); return this; }
        public Bty Csv(object v1, object v2, object v3, object v4)              /**/ { B.Append(v1).Append(",").Append(v2).Append(",").Append(v3).Append(",").Append(v4); return this; }
        public Bty Csv(object v1, object v2, object v3, object v4, object v5)   /**/ { B.Append(v1).Append(",").Append(v2).Append(",").Append(v3).Append(",").Append(v4).Append(",").Append(v5); return this; }
        public Bty Csv(object[] os)
        {
            B.Append(
            string.Join(","
            , new List<object>(os).ConvertAll<string>(delegate(object o) { return o.ToString(); }).ToArray()));
            return this;
        }
        public string ToS() { return B.ToString(); }
    }
}
