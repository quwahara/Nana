/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Nana.Delegates;
using System.Diagnostics;
using Nana.Infr;

namespace Nana.Tokens
{
    public class Token
    {
        public static string ZSourceValue  /**/ = "0Source";
        public static string ZEndValue     /**/ = "0End";
        public static Token ZEnd;

        /// <summary>
        /// left binding power (a terminology of Tdop)
        /// </summary>
        public int Lbp = 0;

        public string Value = "";

        /// <summary>
        /// Groupは構文を定義するものの1つです
        /// 
        /// Groupの決定は正規表現によって決まります
        /// 例えば、次のような正規表現があったとします: @"(?<Bgn>(\.\.+|do|begin))"
        /// 文字列"begin"はこの正規表現にマッチするので、文字列"begin"のGroupは"Bgn"に決まります
        /// 
        /// Groupは以下の処理で参照されます
        /// *** TdopParser.Factors 一覧に含まれるか(そのTokenはFactorなのかの判定)
        /// *** SentenceParser.Defs 一覧に含まれるか(そのTokenはSentenceDefの始まりなのかの判定)
        /// *** SentenceDef.Follows の1つと一致するか(そのTokenはFollowsの中のSentenceDefに一致するかの判定)
        /// SentenceDef.Ends 一覧に含まれるか(そのTokenがある構文の終わりを表すものかの判定)
        /// 
        /// Group is one of to define syntax.
        /// 
        /// Group decisions are determined by regular expressions.
        /// For example, there was a regular expression: @"(?<Bgn>(\.\.+|do|begin)) "
        /// The Group of String "begin" is determined "Bgn", because the regular expression matches the string "begin" 
        /// 
        /// Group is referred to below processes 
        /// It is contained or not in TdopParser.Factors list (To determine whether the Token is a Factor)
        /// It is contained or not in SentenceParser.Defs list (To determine whether the Token is a beginning of SentenceDef)
        /// It is consistent with one of SentenceDef.Follows (To determine the Token matches SentenceDef in Follows)
        /// It is contained or not in SentenceDef.Ends list (Token that represents the end to determine whether there is a syntax)
        /// </summary>
        public string Group = "";
        public string[] Groups = EmptyGroups;
        static public readonly string[] EmptyGroups = new string[] { };


        // components of syntax tree

        public Token First;
        public Token Second;
        public Token Third;
        public Token[] Follows;
        public Token Custom;

        // the locations for traceability

        public string Path = "";
        public int Row = 0;
        public int Col = 0;

        static Token()
        {
            ZEnd = new Token();
            ZEnd.Value = ZEndValue;
        }

        public Token() { }
        public Token(string value)                              /**/ { Value = value; }
        public Token(string value, string group)                /**/ { Value = value; Group = group; }

        public Token SetFirst(Token first) { First = first; return this; }
        public Token SetSecond(Token second) { Second = second; return this; }
        public Token SetFollows(Token[] follows) { Follows = follows; return this; }

        public Token FlwsAdd(string value)                      /**/ { return FlwsAdd(value, ""); }
        public Token FlwsAdd(string value, string group)        /**/ { return FlwsAdd(new Token(value, group)); }

        public Token FlwsAdd(Token t)
        {
            Array.Resize<Token>(ref Follows, Follows == null ? 1 : Follows.Length + 1);
            Follows[Follows.Length - 1] = t;
            return this;
        }

        public Token FlwsTail
        {
            get
            {
                if (Follows == null || Follows.Length == 0) return null;
                return Follows[Follows.Length - 1];
            }
        }

        public bool ForeachGroup(Predicate<Token> prd)
        {
            if (Sty.NotNullOrEmpty(Group) && Group[0] == '_')
            {
                foreach (string g in Group.Split(new char[] { '_' }))
                {
                    if (string.IsNullOrEmpty(g))
                    { continue; }
                    bool yes = prd(new Token(Value, g));
                    if (yes)
                    { return yes; }
                }
                return false;
            }
            return prd(this);
        }

        public bool Contains(string path)
        {
            return 0 != Select(path).Length;
        }

        public Token Find(string path)
        {
            Token[] s = Select(path);
            if (s.Length > 0) { return s[0]; }
            return null;
        }

        public Token[] Select(string path)
        {
            List<Token> ts = new List<Token>();
            if (Follows == null) { return ts.ToArray(); }

            path = path + "";
            string[] pathspl = path.Split(new char[] { '/' }, 2);
            if (pathspl.Length < 1) { return ts.ToArray(); }

            string p = pathspl[0];
            bool matchWithGroup = p.StartsWith("@");
            if (matchWithGroup)
            { p = p.Substring(1); }

            foreach (Token t in Follows)
            {
                if ((matchWithGroup && p == t.Group)
                    || (false == matchWithGroup && p == t.Value))
                {
                    if (pathspl.Length == 1)
                    { ts.Add(t); }
                    else
                    { ts.AddRange(t.Select(pathspl[1])); }
                }
            }

            return ts.ToArray();
        }

        static public Token FromVG(string value_group)
        {
            if (null == value_group)                        /**/ return null;
            if ("" == (value_group = value_group.Trim()))   /**/ return null;

            string[] spl = Regex.Split(value_group, @"\s+");
            if (spl.Length < 1) return null;
            
            Token t = new Token();
            t.Value = spl[0];
            t.Group = spl.Length > 1 ? spl[1] : "";
            return t.Value != ZEnd.Value ? t : ZEnd;
        }

