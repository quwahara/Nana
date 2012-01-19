using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Nana.Delegates;
using Nana.Infr;
using Nana.IMRs;
using Nana.Tokens;

namespace Nana.Semantics
{
    public class SemanticAnalyzer
    {
        //  structures
        public SemanticAnalyzer Above;
        public LinkedList<SemanticAnalyzer> Subs = new LinkedList<SemanticAnalyzer>();

        //  data
        public Token Seed;

        public SemanticAnalyzer(Token seed, SemanticAnalyzer above) { Seed = seed; Above = above; }

        public virtual void ConstructSubs() { }

        public void ConstructSubsAll()
        {
            foreach (SemanticAnalyzer s in Subs)
            { s.ConstructSubs(); }
        }

        public LinkedList<T> CollectTypeIs<T>() where T : class
        {
            LinkedList<T> ls = new LinkedList<T>();
            foreach (SemanticAnalyzer sub in Subs)
            {
                if (sub is T) { ls.AddLast(sub as T); }
                foreach (T it in sub.CollectTypeIs<T>())
                { ls.AddLast(it); }
            }
            return ls;
        }

        public LinkedList<T> CollectTypeOf<T>() where T : SemanticAnalyzer
        {
            LinkedList<T> ls = new LinkedList<T>();
            foreach (SemanticAnalyzer sub in Subs)
            {
                if (sub.GetType() == typeof(T)) { ls.AddLast(sub as T); }
                foreach (T it in sub.CollectTypeOf<T>())
                { ls.AddLast(it); }
            }
            return ls;
        }

        public T FindUpTypeIs<T>() where T : class
        {
            return Above == null ? null : Above is T ? Above as T : Above.FindUpTypeIs<T>();
        }

        public T FindUpTypeOf<T>() where T : SemanticAnalyzer
        {
            return Above == null ? null : Above.GetType() == typeof(T) ? Above as T : Above.FindUpTypeOf<T>();
        }

    }

    public class LineAnalyzer : SemanticAnalyzer
    {
        public Typ ThisTyp;
        public BlockAnalyzer AboveBlock;

        public Stack<Literal> Breaks;
        public Stack<Literal> Continues;
        public Env Env;
        public Actn Actn;
        public Fctn Fctn;
        public TmpVarGenerator TmpVarGen;
        public bool IsInFctn;

        public LineAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        {
            Breaks = new Stack<Literal>();
            Continues = new Stack<Literal>();
            AboveBlock = above;
        }

        public Typ RequireTyp(Token t)
        {
            return AboveBlock.RequireTyp(t);
        }

        public void AnalyzeLine()
        {
            ThisTyp = AboveBlock.ThisTyp;
            Actn = FindUpTypeIs<ActnAnalyzer>().Actn;
            IsInFctn = Actn is Fctn;
            Fctn = IsInFctn ? Actn as Fctn : null;
            Env = Actn.E;
            TmpVarGen = new TmpVarGenerator(Env.GetTempName, Actn.NewVar);
            if (AboveBlock.RequiredReturnValue.Count == 0)
            {
                IExecutable exe = Require<IExecutable>(Seed);
                Actn.Exes.Add(exe);
            }
            else
            {
                ReturnValue rv = AboveBlock.RequiredReturnValue.Pop();
                rv.GiveVal = Require<IValuable>(Seed);
            }

        }

        public TR Require<TR>(Token t)
        {
            object s; if ((s = Gate(t)) is TR) { return (TR)s; }
            throw new SyntaxError("Require :" + typeof(TR).Name + ", Token:" + t.ToString(), t);
        }

        public object Gate(Token t)
        {
            object u = null;
            switch (t.Group)
            {
                case "Prior":       /**/ u = Gate(t.Follows[0]); break;
                //case "Factor":   /**/ u = Factor(t); break;
                case "Num":         /**/ u = Num(t); break;
                case "Str":         /**/ u = Str(t); break;
                case "Bol":         /**/ u = Bol(t); break;
                case "Id":          /**/ u = Id(t); break;
                case "AsgnL":       /**/ u = Asgn(t, t.Second, t.First); break;
                case "AsgnR":       /**/ u = Asgn(t, t.First, t.Second); break;
                case "Ope":         /**/ u = Ope(t); break;
                case "Expr":        /**/ u = Expression(t); break;
                case "Dot":         /**/ u = Dot(t); break;
                case "If":          /**/ u = If(t); break;
                case "While":       /**/ u = While(t); break;
                case "TypeSpec2":   /**/ u = TypeSpec(t); break;
                case "Typ":         /**/ u = DefineVariable(t); break;
                case "Ret":         /**/ u = Ret(t); break;
                case "Nop":         /**/ u = new DoNothing(); break;
                default:
                    throw new SyntaxError(@"Could not process the sentence: " + t.Group, t);
            }
            return u;
        }

        public object Ret(Token t)
        {
            if (IsInFctn)
            {
                ReturnValue rv = new ReturnValue();
                AboveBlock.RequiredReturnValue.Push(rv);
                return rv;
            }
            else
            {
                return new Ret();
            }
        }

        public object Expression(Token t)
        {
            object u = null;
            switch (t.Value)
            {
                case ":":   /**/ u = DefineVariable(t); break;
                case "(":   /**/ u = CallFunc(t); break;
                case "[":   /**/ u = Bracket(t); break;
                case "{":   /**/ u = Curly(t); break;

                default:
                    throw new InternalError(@"The operator is not supported: " + t.Value, t);
            }
            return u;
        }

        public object Num(Token t)
        {
            return new Literal(int.Parse(t.Value), Env.BTY.Int, TmpVarGen);
        }

