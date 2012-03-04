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
            260 <_  >_  <=  >=
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
            300 <   >
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
                if (InfixBps.ContainsKey(_Cur.Value))
                {
                    _Cur.Lbp = InfixBps[_Cur.Value];
                }
                else if (InfixRBps.ContainsKey(_Cur.Value))
                {
                    _Cur.Lbp = InfixRBps[_Cur.Value];
                }
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
            if (IsPrefix(t))          /**/ { t.Lbp = 0; return Sntc(t); }
            if (t.IsGroupOf(Factors))   /**/ { return t; }
            throw new SyntaxError(string.Format(
                @"Cannot place '{0}' at there.", t.Value), t);
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

                //  Does the circumfix have contents?
                if (Cur.Value != end)
                { infix.Second = Expr(0); }

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
        //TODO  unify using and using2
        #region Defs
        static public readonly string DefsText = @"

            _#  syntax root

            0Source.@0      Expr.s* 0End                --  Source


            _#  meta-infomation

            using.@0        0iddotdec.r                 --  Using

            namespace.@0    0iddotdec2.r    0bodydec.r  --  Namespace


            _#  fundamental elements

            0bodydec    _(  Bgn.g@0 Expr.s* _)  End.g   -- Block




            class.@0    Id.g@2 _( ->.?@3    Expr.s  _) _( Bgn.g@1    Expr.s* _)  End.g -- TypeDef    TypeBody   Name  BaseTypeDef

                0iddotdec   _( Id.g _( ..* Id.g _) _)


            using2.@0   0iddotdec2.r            --  Using
                
                0iddotdec2  _( Id.g _( ..? 0iddotdec2.r _) _)


            scons.@0    0conscall.r 0attrdec.r  0bodydec.r      -- Func PrmDef

                0conscall   _(  base.?@0    _(  (.@1    Expr.s* _)  )   _)  -- ConsCall Expr


            cons.@0     _(  (.@1    Expr.s? _)  )   0conscall.r 0attrdec.r  0bodydec.r      -- Func PrmDef

            Fnc.g@0     Id.g 0funcdec.r          -- Func

                0funcdec    _(  (.@0    Expr.s? _)  ) 0typedec.r 0attrdec.r 0bodydec.r          -- PrmDef
                0typedec    _(  :.?@0   Expr.s  _)                                                  --  TypeSpec

            (.@0        Expr.s  )               --  Prior
            [.@0        Expr.s  ]               --  Prior
            
            
            if.@0       Expr.s  _( then Expr.s* _)  _( elif.*@1 Expr.s  _( then Expr.s* _) _)   _( else.?@2 Expr.s* _)  end -- If Elif Else
            while.@0    Expr.s  _( Bgn.g    Expr.s* _)  End.g   -- While
            
            
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
            //  assumes to be overwritten for ExprF by InfixAnalyzer.Expr in actual use case.
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
            PrefixDef sdf = GetPrefixDef(t.Value, t.Group);
            if (sdf == null)
                throw new InternalError(string.Format(
                    @"'{0}' is not a first word for a sentence.", t.Value), t);
            if (sdf.Group != "") t.Group = sdf.Group;
            t.Follows = Follows(sdf.Follows);
            return t;
        }

        public Token[] Follows(PrefixDef d)
        {
            if (d.Kind != "Refer") throw new InternalError("The kind for follows must be Refer, Kind: "  + d.Kind);

            PrefixDef sdf = GetPrefixDef(d.Value, null);
            return Follows(sdf.Follows);
        }

        public PrefixDef GetPrefixDef(string value, string group)
        {
            return Defs.Find(delegate(PrefixDef d) { return IsPrefix(value, group, d); });
        }

        public bool IsPrefix(Token t)
        {
            return Defs.Exists(delegate(PrefixDef d) { return IsPrefix(t.Value, t.Group, d); });
        }

        static public bool IsPrefix(string value, string group, PrefixDef d)
        {
            return d.Kind.StartsWith("Value") && d.Value == value
                || d.Kind.StartsWith("Group") && Token.IsGroupOf(group, d.Value)
                ;
        }

        public Token[] Follows(List<PrefixDef> follows)
        {
            List<Token> result;
            int i;
            PrefixDef flw;
            Token t;

            result = new List<Token>();
            i = 0;
            flw = i < follows.Count ? follows[i] : null;

            while (flw != null)
            {
                t = Tokens.Cur;

                // match
                while (flw != null)
                {
                    // don't have to match
                    if (flw.Kind.StartsWith("Value") == false && flw.Kind.StartsWith("Group") == false) break;

                    if (t == null)
                    {
                        throw new SyntaxError("The sentence is not completed");
                    }

                    // check match
                    if (flw.Kind.StartsWith("Value") && flw.Value == t.Value) break;
                    if (flw.Kind.StartsWith("Group") && t.IsGroupOf(flw.Value)) break;

                    if (flw.Appearance == "1")
                    {
                        if (t == Token.ZEnd)
                        { throw new SyntaxError("Not expected end"); }
                        throw new SyntaxError(string.Format("Not expected word: '{0}'", t.Value), t);
                    }
                    // skip
                    i++;
                    flw = i < follows.Count ? follows[i] : null;
                }

                if (flw == null) break;
                if (t == Token.ZEnd)
                {
                    bool goNext = flw.Kind == "Refer" || flw.Appearance == "?" || flw.Appearance == "*";
                    if (goNext == false)
                    {
                        if (flw.Value != Token.ZEndValue)
                        {
                            throw new SyntaxError("Sentence is end");
                        }
                        break;
                    }
                }

                if (flw.Group != "") t.Group = flw.Group;

                // pluck
                switch (flw.Kind)
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
                        t.Follows = Follows(flw.Follows);
                        break;
                    case "Refer":
                        result.AddRange(Follows(flw));
                        break;
                    case "Expr":
                        if ("?*".Contains(flw.Appearance))
                        {
                            Token tt = Exprs(flw.Ends);
                            if (tt == null)
                            {
                                ++i;
                            }
                            else
                            {
                                result.Add(tt);
                            }
                        }
                        else
                        {
                            result.Add(ExprF(flw.Rbp));
                        }
                        break;
                    default:
                        throw new InternalError(string.Format(
                            @"'{0}' is not a supported kind.", flw.Kind));
                }

                // next
                if (flw.Appearance == "1" || flw.Appearance == "?") i++;
                flw = i < follows.Count ? follows[i] : null;
            }

            return result.ToArray();
        }

        public Token Exprs(string[] ends)
        {
            if (ends == null)       /**/ { throw new InternalError(@"Ends array is null"); }
            if (ends.Length == 0)   /**/ { throw new InternalError(@"Ends array is empty"); }

            if (IsInEnds(Tokens.Cur, ends)) { return null; }
            return ExprF(0);
        }

        static public bool IsInEnds(Token t, string[] ends)
        {
            foreach (string end in ends)
            {
                if (end.EndsWith(".g"))
                {
                    if (t.IsGroupOf(end.Substring(0, end.Length - ".g".Length))) { return true; }
                }
                else
                {
                    if (t.Value == end) { return true; }
                }
            }
            return false;
        }

    }
}
