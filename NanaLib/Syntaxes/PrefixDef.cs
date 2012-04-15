/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Nana.Infr;
using Nana.Delegates;
using Nana.Tokens;

namespace Nana.Syntaxes
{
    public class PrefixDef
    {
        public string Kind = "";
        public string Value = "";
        //public string Key = "";
        //public int Rbp = 0;
        public string Appearance = "";
        public string Group = "";
        public PrefixDef[] EndDefs = null;
        public PrefixDef Parent = null;
        public List<PrefixDef> Follows = null;

        public static PrefixDef[] CreateEndDefs(PrefixDef s)
        {
            List<PrefixDef> result          /**/ = new List<PrefixDef>();

            //  e.g. 'elif' becomes an end by itself.
            if (s.Kind.EndsWith("Clause") && (s.Appearance == "*" || s.Appearance == "+"))
            { result.Add(s); }

            if (s.Parent == null)           /**/ { return result.ToArray(); }
            if (s.Parent.Follows == null)   /**/ { return result.ToArray(); }

            List<PrefixDef> siblings        /**/ = s.Parent.Follows;
            int idx                         /**/ = siblings.IndexOf(s);
            for (int i = idx + 1; i < siblings.Count; ++i)
            {
                PrefixDef sib               /**/ = siblings[i];
                if (sib.Kind == "Value" || sib.Kind.EndsWith("Clause") || sib.Kind == "Group")
                {
                    result.Add(sib);
                    if (sib.Appearance == "1") { return result.ToArray(); }
                }
            }
            result.AddRange(CreateEndDefs(s.Parent));

            return result.ToArray();
        }

        /*
         * inline指定の文法説明
         * 一番右の"."が区切り
         * その左側が値
         * その右側:
         *      1 ? *   -- Appearance
         *      v g r   -- Value Group Refer
         *      s       -- Special (Expr)
         *      @[数字] -- Group index
         * 行末に指定できる "--" の後ろがGroup名。スペースで区切って複数指定可能。どのGroupかは@の後の数字がインデックスになる。0なら複数指定の1番目
         * グループ化は "_("と"_)"で囲む
         * 
         */
        static public PrefixDef FromInline(string v)
        {
            if ((v = ("" + v).Trim()) == "") { return null; }
            if (v.StartsWith("_#")) { return null; }

            // Parse line Group names
            List<string> groups             /**/ = new List<string>();
            int idx                         /**/ = v.LastIndexOf("--");
            if (idx >= 0)
            {
                groups.AddRange(Regex.Split(v.Substring(idx + 2).Trim(), @"\s+"));
                v = v.Substring(0, idx).TrimEnd();
            }

            Func<string, PrefixDef> toSd = delegate(string s_)
            {
                //  *** s_ format ***
                //  s_      = <value>.<ext>
                //  <value> = not{\s} 
                //  <ext>   = or{v g s}or{1 ? *} 

                PrefixDef d_;
                int idx_;
                d_                          /**/ = new PrefixDef();
                d_.Appearance               /**/ = "1";
                d_.Kind                     /**/ = "Value";
                //TODO  check is this using
                //d_.Rbp                      /**/ = 0;
                d_.Group                    /**/ = "";

                idx_ = s_.LastIndexOf('.');
                if (idx_ < 0)
                {
                    // no extentions
                    d_.Value                /**/ = s_;
                    return d_;
                }
                d_.Value                    /**/ = s_.Substring(0, idx_);

                // Parse extentions
                string ext_;
                ext_                        /**/ = s_.Substring(idx_ + 1);
                idx_                        /**/ = ext_.LastIndexOf('@');
                if (idx_ >= 0)
                {
                    d_.Group                /**/ = groups[int.Parse(ext_.Substring(idx_ + 1))];
                    ext_                    /**/ = ext_.Substring(0, idx_);
                }
                foreach (char e_ in ext_)
                {
                    switch (e_)
                    {
                        case '1':
                        case '?':
                        case '*':
                            d_.Appearance = e_.ToString(); break;

                        case 'v': d_.Kind = "Value"; break;
                        case 'g': d_.Kind = "Group"; break;
                        case 'r': d_.Kind = "Refer"; break;
                        case 's': d_.Kind = d_.Value; break;
                    }
                }

                //if (d_.Kind.StartsWith("Group"))
                //{ d_.Key = "@"; }
                //d_.Key += d_.Value;

                return d_;
            };

            // generate hierarchy
            Func<List<string>, PrefixDef> toClause = null;
            toClause = delegate(List<string> ss_)
            {
                // Assertion
                if (ss_ == null) return null;
                if (ss_.Count < 1) return null;

                PrefixDef p_, flw_;
                p_ = toSd(ss_[0]);
                p_.Kind += "Clause";
                p_.Follows = new List<PrefixDef>();
                ss_.RemoveAt(0);
                string s_;

                while(ss_.Count>0)
                {
                    s_ = ss_[0];
                    if (s_ == "_(" || s_ == "_)") ss_.RemoveAt(0);
                    switch (s_)
                    {
                        case "_(":  /**/ flw_ = toClause(ss_); break;
                        case "_)":  /**/ return p_;
                        default:    /**/ flw_ = toSd(s_); ss_.RemoveAt(0); break;
                    }
                    if (flw_ == null) return null;
                    flw_.Parent = p_;
                    p_.Follows.Add(flw_);
                }

                return p_;
            };
            PrefixDef p;
            p = toClause(new List<string>(Regex.Split(v, @"\s+")));

            // set Ends
            Action<List<PrefixDef>> setEnds = null;
            setEnds = delegate(List<PrefixDef> ps_)
            {
                foreach (PrefixDef p_ in ps_)
                {
                    if (p_.Value == "Expr" && "?*".Contains(p_.Appearance))
                    {
                        p_.EndDefs = CreateEndDefs(p_);
                    }
                    else if (p_.Follows != null)
                    {
                        setEnds(p_.Follows);
                    }
                }
            };
            setEnds(p.Follows);

            return p;
        }