        public object Str(Token t)
        {
            return new Literal(t.Value.Substring(1, t.Value.Length - 2), Env.BTY.String);
        }

        public object Bol(Token t)
        {
            return new Literal(t.Value == "true", Env.BTY.Bool, TmpVarGen);
        }

        public object Id(Token t)
        {
            if (t.Value == "break")
            {
                if (Breaks.Count == 0)
                { throw new SyntaxError("Can not place break", t); }
                return new BranchInfo(Breaks.Peek());
            }

            if (t.Value == "continue")
            {
                if (Continues.Count == 0)
                { throw new SyntaxError("Can not place continue", t); }
                return new BranchInfo(Continues.Peek());
            }

            if (string.IsNullOrEmpty(t.ValueImplicit)) { t.ValueImplicit = t.Value; }

            return AboveBlock.FindUp(t) as object ?? new Nmd(t);
        }

        public object Asgn(Token assign, Token give, Token take)
        {
            IValuable gv2 = Require<IValuable>(give);
            object tu = Gate(take);

            if ((tu.GetType() == typeof(Nmd)) == false
                && (tu is IAssignable) == false
                && (tu is ArrayAccessInfo) == false
                )
            {
                throw new SyntaxError("Can not assign to: " + take.Value, take);
            }

            if (tu is ArrayAccessInfo)
            {
                return new ArraySetInfo(tu as ArrayAccessInfo, gv2);
            }
            if (tu.GetType() == typeof(Nmd))
            {
                tu = Actn.NewVar((tu as Nmd).Seed.Value, gv2.Typ);
            }

            return new Assign(gv2, tu as IVariable);
        }

        public object TypeSpec(Token t)
        {
            return Id(t);
        }

        public object DefineVariable(Token t)
        {
            Debug.Assert(t.First != null);
            Debug.Assert(t.First.Group == "Id");
            Debug.Assert(t.Follows != null);
            Debug.Assert(t.Follows.Length == 1);

            object obj = Gate(t.First);
            if (obj.GetType() != typeof(Nmd))
            { throw new SemanticError("The variable is already defined. Variable name:" + t.First.Value, t.First); }
            Nmd id = obj as Nmd;
            Typ ty = RequireTyp(t.Follows[0]);

            return Actn.NewVar(id.Seed.Value, ty);
        }

        public object Ope(Token t)
        {
            IValuable lv, rv;

            lv = Require<IValuable>(t.First);
            rv = Require<IValuable>(t.Second);

            string ope = t.Value;
            Typ tp = lv.Typ;
            if (tp.IsReferencingOf(typeof(int)))
            {
                CalcInfo c;
                switch (ope)
                {
                    case "+":
                    case "-":
                    case "*":
                    case "/":
                    case "%":
                        c = new CalcInfo(ope, lv, rv, Env.BTY.Int); break;

                    case "==":
                    case "!=":
                    case "<":
                    case ">":
                    case "<=":
                    case ">=":
                    case "and":
                    case "or":
                    case "xor":
                        c = new CalcInfo(ope, lv, rv, Env.BTY.Bool); break;

                    default:
                        throw new SyntaxError("Can not use '" + ope + "'", t);
                }
                return c;
            }
            else if (tp.IsReferencingOf(typeof(bool)))
            {
                CalcInfo c;
                switch (ope)
                {
                    case "==":
                    case "!=":
                    case "and":
                    case "or":
                    case "xor":
                        c = new CalcInfo(ope, lv, rv, Env.BTY.Bool); break;

                    default:
                        throw new SyntaxError("Can not use '" + ope + "'", t);
                }
                return c;
            }
            else if (tp.IsReferencingOf(typeof(string)))
            {
                if (ope == "+")
                {
                    Typ ts = Env.BTY.String;
                    Fctn concat = ts.FindActnOvld("Concat").GetActnOf(ts, new Typ[] { ts, ts }, ThisTyp, Actn) as Fctn;
                    return new CallFunction(tp, concat, /* instance */ null, new IValuable[] { lv, rv }, /* isNewObj */ false);
                }
                else
                {
                    throw new SyntaxError("Can not use '" + ope + "'", t);
                }
            }
            else
            {
                throw new SyntaxError("Can not use '" + ope + "'", t);
            }
        }

        public object While(Token while_)
        {
            if (while_.Follows.Length < 2)
            {
                Token e = while_;
                throw new SyntaxError("No condition token or then token for while", e);
            }

            Token cond, do_;
            cond = while_.Follows[0];
            do_ = while_.Follows[1];

            string fix = Env.GetTempName();
            Literal dolbl, endlbl;

            dolbl = new Literal("do" + fix, null);
            endlbl = new Literal("endwhile" + fix, null);

            Breaks.Push(endlbl);
            Continues.Push(dolbl);

            IValuable condv = RequireCondition(cond);
            bool rds;   //  not used
            IExecutable[] lines = CreateBlock(do_.Follows, out rds);

            Continues.Pop();
            Breaks.Pop();

            return new WhileInfo(dolbl, endlbl, condv, lines);
        }

        public object If(Token if_)
        {
            Token cond, then, else_;
            List<Token> elifs;
            IfInfo.Component ifthen;
            List<IfInfo.Component> elifthen = new List<IfInfo.Component>();
            IExecutable[] elsels = null;
            bool rds = true, rdstmp;

            if (if_.Follows.Length < 2)
            {
                Token e = if_;
                throw new SyntaxError("No condition token or then token for if", e);
            }

            cond = if_.Follows[0];
            then = if_.Follows[1];
            elifs = new List<Token>();
            else_ = null;
            for (int i = 2; i < (if_.Follows.Length - 1); i++)
            {
                Token f = if_.Follows[i];
                if (f.Group == "Elif") { elifs.Add(f); continue; }
                if (f.Group == "Else") { else_ = f; continue; }
            }

