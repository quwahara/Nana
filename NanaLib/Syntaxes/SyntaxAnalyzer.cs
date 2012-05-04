/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Nana.Infr;
using Nana.Delegates;
using Nana.Tokens;

namespace Nana.Syntaxes
{
    public class SyntaxAnalyzer
    {
        public ITokenEnumerator Tokens;
        public InfixAnalyzer InfixAzr;
        public PrefixAnalyzer PrefixAzr;

        public SyntaxAnalyzer() : this(new InfixAnalyzer(), new PrefixAnalyzer()) { }

        public SyntaxAnalyzer(InfixAnalyzer infixAzr, PrefixAnalyzer prefixAzr)
        {
            InfixAzr = infixAzr;
            PrefixAzr = prefixAzr;

            InfixAzr.IsPrefix = PrefixAzr.IsPrefix;
            InfixAzr.Sntc = PrefixAzr.Analyze;

            PrefixAzr.ExprF = InfixAzr.Expr;
        }

        public void Init(string src, string path)
        {
            TokenizerBase tkz = new ScriptTokenizer();
            LineBufferedReader r = LineBufferedReader.GetInstanceWithText(src, path);
            tkz.Init(r);
            Init(tkz);
        }

        public void Init(ITokenEnumerator tokens)
        {
            Token src;
            src = new Token();
            src.Value = Token.ZSourceValue;
            Tokens = new Prepend(tokens, src);
            Tokens = new Append(Tokens, Token.ZEnd);
            InfixAzr.Init(Tokens);
            PrefixAzr.Init(Tokens);
        }

        public Token Analyze()
        {
            return PrefixAzr.Analyze();
        }

        public Token Run(string src, string path)
        {
            Init(src, path);
            return Analyze();
        }
    }

    public class InfixAnalyzer
    {
        #region Defs

        static public readonly string InfixBpsTxt = @"
            300 .
            290 :
            280 *   /   %
            270 +   -
            260 <   >   <=  >=
            240 ==  !=
            230 and
            220 xor
            210 or
            110 ->
            100 ,
            -1  ;
            -1  return
            ";

        static public readonly string InfixRBpsTxt = @"
            110 =   <-
            ";