        public bool IsMatchRequired
        {
            get { return Kind.StartsWith("Value") || Kind.StartsWith("Group"); }
        }

        public bool MatchTo(Token t)
        {
            if (t == null)
            { return false; }

            string group = t.Group;
            if (Sty.NotNullOrEmpty(group) && group[0] == '_')
            {
                foreach (string g in group.Split(new char[] { '_' }))
                {
                    if (string.IsNullOrEmpty(g))
                    { continue; }
                    bool yes = MatchTo(new Token(t.Value, g));
                    if (yes)
                    { return yes; }
                }
                return false;
            }

            return MatchToValue(t.Value) || MatchToGroup(t.Group);
        }

        public bool MatchToValue(string value)
        {
            return Kind.StartsWith("Value") && this.Value == value;
        }

        public bool MatchToGroup(string group)
        {
            return Kind.StartsWith("Group") && this.Value == group;
        }

        #region ToString()
        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            if (Value == "(" || Value == ")" || Value == ":") b.Append(@"\");
            b.Append(Value).Append(":").Append(Kind);
            //if (Rbp > 0) b.Append(":" + Rbp.ToString());
            if (Appearance != "") b.Append(":" + Appearance);
            if (Group != "") b.Append(":" + Group);
            if (EndDefs != null)
            {
                b.Append(":(");
                if (EndDefs.Length > 0) b.Append(EndDefs[0].Value);
                for (int i = 1; i < EndDefs.Length; i++) b.Append("," + EndDefs[i].Value);
                b.Append(")");
            }
            if (Follows != null)
            {
                b.Append(":(");
                if (Follows.Count > 0) b.Append(Follows[0].ToString());
                for (int i = 1; i < Follows.Count; i++) b.Append(", " + Follows[i].ToString());
                b.Append(")");
            }
            return b.ToString();
        }
        #endregion
    }
}