            string fix = Env.GetTempName();
            List<IExecutable> lines = new List<IExecutable>();

            ifthen = new IfInfo.Component();
            ifthen.Condition = RequireCondition(cond);
            ifthen.Lines = CreateBlock(then.Follows, out rdstmp);
            rds &= rdstmp;

            elifs.ForEach(delegate(Token elif)
            {
                IfInfo.Component elifc = new IfInfo.Component();
                elifc.Condition = RequireCondition(elif.Follows[0]);
                elifc.Lines = CreateBlock(elif.Follows[1].Follows, out rdstmp);
                rds &= rdstmp;
                elifthen.Add(elifc);
            });

            if (else_ != null)
            {
                elsels = CreateBlock(else_.Follows, out rdstmp);
                rds &= rdstmp;
            }
            else
            {
                rds = false;
            }

            return new IfInfo(fix, ifthen, elifthen.ToArray(), elsels, rds);
        }

        public IValuable RequireCondition(Token cond)
        {
            IValuable condv = Require<IValuable>(cond);
            if (false == condv.Typ.IsReferencingOf(typeof(bool)))
            {
                throw new TypeError("condition expression was not bool type", cond);
            }
            return condv;
        }

        public IExecutable[] CreateBlock(Token[] block, out bool rds)
        {
            List<IExecutable> lines = new List<IExecutable>();
            rds = false;

            int i = 0;
            while (i < block.Length)
            {
                Token line = block[i];
                IExecutable exe = Require<IExecutable>(line);
                lines.Add(exe);
                if (exe is IReturnDeterminacyState)
                { rds |= (exe as IReturnDeterminacyState).RDS; }
                ++i;
                if (false == (exe is ReturnValue)) { continue; }

                if (i >= block.Length)
                { throw new SyntaxError("Value is not specified for the return", line); }
                ReturnValue rv = exe as ReturnValue;
                line = block[i];

                rv.GiveVal = Require<IValuable>(line);
                ++i;
            }

            return lines.ToArray();
        }

        public object CallFunc(Token t)
        {
            List<Typ> argtyps = new List<Typ>();
            List<IValuable> argvals = new List<IValuable>();
            object first;
            Type firstty;

            // arguments
            if (t.Second.Follows.Length > 0)
            {
                Token[] args = t.Second.Follows;
                if ((args.Length % 2) != 1)
                {
                    throw new SyntaxError("The argument count must be odd", t);
                }
                for (int i = 1; i < args.Length; i += 2)
                {
                    if (args[i].Value != ",")
                    {
                        throw new SyntaxError("No comma is between arguments", t);
                    }
                }
                for (int i = 0; i < args.Length; i += 2)
                {
                    Token argt = args[i];
                    IValuable v;
                    v = Require<IValuable>(argt);
                    argvals.Add(v);
                    argtyps.Add(v.Typ);
                }
            }

            first = Gate(t.First);
            if (first == null)
            { throw new SyntaxError("It is not a function or constructor", t.First); }
            firstty = first.GetType();

            if (firstty != typeof(Member) && firstty != typeof(ActnOvld) && firstty != typeof(Typ))
            { throw new SyntaxError("It is not a function or constructor", t.First); }

            ActnOvld actovl = null;
            Member mbr = null;
            Typ calleetyp = null;
            if (firstty == typeof(Member))
            {
                mbr = first as Member;
                if (mbr.Value.GetType() != typeof(ActnOvld))
                { throw new NotImplementedException(); }

                calleetyp = mbr.Ty;
                actovl = mbr.Value as ActnOvld;
            }
            if (firstty == typeof(ActnOvld))
            {
                actovl = first as ActnOvld;
            }
            bool isNewObj = false;
            if (firstty == typeof(Typ))
            {
                calleetyp = first as Typ;
                isNewObj = true;
                actovl = calleetyp.FindActnOvld(Nana.IMRs.IMRGenerator.InstCons);
            }
            Debug.Assert(actovl != null);

            Actn sig = null;

            sig = actovl.GetActnOf(calleetyp, argtyps.ToArray(), ThisTyp, Actn);
            if (sig == null) { throw new SyntaxError("It is not a member", t.First); }

            IValuable instance = mbr == null ? null : mbr.Instance;

            if (sig.GetType() == typeof(Actn))
            { return new CallAction(calleetyp, sig, instance, argvals.ToArray(), false /*:isNewObj*/); }

            if (sig.GetType() == typeof(Fctn))
            { return new CallFunction(calleetyp, sig as Fctn, instance, argvals.ToArray(), isNewObj); }

            throw new NotImplementedException();
        }

        public object Dot(Token t)
        {
            Debug.Assert(t.First != null);
            Debug.Assert(t.Second != null);

            object holder = Gate(t.First);
            object comb = null;

            if (holder.GetType() == typeof(Nsp))
            {
                Nsp nsp = holder as Nsp;
                Token sec = t.Second;
                if (sec.Group != "Id")
                { throw new SyntaxError("Specify type, func or variable name", sec); }
                sec.ValueImplicit = nsp.Name + "." + sec.Value;
                comb = Gate(sec);
                return comb;
            }

            Typ y = null;
            IValuable v = null;
            if (holder is IValuable)
            {
                v = holder as IValuable;
                y = v.Typ;
            }
            if (holder.GetType() == typeof(Typ))
            {
                y = holder as Typ;
            }
            if (y == null) { throw new SyntaxError("It has no member", t.First); }

            //TODO load funcs automaticaly
            //y.GetActions();

