/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

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

        public void ErNameDuplication(Token dupname, Nsp n)
        { throw new SemanticError(string.Format("The {0} is already defined in {1}", dupname.Value, n.Name), dupname); }

    }

    public class LineAnalyzer : SemanticAnalyzer
    {
        public BlockAnalyzer AboveBlock;

        public Stack<Literal> Breaks;
        public Stack<Literal> Continues;
        public Env E;
        public App Ap;
        public Typ Ty;
        public Fun Fu;
        public Nsp Ns;
        public TmpVarGenerator TmpVarGen;
        public bool IsInFun;

        public LineAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        {
            Breaks = new Stack<Literal>();
            Continues = new Stack<Literal>();
            AboveBlock = above;

            EnvAnalyzer eaz = FindUpTypeOf<EnvAnalyzer>();
            if (null != eaz) { E = eaz.E; }
        }

        public Token GetTargetWithCustom(Token t)
        {
            Token target = t.Follows[1];
            if (target.Group == "Cstm")
            { target = GetTargetWithCustom(target); }

            Token holder = target;
            while (holder.Custom != null)
            { holder = holder.Custom; }
            holder.Custom = t.Follows[0];

            return target;
        }

        public virtual Typ RequireTyp(Token t)
        {
            object obj = Gate(t);
            if (obj == null
                || obj.GetType() != typeof(Typ)
                )
            {
                throw new NotImplementedException(
                    TokenEx.ToTree(t, delegate(Token t_) { return t_.Value + "@" + t_.Group; }));
            }

            return obj as Typ;
        }

        virtual public Variable NewVar(string name, Typ typ)
        {
            return Fu.NewVar(name, typ);
        }

        public void FindUpNsps()
        {
            Ap = FindUpTypeOf<AppAnalyzer>().Ap;
            Ty = FindUpTypeIs<TypAnalyzer>().Ty;
            Fu = FindUpTypeIs<FunAnalyzer>().Fu;
            Ns = FindUpTypeIs<BlockAnalyzer>().Ns;
        }

        public void AnalyzeLine()
        {
            FindUpNsps();
            
            IsInFun = Fu.Att.CanGet;
            TmpVarGen = new TmpVarGenerator(E.GetTempName, NewVar);
            if (AboveBlock.RequiredReturnValue.Count == 0)
            {
                Sema exe = RequireExec(Seed);
                Fu.Exes.Add(exe);
            }
            else
            {
                ReturnValue rv = AboveBlock.RequiredReturnValue.Pop();
                rv.GiveVal = Require<Sema>(Seed);
            }

        }

        public TR Require<TR>(Token t)
        {
            object s; if ((s = Gate(t)) is TR) { return (TR)s; }
            throw new SyntaxError("Require :" + typeof(TR).Name + ", Token:" + t.ToString(), t);
        }

        public Sema RequireExec(Token t)
        {
            object o = Gate(t);
            if (false == (o is Sema))
            { throw new SemanticError("Required executable sentence:" + t.ToString(), t); }
            Sema s = o as Sema;
            if (false == s.Att.CanExec)
            { throw new SemanticError("Required executable sentence:" + t.ToString(), t); }

            return s;
        }

        public object Gate(Token t)
        {
            if (t == null)
            { return EmptyS; }

            object u = null;
            switch (t.Group)
            {
                case "Prior":       /**/ u = Gate(t.Follows[0]); break;
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
                case "Typ":         /**/ u = DefineVariable(t); break;
                case "Ret":         /**/ u = Ret(t); break;
                case "Nop":         /**/ u = new DoNothing(); break;
                case "_End_Cma_":   /**/ u = Cma(t); break;
                case "Throw":       /**/ u = Throw(t); break;
                case "Try":         /**/ u = Try(t); break;
                default:
                    throw new SemanticError(string.Format("'{0}' cannot be in there", t.Value), t);
            }
            return u;
        }

        public object Cma(Token t)
        {
            if (Regex.IsMatch(t.Value, @",,+") == false)
            {
                return new Chain(Gate(t.First), Gate(t.Second));
            }
            else
            {
                Chain c = new Chain(EmptyS);
                foreach (char chr in t.Value)
                {
                    c = new Chain(c, EmptyS);
                }
                return c;
            }
        }

        public object Ret(Token t)
        {
            if (IsInFun)
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
                case "`<":   /**/ u = Generics(t); break;

                default:
                    throw new InternalError(@"The operator is not supported: " + t.Value, t);
            }
            return u;
        }

        public object Num(Token t)
        {
            return new Literal(int.Parse(t.Value), E.BTY.Int, TmpVarGen);
        }

        public object Str(Token t)
        {
            return new Literal(t.Value.Substring(1, t.Value.Length - 2), E.BTY.String);
        }

        public object Bol(Token t)
        {
            return new Literal(t.Value == "true", E.BTY.Bool, TmpVarGen);
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

            return AboveBlock.FindUp(t.Value) as object ?? new Nmd(t.Value);
        }

        public object Asgn(Token assign, Token give, Token take)
        {
            Sema gv2 = Require<Sema>(give);
            object tu = Gate(take);

            if ((tu.GetType() == typeof(Nmd)) == false
                && (tu is ArrayAccessInfo) == false
                && (tu is FieldAccessInfo) == false
                && ((tu is Sema) && (tu as Sema).Att.CanSet) == false
                )
            {
                throw new SyntaxError("Can not assign to: " + take.Value, take);
            }

            Sema prepare = null;
            if (tu is Assign)
            { prepare = (tu as Assign).Prepare; }
            else if (tu is FieldAccessInfo)
            { prepare = (tu as FieldAccessInfo).Instance; }

            if (tu is ArrayAccessInfo)
            {
                return new ArraySetInfo(tu as ArrayAccessInfo, gv2);
            }
            if (tu.GetType() == typeof(CallPropInfo))
            {
                return new PropSet(tu as CallPropInfo, gv2);
            }
            if (tu.GetType() == typeof(Nmd))
            {
                tu = NewVar((tu as Nmd).Name, gv2.Att.TypGet);
            }

            return new Assign(gv2, tu as Sema, prepare);
        }

        public object DefineVariable(Token t)
        {
            Debug.Assert(t.First != null);
            Debug.Assert(t.First.Group == "Id");

            object obj = Gate(t.First);
            if (obj.GetType() != typeof(Nmd))
            { throw new SemanticError("The variable is already defined. Variable name:" + t.First.Value, t.First); }
            Nmd id = obj as Nmd;
            Typ ty = RequireTyp(t.Second);

            return NewVar(id.Name, ty);
        }

        public object Ope(Token t)
        {
            //  transform infix to call-fun
            Token callfun = new Token("(", "Expr");
            callfun.First = new Token(t.Value, "Id");
            Token prm = callfun.Second = new Token(",", "_End_Cma_");
            prm.First = t.First;
            prm.Second = t.Second;
            return Gate(callfun);
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

            string fix = E.GetTempName();
            Literal dolbl, endlbl;

            dolbl = new Literal("do" + fix, null);
            endlbl = new Literal("endwhile" + fix, null);

            Breaks.Push(endlbl);
            Continues.Push(dolbl);

            Sema condv = RequireCondition(cond);
            bool rds;   //  not used
            Sema[] lines = CreateBlock(do_.Follows, out rds);

            Continues.Pop();
            Breaks.Pop();

            return new WhileInfo(dolbl, endlbl, condv, lines);
        }

        public object Throw(Token t)
        {
            Sema s = Gate(t.Follows[0]) as Sema;
            if (s == null
                || false == s.Att.CanGet
                || false == s.Att.TypGet.IsReferencing
                || false == typeof(Exception).IsAssignableFrom(s.Att.TypGet.RefType)
                ) { throw new SemanticError("Cannot throw the value", t.Follows[0]); }
            return new ThrowStmt(s);
        }

        public object Try(Token t)
        {
            List<Token> tryls = new List<Token>();
            LinkedList<Token> catchls = new LinkedList<Token>();
            Token finallyt = null;

            foreach (Token f in t.Follows)
            {
                switch (f.Group)
                {
                    case "End": break;
                    case "Catch": catchls.AddLast(f); break;
                    case "Finally": finallyt = f; break;
                    default: tryls.Add(f); break;
                }
            }

            bool rds = true;
            bool rdstmp;
            Sema[] try_ = CreateBlock(tryls.ToArray(), out rdstmp);
            rds &= rdstmp;
            List<TryStmt.CatchStmt> catches = new List<TryStmt.CatchStmt>();
            foreach (Token c in catchls)
            {
                TryStmt.CatchStmt cs = new TryStmt.CatchStmt();
                object o = Gate(c.Follows[0]);
                Typ ty = null;
                Variable vr = null;
                if (o != null)
                {
                    if (o.GetType() == typeof(Typ)) { ty = o as Typ; }
                    else if (o.GetType() == typeof(Variable)) { vr = o as Variable; ty = vr.Att.TypGet; }
                }
                if (ty == null || false == ty.IsReferencing
                    || (ty.RefType != typeof(Exception)
                    && false == (ty.RefType.IsSubclassOf(typeof(Exception))))
                    )
                { throw new SemanticError("Did not specify exception", c.Follows[0]); }
                cs.ExcpTyp = ty;
                cs.ExcpVar = vr;
                cs.Block = CreateBlock(c.Follows[1].Follows, out rdstmp);
                catches.Add(cs);
                rds &= rdstmp;
            }

            Sema[] finally_ = null;
            if (finallyt != null)
            {
                finally_ = CreateBlock(finallyt.Follows, out rdstmp);
                rds &= rdstmp;
            }

            string fix = E.GetTempName();

            return new TryStmt(fix, try_, catches.ToArray(), finally_, rds);
        }

        public object If(Token if_)
        {
            Token cond, then, else_;
            List<Token> elifs;
            IfInfo.Component ifthen;
            List<IfInfo.Component> elifthen = new List<IfInfo.Component>();
            Sema[] elsels = null;
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

            string fix = E.GetTempName();
            List<Sema> lines = new List<Sema>();

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

        public Sema RequireCondition(Token cond)
        {
            Sema condv = Require<Sema>(cond);
            if (false == condv.Att.TypGet.IsReferencingOf(typeof(bool)))
            {
                throw new TypeError("condition expression was not bool type", cond);
            }
            return condv;
        }

        public Sema[] CreateBlock(Token[] block, out bool rds)
        {
            List<Sema> lines = new List<Sema>();
            rds = false;

            int i = 0;
            while (i < block.Length)
            {
                Token line = block[i];
                Sema exe = RequireExec(line);
                lines.Add(exe);
                if (exe is IReturnDeterminacyState)
                { rds |= (exe as IReturnDeterminacyState).RDS; }
                ++i;
                if (false == (exe is ReturnValue)) { continue; }

                if (i >= block.Length)
                { throw new SyntaxError("Value is not specified for the return", line); }
                ReturnValue rv = exe as ReturnValue;
                line = block[i];

                rv.GiveVal = Require<Sema>(line);
                ++i;
            }

            return lines.ToArray();
        }

        public object CallFunc(Token t)
        {
            List<Typ> argtyps = new List<Typ>();
            List<Sema> argvals = new List<Sema>();
            object first;
            Type firstty;

            // arguments
            object obj = Gate(t.Second);
            if (obj != EmptyS)
            {
                Chain argschain = obj is Chain ? obj as Chain : new Chain(obj);
                foreach (object a in argschain)
                {
                    if (a.GetType() == typeof(Nmd))
                    {
                        Nmd n = a as Nmd;
                        throw new SemanticError(string.Format("Assign value to variable:'{0}' before reference it", n.Name));
                    }
                    if (false == (a is Sema))
                    { throw new SemanticError("Cannot be an argument", t.Second.Follows[0]); }
                    Sema v = a as Sema;
                    argvals.Add(v);
                    argtyps.Add(v.Att.TypGet);
                }
            }

            first = Gate(t.First);
            if (first == null)
            { throw new SyntaxError("It is not a function or constructor", t.First); }
            firstty = first.GetType();

            if (firstty != typeof(Member) && firstty != typeof(Ovld) && firstty != typeof(Typ))
            { throw new SyntaxError("It is not a function or constructor", t.First); }

            Ovld ovl = null;
            Member mbr = null;
            Typ calleetyp = null;
            if (firstty == typeof(Member))
            {
                mbr = first as Member;
                if (mbr.Value.GetType() != typeof(Ovld))
                { throw new NotImplementedException(); }

                calleetyp = mbr.Ty;
                ovl = mbr.Value as Ovld;
            }
            if (firstty == typeof(Ovld))
            {
                ovl = first as Ovld;
            }
            bool isNewObj = false;
            if (firstty == typeof(Typ))
            {
                calleetyp = first as Typ;
                isNewObj = true;
                ovl = calleetyp.FindOvld(Nana.IMRs.IMRGenerator.InstCons);
            }
            Debug.Assert(ovl != null);

            Fun sig = null;

            sig = ovl.GetFunOf(calleetyp, argtyps.ToArray(), Ty);
            if (sig == null) { throw new SyntaxError("It is not a member", t.First); }

            Sema instance = mbr == null ? null : mbr.Instance;


            return new CallFun(calleetyp, sig, instance, argvals.ToArray(), isNewObj);
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
                { throw new SyntaxError("Specify type, function or variable name", sec); }

                return Gate(new Token(nsp.Name + "." + sec.Value, "Id"));
            }

            Typ y = null;
            Sema v = null;
            if (holder is Sema)
            {
                v = holder as Sema;
                y = v.Att.TypGet;
            }
            if (holder.GetType() == typeof(Typ))
            {
                y = holder as Typ;
            }
            if (y == null) { throw new SemanticError(string.Format("It has no member: {0}", t.Second.Value), t.Second); }

            //TODO load funcs automaticaly
            //y.GetActions();

            Nmd mbr = y.FindMemeber(t.Second.Value);

            //if (mbr == null) { throw new SyntaxError("It is not a member", t.Second); }
            if (mbr == null) { throw new SemanticError(string.Format("{0} is not a member of {1}", t.Second.Value, y._FullName), t.Second); }
            if (mbr is Enu) { return mbr; }
            if (mbr is Prop) { return new CallPropInfo(y, mbr as Prop, v); };
            if (mbr is Variable && (mbr as Variable).VarKind == Variable.VariableKind.Field)
            { return new FieldAccessInfo(mbr as Variable, v); }

            return new Member(y, mbr, v);
        }

        static public readonly Token Empty = new Token("(Empty)", "Empty");
        static public readonly Token Comma = new Token(",", "Factor");
        static public readonly Sema EmptyS = new Sema();

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
            //  Bracket can be translated to one of in:
            //      type, array instantiation, array accessing

            //  (First)             (Second)                    (Translation)
            //  Typ                 Empty or Empty Chain        type
            //  Typ                 Index or Index Chain    =>  array instantiation
            //  ArrayInstatiation   Empty or Empty Chain        array instantiation
            //  array instance      Index or Index Chain        array accessing

            object f = Gate(t.First);
            ArrayInstatiation arins = null;
            Typ ty = null;
            Sema val = null;
            if (f.GetType() == typeof(ArrayInstatiation))
            {
                arins = f as ArrayInstatiation;
            }
            if (f.GetType() == typeof(Typ))
            { 
                ty = f as Typ; 
            }
            if (ty == null && f is Sema)
            {
                val = f as Sema;
                if (val.Att.TypGet.IsVectorOrArray == false)
                { throw new SemanticError("require an array value in front of '[]'", t.First); }
            }
            if (ty == null && val == null)
            { throw new SemanticError("require a type or  an array value in front of '[]'", t.First); }

            object s = Gate(t.Second);
            Chain contents = s is Chain ? s as Chain : new Chain(s);

            bool isEmpty = true;
            foreach (object c in contents)
            {
                if (false == (c is Sema))
                {
                    isEmpty = false;
                    break;
                }
                if ((c as Sema) != EmptyS)
                {
                    isEmpty = false;
                    break;
                }
            }

            bool isIndex = false;
            if (false == isEmpty)
            {
                isIndex = true;
                foreach (object c in contents)
                {
                    if (false == (c is Sema))
                    {
                        isIndex = false;
                        break;
                    }
                    Typ y = (c as Sema).Att.TypGet;
                    if (y.IsReferencing == false || y.RefType != typeof(int))
                    {
                        isIndex = false;
                        break;
                    }
                }
            }

            if (ty != null && isEmpty)
            {
                Typ typ = E.FindOrNewArrayTyp(ty, contents.Count);
                return typ;
            }

            if (ty != null && isIndex)
            {
                Typ typ = E.FindOrNewArrayTyp(ty, contents.Count);

                Sema[] indexes = new List<object>(contents)
                    .ConvertAll<Sema>(delegate(object c) { return c as Sema; })
                    .ToArray();

                ArrayInstatiation ins = new ArrayInstatiation(typ, indexes, TmpVarGen);
                return ins;
            }

            if (arins != null && isEmpty)
            {
                Typ typ = E.FindOrNewArrayTyp(arins.Att.TypGet, contents.Count);

                Sema[] indexes = new List<object>(contents)
                    .ConvertAll<Sema>(delegate(object c) { return c as Sema; })
                    .ToArray();
                ;

                ArrayInstatiation ins = new ArrayInstatiation(typ, arins.Lens, TmpVarGen);
                return ins;
            }

            if (val != null && isIndex)
            {
                Sema[] indexes = new List<object>(contents)
                    .ConvertAll<Sema>(delegate(object c) { return c as Sema; })
                    .ToArray();
                ;

                ArrayAccessInfo acc = new ArrayAccessInfo(val, indexes);
                return acc;
            }

            return null;
        }

        public object Generics(Token t)
        {
            // get type parameters
            List<Typ> tprms = new List<Typ>();
            {
                foreach (object o in new Chain(Gate(t.Second)))
                {
                    if (o == null || o.GetType() != typeof(Typ))
                    { throw new SyntaxError("specified not type in type parameter", t.Second); }
                    tprms.Add(o as Typ);
                }
            }

            Typ tp = null;
            {
                // get type or valuable
                Token spc = t.First;
                object specsem = Gate(spc);
                if (specsem.GetType() != typeof(Nmd))
                { throw new SyntaxError("Not a generic type", spc); }

                tp = RequireTyp(new Token((specsem as Nmd).Name + "`" + tprms.Count.ToString(), "Id"));

                if (tp == null)
                { throw new SyntaxError("Unkown generic type:" + (specsem as Nmd).Name); }
            }


            //  find instance type
            Typ typinst = E.FindOrNewGenericTypInstance(tp, tprms.ToArray());
            return typinst;
        }
    }

    public class BlockAnalyzer : LineAnalyzer
    {
        public Stack<ReturnValue> RequiredReturnValue = new Stack<ReturnValue>();

        public BlockAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        {
            Token c = Seed.Custom;
            while (c != null)
            {
                Subs.AddLast(new CustomAnalyzer(c, this));
                c = c.Custom;
            }
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
            Ns = FindUpTypeIs<FunAnalyzer>().Fu;
            foreach (SemanticAnalyzer a in Subs)
            {
                if (a.GetType() != typeof(LineAnalyzer))
                { continue; }
                (a as LineAnalyzer).AnalyzeLine(); 
            }
        }

        virtual public object Find(string name)
        {
            if (Ns == null) { return null; }
            return Ns.Find(name);
        }

        virtual public object FindUp(string name)
        {
            return Find(name) ?? (AboveBlock != null ? AboveBlock.FindUp(name) : null);
        }

    }

    public class FunAnalyzer : BlockAnalyzer
    {
        public FunAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        { }

        public override void ConstructSubs()
        {
            Token block = Seed.Find("@Block");
            if (block == null) { return; }
            Subs.AddLast(new BlockAnalyzer(block, this));
            ConstructSubsAll();
        }

        public List<Variable> prmls = new List<Variable>();

        public void AnalyzeFun()
        {
            Ap = FindUpTypeOf<AppAnalyzer>().Ap;
            Ty = FindUpTypeIs<TypAnalyzer>().Ty;

            Token t = Seed;
            bool isStatic, isCtor;
            bool isInTypDecl = false == (Ty is App);
            string ftyp = ResolveFuncType(t.Value, isInTypDecl);

            MethodAttributes attrs = AnalyzeAttrs(ftyp);
            isStatic = (attrs & MethodAttributes.Static) == MethodAttributes.Static;
            string nameasm = AnalyzeName(ftyp, t);
            isCtor = nameasm == Nana.IMRs.IMRGenerator.InstCons;

            TypAnalyzer typazr2 = FindUpTypeOf<TypAnalyzer>()
                ?? FindUpTypeOf<AppAnalyzer>()
                ;
            Debug.Assert(typazr2 != null);

            Ovld ovld = typazr2.Ty.FindOrNewOvld(nameasm);

            List<Token> prms = new List<Token>();
            Token prmpre = t.Find("@PrmDef");
            if (prmpre.Follows != null && prmpre.Follows.Length > 0)
            { Gate(prmpre.Follows[0]); }

            Token ty;

            Typ voidtyp = FindUpTypeOf<EnvAnalyzer>().E.BTY.Void;
            Typ returnType = voidtyp;
            if (isCtor)
            {
                returnType = FindUpTypeIs<TypAnalyzer>().Ty;
            }
            else if (null != (ty = t.Find("@TypeSpec")))
            {
                returnType = RequireTyp(ty.Follows[0]);
            }

            List<Typ> signature = prmls.ConvertAll<Typ>(delegate(Variable v) { return v.Att.TypGet; });

            if (ovld.Contains(signature.ToArray()))
            { throw new SemanticError("The function is already defined. Function name:" + nameasm, t); }

            Fu = ovld.NewFun(nameasm, prmls, returnType);

            base.Ns = Fu;

            Fu.MthdAttrs = attrs;

            //  generate instance variable
            if (Fu.IsInstance)
            {
                TypAnalyzer typazr = FindUpTypeOf<TypAnalyzer>();
                if (typazr == null)
                { throw new SyntaxError("Cannot define instance constructor in this sapce", t); }
                Fu.NewThis(typazr.Ty);
            }
        }

        public override Variable NewVar(string name, Typ typ)
        {
            Variable v = new Variable(name, typ, Variable.VariableKind.Param);
            prmls.Add(v);
            return v;
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

        static public void EnusureReturn(Fun a)
        {
            bool rds = false;
            foreach (Sema x in a.Exes)
            {
                if (x is IReturnDeterminacyState)
                { rds |= (x as IReturnDeterminacyState).RDS; }
            }
            if (a.IsConstructor == false && a.Att.CanGet)
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

    public class CustomAnalyzer : LineAnalyzer
    {
        public CustomAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        {
        }

        public void AnalyzeCustom()
        {
            FindUpNsps();

            Token t = Seed;
            object o = Gate(t);
            if (typeof(Typ) == o.GetType())
            {
                Typ calleetyp = o as Typ;
                Ovld ovl = calleetyp.FindOvld(Nana.IMRs.IMRGenerator.InstCons);
                Fun f = ovl.GetFunOf(calleetyp, new Typ[] { }, Ty);
                if (f == null) { throw new SyntaxError("It is not a member", t.First); }
                Nsp n = AboveBlock.Ns;
                if (n.Customs == null)
                { n.Customs = new LinkedList<Custom>(); }
                n.Customs.AddLast(new Custom(calleetyp, f, new Typ[] { }, new Custom.FieldOrProp[] { }));
            }
        }
    }

    public class TypAnalyzer : FunAnalyzer
    {
        public TypAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        { }

        public override void ConstructSubs()
        {
            foreach (Token t in Seed.Select("@Block/@Fnc"))
            { Subs.AddLast(new FunAnalyzer(t, this)); }
            ConstructSubsAll();
        }

        public void AnalyzeTyp()
        {
            Token s = Seed;
            Token name = s.Find("@Name");
            if (name == null || string.IsNullOrEmpty(name.Value))
            { throw new InternalError("Specify name to the type", s); }

            AppAnalyzer appazr = FindUpTypeOf<AppAnalyzer>();
            App app = appazr.Ap;
            if (app.HasMember(name.Value))
            { throw new SemanticError("The type is already defined. Type name:" + name.Value, name); }
            base.Ns = base.Fu = base.Ty = app.NewTyp(name.Value);

            foreach (Token t in Seed.Find("@Block").Follows)
            {
                if (t.Group == "Fnc")
                { continue; }

                Gate(t);
            }
        }

        public void AnalyzeBaseTyp()
        {
            Token baseTypeDef = Seed.Find("@BaseTypeDef");
            Ty.BaseTyp = baseTypeDef != null
                ? RequireTyp(baseTypeDef.Follows[0])
                : E.BTY.Object;
        }

        public override object Find(string name)
        {
            Nmd n = Ty.Find(name);
            if (n == null)
            { return null; }
            Type nt = n.GetType();
            if (nt == typeof(Ovld))
            { return new Member(Ty, n, null); }
            return n;
        }

        public override Variable NewVar(string name, Typ typ)
        {
            if (Ty.HasMember(name))
            { ErNameDuplication(new Token(name), Ty); }

            return Ty.NewVar(name, typ);
        }

    }

    public class SrcAnalyzer : BlockAnalyzer
    {
        public LinkedList<Token> UsingSeeds;
        public LinkedList<Nsp> UsingNsp = new LinkedList<Nsp>();

        public SrcAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        {
        }

        public override void ConstructSubs()
        {
            if (Seed.Follows == null) { return; }
            SemanticAnalyzer a;
            foreach (Token t in Seed.Follows)
            {
                a = null;
                Token targ = t.Group != "Cstm" ? t : GetTargetWithCustom(t);
                switch (targ.Group)
                {
                    case "TypeDef": a = new TypAnalyzer(targ, this); break;
                    case "Fnc": a = new FunAnalyzer(targ, this); break;
                    case "Using":
                        if (UsingSeeds == null) { UsingSeeds = new LinkedList<Token>(); }
                        UsingSeeds.AddLast(targ);
                        break;
                    default: a = new LineAnalyzer(targ, this); break;
                }
                if (a != null)
                { Subs.AddLast(a); }
            }
            ConstructSubsAll();
        }

        public void AnalyzeSrc()
        {
            Typ ty = FindUpTypeOf<AppAnalyzer>().Ty;
            UsingNsp.AddLast(ty.E.FindOrNewNsp("System"));
        }

        public void AnalyzeUsing()
        {
            if (UsingSeeds == null) { return; }
            foreach (Token s in UsingSeeds)
            {
                object o = Gate(s.Follows[0]);
                if (o.GetType() != typeof(Nsp))
                { throw new SemanticError("Specify namespace", s); }
                UsingNsp.AddLast(o as Nsp);
            }
        }

        public override object FindUp(string name)
        {
            if (AboveBlock == null)
            { return null; }

            object o = AboveBlock.FindUp(name);
            if (o != null) { return o; }

            foreach (Nsp n in UsingNsp)
            {
                o = AboveBlock.FindUp(n.Name + "." + name);
                if (o != null) { return o; }
            }

            return null;
        }

    }

    public class AppAnalyzer : TypAnalyzer
    {
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
            base.Fu = base.Ty = base.Ap = FindUpTypeOf<EnvAnalyzer>().E.NewApp(Seed.Value);
        }

        override public object FindUp(string name)
        {
            return Find(name) ?? (AboveBlock != null ? AboveBlock.FindUp(name) : null);
        }

    }

    public class EnvAnalyzer : AppAnalyzer
    {
        public EnvAnalyzer(Token seed)
            : base(seed, null)
        { }

        public static Env Run(Token root)
        {
            EnvAnalyzer ea = new EnvAnalyzer(root);
            ea.Prelude();
            ea.Main();
            ea.Finale();
            return ea.E;
        }

        public void Prelude()
        {
            E = new Env();
            base.Ns = E;
            foreach (Token opt in Seed.Find("@CompileOptions").Follows)
            {
                switch (opt.Group.ToLower())
                {
                    case "include":     /**/ E.TypeLdr.InAssembly.Includes.Add(opt.Value); break;
                    case "reference":   /**/ E.TypeLdr.InAssembly.LoadFrameworkClassLibrarie(opt.Value); break;
                    case "out":         /**/ E.OutPath = opt.Value; break;
                    case "verbose":     /**/ break;
                    default:
                        if (opt.Group.ToLower().StartsWith("xxx")) { break; }
                        throw new InternalError("The compile option is not supported: " + opt.Group, opt);
                }
            }
            ConstructSubs();
        }

        public override void ConstructSubs()
        {
            Subs.AddLast(new AppAnalyzer(Seed.Find("@Syntax"), this));
            ConstructSubsAll();
        }

        public void Main()
        {
            AnalyzeAppAll();
            AnalyzeSrcAll();
            AnalyzeTypAll();
            AnalyzeUsingAll();
            AnalyzeBaseTypAll();
            AnalyzeFunAll();
            EnsureTypAll();
            EnsureBaseInstanceConstructorCallAll();
            AnalyzeBlockAll();
            AnalyzeCustomAll();
        }

        public void Finale()
        {
            EnsureAppExe();
            EnsureEntryPoint();
            EnsureFunctionReturnAll();
            RemoveReferencingType(E);
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

        public void AnalyzeUsingAll()
        {
            foreach (SrcAnalyzer a in CollectTypeOf<SrcAnalyzer>())
            { a.AnalyzeUsing(); }
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

        public void AnalyzeFunAll()
        {
            foreach (FunAnalyzer a in CollectTypeOf<FunAnalyzer>())
            { a.AnalyzeFun(); }
        }

        public void EnsureTypAll()
        {
            foreach (TypAnalyzer ta in CollectTypeOf<TypAnalyzer>())
            {
                Typ y = ta.Ty;
                if (y.Ovlds
                    .Exists(delegate(Ovld ao_)
                    {
                        return ao_.Funs
                            .Exists(delegate(Fun f) { return f.IsInherited == false && f.Name == ".ctor"; });
                    }))
                { continue; }

                Token t = GenFuncToken("cons", /* name */ null, /* returnType */ null);
                FunAnalyzer aa = new FunAnalyzer(t, ta);
                ta.Subs.AddLast(aa);
                aa.AnalyzeFun();
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
            foreach (FunAnalyzer faz in CollectTypeOf<FunAnalyzer>())
            {
                Fun fun = faz.Fu;
                if (false == Nana.IMRs.IMRGenerator.IsInstCons(fun.Name))
                { continue; }
                Typ myty = faz.Ty;
                Typ bsty = myty.BaseTyp;
                Fun callee = bsty.FindOvld(".ctor").GetFunOf(bsty, new Typ[] { }, myty);
                Sema instance = fun.FindVar("this");
                fun.Exes.Add(new CallFun(bsty, callee, instance, new Sema[] { }, false /*:isNewObj*/));
            }
        }

        public void AnalyzeBlockAll()
        {
            foreach (SrcAnalyzer a in CollectTypeOf<SrcAnalyzer>())
            { a.AnalyzeBlock(); }
            foreach (BlockAnalyzer a in CollectTypeOf<BlockAnalyzer>())
            { a.AnalyzeBlock(); }
        }

        public void AnalyzeCustomAll()
        {
            foreach (CustomAnalyzer a in CollectTypeOf<CustomAnalyzer>())
            { a.AnalyzeCustom(); }
        }

        public override object Find(string name)
        {
            if (TypeUtil.IsBuiltIn(name))
            { return E.FindOrNewRefType(TypeUtil.FromBuiltIn(name)); }

            {
                Member m;
                if (E.BFN.Members.TryGetValue(name, out m))
                { return m; }
            }

            Nmd n;
            if (null != (n = E.Find(name))) { return n; }

            if (E.TypeLdr.IsNamespace(name))
            { return E.FindOrNewNsp(name); }

            Type type = E.TypeLdr.GetTypeByName(name);
            if (null != type)
            { return E.FindOrNewRefType(type); }

            return null;
        }

        public void EnsureAppExe()
        {
            AppAnalyzer appaz = CollectTypeOf<AppAnalyzer>().First.Value;
            App app = appaz.Ap;
            if (app.Exes.Count == 0) { return; }

            Token t = GenFuncToken("scons", Fun.EntryPointNameImplicit, "void");
            FunAnalyzer funaz = new FunAnalyzer(t, appaz);
            funaz.AnalyzeFun();
            Fun cctor = funaz.Fu;
            cctor.Exes.AddRange(app.Exes);
            app.Exes.Clear();
        }

        public void EnsureEntryPoint()
        {
            AppAnalyzer aa = CollectTypeOf<AppAnalyzer>().First.Value;
            App app = aa.Ap;
            List<Nmd> funs = app.FindDownAll(delegate(Nmd n) { return n is Fun; });
            List<Nmd> founds = funs.FindAll(delegate(Nmd f) { return  (f as Fun).IsEntryPoint; });
            if (founds.Count > 1)
            { throw new SyntaxError("Specify one entry point. There were two entry points or more."); }
            if (founds.Count == 1)
            { return; }

            Token t = GenFuncToken("sfun", Fun.EntryPointNameImplicit, "void");
            (new FunAnalyzer(t, aa)).AnalyzeFun();
        }

        public void EnsureFunctionReturnAll()
        {
            AppAnalyzer aa = CollectTypeOf<AppAnalyzer>().First.Value;
            App app = aa.Ap;

            Predicate<Nmd> pred = delegate(Nmd n)
            { return n.GetType() == typeof(Fun); };

            foreach (Fun a in app.FindDownAll(pred))
            { FunAnalyzer.EnusureReturn(a); }
        }

        public static void RemoveReferencingType(Env env)
        {
            env.Members.RemoveAll(delegate(Nmd n)
            {
                return n is Typ && (n as Typ).IsReferencing == true;
            });

        }

    }

}