        static public readonly string CircumfixDefsTxt = @"
            300 (   )
            300 [   ]
            300 {   }
            300 `<  >
            ";

        static public readonly string SuffixBpsTxt = @"
            ";

        static public readonly string FactorsTxt = @"
            Str
            Num
            Bol
            Id
            End
            _End_Cma_
            ";

        #endregion

        public Dictionary<string, int> InfixBps;
        public Dictionary<string, int> InfixRBps;
        public Dictionary<string, int> CircumfixBps;
        public Dictionary<string, string> CircumfixR;
        public Dictionary<string, int> SuffixBps;
        public List<string> Factors;
        public Token _Cur;
        public Stack<string> DisabledInfix;

        public ITokenEnumerator Tokens;
        public Func<Token, bool> IsPrefix = delegate(Token t) { return false; };
        public Func<Token, Token> Sntc = delegate(Token t) { throw new NotImplementedException("Func<Token, Token> Sntc"); };

        public InfixAnalyzer()
            : this(InfixBpsTxt, InfixRBpsTxt, CircumfixDefsTxt, FactorsTxt, SuffixBpsTxt)
        {
        }

        public InfixAnalyzer(string infixBps, string infixRBps, string circumfixBps, string factors, string suffixBps)
        {
            InfixBps        /**/ = CreateBps(infixBps,      /*pickOne=*/ false);
            InfixRBps       /**/ = CreateBps(infixRBps,     /*pickOne=*/ false);
            CircumfixBps    /**/ = CreateBps(circumfixBps,  /*pickOne=*/ true);
            Factors = Sty.ToStringListAndClean(factors);
            SuffixBps       /**/ = CreateBps(suffixBps,     /*pickOne=*/ true);
            CircumfixR = CreateWith1and2(circumfixBps);
            DisabledInfix = new Stack<string>();
        }

        static public Dictionary<string, string> CreateWith1and2(string src)
        {
            //return Sty.ToStringListAndClean(src)
            //    .ConvertAll<string[]>(delegate(string s)
            //    {
            //        string[] spl;
            //        return (spl = Regex.Split(s, @"\s+")).Length >= 3 ? spl : null;
            //    })
            //    .FindAll(Util.NotNull)
            //;

            return SFList.FromText(src).Clean()
                .Map<string[]>(delegate(string s)
                {
                    string[] spl;
                    return (spl = Regex.Split(s, @"\s+")).Length >= 3 ? spl : null;
                })
                .NotNulls()
                .Deriv<Dictionary<string, string>>(delegate(FList<string[]> ls)
                {
                    Dictionary<string, string> dic_ = new Dictionary<string, string>();
                    foreach (string[] ss in ls) { dic_.Add(ss[1], ss[2]); }
                    return dic_;
                });
        }

        static public Dictionary<string, int> CreateBps(string bps, bool pickOne)
        {
            /*
             * expected string bps format
             *  [bp]    [sig]   [sig]   ...
             *  60      *       /
             *  50      +       -
             *  40      <       >       <=      >=
             */

            return SFList.FromText(bps).Clean()
                .Map<string[]>(delegate(string s)
                {
                    string[] spl;
                    return (spl = Regex.Split(s, @"\s+")).Length >= 2 ? spl : null;
                })
                .NotNulls()
                .Deriv<Dictionary<string, int>>(delegate(FList<string[]> ls)
                {
                    Dictionary<string, int> dic_ = new Dictionary<string, int>();
                    foreach (string[] ss in ls)
                    {
                        int bp = int.Parse(ss[0]);
                        int len = pickOne ? 2 : ss.Length;
                        for (int i = 1; i < len; i++)
                        {
                            dic_.Add(ss[i], bp);
                        }
                    }
                    return dic_;
                });
        }

        public void Init(ITokenEnumerator tokens)
        {
            _Cur = null;
            Tokens = tokens;
        }

        public Token Cur
        {
            get
            {
                if (_Cur == Tokens.Cur) return _Cur;
                
                _Cur = Tokens.Cur;
                // apply sentence kind
                if (InfixBps.ContainsKey(_Cur.Value) && false == IsDisabledInfix(_Cur.Value))
                {
                    _Cur.Lbp = InfixBps[_Cur.Value];
                }
                else if (InfixRBps.ContainsKey(_Cur.Value) && false == IsDisabledInfix(_Cur.Value))
                {
                    _Cur.Lbp = InfixRBps[_Cur.Value];
                }
                //if (InfixBps.ContainsKey(_Cur.Value))
                //{
                //    _Cur.Lbp = InfixBps[_Cur.Value];
                //}
                //else if (InfixRBps.ContainsKey(_Cur.Value))
                //{
                //    _Cur.Lbp = InfixRBps[_Cur.Value];
                //}
                else if (CircumfixBps.ContainsKey(_Cur.Value))
                {
                    _Cur.Lbp = CircumfixBps[_Cur.Value];
                    _Cur.Group = "Expr";
                }
                else if (SuffixBps.ContainsKey(_Cur.Value))
                {
                    _Cur.Lbp = SuffixBps[_Cur.Value];
                }
                return _Cur;
            }
        }

        public bool IsDisabledInfix(string s)
        {
            return DisabledInfix.Count > 0
                && DisabledInfix.Peek() == s
                ;
        }

        public Token Analyze()
        {
            return Expr(0);
        }

        public Token Expr(int rbp)
        {
            Token t, left;

            t = Cur;
            Tokens.Next();

            if (t.Lbp < 0)          /**/ { return t; }  // negative binding power means end of line

            left = Left(t);

            while (Cur.Lbp > rbp)
            {
                Token infix = Cur;
                Tokens.Next();
                left = Infix(infix, left);
            }
            return left;
        }

        public Token Left(Token t)
        {
            if (IsPrefix(t))                /**/ { t.Lbp = 0; return Sntc(t); }
            if (t.ForeachGroup(IsFactor))   /**/ { return t; }
            throw new SyntaxError(string.Format(
                @"Cannot place '{0}' at there.", t.Value), t);
        }

        public bool IsFactor(Token t)
        {
            return Factors.Contains(t.Group);
        }

        public Token Infix(Token infix, Token left)
        {
            infix.First = left;

            if /**/ (InfixBps.ContainsKey       /**/ (infix.Value)) { infix.Second = Expr(infix.Lbp); }
            else if (InfixRBps.ContainsKey      /**/ (infix.Value)) { infix.Second = Expr(infix.Lbp - 1); }
            else if (CircumfixBps.ContainsKey   /**/ (infix.Value))
            {
                if (CircumfixR.ContainsKey(infix.Value) == false)
                { throw new InternalError(string.Format(@"Could not find end for '{0}'.", infix.Value), infix); }

                string begin = infix.Value;
                string end = CircumfixR[begin];
                
                bool doDisableInfix = InfixBps.ContainsKey(end) || InfixRBps.ContainsKey(end);
                if (doDisableInfix)
                { DisabledInfix.Push(end); }

                //  Does the circumfix have contents?
                if (Cur.Value != end)
                { infix.Second = Expr(0); }

                if (doDisableInfix)
                { DisabledInfix.Pop(); }

                //  Is the circumfix closed?
                if (Cur.Value != end)
                { throw new SyntaxError(string.Format(@"'{0}' was not closed. '{1}' was there.", begin, Cur.Value), Cur); }

                infix.Third = Cur;
                Tokens.Next();
            }
            else if (SuffixBps.ContainsKey(infix.Value))
            {
                Sntc(infix);
            }
            else
            {
                throw new InternalError(string.Format(
                    @"InternalError int InInfix(). Token:'{0}'.", infix));

            }
            return infix;
        }
    }

    public class PrefixAnalyzer
    {
        #region Defs
        static public readonly string DefsText = @"

            _#  syntax root

            0Source.@0      Expr.s* 0End                --  Source


            _#  meta-infomation

            using.@0        Expr.s                      --  Using

            namespace.@0    0iddotdec2.r    0bodydec.r  --  Namespace


            _#  fundamental elements

            0bodydec    _(  Bgn.g@0 Expr.s* _)  End.g   -- Block




            class.@0    Id.g@1 _( ->.?@2    Expr.s  _) 0bodydec.r   -- TypeDef  Name  BaseTypeDef

                0iddotdec   _( Id.g _( ..* Id.g _) _)

                0iddotdec2  _( Id.g _( ..? 0iddotdec2.r _) _)


            scons.@0    0conscall.r 0attrdec.r  0bodydec.r      -- Fnc PrmDef

                0conscall   _(  base.?@0    _(  (.@1    Expr.s* _)  )   _)  -- ConsCall Expr


            cons.@0     _(  (.@1    Expr.s? _)  )   0conscall.r 0attrdec.r  0bodydec.r      -- Fnc  PrmDef

            Fnc.g   Id.g 0funcdec.r

                0funcdec    _(  (.@0    Expr.s? _)  ) 0typedec.r 0attrdec.r 0bodydec.r          -- PrmDef
                0typedec    _(  :.?@0   Expr.s  _)                                                  --  TypeSpec

            `(  Expr.s? )   0typedec.r  0bodydec.r