            Nmd mbr = y.FindMemeber(t.Second.Value);

            if (mbr == null) { throw new SyntaxError("It is not a member", t.Second); }

            if (mbr is Prop) { return new CallPropInfo(y, mbr as Prop, v); };

            return new Member(y, mbr, v);
        }

        static public readonly Token Empty = new Token("(Empty)", "Empty");
        static public readonly Token Comma = new Token(",", "Factor");

        /// <summary>
        /// collect separated tokens in circumfixes by commas
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        static public Token[] Circumfixed(Token t)
        {
            Token[] fs;
            List<Token> fs2;
            List<Token> ls;
            string prev;

            fs = t != null && t.Follows != null
                    ? t.Follows
                    : new Token[0]
                    ;

            // separate ',,'(consecutive commas) into one token
            fs2 = new List<Token>();
            foreach (Token f in fs)
            {
                if (Regex.IsMatch(f.Value, @",,+") == false)
                { fs2.Add(f); continue; }

                foreach (char c in f.Value)
                { fs2.Add(Comma); }
            }

            // for first token collection
            prev = ",";
            // for last token collection. a sentinel
            fs2.Add(Comma);
            ls = new List<Token>();
            foreach (Token f in fs2)
            {
                if (f.Value == ",")
                {
                    // no token between commas
                    if (prev == ",") { ls.Add(Empty); }
                }
                else if (prev == ",")
                {
                    // there is a token after comma
                    ls.Add(f);
                }
                else
                {
                    throw new SyntaxError("bad separated form by comma", f);
                }
                prev = f.Value;
            }

            return ls.ToArray();
        }

        public object Bracket(Token t)
        {
            Token r;                                        // root of "[]" syntax
            List<Token> rs = new List<Token>();                                        // root of "[]" syntax
            Token[] cirfxd;                                 // contents in "[]"
            List<Token[]> cirfxdlst = new List<Token[]>();  // list of cirfxd

            // collect circumfixed
            r = t;
            while (r.Group == "Expr" && r.Value == "[")
            {
                if (r.First == null) { throw new SyntaxError("not specified identifier in front of '[]'", r); }
                rs.Add(r);
                cirfxd = Circumfixed(r.Second);
                cirfxdlst.Add(cirfxd);
                r = r.First;
            }

            // <contents are:>                          --> <meaning can be:>
            // all empty                                --> type
            // 1st has length, follows are all empty    --> instantiation
            // all has indices                          --> accessing array
            // (not above)                              --> error

            Predicate<Token>    /**/ tprd = delegate(Token t_) { return t_ == Empty; };
            Predicate<Token[]>  /**/ tsprd = delegate(Token[] ts_) { return Array.TrueForAll<Token>(ts_, tprd); };

            bool isFirstEmpty = tsprd(cirfxdlst[cirfxdlst.Count - 1]);
            bool isFollowEmpty = cirfxdlst.GetRange(0, cirfxdlst.Count - 1).TrueForAll(tsprd);

            // resolve the spec(ifier) that specifies meaning of "[]" syntax is type or array access
            object spec;
            Typ typ = null;
            IValuable val = null;
            spec = Gate(r);

            if (spec.GetType() == typeof(Typ))
            {
                typ = spec as Typ;
                for (int i = 0; i < rs.Count; ++i)
                { typ = Env.FindOrNewArrayTyp(typ, cirfxdlst[i].Length); }
            }
            else if (spec is IValuable)
            {
                val = spec as IValuable;
                if (val.Typ.IsVectorOrArray == false)
                { throw new SyntaxError("require an array value in front of '[]'", r); }
            }
            else
            {
                throw new SyntaxError("require an arrya type or value in front of '[]'", r);
            }

            // meaning: type
            if (typ != null && isFirstEmpty && isFollowEmpty)
            { return typ; }

            // meaning: instantiation
            if (typ != null)
            {
                if (isFirstEmpty)
                { throw new SyntaxError("not specified array lenght", r); }

                if (isFollowEmpty == false)
                { throw new SyntaxError("cannot specify lenght", r); }

                // get array length
                IValuable len;
                List<IValuable> lens = new List<IValuable>();
                foreach (Token c in cirfxdlst[cirfxdlst.Count - 1])
                {
                    len = Require<IValuable>(c);
                    Typ y = len.Typ;
                    if (y.IsReferencing == false || y.RefType != typeof(int))
                    { throw new TypeError("specified not int value to array lenght", c); }
                    lens.Add(len);
                }

                ArrayInstatiation ins = new ArrayInstatiation(typ, lens.ToArray(), TmpVarGen);
                return ins;
            }

            // meaning: accessing array
            {
                ArrayAccessInfo acc = null;
                List<Token[]> rev = new List<Token[]>(cirfxdlst);
                rev.Reverse();
                foreach (Token[] cf_ in rev)
                {
                    IValuable idx;
                    List<IValuable> idcs = new List<IValuable>();
                    foreach (Token c in cf_)
                    {
                        idx = Require<IValuable>(c);
                        Typ y = idx.Typ;
                        if (y.IsReferencing == false || y.RefType != typeof(int))
                        { throw new TypeError("specified not int value to array lenght", c); }
                        idcs.Add(idx);
                    }
                    acc = new ArrayAccessInfo(val, idcs.ToArray());
                    val = acc;
                }
                return acc;
            }
        }

        public object Curly(Token t)
        {
            //System.Collections.Generic.List`1

            // get the contents
            Token spc = t.First;
            Token[] cts = t.Second.Follows;
            if (cts == null || cts.Length == 0)
            {
                throw new SyntaxError("No type parameter(s) for generic type", t);
            }
            if (cts.Length % 2 != 1)
            {
                throw new SyntaxError("Type parameter(s) is bad format", t);
            }