        public override string ToString()
        {
            return Value + "|" + Group;
        }
    }

    public static class TokenEx
    {
        public static StringBuilder Apd(StringBuilder b, string v)
        {
            b.Append(v);
            return b;
        }

        public static string ToTree(Token t)
        {
            return ToTree(t, delegate(Token _t) { return _t.Value; });
        }

        public static string ToTree(Token t, Func<Token, string> toString)
        {
            return ToTree(t, "", toString);
        }

        public static string ToTree(Token t, string indent, Func<Token, string> toString)
        {
            string vrt = "|   ";
            string bra = "+---";
            string bla = "    ";
            string ind = "";
            StringBuilder b = new StringBuilder();
            b.Append(toString(t)).AppendLine();
            if (t.First != null)
            {
                if (t.Second == null && t.Third == null) ind = indent + bla;
                else ind = indent + vrt;
                b.Append(indent).Append(bra).Append("[F]").Append(TokenEx.ToTree(t.First, ind, toString));
                if (t.Second != null)
                {
                    if (t.Third == null) ind = indent + bla;
                    else ind = indent + vrt;
                    b.Append(indent).Append(bra).Append("[S]").Append(TokenEx.ToTree(t.Second, ind, toString));
                }
                if (t.Third != null)
                {
                    b.Append(indent).Append(bra).Append("[T]").Append(TokenEx.ToTree(t.Third, indent + bla, toString));
                }
            }
            if (t.Follows != null)
            {
                for (int i = 0; i < t.Follows.Length; i++)
                {
                    if (i == (t.Follows.Length - 1)) ind = indent + bla;
                    else ind = indent + vrt;
                    b.Append(indent).Append(bra).Append("[" + i.ToString() + "]").Append(TokenEx.ToTree(t.Follows[i], ind, toString));
                }
            }
            return b.ToString();
        }

        public static string ToInLine(Token t)
        {
            if (t.Value == Token.ZSourceValue)
            {
                if (t.Follows.Length == 0) return "";
                else if (t.Follows.Length == 1) return TokenEx.ToInLine(t.Follows[0]);
            }

            string v = t.Value == "(" || t.Value == ")" ? @"\" + t.Value : t.Value;

            if (t.First != null)
            {
                StringBuilder b = new StringBuilder();
                b.Append("(");
                b.Append(v);
                b.Append(" ");
                b.Append(TokenEx.ToInLine(t.First));
                if (t.Second != null)
                {
                    b.Append(" ");
                    b.Append(TokenEx.ToInLine(t.Second));
                }
                if (t.Third != null)
                {
                    b.Append(" ");
                    b.Append(TokenEx.ToInLine(t.Third));
                }
                b.Append(")");
                return b.ToString();
            }
            else if (t.Follows != null)
            {
                StringBuilder b = new StringBuilder();
                b.Append("(");
                b.Append(v);
                foreach (Token itm in t.Follows)
                {
                    b.Append(" ");
                    b.Append(TokenEx.ToInLine(itm));
                }
                b.Append(")");
                return b.ToString();
            }
            else
            {
                return v;
            }
        }

        public static List<Token> ToList(Token t, List<Token> l)
        {
            if (l == null) l = new List<Token>();
            
            if (t.First == null)
            {
                l.Add(t);
                return l;
            }
            ToList(t.First, l);
            if (t.Second != null)
                ToList(t.Second, l);

            return l;
        }

        public static List<Token> FromHeadAndFollowsOf(Token t, string separator, string group)
        {
            List<Token> l;

            l = new List<Token>();

            if (t == null) return l;

            l.Add(t);
            foreach (Token f in t.Follows)
            {
                if (f.Group != group || f.Value != separator) throw new Exception("Unmatch separator. separator: " + f.Value + ", group: " + group);
                if (f.Follows.Length == 0) throw new Exception("No follow value.");
                if (f.Follows.Length > 1) throw new Exception("Too much follow values.");

                l.Add(f.Follows[0]);
            }

            return l;
        }

        public static List<Token> FromBinaryTreeOf(Token t, string sign, string group)
        {
            List<Token> l;
            Stack<Token> s;

            l = new List<Token>();
            s = new Stack<Token>();

            while (t != null)
            {
                if (t.First == null || t.Group != group || t.Value != sign)
                {
                    l.Add(t);
                    if (s.Count == 0) return l;
                    t = s.Pop();
                    continue;
                }

                if (t.Second != null) s.Push(t.Second);
                t = t.First;
            }

            return l;
        }

        public static string ToChainedValue(List<Token> l, string chain)
        {
            StringBuilder b;
            b = new StringBuilder();
            if (l.Count >= 1) b.Append(l[0].Value);
            for (int i = 1; i < l.Count; i++)
                b.Append(chain).Append(l[i].Value);
            return b.ToString();
        }



        public static Token Find(Token t, Predicate<Token> match)
        {
            if (match(t)) return t;
            if (t.Follows == null) return null;
            Token tt;
            foreach (Token f in t.Follows)
                if ((tt = Find(f, match)) != null) return tt;
            return null;
        }

        public static void Visit(Token t, Action<Token> a)
        {
            a(t);
            if (t.Follows == null) return;
            foreach (Token f in t.Follows) Visit(f, a);
        }

        public static string ToUTStr(Token t)
        {
            return "V:" + t.Value + " G:" + t.Group + " B:" + t.Lbp;
        }

    }
}
