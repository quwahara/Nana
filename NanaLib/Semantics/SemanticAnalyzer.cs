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
    public class LineAnalyzer
    {
        public void ErNameDuplication(Token dupname, Blk n)
        { throw new SemanticError(string.Format("The {0} is already defined in {1}", dupname.Value, n.Name), dupname); }

        public Token Seed;
        public BlockAnalyzer AboveBlock;
        
        public Stack<Literal> Breaks;
        public Stack<Literal> Continues;
        public Env E;
        public App Ap;
        public Typ Ty;
        public Fun Fu;
        public Blk Bl;
        public TmpVarGenerator TmpVarGen;
        public bool IsInFun;

        public LineAnalyzer(Token seed, BlockAnalyzer above)
        {
            Seed = seed;
            AboveBlock = above;

            Breaks = new Stack<Literal>();
            Continues = new Stack<Literal>();

            if (null != above && null != above.Ez)
            { E = AboveBlock.Ez.E; }
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

        public void PrepareAboveBlock()
        {
            BlockAnalyzer ab = AboveBlock;
            Ap = ab.Ap;
            Ty = ab.Ty;
            Fu = ab.Fu;
            Bl = ab.Bl;
        }

        public void AnalyzeLine()
        {
            PrepareAboveBlock();
            
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
                case "TypSpc":      /**/ u = DefineVariable(t); break;
                case "Ret":         /**/ u = Ret(t); break;
                case "Nop":         /**/ u = new DoNothing(); break;
                case "_End_Cma_":   /**/ u = Cma(t); break;
                case "Throw":       /**/ u = Throw(t); break;
                case "Try":         /**/ u = Try(t); break;
                case "Cls":         /**/ u = Closure(t); break;
                default:
                    throw new SemanticError(string.Format("'{0}' cannot be in there", t.Value), t);
            }
            return u;
        }

        public static readonly string ClosurePrefix = "'0clsr";

        public object Closure(Token t)
        {
            AppAnalyzer apz = AboveBlock.Apz;
            SrcAnalyzer srz = AboveBlock.Srz;

            string tmpnm = E.GetTempName();
            Token prm = null;
            if (")" != t.Follows[0].Value)
            { prm = t.Follows[0]; }

            Token typspc = t.Find("@TypSpc");

            string clsname = ClosurePrefix + tmpnm + "'";
            {
                Token classtkn = CreateClassToken(clsname, /*basename*/ null);
                Token classblk = classtkn.Find("@Block");

                Token contkn = CreateFunToken("cons", /* name */ null, /* returnType */ null);
                Token funtkn = CreateFunToken("nfun", "'0impl'", "void");
                classblk.FlwsAdd(contkn);
                classblk.FlwsAdd(funtkn);

                if (null != prm)
                { funtkn.Find("@Prm").FlwsAdd(prm); }
                if (null != typspc)
                { funtkn.Find("@TypSpc").Follows = typspc.Follows; }

                funtkn.Find("@Block").Follows = t.Find("@Block").Follows;

                TypAnalyzer taz = srz.NewTyz(classtkn);
                
                taz.ConstructSub();
                taz.AnalyzeTyp();

                List<BlockAnalyzer> blks = new List<BlockAnalyzer>(2);
                foreach (FunAnalyzer f in taz.Fuzs)
                {
                    f.AnalyzeFun();
                    blks.AddRange(f.Blzs);
                }
                foreach (BlockAnalyzer blk in blks)
                {
                    if (typeof(BlockAnalyzer) != blk.GetType()) { continue; }
                    (blk as BlockAnalyzer).AnalyzeBlock();
                }

            }

            string dlgname = "'0dlgt" + tmpnm + "'";
            {
                Token classtkn = CreateClassToken(dlgname, "System.MulticastDelegate");
                Token classblk = classtkn.Find("@Block");

                Token contkn = CreateFunToken("cons", /* name */ null, /* returnType */ null);
                contkn.Find("@Prm").FlwsAdd(CreateParamToken(new string[] { "obj:object", "mth:System.IntPtr" }));
                Token funtkn = CreateFunToken("nfun", "Invoke", "void");

                classblk.FlwsAdd(contkn);
                classblk.FlwsAdd(funtkn);

                if (null != prm)
                { funtkn.Find("@Prm").FlwsAdd(prm); }
                if (null != typspc)
                { funtkn.Find("@TypSpc").Follows = typspc.Follows; }

                TypAnalyzer taz = srz.NewTyz(classtkn);

                taz.ConstructSub();
                taz.AnalyzeTyp();
                taz.AnalyzeBaseTyp();
                foreach (FunAnalyzer f in taz.Fuzs)
                { f.AnalyzeFun(); }

            }

            Typ clstyp = AboveBlock.FindUp(clsname) as Typ;
            Fun clscon = clstyp.FindOvld(".ctor").Funs[0];
            Sema inst = new CallFun(clstyp, clscon, /*instance*/ null, new Sema[0], /*isNewObj*/ true);
            Fun clsfun = clstyp.FindOvld("'0impl'").Funs[0];

            Typ dlgtyp = AboveBlock.FindUp(dlgname) as Typ;
            Fun dlgcon = dlgtyp.FindOvld(".ctor").Funs[0];

            Sema[] args = new Sema[] { inst, new LoadFun(clstyp, clsfun) };

            return new CallFun(dlgtyp, dlgcon, /*instance*/ null, args, /*isNewObj*/ true);
        }

        public static Token CreateParamToken(string[] nameAndTypes)
        {
            Token cur = null;
            Token cma = null;
            foreach (string nt in nameAndTypes)
            {
                if (null != cma)
                {
                    Token tmp = cma;
                    cma = new Token(",", "_End_Cma_");
                    cma.First = tmp;
                }

                string[] spl = nt.Split(new char[] { ':' });
                cur = new Token(":", "TypSpc");
                cur.First = new Token(spl[0], "Id");
                cur.Second = new Token(spl[1], "Id");

                if (cma == null)
                { cma = cur; }
                else
                { cma.Second = cur; }
            }

            return cma;
        }

        public static Token CreateClassToken(string name, string basename)
        {
            Token  root  = new Token("class", "Typ");
            root.FlwsAdd(name, "Name");
            if (null != basename)
            {
                Token bs = new Token("->", "BaseTypeDef");
                bs.FlwsAdd(basename, "Id");
                root.FlwsAdd(bs);
            }
            root.FlwsAdd("...", "Block")
                .FlwsAdd(",,,");
            return root;
        }

        static public Token CreateFunToken(string func, string name, string returnType)
        {
            Debug.Assert(false == string.IsNullOrEmpty(func));

            Token f;

            f = new Token(func, "Fun");

            if (string.IsNullOrEmpty(name) == false)
            { f.FlwsAdd(name); }

            f.FlwsAdd("(", "Prm").FlwsAdd(")");

            if (string.IsNullOrEmpty(returnType) == false)
            {
                f.FlwsAdd(":", "TypSpc");
                f.FlwsTail.FlwsAdd(returnType, "Id");
            }

            f.FlwsAdd("..", "Block");
            f.FlwsTail.Follows = new Token[0];

            return f;
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

            first = Gate(t.First);
            if (null == first)
            { throw new InternalError("Could not analyze the token for callee function", t.First); }

            firstty = first.GetType();
            bool dottedMethodCall = firstty == typeof(Member);
            bool notDottedMethodCall = firstty == typeof(Ovld);
            bool constructorCall = firstty == typeof(Typ);
            bool delegateCall = false;
            if (first is Sema)
            {
                Typ typget = (first as Sema).Att.TypGet;
                if (null != typget) { delegateCall = typget.IsDelegate; }
            }

            if (false == (dottedMethodCall | notDottedMethodCall | constructorCall | delegateCall))
            { throw new SemanticError("Cannot call it. It is not a function constructor", t); }

            Ovld ovl = null;
            Sema instance = null;
            Typ calleetyp = null;
            bool isNewObj = false;
            bool delegateConstructorCall = false;

            if (dottedMethodCall)
            {
                Member mbr = first as Member;

                calleetyp = mbr.Ty;
                if (null == calleetyp)
                { throw new InternalError("Could not get Typ for declaring the callee function", t); }

                ovl = mbr.Value as Ovld;
                if (null == ovl)
                { throw new InternalError("Could not get overload for the callee function", t); }

                instance = mbr.Instance;
            }
            else if (notDottedMethodCall)
            {
                ovl = first as Ovld;
            }
            else if (constructorCall)
            {
                calleetyp = first as Typ;
                isNewObj = true;
                delegateConstructorCall = calleetyp.IsDelegate;
                ovl = calleetyp.FindOvld(Nana.IMRs.IMRGenerator.InstCons);
                if (null == ovl)
                { throw new SemanticError(string.Format("No constructor for the type:{0}", calleetyp.Name), t); }
            }
            else if (delegateCall)
            {
                instance = first as Sema;
                calleetyp = instance.Att.TypGet;
                ovl = calleetyp.FindOvld("Invoke");
                if (null == ovl)
                { throw new SemanticError(string.Format("No constructor for the type:{0}", calleetyp.Name), t); }
            }

            // arguments
            object obj = Gate(t.Second);
            if (obj != EmptyS)
            {
                Chain argschain = obj is Chain ? obj as Chain : new Chain(obj);
                if (false == delegateConstructorCall)
                {
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
                else
                {
                    //  create delegate
                    if (2 != argschain.Count)
                    { throw new SemanticError(string.Format("Argument count must be 2 to create delegate but actual count is: {0}", argschain.Count.ToString())); }

                    //  put instance for invoking
                    Sema trginst = argschain.First.Value as Sema;
                    argvals.Add(trginst);
                    argtyps.Add(trginst.Att.TypGet);
                    
                    //  to detect target method, retrieve signature of Invoke() method
                    Ovld invkovl = calleetyp.FindOvld("Invoke");
                    if (0 == invkovl.Funs.Count)
                    { throw new SemanticError(string.Format("No Invoke method in delegate calss: {0}", invkovl.Name)); }
                    if (2 <= invkovl.Funs.Count)
                    { throw new SemanticError(string.Format("Two or more Invoke methods in delegate calss: {0}", invkovl.Name)); }
                    Typ[] invksig = invkovl.Funs[0].Signature;
                    
                    //  retrieve target method
                    Member funmbr = argschain.Last.Value as Member;
                    Ovld mbrovld = funmbr.Value as Ovld;
                    Fun targetfun = mbrovld.GetFunOf(funmbr.Ty, invksig, Ty);
                    
                    //  put function loading to arguments
                    LoadFun ldfun = new LoadFun(funmbr.Ty, targetfun);
                    argvals.Add(ldfun);
                    argtyps.Add(ldfun.Att.TypGet);
                }
            }

            {
                Fun calleefun = ovl.GetFunOf(calleetyp, argtyps.ToArray(), Ty);
                if (calleefun == null)
                { throw new SemanticError(string.Format("No method matches for the calling: {0}", ovl.Name), t); }
                return new CallFun(calleetyp, calleefun, instance, argvals.ToArray(), isNewObj);
            }
        }

        public object Dot(Token t)
        {
            Debug.Assert(t.First != null);
            Debug.Assert(t.Second != null);

            object holder = Gate(t.First);

            if (holder.GetType() == typeof(Blk))
            {
                Blk nsp = holder as Blk;
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

        static public readonly Sema EmptyS = new Sema();

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

        public override string ToString()
        {
            return ((Seed ?? Token.Empty).Find("@Name") ?? Token.Empty).Value
                + ":" + GetType().Name.Replace("Analyzer", "Az")
                + (null != AboveBlock ? ">" + AboveBlock.ToString() : "")
                ;
        }

    }

    public class BlockAnalyzer : LineAnalyzer
    {
        public EnvAnalyzer Ez;
        public AppAnalyzer Apz;
        public SrcAnalyzer Srz;
        public TypAnalyzer Tyz;
        public FunAnalyzer Fuz;
        public BlockAnalyzer Blz;
        public List<LineAnalyzer> Lizs;

        public LinkedList<TypAnalyzer> Tyzs = new LinkedList<TypAnalyzer>();
        public LinkedList<FunAnalyzer> Fuzs = new LinkedList<FunAnalyzer>();

        public static readonly BlockAnalyzer EmptyBlz = new BlockAnalyzer();
        public BlockAnalyzer() : base(null, null) { }

        public Stack<ReturnValue> RequiredReturnValue = new Stack<ReturnValue>();
        public bool IsClosure = false;

        public BlockAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        {
            CopyAboveAnalyzers(above);
            Blz = this;

            Token c = Seed.Custom;
            while (c != null)
            {
                CustomAnalyzer cuz = new CustomAnalyzer(c, this);
                Apz.AllCuzs.AddLast(cuz);
                c = c.Custom;
            }

            if (null != Tyz && null != Tyz.Seed)
            { IsClosure = (Tyz.Seed.Find("@Name") ?? Token.Empty).Value.StartsWith(ClosurePrefix); }
        }

        public void CopyAboveAnalyzers(BlockAnalyzer above)
        {
            Fuz = above.Fuz;
            Tyz = above.Tyz;
            Srz = above.Srz;
            Apz = above.Apz;
            Ez = above.Ez;
        }

        public virtual void ConstructSub()
        {
            if (Seed.Follows == null) { return; }
            foreach (Token f in Seed.Follows)
            { LineAnalyzer liz = NewLiz(f); }
        }

        public TypAnalyzer NewTyz(Token seed)
        {
            TypAnalyzer tyz = new TypAnalyzer(seed, this);
            Tyzs.AddLast(tyz);
            Apz.AllTyzs.AddLast(tyz);
            return tyz;
        }

        public FunAnalyzer NewFuz(Token seed)
        {
            FunAnalyzer fuz = new FunAnalyzer(seed, this);
            Fuzs.AddLast(fuz);
            Apz.AllFuzs.Add(fuz);
            return fuz;
        }

        public LineAnalyzer NewLiz(Token t)
        {
            LineAnalyzer liz = new LineAnalyzer(t, this);
            if (null == Lizs) { Lizs = new List<LineAnalyzer>(); }
            Lizs.Add(liz);
            return liz;
        }

        public void AnalyzeBlock()
        {
            Ty = Tyz.Ty;
            Bl = Fu = Fuz.Fu;
            if (null == Lizs) { return; }
            foreach (LineAnalyzer z in Lizs)
            { z.AnalyzeLine(); }
        }

        virtual public object Find(string name)
        {
            if (Bl == null) { return null; }
            return Bl.Find(name);
        }

        virtual public object FindUp(string name)
        {
            return Find(name) ?? (AboveBlock != null ? AboveBlock.FindUp(name) : null);
        }

    }

    public class FunAnalyzer : BlockAnalyzer
    {
        public LinkedList<BlockAnalyzer> Blzs = new LinkedList<BlockAnalyzer>();

        public FunAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        {
            CopyAboveAnalyzers(above);
            Blz = Fuz = this;
        }

        public override void ConstructSub()
        {
            Token block = Seed.Find("@Block");
            if (block == null) { return; }
            BlockAnalyzer blz = NewBlz(block);
            blz.ConstructSub();
        }

        public BlockAnalyzer NewBlz(Token t)
        {
            BlockAnalyzer blz = new BlockAnalyzer(t, this);
            Blzs.AddLast(blz);
            return blz;
        }

        public List<Variable> prmls = new List<Variable>();

        public void AnalyzeFun()
        {
            Ap = Apz.Ap;
            Ty = Tyz.Ty;

            Token s = Seed;
            bool isInTypDecl = false == (Ty is App);
            
            string ftyp = s.Value;
            if (ftyp == "fun")
            { ftyp = Ty is App ? "sfun" : "vfun"; }
            
            string nameasm = AnalyzeName(ftyp, s);
            bool isCtor = nameasm == Nana.IMRs.IMRGenerator.InstCons;

            List<Token> prms = new List<Token>();
            Token prmpre = s.Find("@Prm");
            if (prmpre.Follows != null && prmpre.Follows.Length > 0)
            { Gate(prmpre.Follows[0]); }

            Typ returnType = E.BTY.Void;
            Token ty;
            if (isCtor)
            {
                returnType = Tyz.Ty;
            }
            else if (null != (ty = s.Find("@TypSpc")))
            {
                returnType = RequireTyp(ty.Follows[0]);
            }

            List<Typ> signature = prmls.ConvertAll<Typ>(delegate(Variable v) { return v.Att.TypGet; });

            Ovld ovld = Ty.FindOrNewOvld(nameasm);

            if (ovld.Contains(signature.ToArray()))
            { throw new SemanticError("The function is already defined. Function name:" + nameasm, s); }

            Bl = Fu = ovld.NewFun(nameasm, prmls, returnType);

            MethodAttributes attrs = MethodAttributes.Public;
            bool isStatic = ftyp[0] == 's';
            if (isStatic) { attrs |= MethodAttributes.Static; }
            if (ftyp == "vfun") { attrs |= MethodAttributes.Virtual; }
            Fu.MthdAttrs = attrs;

            //  generate instance variable
            if (Fu.IsInstance)
            {
                if (Tyz == null)
                { throw new SyntaxError("Cannot define instance constructor in this sapce", s); }
                Fu.NewThis(Tyz.Ty);
            }

            //  record Fun to be convenience
            Apz.Ap.AllFuns.Add(Fu);
        }

        public override Variable NewVar(string name, Typ typ)
        {
            Variable v = new Variable(name, typ, Variable.VariableKind.Param);
            prmls.Add(v);
            return v;
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
            PrepareAboveBlock();

            Token t = Seed;
            object o = Gate(t);
            if (typeof(Typ) == o.GetType())
            {
                Typ calleetyp = o as Typ;
                Ovld ovl = calleetyp.FindOvld(Nana.IMRs.IMRGenerator.InstCons);
                Fun f = ovl.GetFunOf(calleetyp, new Typ[] { }, Ty);
                if (f == null) { throw new SyntaxError("It is not a member", t.First); }
                Blk n = AboveBlock.Bl;
                if (n.Customs == null)
                { n.Customs = new LinkedList<Custom>(); }
                n.Customs.AddLast(new Custom(calleetyp, f, new Typ[] { }, new Custom.FieldOrProp[] { }));
            }
        }
    }

    public class TypAnalyzer : FunAnalyzer
    {
        //public LinkedList<TypAnalyzer> Tyzs = new LinkedList<TypAnalyzer>();

        public TypAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        {
            CopyAboveAnalyzers(above);
            Blz = Fuz = Tyz = this;
        }

        public override void ConstructSub()
        {
            foreach (Token t in Seed.Select("@Block/@Fun"))
            { FunAnalyzer fuz = NewFuz(t); }
            foreach (FunAnalyzer z in Fuzs)
            { z.ConstructSub(); }
        }

        public void AnalyzeTyp()
        {
            Token s = Seed;
            Token name = s.Find("@Name");
            if (name == null || string.IsNullOrEmpty(name.Value))
            { throw new InternalError("Specify name to the type", s); }

            App ap = Apz.Ap;
            if (ap.HasMember(name.Value))
            { throw new SemanticError("The type is already defined. Type name:" + name.Value, name); }
            
            Bl = Fu = Ty = ap.NewTyp(name.Value);

            foreach (Token t in Seed.Find("@Block").Follows)
            {
                if (t.Group == "Fun")
                { continue; }

                Gate(t);
            }
        }

        public void AnalyzeBaseTyp()
        {
            Token baseTypeDef = Seed.Find("@BaseTypeDef");
            Typ bsty = baseTypeDef != null
                ? RequireTyp(baseTypeDef.Follows[0])
                : E.BTY.Object;
            Ty.SetBaseTyp(bsty);
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
        public LinkedList<Blk> UsingNsp = new LinkedList<Blk>();

        public SrcAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        {
            CopyAboveAnalyzers(above);
            Blz = Srz = this;
        }

        public override void ConstructSub()
        {
            if (Seed.Follows == null) { return; }
            foreach (Token t in Seed.Follows)
            {
                Token targ = t.Group != "Cstm" ? t : GetTargetWithCustom(t);
                switch (targ.Group)
                {
                    case "Typ": NewTyz(targ); break;
                    case "Fun": NewFuz(targ); break;
                    case "Using":
                        if (UsingSeeds == null) { UsingSeeds = new LinkedList<Token>(); }
                        UsingSeeds.AddLast(targ);
                        break;
                    default: NewLiz(targ); break;
                }
            }
            foreach (TypAnalyzer z in Tyzs)
            { z.ConstructSub(); }
            foreach (FunAnalyzer z in Fuzs)
            { z.ConstructSub(); }
        }

        public void AnalyzeSrc()
        {
            Fu = Ty = Ap = Apz.Ap;
            UsingNsp.AddLast(Apz.E.FindOrNewNsp("System"));
        }

        public void AnalyzeUsing()
        {
            if (UsingSeeds == null) { return; }
            foreach (Token s in UsingSeeds)
            {
                object o = Gate(s.Follows[0]);
                if (o.GetType() != typeof(Blk))
                { throw new SemanticError("Specify namespace", s); }
                UsingNsp.AddLast(o as Blk);
            }
        }

        public override object FindUp(string name)
        {
            if (AboveBlock == null)
            { return null; }

            object o = AboveBlock.FindUp(name);
            if (o != null) { return o; }

            foreach (Blk n in UsingNsp)
            {
                o = AboveBlock.FindUp(n.Name + "." + name);
                if (o != null) { return o; }
            }

            return null;
        }

    }

    public class AppAnalyzer : TypAnalyzer
    {
        public LinkedList<SrcAnalyzer> Srzs = new LinkedList<SrcAnalyzer>();
        public LinkedList<TypAnalyzer> AllTyzs = new LinkedList<TypAnalyzer>();
        public List<FunAnalyzer> AllFuzs = new List<FunAnalyzer>();
        public LinkedList<CustomAnalyzer> AllCuzs = new LinkedList<CustomAnalyzer>();

        public AppAnalyzer(Token seed, BlockAnalyzer above)
            : base(seed, above)
        {
            CopyAboveAnalyzers(above);
            Blz = Fuz = Tyz = Apz = this;
        }

        public override void ConstructSub()
        {
            foreach (Token t in Seed.Select("@Source"))
            {
                SrcAnalyzer srz = new SrcAnalyzer(t, this);
                Srzs.AddLast(srz);
            }
            foreach (SrcAnalyzer z in Srzs)
            { z.ConstructSub(); }
        }

        public void AnalyzeAppAll()
        {
            AnalyzeApp();
            AnalyzeSrcAll();
            AnalyzeTypAll();
            AnalyzeUsingAll();
            AnalyzeBaseTypAll();
            AnalyzeFunAll();
            EnsureTypAll();
            EnsureBaseInstanceConstructorCallAll();
            AnalyzeBlockAll();
            AnalyzeCustomAll();
            AnalyzeAppExes();
            EnsureEntryPoint();
            EnsureDelegateClassAll();
            EnsureFunctionReturnAll();
        }

        public void AnalyzeApp()
        {
            Bl = Fu = Ty = Ap = E.NewApp(Seed.Value);
        }

        public void AnalyzeSrcAll()
        {
            foreach (SrcAnalyzer a in Srzs)
            { a.AnalyzeSrc(); }
        }

        public void AnalyzeTypAll()
        {
            foreach (TypAnalyzer a in AllTyzs)
            { a.AnalyzeTyp(); }
        }

        public void AnalyzeUsingAll()
        {
            foreach (SrcAnalyzer a in Srzs)
            { a.AnalyzeUsing(); }
        }

        public void AnalyzeBaseTypAll()
        {
            foreach (TypAnalyzer a in AllTyzs)
            { a.AnalyzeBaseTyp(); }
        }

        public void AnalyzeFunAll()
        {
            foreach (FunAnalyzer a in AllFuzs)
            { a.AnalyzeFun(); }
        }

        public void EnsureTypAll()
        {
            foreach (TypAnalyzer ta in AllTyzs)
            {
                Typ y = ta.Ty;
                if (y.Ovlds
                    .Exists(delegate(Ovld ao_)
                    {
                        return ao_.Funs
                            .Exists(delegate(Fun f) { return f.IsInherited == false && f.Name == ".ctor"; });
                    }))
                { continue; }

                Token t = CreateFunToken("cons", /* name */ null, /* returnType */ null);
                FunAnalyzer fuz = ta.NewFuz(t);
                fuz.AnalyzeFun();
            }
        }

        public void EnsureBaseInstanceConstructorCallAll()
        {
            foreach (FunAnalyzer faz in AllFuzs)
            {
                Fun fun = faz.Fu;
                if (false == Nana.IMRs.IMRGenerator.IsInstCons(fun.Name)) { continue; }
                Typ myty = faz.Ty;
                //  delegate derived class must not to call base class constructor
                if (myty.IsDelegate) { continue; }
                Typ bsty = myty.BaseTyp;
                Fun callee = bsty.FindOvld(".ctor").GetFunOf(bsty, new Typ[] { }, myty);
                Sema instance = fun.FindVar("this");
                fun.Exes.Add(new CallFun(bsty, callee, instance, new Sema[] { }, false /*:isNewObj*/));
            }
        }

        public void AnalyzeBlockAll()
        {
            foreach (SrcAnalyzer a in Srzs)
            { a.AnalyzeBlock(); }

            //  AllFuzs may be added element while calling AnalyzeBlock method in foreach loop.
            //  So copy AllFuzs to allfuzs temporary.
            //  After foreach loop, add temporary to AllFuzs again.
            List<FunAnalyzer> allfuzs = AllFuzs;
            AllFuzs = new List<FunAnalyzer>();

            foreach (FunAnalyzer fuz in allfuzs)
            {
                if (fuz.IsClosure) { continue; }
                foreach (BlockAnalyzer blz in fuz.Blzs)
                { blz.AnalyzeBlock(); }
            }

            AllFuzs.AddRange(allfuzs);
        }

        public void AnalyzeCustomAll()
        {
            foreach (CustomAnalyzer a in AllCuzs)
            { a.AnalyzeCustom(); }
        }

        public void AnalyzeAppExes()
        {
            //  Ap.Exes holds semantics to be opecode in global.
            if (Ap.Exes.Count == 0) { return; }

            //  Thanks of IL spec, we cannot write opecode in global.
            //  So we create the module class constructor and write opecode in it,
            //  instead of writing opecode in global. 
            Token t = CreateFunToken("scons", /* name */ null, /* returnType */ null);
            FunAnalyzer fuz = NewFuz(t);
            fuz.AnalyzeFun();
            Fun cctor = fuz.Fu;
            cctor.Exes.AddRange(Ap.Exes);
            Ap.Exes.Clear();
        }

        public void EnsureEntryPoint()
        {
            List<Fun> founds = Ap.AllFuns.FindAll(delegate(Fun f) { return f.IsEntryPoint; });
            if (founds.Count > 1)
            { throw new SyntaxError("Specify one entry point. There were two entry points or more."); }
            if (founds.Count == 1)
            { return; }

            Token t = CreateFunToken("sfun", Fun.EntryPointNameImplicit, "void");
            NewFuz(t).AnalyzeFun();
        }

        public void EnsureDelegateClassAll()
        {
            List<Typ> tys = new List<Typ>();
            foreach (TypAnalyzer tyz in AllTyzs)
            {
                Typ ty = tyz.Ty;
                if (false == ty.IsDelegate) { continue; }
                tys.Add(ty);
            }

            List<string> targs = new List<string>(
                ".ctor,Invoke,BeginInvoke,EndInvoke"
                .Split(new char[] { ',' }));

            foreach (Typ ty in tys)
            {
                foreach (Ovld ov in ty.Ovlds)
                {
                    foreach (Fun f in ov.Funs)
                    {
                        if (false == targs.Contains(f.Name)) { continue; }
                        f.MthdAttrs |= MethodAttributes.HideBySig
                            | MethodAttributes.NewSlot;
                        f.ImplAttrs |= MethodImplAttributes.Runtime;
                    }
                }
            }
        }

        public void EnsureFunctionReturnAll()
        {
            foreach (Fun f in Ap.AllFuns)
            {
                if (MethodImplAttributes.Runtime == (f.ImplAttrs & MethodImplAttributes.Runtime))
                { continue; }
                FunAnalyzer.EnusureReturn(f);
            }
        }

        override public object FindUp(string name)
        {
            return Find(name) ?? (AboveBlock != null ? AboveBlock.FindUp(name) : null);
        }

    }

    public class EnvAnalyzer : AppAnalyzer
    {
        public EnvAnalyzer(Token seed)
            : base(seed, /*above*/ BlockAnalyzer.EmptyBlz)
        {
            Ez = this;
        }

        public static Env Run(Token root)
        {
            EnvAnalyzer ez = new EnvAnalyzer(root);
            ez.AnalyzeEnv();
            return ez.E;
        }

        public void AnalyzeEnv()
        {
            PrepareEnv();
            AnalyzeCompileOptions();
            ConstructSub();
            Apz.AnalyzeAppAll();
            RemoveReferencingType(E);
        }

        public void PrepareEnv()
        {
            Bl = Fu = Ty = Ap = E = new Env();
        }

        public void AnalyzeCompileOptions()
        {
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
        }

        public override void ConstructSub()
        {
            Apz = new AppAnalyzer(Seed.Find("@Syntax"), this);
            Apz.ConstructSub();
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

        public static void RemoveReferencingType(Env env)
        {
            env.Members.RemoveAll(delegate(Nmd n)
            {
                return n is Typ && (n as Typ).IsReferencing == true;
            });

        }

    }

}