            List<Token> contents = new List<Token>();
            contents.Add(cts[0]);
            for (int i = 1; i < cts.Length; i += 2)
            {
                if (cts[i].Value != "," || cts[i].Group != "Id")
                {
                    throw new SyntaxError("Type parameter(s) is bad format", t);
                }
                contents.Add(cts[i + 1]);
            }

            Typ tp = null;
            {
                // get type or valuable
                object specsem = Gate(spc);
                if (specsem.GetType() != typeof(Nmd))
                { throw new SyntaxError("Not a generic type", spc); }
                //  Generic type name consists of name, "`" and count of type parameter.
                Token ttt = (specsem as Nmd).Seed;
                //ttt.Value += "`" + contents.Count.ToString();
                ttt.ValueImplicit += "`" + contents.Count.ToString();
                tp = RequireTyp(ttt);
                //TODO  require style get
                //tp = Gate(ttt) as Typ2;
                if (tp == null)
                { throw new SyntaxError("Unkown generic type:" + ttt.ValueImplicit, ttt); }
            }

            // get type parameters
            List<Typ> tprms = new List<Typ>();
            {
                // get array length
                foreach (Token c in contents)
                {
                    Typ s = RequireTyp(c);
                    if (s == null)
                    { throw new SyntaxError("specified not type in type parameter", c); }
                    tprms.Add(s);
                }
            }

            //  find instance type
            Typ typinst = Env.FindOrNewGenericTypInstance(tp, tprms.ToArray());
            return typinst;
        }

    }

    public class BlockAnalyzer : SemanticAnalyzer
    {
        public BlockAnalyzer AboveBlock;

        public Nsp Nsp;
        public Stack<ReturnValue> RequiredReturnValue = new Stack<ReturnValue>();

        public BlockAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        {
            AboveBlock = above;
        }

        public override void ConstructSubs()
        {
            if (Seed.Follows == null) { return; }
            foreach (Token f in Seed.Follows)
            { Subs.AddLast(new LineAnalyzer(f, this)); }
            ConstructSubsAll();
        }

        public void AnalyzeBlock()
        {
            Nsp = FindUpTypeIs<ActnAnalyzer>().Actn;
            foreach (SemanticAnalyzer a in Subs)
            {
                if (a.GetType() != typeof(LineAnalyzer))
                { continue; }
                (a as LineAnalyzer).AnalyzeLine(); 
            }
        }

        virtual public object Find(Token t)
        {
            return Nsp.Find(t.Value);
        }

        virtual public object FindUp(Token t)
        {
            return Find(t) ?? (AboveBlock != null ? AboveBlock.FindUp(t) : null);
        }

        public virtual Typ RequireTyp(Token t)
        { return AboveBlock == null ? null : AboveBlock.RequireTyp(t); }

        public Typ ThisTyp_ = null;
        public Typ ThisTyp
        {
            get
            {
                if (ThisTyp_ == null)
                {
                    TypAnalyzer ta
                        = FindUpTypeOf<TypAnalyzer>()
                        ?? FindUpTypeOf<AppAnalyzer>()
                        ;
                    ThisTyp_ = ta.Typ;
                }
                return ThisTyp_;
            }
        }
    }

    public class ActnAnalyzer : BlockAnalyzer
    {
        public Actn Actn_;
        public Actn Actn
        {
            get { return Actn_; }
            set { Nsp = Actn_ = value; }
        }

        public ActnAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        { }

        public override void ConstructSubs()
        {
            Token block = Seed.Find("@Block");
            if (block == null) { return; }
            Subs.AddLast(new BlockAnalyzer(block, this));
            ConstructSubsAll();
        }

        public void AnalyzeActn()
        {
            Token t = Seed;
            bool isStatic, isCtor;
            bool isInTypDecl = false;
            string ftyp = ResolveFuncType(t.Value, isInTypDecl);

            MethodAttributes attrs = AnalyzeAttrs(ftyp);
            isStatic = (attrs & MethodAttributes.Static) == MethodAttributes.Static;
            string nameasm = AnalyzeName(ftyp, t);
            isCtor = nameasm == Nana.IMRs.IMRGenerator.InstCons;

            TypAnalyzer typazr2 = FindUpTypeOf<TypAnalyzer>()
                ?? FindUpTypeOf<AppAnalyzer>()
                ;
            Debug.Assert(typazr2 != null);
            ActnOvld ovld = typazr2.Typ.FindOrNewActnOvld(nameasm);

            List<Token> prms = new List<Token>();
            Token prmpre = t.Find("@PrmDef");
            Token prm;
            while (prmpre != null && (prm = prmpre.Find("@Prm")) != null)
            {
                prms.Add(prm);
                prmpre = prm.Find("@Separator");
            }

            List<Variable> prmls = new List<Variable>();
            List<Typ> signature = new List<Typ>();
            Token ty;
            foreach (Token p in prms)
            {
                ty = p.Find("@TypeSpec/@TypeSpec2");
                Typ typ = RequireTyp(ty);
                Debug.Assert(typ != null);
                Debug.Assert(string.IsNullOrEmpty(p.Value) == false);
                prmls.Add(new Variable(p.Value, typ, Variable.VariableKind.Param));
                signature.Add(typ);
            }

            Typ voidtyp = FindUpTypeOf<EnvAnalyzer>().Env.BTY.Void;
            Typ returnType = voidtyp;
            if (isCtor)
            {
                returnType = FindUpTypeIs<TypAnalyzer>().Typ;
            }
            else if (null != (ty = t.Find("@TypeSpec/@TypeSpec2")))
            {
                returnType = RequireTyp(ty);
            }

            if (ovld.Contains(signature.ToArray()))
            { throw new SemanticError("The function is already defined. Function name:" + nameasm, t); }

            Actn = returnType == voidtyp
                ? ovld.NewActn(new Token(nameasm), prmls)
                : ovld.NewFctn(new Token(nameasm), prmls, returnType);

            base.Nsp = Actn;

            Actn.MthdAttrs = attrs;

            //  generate instance variable
            if (Actn.IsInstance)
            {
                TypAnalyzer typazr = FindUpTypeOf<TypAnalyzer>();
                if (typazr == null)
                { throw new SyntaxError("Cannot define instance constructor in this sapce", t); }
                Actn.NewThis(typazr.Typ);
            }
        }

