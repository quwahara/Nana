/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Nana.Delegates;

namespace Nana.Infr
{
    /// <summary>Collection Utility</summary>
    public static class Cty
    {
        public static string ToText<T>(List<T> value)
        {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < value.Count; i++) b.AppendLine(value[i].ToString());
            return b.ToString();
        }

        public static string ToText(List<string> value)
        {
            return ToText<string>(value);
        }

        public static string ToLine<T>(List<T> value)
        {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < value.Count; i++) b.Append(value[i].ToString());
            return b.ToString();
        }

        public static string ToLine(List<string> value)
        {
            return ToLine<string>(value);
        }

        public static Dictionary<string, string> ToDic(List<string> value)
        {
            return ToDic(value, @"\s+");
        }

        public static Dictionary<string, string> ToDic(List<string> value, string pattern)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            Match m;
            string n, v;
            foreach (string a in value)
            {
                m = Regex.Match(a, pattern);
                if (m.Success == false) continue;
                n = a.Substring(0, m.Index).Trim();
                if (n == "") continue;
                v = a.Substring(m.Index + m.Length);
                result[n] = v;
            }
            return result;
        }

        public static Dictionary<string, int> ParseToSIDic(Dictionary<string, string> value)
        {
            Dictionary<string,int> result;
            string[] ks, vs;
            result = new Dictionary<string,int>();
            ks=new string[value.Count];
            vs=new string[value.Count];
            value.Keys.CopyTo(ks, 0);
            value.Values.CopyTo(vs, 0);
            for (int i = 0; i < value.Count; i++)
            {
                result[ks[i]] = int.Parse(vs[i]);
            }
            return result;
        }

        public static List<string> NoEmpty(List<string> value)
        {
            List<string> result;
            result = new List<string>();
            value.ForEach(delegate(string m) { if (m.Trim() != "") result.Add(m); });
            return result;
        }

        public static List<string> Wrap(List<string> value, string l, string r)
        {
            List<string> result;
            result = new List<string>();
            value.ForEach(delegate(string m) { result.Add(l + m + r); });
            return result;
        }

        public static List<T> CollectUntilReturnNull<T>(Func<T, T> next, T origin) where T : class
        {
            List<T> ls = new List<T>();
            T item = origin;
            while (item != null)
            {
                ls.Add(item);
                item = next(item);
            }
            return ls;
        }

        public static bool NotNull(object obj) { return obj != null; }

        public static bool EqualForAll(object[] a, object[] b)
        {
            if (a == null || b == null) { return false; }
            if (a.Length != b.Length) { return false; }
            int len = a.Length;
            for (int i = 0; i < len; ++i)
            { if (a[i] != b[i]) { return false; } }
            return true;
        }

    }

    /// <summary>Funkadelic List</summary>
    public class FList<T> : List<T>
    {
        public FList() : base() { }
        public FList(int capacity) : base(capacity) { }
        public FList(IEnumerable<T> collection) : base(collection) { }

        public static FList<T> Parse(IEnumerable<T> collection) { return new FList<T>(collection); }
        public static FList<T> Parse(T a)
        {
            FList<T> fl = new FList<T>();
            fl.Add(a);
            return fl;
        }

        public FList<T> Append(IEnumerable<T> collection)
        {
            FList<T> ls = new FList<T>(this);
            ls.AddRange(collection);
            return ls;
        }

        public FList<T> CopyTo(FList<T> ls)
        {
            ls.AddRange(this);
            return ls;
        }

        public List<T> CopyTo(List<T> ls)
        {
            ls.AddRange(this);
            return ls;
        }

        public FList<TR> Map<TR>(Func<T, TR> f)
        {
            FList<TR> ls = new FList<TR>(this.Capacity);
            for (int i = 0; i < this.Count; i++) ls.Add(f(this[i]));
            return ls;
        }

        public FList<T> Map(Func<T, T> f) { return Map<T>(f); }

        /// <summary>Derivation. A list into one thing.</summary>
        public TR Deriv<TR>(Func<FList<T>, TR> f) { return f(this); }

        /// <summary>Derivation. A list into one thing.</summary>
        public T Deriv(Func<FList<T>, T> f) { return f(this); }

        public TR Pit<TR>(TR init, Func<TR, T, TR> item, Func<TR, TR> pit)
        {
            TR r = init;
            if (Count >= 1) r = item(r, this[0]);
            for (int i = 1; i < Count; i++)
            {
                r = pit(r);
                r = item(r, this[i]);
            }
            return r;
        }

        new public FList<T> FindAll(Predicate<T> f) { return new FList<T>(base.FindAll(f)); }

        public FList<T> NotNulls() { return FindAll(delegate(T t) { return t != null; }); }
    }

    public class SFList : FList<string>
    {
        static public SFList FromText(string text)
        {
            SFList ss = new SFList();
            string ln;
            using (StringReader r = new StringReader(text))
                while ((ln = r.ReadLine()) != null) ss.Add(ln);
            return ss;
        }

        static public SFList Cast(FList<string> ls)
        {
            SFList ls2 = new SFList();
            ls.ForEach(delegate(string s) { ls2.Add(s); });
            return ls2;
        }

        new public SFList NotNulls()    /**/ { return Cast(base.NotNulls()); }
        public SFList Trim()            /**/ { return Cast(base.Map(Sty.Trim)); }
        public SFList NotNullOrEmpty()  /**/ { return Cast(base.FindAll(Sty.NotNullOrEmpty)); }
        public SFList Clean()           /**/ { return Cast(base.Map(Sty.Trim).FindAll(Sty.NotNullOrEmpty)); }
        public string Xsv(string sep)
        {
            return Pit<string>("",
                   delegate(string agr, string item) { return agr + item; },
                   delegate(string agr) { return agr + sep; }
                   );
        }
        public string Csv(string sep)   /**/ { return Xsv(","); }
    }
}