            (.@0        Expr.s  )               --  Prior
            [.@0        Expr.s  ]               --  Prior
            
            
            if.@0       Expr.s  _( then Expr.s* _)  _( elif.*@1 Expr.s  _( then Expr.s* _) _)   _( else.?@2 Expr.s* _)  end -- If Elif Else
            while.@0    Expr.s  _( Bgn.g    Expr.s* _)  End.g   -- While

            throw.@0    Expr.s  --  Throw
            try.@0      Expr.s* _(  catch.*@1   Expr.s1 _(  Bgn.g   Expr.s* _)  _)  _( finally.?@2  Expr.s* _)  End.g   --  Try     Catch   Finally

            0attrdec    _(  @.*@0   Expr.s _)       -- Attr
            
            @.@0    Expr.s  Expr.s          -- Cstm
            ";
        #endregion

        public List<PrefixDef> Defs;
        public ITokenEnumerator Tokens;
        public bool IsEnd;
        /// <summary>
        /// Open method to handle an expression
        /// </summary>
        public Func<int, Token> ExprF;

        public PrefixAnalyzer() : this(DefsText) { }

        public PrefixAnalyzer(string defs)
            : this(
            Sty.ToStringListAndClean(defs)
            .ConvertAll<PrefixDef>(PrefixDef.FromInline)
            .FindAll(Cty.NotNull)
            )
        {
        }