        public string ResolveFuncType(string func, bool isInTypDecl)
        {
            if (func != "fun") return func;
            if (isInTypDecl) { return "vfun"; }
            return "sfun";
        }

        static public MethodAttributes AnalyzeAttrs(string ftyp)
        {
            MethodAttributes attrs;
            attrs = MethodAttributes.Public;
            if (ftyp == "sfun" || ftyp == "scons") { attrs |= MethodAttributes.Static; }
            if (ftyp == "vfun") { attrs |= MethodAttributes.Virtual; }
            return attrs;
        }

        static public string AnalyzeName(string ftyp, Token seed)
        {
            string nameasm = null;
            if (ftyp == "cons") { nameasm = Nana.IMRs.IMRGenerator.InstCons; }
            if (ftyp == "scons") { nameasm = Nana.IMRs.IMRGenerator.StatCons; }
            if (ftyp == "sfun" || ftyp == "nfun" || ftyp == "vfun") { nameasm = seed.Follows[0].Value; }
            return nameasm;
        }

        static public void EnusureReturn(Actn a)
        {
            bool rds = false;
            foreach (IExecutable x in a.Exes)
            {
                if (x is IReturnDeterminacyState)
                { rds |= (x as IReturnDeterminacyState).RDS; }
            }
            if (a.IsConstructor == false && a is Fctn)
            {
                if (false == rds)
                { throw new SyntaxError("Function doesn't return value"); }
            }
            else
            {
                if (false == rds)
                { a.Exes.Add(new Ret()); }
            }
        }

    }

    public class TypAnalyzer : ActnAnalyzer
    {
        public Typ Typ_;
        public Typ Typ
        {
            get { return Typ_; }
            set { base.Actn = Typ_ = value; }
        }

        public TypAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        { }

        public override void ConstructSubs()
        {
            foreach (Token t in Seed.Select("@TypeBody/@Func"))
            { Subs.AddLast(new ActnAnalyzer(t, this)); }
            ConstructSubsAll();
        }

        public void AnalyzeTyp()
        {
            Token t = Seed;
            Token name = t.Find("@Name");
            if (name == null || string.IsNullOrEmpty(name.Value))
            { throw new InternalError("Specify name to the type", t); }

            AppAnalyzer appazr = FindUpTypeOf<AppAnalyzer>();
            App app = appazr.App;
            if (app.ContainsKey(name.Value))
            { throw new SemanticError("The type is already defined. Type name:" + name.Value, name); }
            Typ = app.NewTyp(name);

            base.Nsp = base.Actn = Typ;
        }
        
        public void AnalyzeBaseTyp()
        {
            Token t = Seed;
            Token baseTypeDef = t.Find("@BaseTypeDef");
            if (baseTypeDef == null)
            {
                baseTypeDef = new Token();
                baseTypeDef.FlwsAdd("System.Object", "Id");
            }
            Typ.BaseTyp = RequireTyp(baseTypeDef.Follows[0]);
        }

        public override object Find(Token t)
        {
            Nmd n = Typ.Find(t.Value);
            if (n == null)
            { return null; }
            Type nt = n.GetType();
            if (nt == typeof(ActnOvld))
            { return new Member(Typ, n, null); }
            return n;
        }

    }

    public class SrcAnalyzer : TypAnalyzer
    {
        public List<string> Usings;

        public SrcAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        {
            Usings = new List<string>();
        }

        public override void ConstructSubs()
        {
            if (Seed.Follows == null) { return; }
            SemanticAnalyzer a;
            foreach (Token t in Seed.Follows)
            {
                switch (t.Group)
                {
                    case "TypeDef": a = new TypAnalyzer(t, this); break;
                    case "Func": a = new ActnAnalyzer(t, this); break;
                    default: a = new LineAnalyzer(t, this); break;
                }
                Subs.AddLast(a);
            }
            ConstructSubsAll();
        }

        public void AnalyzeSrc()
        {
            base.Typ = FindUpTypeOf<AppAnalyzer>().Typ;
        }

        public override Typ RequireTyp(Token t)
        {
            Token last = SpecifiedTypAnalyzer.GoToLastIdAndBuildName(t);
            Typ typ = AboveBlock.RequireTyp(last);
            int i = 0;
            while (typ == null && i < Usings.Count)
            {
                typ = AboveBlock.RequireTyp(new Token(Usings[i] + "." + last.ValueImplicit));
                ++i;
            }
            if (typ == null) { return null; }

            Token array = last;
            Env env = FindUpTypeOf<EnvAnalyzer>().Env;
            while (array != null && SpecifiedTypAnalyzer.IsArray(array.Follows))
            {
                int dim = SpecifiedTypAnalyzer.GetDimension(array.Follows);
                typ = env.NewArrayTyp(typ, dim);
                array = array.Follows != null && array.Follows.Length > 0
                    ? array.Follows[array.Follows.Length - 1]
                    : null;
            }

            return typ;
        }

    }

    public class AppAnalyzer : SrcAnalyzer
    {
        public App App_;
        public App App
        {
            get { return App_; }
            set { base.Typ = App_ = value; }
        }

        public AppAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        { }

        public override void ConstructSubs()
        {
            foreach (Token t in Seed.Select("@Source"))
            { Subs.AddLast(new SrcAnalyzer(t, this)); }
            ConstructSubsAll();
        }

        public void AnalyzeApp()
        {
            App = FindUpTypeOf<EnvAnalyzer>().Env.NewApp(Seed);
        }

        public override Typ RequireTyp(Token t)
        {
            Typ y = (App.Find(t.Value) as Typ)
                ?? (App.Find(t.ValueImplicit) as Typ)
                ;
            if (y != null) { return y; }
            return AboveBlock.RequireTyp(t);
        }

    }

    public class EnvAnalyzer : AppAnalyzer
    {
        public Env Env;
        public Dictionary<string, Member> BuiltInFunctions = new Dictionary<string, Member>();

        public EnvAnalyzer(Token seed)
            : base(seed, null)
        { }

        public static Env Run(Token root)
        {
            EnvAnalyzer ea = new EnvAnalyzer(root);
            ea.Prelude();
            ea.Main();
            ea.Finale();
            return ea.Env;
        }

        public void Prelude()
        {
            Env = new Env(Seed);
            base.Nsp = Env;
            AddBuiltInFunction("`p", "WriteLine", typeof(Console));
            foreach (Token opt in Seed.Find("@CompileOptions").Follows)
            {
                switch (opt.Group.ToLower())
                {
                    case "include":     /**/ Env.TypeLdr.InAssembly.Includes.Add(opt.Value); break;
                    case "reference":   /**/ Env.TypeLdr.InAssembly.LoadFrameworkClassLibrarie(opt.Value); break;
                    case "out":         /**/ Env.OutPath = opt.Value; break;
                    default:
                        if (opt.Group.ToLower().StartsWith("xxx")) { break; }
                        throw new InternalError("The compile option is not supported: " + opt.Value, opt);
                }
            }
            ConstructSubs();
        }

        public override void ConstructSubs()
        {
            Subs.AddLast(new AppAnalyzer(Seed.Find("@Syntax"), this));
            ConstructSubsAll();
        }

        public void AddBuiltInFunction(string built_in_function_name, string actualname, Type holdertype)
        {
            Typ hty = Env.FindOrNewRefType(holdertype);
            ActnOvld actualao = hty.FindMemeber(actualname) as ActnOvld;
            BuiltInFunctions.Add(
                built_in_function_name
                , new Member(hty, actualao, null)
                );
        }

        public void Main()
        {
            AnalyzeAppAll();
            AnalyzeSrcAll();
            AnalyzeTypAll();
            AnalyzeBaseTypAll();
            AnalyzeActnAll();
            EnsureTypAll();
            EnsureBaseInstanceConstructorCallAll();
            AnalyzeBlockAll();
        }

        public void Finale()
        {
            EnsureAppExe();
            EnsureEntryPoint();
            EnsureActnReturnAll();
            RemoveReferencingType(Env);
        }

        public void AnalyzeAppAll()
        {
            CollectTypeOf<AppAnalyzer>().First.Value.AnalyzeApp();
        }

        public void AnalyzeSrcAll()
        {
            foreach (SrcAnalyzer a in CollectTypeOf<SrcAnalyzer>())
            { a.AnalyzeSrc(); }
        }

        public void AnalyzeTypAll()
        {
            foreach (TypAnalyzer a in CollectTypeOf<TypAnalyzer>())
            { a.AnalyzeTyp(); }
        }

        public void AnalyzeBaseTypAll()
        {
            foreach (TypAnalyzer a in CollectTypeOf<TypAnalyzer>())
            { a.AnalyzeBaseTyp(); }
        }

        public void AnalyzeActnAll()
        {
            foreach (ActnAnalyzer a in CollectTypeOf<ActnAnalyzer>())
            { a.AnalyzeActn(); }
        }

        public void EnsureTypAll()
        {
            foreach (TypAnalyzer ta in CollectTypeOf<TypAnalyzer>())
            {
                Typ y = ta.Typ;
                if (y.Ovlds
                    .Exists(delegate(ActnOvld ao_)
                    {
                        return ao_.Actns
                            .Exists(delegate(Actn a) { return a.IsInherited == false && a.Name == ".ctor"; });
                    }))
                { continue; }

                Token t = GenFuncToken("cons", /* name */ null, /* returnType */ null);
                ActnAnalyzer aa = new ActnAnalyzer(t, ta);
                ta.Subs.AddLast(aa);
                aa.AnalyzeActn();
            }
        }

        static public Token GenFuncToken(string func, string name, string returnType)
        {
            Debug.Assert(false == string.IsNullOrEmpty(func));

            Token f;

            f = new Token(func);

            if (string.IsNullOrEmpty(name) == false)
            { f.FlwsAdd(name); }

            f.FlwsAdd("(", "PrmDef").FlwsAdd(")");

            if (string.IsNullOrEmpty(returnType) == false)
            {
                f.FlwsAdd(":", "TypeSpec");
                f.FlwsTail.FlwsAdd(returnType, "Id");
            }

            f.FlwsAdd("..", "Block");
            f.FlwsTail.Follows = new Token[0];

            return f;
        }