        public PrefixAnalyzer(List<PrefixDef> defs)
        {
            Defs = defs;

            //  this declaration is only good for test purpose. 
            //  assumes to overwrite for ExprF by InfixAnalyzer.Expr in ordinary use case.
            ExprF = delegate(int rbp)
            {
                Token t_ = this.Tokens.Cur;
                this.Tokens.Next();
                return t_;
            };
        }

        public void Init(ITokenEnumerator tokens)
        {
            Tokens = tokens;
            IsEnd = false;
        }

        public Token Analyze()
        {
            Token t = Tokens.Cur;
            Tokens.Next();
            return Analyze(t);
        }

        public Token Analyze(Token t)
        {
            PrefixDef sdf = GetPrefixDef(t);
            if (sdf == null)
                throw new InternalError(string.Format(
                    @"'{0}' is not a first word for a sentence.", t.Value), t);
            if (sdf.Group != "") t.Group = sdf.Group;
            t.Follows = Follows(sdf.Follows);
            return t;
        }

        public PrefixDef GetPrefixDef(Token t)
        {
            return Defs.Find(delegate(PrefixDef d) { return d.MatchTo(t); });
        }

        public bool IsPrefix(Token t)
        {
            return Defs.Exists(delegate(PrefixDef d) { return d.MatchTo(t); });
        }

        public Token[] Follows(List<PrefixDef> follows)
        {
            List<Token> result;
            int i;
            PrefixDef def;
            Token t;

            result = new List<Token>();
            i = 0;
            // get first definition
            def = i < follows.Count ? follows[i] : null;

            while (def != null)
            {
                t = Tokens.Cur;

                // check matching with value and/or group
                while (def != null)
                {
                    // don't have to match
                    if (false == def.IsMatchRequired)
                    { break; }

                    //  or it's matched
                    if (def.MatchTo(t))
                    { break; }

                    if (def.Appearance == "1")
                    {
                        if (t == Token.ZEnd)
                        { throw new SyntaxError("Got at end of source that was not expected"); }
                        throw new SyntaxError(string.Format("Unexpected word is found: '{0}'", t.Value), t);
                    }
                    // skip
                    i++;
                    def = i < follows.Count ? follows[i] : null;
                }

                if (def == null || def.MatchTo(Token.ZEnd))
                { break; }

                if (def.Group != "")
                { t.Group = def.Group; }

                // pluck
                switch (def.Kind)
                {
                    case "Value":
                    case "Group":
                        result.Add(t);
                        Tokens.Next();
                        break;
                    case "ValueClause":
                    case "GroupClause":
                        result.Add(t);
                        Tokens.Next();
                        t.Follows = Follows(def.Follows);
                        break;
                    case "Refer":
                        PrefixDef refdef = GetPrefixDef(new Token(def.Value));
                        Token[] rs = Follows(refdef.Follows);
                        result.AddRange(rs);
                        break;
                    case "Expr":
                        if ("?*".Contains(def.Appearance))
                        {
                            Token xs = Exprs(def.EndDefs);
                            if (null != xs)
                            { result.Add(xs); }
                            else if ("*" == def.Appearance)
                            { ++i; }
                        }
                        else
                        {
                            result.Add(ExprF(0));
                        }
                        break;
                    default:
                        throw new InternalError(string.Format(
                            @"'{0}' is not a supported kind.", def.Kind));
                }

                // get next definition
                if (def.Appearance == "1" || def.Appearance == "?") i++;
                def = i < follows.Count ? follows[i] : null;
            }

            return result.ToArray();
        }

        public Token Exprs(PrefixDef[] ends)
        {
            if (ends == null)       /**/ { throw new InternalError(@"Ends array is null"); }
            if (ends.Length == 0)   /**/ { throw new InternalError(@"Ends array is empty"); }

            foreach (PrefixDef d in ends)
            {
                if (d.MatchTo(Tokens.Cur))
                { return null; }
            }
            return ExprF(0);
        }

    }   //  end of PrefixAnalyzer

}