        public void EnsureBaseInstanceConstructorCallAll()
        {
            foreach (ActnAnalyzer aa in CollectTypeOf<ActnAnalyzer>())
            {
                Typ mytyp
                    = (aa.FindUpTypeOf<TypAnalyzer>()
                    ?? aa.FindUpTypeOf<AppAnalyzer>()
                    ).Typ
                    ;
                Actn actn = aa.Actn;
                if (false == Nana.IMRs.IMRGenerator.IsInstCons(actn.Name))
                { continue; }
                Typ bty = mytyp.BaseTyp;
                Actn callee = bty.FindActnOvld(".ctor").GetActnOf(bty, new Typ[] { }, mytyp, actn);
                IValuable instance = actn.FindVar("this");
                actn.Exes.Add(new CallAction(bty, callee, instance, new IValuable[] { }, false /*:isNewObj*/));
            }
        }

        public void AnalyzeBlockAll()
        {
            foreach (SrcAnalyzer a in CollectTypeOf<SrcAnalyzer>())
            { a.AnalyzeBlock(); }
            foreach (BlockAnalyzer a in CollectTypeOf<BlockAnalyzer>())
            { a.AnalyzeBlock(); }
        }

        public override Typ RequireTyp(Token t)
        {
            return Find(t) as Typ;
        }

        public override object Find(Token t)
        {
            if (TypeUtil.IsBuiltIn(t.Value))
            { return Env.FindOrNewRefType(TypeUtil.FromBuiltIn(t.Value)); }

            {
                Member m;
                if (BuiltInFunctions.TryGetValue(t.Value, out m))
                { return m; }
            }

            Nmd n;
            if (null != (n = Env.Find(t.Value))) { return n; }
            if (null != (n = Env.Find(t.ValueImplicit))) { return n; }

            if (Env.TypeLdr.IsNamespace(t.ValueImplicit))
            { return Env.NewNsp(t.ValueImplicit); }

            Type type;
            if (null != (type = Env.TypeLdr.GetTypeByName(t.ValueImplicit)))
            { return Env.FindOrNewRefType(type); }

            return null;
        }

        public void EnsureAppExe()
        {
            AppAnalyzer appaz = CollectTypeOf<AppAnalyzer>().First.Value;
            App app = appaz.App;
            if (app.Exes.Count == 0) { return; }

            Token t = GenFuncToken("scons", Actn.EntryPointNameImplicit, "void");
            ActnAnalyzer actaz = new ActnAnalyzer(t, appaz);
            actaz.AnalyzeActn();
            Actn cctor = actaz.Actn;
            cctor.Exes.AddRange(app.Exes);
            app.Exes.Clear();
        }

        public void EnsureEntryPoint()
        {
            AppAnalyzer aa = CollectTypeOf<AppAnalyzer>().First.Value;
            App app = aa.App;
            List<Nmd> actns = app.FindDownAll(delegate(Nmd n) { return n is Actn; });
            List<Nmd> founds = actns.FindAll(delegate(Nmd a) { return  (a as Actn).IsEntryPoint; });
            if (founds.Count > 1)
            { throw new SyntaxError("Specify one entry point. There were two entry points or more."); }
            if (founds.Count == 1)
            { return; }

            Token t = GenFuncToken("sfun", Actn.EntryPointNameImplicit, "void");
            (new ActnAnalyzer(t, aa)).AnalyzeActn();
        }

        public void EnsureActnReturnAll()
        {
            AppAnalyzer aa = CollectTypeOf<AppAnalyzer>().First.Value;
            App app = aa.App;

            Predicate<Nmd> pred = delegate(Nmd n)
            { return n.GetType() == typeof(Actn) || n.GetType() == typeof(Fctn); };

            foreach (Actn a in app.FindDownAll(pred))
            { ActnAnalyzer.EnusureReturn(a); }
        }

        public static void RemoveReferencingType(Env env)
        {
            env.Members.RemoveAll(delegate(Nmd n)
            {
                return n is Typ && (n as Typ).IsReferencing == true;
            });

        }

    }

    public class SpecifiedTypAnalyzer
    {

        static public Token GoToLastIdAndBuildName(Token t)
        {
            Debug.Assert(t != null);
            Debug.Assert(t.Group == "Id" || t.Group == "TypeSpec2");

            Token pre = t;
            Token next;

            if (pre.ValueImplicit == "") { pre.ValueImplicit = pre.Value; }
            while (pre.Follows != null && pre.Follows.Length > 0)
            {
                if (pre.Follows[0].Value == "[") { break; }

                Debug.Assert(pre.Follows.Length == 1);
                Debug.Assert(pre.Follows[0].Value == ".");
                Debug.Assert(pre.Follows[0].Follows != null);
                Debug.Assert(pre.Follows[0].Follows.Length == 1);
                Debug.Assert(pre.Follows[0].Follows[0] != null);
                Debug.Assert(pre.Follows[0].Follows[0].Group == "Id" || pre.Follows[0].Follows[0].Group == "TypeSpec2");

                next = pre.Follows[0].Follows[0];
                next.ValueImplicit = pre.ValueImplicit + "." + next.Value;
                pre = next;
            }

            return pre;
        }

        static public bool IsArray(Token[] follows)
        {
            return follows != null && follows.Length > 0
                && follows[follows.Length - 1].Value == "[";
        }

        public static int GetDimension(Token[] follows)
        {
            Debug.Assert(follows != null && follows.Length > 0);

            Token a_ = follows[follows.Length - 1];
            Token[] fs;
            List<Token> fs2;
            int dim;
            dim = 1;
            fs = a_.Follows != null ? a_.Follows : new Token[0];
            fs2 = new List<Token>();
            foreach (Token f in fs)
            {
                if (Regex.IsMatch(f.Value, @",+"))
                { dim += f.Value.Length; }
            }
            return dim;
        }

    }

}