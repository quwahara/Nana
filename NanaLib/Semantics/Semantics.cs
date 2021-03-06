/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using Nana.Tokens;
using Nana.IMRs;
using Nana.Infr;
using System.Diagnostics;
using System.Reflection;
using Nana.Delegates;
using System.IO;

namespace Nana.Semantics
{
    public class Attr
    {
        public bool IsStatic = false;
        public bool CanExec_ = false;
        public bool CanExec { get { return CanExec_ || CanGet || CanSet; } }
        public bool CanGet { get { return CanDo(TypGet); } }
        public bool CanSet { get { return CanDo(TypSet); } }
        public bool CanDo(Typ t)
        {
            return t != null
                && (t.IsReferencing == false
                || t.RefType != typeof(void)
                );
        }
        public Typ TypGet = null;
        public Typ TypSet = null;
        public Typ TakeReturnTyp = null;
        public bool TakeReturns
        {
            get
            {
                Typ t = TakeReturnTyp;
                if (null == t) { return false; }
                if (t.IsReferencing && t.RefType == typeof(void)) { return false; }
                return true;
            }
        }
    }

    public class Sema
    {
        public Attr Att = new Attr();
        public virtual void Prepare(IMRGenerator gen) { }
        public virtual void Exec(IMRGenerator gen) { }
        public virtual void Give(IMRGenerator gen) { }
        public virtual void Take(IMRGenerator gen) { }
        public virtual void Addr(IMRGenerator gen) { }
    }

    public class Member
    {
        public Typ Ty;
        public Nmd Value;
        public Sema Instance;

        public Member(Typ ty, Nmd value, Sema instance)
        {
            Ty = ty;
            Value = value;
            Instance = instance;
        }
    }

    public class Chain : LinkedList<object>
    {
        public Chain(object a)
        {
            Add(a);
        }

        public Chain(object a, object b)
        {
            Add(a);
            Add(b);
        }

        public Chain Add(object v)
        {
            if (v is Chain)
            {
                foreach (object o in v as Chain)
                { AddLast(o); }
            }
            else
            {
                AddLast(v);
            }
            return this;
        }

    }

    public class Nmd : Sema
    {
        public string Name;

        public Nmd() { }
        public Nmd(string name) { Name = name; }

        public override string ToString() { return Name + ":" + typeof(Nmd).Name; }
    }

    public class Blk : Nmd
    {
        public Env E;
        public bool IsReferencing = false;

        public List<Nmd> Members_ = new List<Nmd>();
        public LinkedList<Action> EnsureMembersList = new LinkedList<Action>();
        public LinkedList<Custom> Customs;
        public List<Variable> Vars = new List<Variable>();

        public Blk(string name, Env env)
            : base(name)
        {
            E = env;
        }

        public void DoEnsureMembers()
        {
            if (EnsureMembersList.Count == 0) { return; }
            LinkedList<Action> tmp = EnsureMembersList;
            EnsureMembersList = new LinkedList<Action>();
            foreach (Action a in tmp) { a(); }
        }

        public List<Nmd> Members
        {
            get
            {
                DoEnsureMembers();
                return Members_;
            }
        }

        public bool HasMember(string name)
        { return Members.Exists(GetNamePredicate<Nmd>(name)); }

        public T BeAMember<T>(T member) where T : Nmd
        {
            if (member.Name == null) { return default(T); }
            Members.Add(member);
            return member;
        }

        virtual public Variable NewVar(string name, Typ typ)
        {
            Variable v = new Variable(name, typ, Variable.VariableKind.Local);
            Vars.Add(v);
            return BeAMember(v);
        }

        virtual public Nmd Find(string name)
        {
            return Members.Find(GetNamePredicate<Nmd>(name));
        }

        public override string ToString()
        {
            return Bty.New().Add("{").Nv("Name", Name).Add("}").Add(":").Add(GetType().Name).ToS();
        }

        static public Predicate<T> GetNamePredicate<T>(string name) where T : Nmd
        { return delegate(T v) { return v.Name == name; }; }

    }

    public class Env : App
    {
        public BuiltInTyp BTY;
        public BuiltInFun BFN;
        public App Ap;
        public TypeLoader TypeLdr = new TypeLoader();
        public int Sequence = 0;
        public string GetTempName() { ++Sequence; return "$" + Sequence.ToString("D6"); }
        public string OutPath = "";

        public List<Typ> RefTyps = new List<Typ>();
        public List<Typ> ArrayTyps = new List<Typ>();
        public List<Typ> GenericTypInstances = new List<Typ>();

        public Env()
            : base(/*name*/ "", /*env*/ null)
        {
            E = this;
            BTY = new BuiltInTyp(this);
            BFN = new BuiltInFun(this);
        }

        public App NewApp(string name)
        {
            Ap = new App(name, this);
            return BeAMember(Ap);
        }

        public Typ NewRefTyp(Type refType)
        {
            Typ t = new Typ(refType, this);
            RefTyps.Add(t);
            BeAMember(t);
            t.EnsureMembersList.AddLast(t.EnsureMembers);
            if (refType.BaseType != null)
            {
                Typ bt = FindOrNewRefType(refType.BaseType);
                t.SetBaseTyp(bt);
            }
            return t;
        }

        public Typ FindRefTyp(Type refType)
        {
            DoEnsureMembers();
            return RefTyps.Find(GetNamePredicate<Typ>(refType.FullName ?? refType.Name));
        }

        public Typ FindOrNewRefType(Type refType)
        {
            if (refType.IsArray)
            {
                Typ ety = FindOrNewRefType(refType.GetElementType());
                return FindOrNewArrayTyp(ety, refType.GetArrayRank());
            }
            return FindRefTyp(refType) ?? NewRefTyp(refType);
        }

        public Typ NewArrayTyp(Typ typ, int dimension)
        {
            Typ t = new Typ(typ, this, dimension);
            ArrayTyps.Add(t);
            BeAMember(t);
            return t;
        }

        public Typ FindArrayTyp(Typ typ, int dimension)
        {
            DoEnsureMembers();
            return ArrayTyps.Find(delegate(Typ t) { return t.ArrayType == typ && t.Dimension == dimension; });
        }

        public Typ FindOrNewArrayTyp(Typ typ, int dimension)
        {
            return FindArrayTyp(typ, dimension) ?? NewArrayTyp(typ, dimension);
        }

        public Typ NewGenericTypInstance(Typ typ, Typ[] genericTypeParams)
        {
            Typ t = BeAMember(new Typ(typ, this, genericTypeParams));
            GenericTypInstances.Add(t);
            return t;
        }

        public Typ FindGenericTypInstance(Typ typ, Typ[] genericTypeParams)
        {
            DoEnsureMembers();
            return GenericTypInstances.Find(delegate(Typ t)
                {
                    bool b = t.GenericType == typ
                        && Cty.EqualForAll(t.GenericTypeParams, genericTypeParams)
                        ;
                    return b;
                });
        }

        public Typ FindOrNewGenericTypInstance(Typ typ, Typ[] genericTypeParams)
        {
            return FindGenericTypInstance(typ, genericTypeParams)
                ?? NewGenericTypInstance(typ, genericTypeParams);
        }

        public override Blk NewNsp(string ns)
        {
            DoEnsureMembers();
            Blk nsp = new Blk(ns, this);
            nsp.IsReferencing = true;
            return BeAMember(nsp);
        }

        public Blk FindNsp(string ns)
        {
            DoEnsureMembers();
            return Members_.Find(GetNamePredicate<Nmd>(ns)) as Blk;
        }

        public Blk FindOrNewNsp(string ns)
        {
            return FindNsp(ns) ?? NewNsp(ns);
        }

    }

    public class BuiltInTyp
    {
        public Typ Object;
        public Typ ValueType;
        public Typ Void;
        public Typ Bool;
        public Typ Int;
        public Typ Long;
        public Typ Sbyte;
        public Typ Byte;
        public Typ Short;
        public Typ Ushort;
        public Typ Uint;
        public Typ Ulong;
        public Typ Float;
        public Typ Double;
        public Typ Array;
        public Typ String;
        public Typ Delegate;
        public Typ IntPtr;

        public BuiltInTyp(Env e)
        {
            this.Object = e.FindOrNewRefType(typeof(object));
            this.ValueType = e.FindOrNewRefType(typeof(System.ValueType));
            this.Void = e.FindOrNewRefType(typeof(void));
            this.Bool = e.FindOrNewRefType(typeof(bool));
            this.Int = e.FindOrNewRefType(typeof(int));
            this.Long = e.FindOrNewRefType(typeof(long));
            this.Sbyte = e.FindOrNewRefType(typeof(sbyte));
            this.Byte = e.FindOrNewRefType(typeof(byte));
            this.Short = e.FindOrNewRefType(typeof(short));
            this.Ushort = e.FindOrNewRefType(typeof(ushort));
            this.Uint = e.FindOrNewRefType(typeof(uint));
            this.Ulong = e.FindOrNewRefType(typeof(ulong));
            this.Float = e.FindOrNewRefType(typeof(float));
            this.Double = e.FindOrNewRefType(typeof(double));
            this.Array = e.FindOrNewRefType(typeof(System.Array));
            this.String = e.FindOrNewRefType(typeof(string));
            this.Delegate = e.FindOrNewRefType(typeof(System.Delegate));
            this.IntPtr = e.FindOrNewRefType(typeof(System.IntPtr));
        }

        public static Type[] UnsignedIntegerTypes           /**/ = new Type[] { typeof(byte), typeof(ushort), typeof(uint), typeof(ulong) };
        public static Type[] SignedIntegerTypes             /**/ = new Type[] { typeof(sbyte), typeof(short), typeof(int), typeof(long) };

        public static bool IsUnsignedIntegerType(Type a)    /**/ { return 0 <= System.Array.IndexOf(UnsignedIntegerTypes, a); }
        public static bool IsSignedIntegerType(Type a)      /**/ { return 0 <= System.Array.IndexOf(SignedIntegerTypes, a); }
        public static bool IsIntegerType(Type a)            /**/ { return IsSignedIntegerType(a) || IsUnsignedIntegerType(a); }

        public static bool IsRealType(Type a)               /**/ { return typeof(float) == a || typeof(double) == a; }

        public static Type[] NumericTypes = new Type[]
        {
            typeof(sbyte)
            , typeof(byte)
            , typeof(short)
            , typeof(ushort)
            , typeof(int)
            , typeof(uint)
            , typeof(long)
            , typeof(ulong)
            , typeof(float)
            , typeof(double)
            };

        public static bool IsNumericType(Type a) { return 0 <= System.Array.IndexOf(NumericTypes, a); }
        public static bool IsNumericType(Typ a) { return a.IsReferencing && 0 <= System.Array.IndexOf(NumericTypes, a.RefType); }

    }

    public class BuiltInFun
    {
        public Env E;

        public Dictionary<string, Member> Members = new Dictionary<string, Member>();
        public Dictionary<string, Ovld> Ovrlds = new Dictionary<string, Ovld>();

        public BuiltInFun(Env e)
        {
            E = e;
            Typ bol = e.BTY.Bool;
            Typ[] numtyps = new Typ[] {
                e.BTY.Short
                , e.BTY.Ushort
                , e.BTY.Int
                , e.BTY.Uint
                , e.BTY.Long
                , e.BTY.Ulong
                , e.BTY.Float
                , e.BTY.Double
            };
            foreach (Typ numtyp in numtyps)
            {
                foreach (string sig in "+,-,*,/,%".Split(new char[] { ',' }))
                {
                    string fn = SignToFuncationName(sig);
                    NewOpeFunByOpe(fn, numtyp, numtyp, numtyp);
                }

                NewOpeFunByOpe("op_UnaryNegation", numtyp, null /* unuse to be unary */, numtyp);

                foreach (string sig in "==,!=,<,>,<=,>=,and,or,xor".Split(new char[] { ',' }))
                {
                    string fn = SignToFuncationName(sig);
                    NewOpeFunByOpe(fn, numtyp, numtyp, bol);
                }
            }

            foreach (string sig in "==,!=,and,or,xor".Split(new char[] { ',' }))
            {
                string fn = SignToFuncationName(sig);
                NewOpeFunByOpe(fn, bol, bol, bol);
            }

            {
                Typ str = e.BTY.String;
                string fn = SignToFuncationName("+");
                str.NewOpeFunByFun(fn, "Concat", new Typ[] { str, str });
            }

            RegisterOvldAlias(E.FindOrNewRefType(typeof(Console)), "`p", "WriteLine");
        }

        public void NewOpeFunByOpe (string funnm, Typ left, Typ right, Typ ret)
		{
            if (null != left.FindOvld (funnm))
            { return; }
            List<Variable> vs = new List<Variable>();
            vs.Add(new Variable("a", left, Variable.VariableKind.Param));
            if (null != right)
            { vs.Add(new Variable("b", right, Variable.VariableKind.Param)); }
            Fun f = left.NewOvldAndFun(funnm, vs, ret);
            f.IsOperator = true;
            f.MthdAttrs = MethodAttributes.Public | MethodAttributes.Static;
        }

        public void RegisterOvldAlias(Typ calleetyp, string name, string actualname)
        {
            Ovld actual = calleetyp.FindMemeber(actualname) as Ovld;
            AddToMembers(calleetyp, name, actual);
        }

        public void AddToMembers(Typ calletyp, string name, Ovld o)
        {
            if (Members.ContainsKey(name))
            { return; }
            Members.Add(name, new Member(calletyp, o, /*instance=*/ null));
        }

        public static string SignToFuncationName(string sig)
        {
            switch (sig)
            {
                case "+":   /**/ return "op_Addition";
                case "--":  /**/ return "op_Decrement";
                case "/":   /**/ return "op_Division";
                case "==":  /**/ return "op_Equality";
                case ">":   /**/ return "op_GreaterThan";
                case ">=":  /**/ return "op_GreaterThanOrEqual";
                case "++":  /**/ return "op_Increment";
                case "!=":  /**/ return "op_Inequality";
                case "<":   /**/ return "op_LessThan";
                case "<=":  /**/ return "op_LessThanOrEqual";
                case "%":   /**/ return "op_Modulus";
                case "*":   /**/ return "op_Multiply";
                case "-":   /**/ return "op_Subtraction";
                //case "":    /**/ return "op_Explicit";
                //case "":    /**/ return "op_Implicit";
                //case "":    /**/ return "op_UnaryNegation";
                //case "":    /**/ return "op_UnaryPlus";

                /* Extension */
                case "and":     /**/ return "op_And";
                case "or":      /**/ return "op_Or";
                case "xor":     /**/ return "op_Xor";

                default:
                    throw new InternalError(string.Format("The sign is not an operator:'{0}'", new object[] { sig }));
            }

            //  reference
            //  http://msdn.microsoft.com/en-us/library/system.decimal.op_unaryplus(v=vs.80)
        }

    }

    public class Ovld : Blk
    {
        public List<Fun> Funs = new List<Fun>();

        public Ovld(string name, Env env)
            : base(name, env)
        {
        }

        public Fun NewFun(string name, List<Variable> params_, Typ returnTyp)
        {
            Fun f = new Fun(name, E);
            f.SetParams(params_);
            f.SetReturnTyp(returnTyp);
            Funs.Add(f);
            return BeAMember(f);
        }

        public Fun NewFun(MethodBase mb)
        {
            Fun f = new Fun(mb, E);
            f.MthdAttrs = mb.Attributes;
            Funs.Add(f);
            return BeAMember(f);
        }

        public void NewOpeFunByFun(Typ calleetyp, Fun actual)
        {
            Fun newfun = NewFun(actual.Name, actual.Params, actual.ReturnTyp);
            newfun.MthdAttrs = actual.MthdAttrs;
            newfun.IsOperatorLikeFun = true;
            newfun.CalleeTypOfOperatorLikeFun = calleetyp;
            Members.Add(newfun);
            Funs.Add(newfun);
        }

        public Fun GetFunOf(Typ calleetyp, Typ[] argtyps, Typ callertyp)
        {
            List<Tuple2<Typ, Fun>> cand = CreateCandidateFunList(calleetyp, argtyps);

            Predicate<Tuple2<Typ, Fun>> canAccess
                = delegate(Tuple2<Typ, Fun> tf)
                {
                    Accessibility acc = AccessibilityControl.FromMethodAttributes(tf.F2.MthdAttrs);
                    return AccessibilityControl.CanAccess(acc, tf.F1, callertyp);
                };

            List<Tuple2<Typ, Fun>> sel = new List<Tuple2<Typ, Fun>>();
            Tuple2<Typ, Fun> callee;
            foreach (Tuple2<Typ, Fun> tf in cand)
            {
                Fun c = tf.F2;
                if (c.IsSameSignature(argtyps) == false) { continue; }
                sel.Add(tf);
            }
            if (sel.Count > 1)
            { sel = sel.FindAll(canAccess); }
            if (sel.Count > 1)
            { throw new SyntaxError("More than 2 candidates methods:" + Name); }
            if (sel.Count == 1)
            {
                callee = sel[0];
                if (false == canAccess(callee))
                { throw new SyntaxError("Can not access the function: " + Name); }
                return callee.F2;
            }

            sel.Clear();
            foreach (Tuple2<Typ, Fun> tf in cand)
            {
                Fun c = tf.F2;
                if (c.IsAssignableSignature(argtyps) == false) { continue; }
                sel.Add(tf);
            }
            if (sel.Count == 0)
            { throw new SyntaxError("No candidate method for:" + Name); }
            if (sel.Count > 1)
            { sel = sel.FindAll(canAccess); }
            if (sel.Count > 1)
            { throw new SyntaxError("More than 2 candidates methods:" + Name); }
            callee = sel[0];
            if (false == canAccess(callee))
            {
                throw new SyntaxError("Can not access the function: " + Name);
            }
            return callee.F2;
        }

        public List<Tuple2<Typ, Fun>> CreateCandidateFunList(Typ ty, Typ[] argtyps)
        {
            List<Tuple2<Typ, Fun>> candidates = new List<Tuple2<Typ, Fun>>();

            //  Typ collection: inheritance hierarchy into list
            List<Typ> typs = Cty.CollectUntilReturnNull<Typ>(delegate(Typ y) { return y.BaseTyp; }, ty);

            //  Ovld collection: go up hierarchy and find same name ActnOvld
            List<Tuple2<Typ, Ovld>> ovlds
                = typs.FindAll(delegate(Typ y) { return null != y.FindOvld(this.Name); })
                .ConvertAll<Tuple2<Typ, Ovld>>(delegate(Typ y) { return new Tuple2<Typ, Ovld>(y, y.FindOvld(this.Name)); });

            //  Fun collection: collect Fun in ovlds
            List<Tuple2<Typ, Fun>> srclst
                = (new List<Fun>(this.Funs))
                .ConvertAll<Tuple2<Typ, Fun>>(delegate(Fun f) { return new Tuple2<Typ, Fun>(ty, f); }); ;

            foreach (Tuple2<Typ, Ovld> ovld in ovlds)
            {
                srclst.AddRange(
                    new List<Fun>(ovld.F2.Funs)
                    .ConvertAll<Tuple2<Typ, Fun>>(delegate(Fun f) { return new Tuple2<Typ, Fun>(ovld.F1, f); })
                    );
            }

            //  collect Actn that has same signature or assignalbe signature but not in candidate list
            foreach (Tuple2<Typ, Fun> tf in srclst)
            {
                Fun f = tf.F2;
                if (f.Params.Count != argtyps.Length) { continue; }
                if (f.IsAssignableSignature(argtyps) == false) { continue; }
                if (candidates.Exists(delegate(Tuple2<Typ, Fun> a_) { return a_.F2.IsSameSignature(f.Signature); })) { continue; }
                candidates.Add(tf);
            }

            return candidates;
        }

        public bool Contains(Typ[] signature)
        {
            Fun a;
            for (int i = 0; i < Members.Count; ++i)
            {
                if (null == (a = (Members[i] as Fun)))
                { continue; }
                if (a.IsSameSignature(signature))
                { return true; }
            }
            return false;
        }

    }

    public class OvldAccessInfo
    {
        public Typ Holder;
        public Sema Instance;
        public Ovld Value;
        public OvldAccessInfo(Typ holder, Sema instance, Ovld value)
        {
            Holder = holder;
            Instance = instance;
            Value = value;
        }
    }

    public class Fun : Blk
    {
        static public readonly string EntryPointNameDefault = "Main";
        static public readonly string EntryPointNameImplicit = "'0'";

        public string SpecialName = "";
        public MethodAttributes MthdAttrs;
        public MethodImplAttributes ImplAttrs = MethodImplAttributes.IL | MethodImplAttributes.Managed;
        public List<Variable> Params = new List<Variable>();
        public Typ ReturnTyp;
        public ReturnPoint RetPoint;

        public Typ[] Signature
        {
            get
            {
                return Params.ConvertAll<Typ>(delegate(Variable v) { return v.Att.TypGet; }).ToArray();
            }
        }

        public bool IsConstructor { get { return Nana.IMRs.IMRGenerator.IsAnyCons(Name); } }
        public bool IsInstanceConstructor { get { return IsInstance && IsConstructor; } }
        public bool IsEntryPoint { get { return Name == EntryPointNameDefault || Name == EntryPointNameImplicit; } }
        public bool IsInherited = false;
        public bool IsStatic { get { return (MthdAttrs & MethodAttributes.Static) == MethodAttributes.Static; } }
        public bool IsInstance { get { return (MthdAttrs & MethodAttributes.Static) != MethodAttributes.Static; } }
        public bool IsVirtual { get { return (MthdAttrs & MethodAttributes.Virtual) == MethodAttributes.Virtual; } }
        public bool Inherited = false;
        public bool IsOperator = false;
        public bool IsOperatorLikeFun = false;
        public bool IsAssign = false;
        public Typ CalleeTypOfOperatorLikeFun;
        public bool IsReturnValue { get { return E.BTY.Void != ReturnTyp; } }

        public bool IsAssignableSignature(Typ[] callertyps)
        {
            if (callertyps == null) { return false; }
            Typ[] sign =  Signature;
            if (sign.Length != callertyps.Length) { return false; }
            for (int i = 0; i < sign.Length; ++i)
            { if (sign[i].IsAssignableFrom(callertyps[i]) == false) { return false; } }
            return true;
        }

        public bool IsSameSignature(Typ[] signature)
        {
            return Cty.EqualForAll(Signature, signature);
        }

        public List<Sema> Exes = new List<Sema>();

        public List<IMR> IMRs = new List<IMR>();

        public Fun(string name, Env env)
            : base(name, env)
        {
            if (null != E && null != E.BTY) { Att.TypGet = E.BTY.IntPtr; }
        }

        public void SetReturnTyp(Typ returnTyp)
        {
            ReturnTyp = returnTyp;
        }

        public void SetParams(List<Variable> params_)
        {
            params_.ForEach(delegate(Variable v)
            {
                Params.Add(v);
                BeAMember(v);
            });
        }

        public MethodBase Mb;

        public Fun(MethodBase mb, Env env)
            : base(mb.Name, env)
        {
            Mb = mb;
            new List<ParameterInfo>(mb.GetParameters()).ForEach(delegate(ParameterInfo p)
                    { NewParam(p.Name, E.FindOrNewRefType(p.ParameterType)); });
            IsReferencing = true;
            MthdAttrs = mb.Attributes;

            Type tt = null;

            if (mb is MethodInfo)
            {
                tt = (mb as MethodInfo).ReturnType;
            }
            if (mb is ConstructorInfo)
            {
                if (mb.IsStatic)        /**/ { tt = typeof(void); }
                else                    /**/ { tt = (mb as ConstructorInfo).DeclaringType; }
            }
            ReturnTyp = env.FindOrNewRefType(tt);
            Att.TypGet = E.BTY.IntPtr;
        }

        public Variable NewThis(Typ typ)
        {
            if (typ == null) { throw new NotImplementedException("Cannot define this in here"); }
            Variable v = new Variable("this", typ, Variable.VariableKind.This);
            Vars.Add(v);
            return BeAMember(v);
        }

        public Variable NewParam(string name, Typ typ)
        {
            Variable v = new Variable(name, typ, Variable.VariableKind.Param);
            Params.Add(v);
            return BeAMember(v);
        }

        public Variable FindVar(string name)
        {
            DoEnsureMembers();
            return Vars.Find(GetNamePredicate<Variable>(name));
        }

        static public string GenSignature(MethodBase mb)
        {
            StringBuilder b = new StringBuilder();
            b.Append(mb.Name);
            if (mb.IsGenericMethod)
            {
                b.Append(" [")
                    .Append(string.Join(" "
                    , new List<Type>(mb.GetGenericArguments())
                        .ConvertAll<string>(delegate(Type t)
                        { return GetNameForSignature(t); })
                        .ToArray()
                        )
                    )
                    .Append("]");
            }

            new List<ParameterInfo>(mb.GetParameters())
                .ConvertAll<StringBuilder>(delegate(ParameterInfo p)
                { return b.Append(" ").Append(GetNameForSignature(p.ParameterType)); });
            return b.ToString();
        }

        static public string GetNameForSignature(Type t)
        {
            if (t.IsGenericParameter) { return t.Name; }
            
            string name = (string.IsNullOrEmpty(t.Namespace) ? "" : t.Namespace + ".")
                    + t.Name;
            bool gtd = t.IsGenericTypeDefinition;
            if (t.IsGenericType && gtd) { return name; }
            if (t.IsGenericType && gtd == false)
            { return name + "[" + GetNameForSignature(t.GetGenericArguments()) + "]"; }
            return name;
        }

        static public string GetNameForSignature(Type[] t)
        {
            if (t == null) { return ""; }
            return string.Join(" "
                , new List<Type>(t)
                .ConvertAll<string>(delegate(Type item) { return GetNameForSignature(item); })
                .ToArray());
        }
    }

    public class Typ : Fun
    {
        public Typ BaseTyp;
        public TypeAttributes TypAttributes;
        public string AssemblyName;
        public string _FullName;
        public bool IsValueType;

        public bool IsVector = false;
        public bool IsArray = false;
        public Typ ArrayType;
        public int Dimension;
        public bool IsVectorOrArray;

        public bool IsGeneric = false;
        public bool IsGenericInstance = false;

        public List<Ovld> Ovlds = new List<Ovld>();
        public List<Prop> Props = new List<Prop>();
        public List<Evnt> Evnts = new List<Evnt>();

        public List<Nmd> DebuggerDisplayMembers { get { return Members_; } }

        public Typ(string name, Env env)
            : base(name, env)
        {
            _FullName = Name;
            TypAttributes
                = TypeAttributes.Class
                | TypeAttributes.Public
                | TypeAttributes.AutoLayout
                | TypeAttributes.AnsiClass
                ;
            IsValueType = false;
        }

        public bool IsDelegate = false;

        public void SetBaseTyp(Typ baseTyp)
        {
            BaseTyp = baseTyp;
            if (E.BTY == null) { return; }
            IsDelegate = E.BTY.Delegate.IsAssignableFrom(baseTyp);
            if (IsDelegate 
                && (false == (TypeAttributes.Sealed == (TypAttributes & TypeAttributes.Sealed))))
            { TypAttributes |= TypeAttributes.Sealed; }
        }

        public List<Variable> Flds = new List<Variable>();

        override public Variable NewVar(string name, Typ typ)
        {
            Variable v = new Variable(name, typ, Variable.VariableKind.Field);
            Flds.Add(v);
            return BeAMember(v);
        }

        public Ovld FindOrNewOvld(string name)
        {
            return FindOvld(name) ?? NewOvld(name);
        }

        public Ovld FindOvld(string name)
        {
            DoEnsureMembers();
            return Ovlds.Find(GetNamePredicate<Ovld>(name));
        }

        public Ovld NewOvld(string name)
        {
            if (Members_.Exists(GetNamePredicate<Nmd>(name)))
            { throw new SemanticError("The name is already defined: " + name); }
            Ovld ovl = new Ovld(name, E);
            Ovlds.Add(ovl);
            BeAMember(ovl);
            return ovl;
        }

        public Fun NewOvldAndFun(string name, List<Variable> params_, Typ returnTyp)
        {
            return NewOvld(name).NewFun(name, params_, returnTyp);
        }

        public Type RefType = null;

        public Typ(Type refType, Env env)
            : base(refType.FullName ?? refType.Name, env)
        {
            RefType = refType;
            IsValueType = refType.IsValueType;
            IsReferencing = true;
            _FullName = refType.FullName;
            AssemblyName = refType.Assembly.GetName().Name;
        }

        public GenericArgument[] GetGenericArguments()
        {
            if (IsReferencing)
            {
                return GetGenericArgumentsOfReferencing();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public GenericArgument[] GetGenericArgumentsOfReferencing()
        {
            List<GenericArgument> ls = new List<GenericArgument>();
            foreach (Type t in RefType.GetGenericArguments())
            {
                ls.Add(new GenericArgument(t));
            }
            return ls.ToArray();
        }

        public Typ(Typ typ, Env env, int dimension)
            : base(typ._FullName + "[" + dimension + "]", env)
        {
            Dimension = dimension;
            IsVector = dimension == 1;
            IsArray = dimension > 1;
            IsVectorOrArray = IsVector || IsArray;
            ArrayType = typ;
            AssemblyName = typ.AssemblyName;
            SetBaseTyp(env.BTY.Array);
        }

        public Typ GenericType = null;
        public Typ[] GenericTypeParams = null;
        public Dictionary<string, Typ> GenericDic = null;

        public Typ(Typ genericTyp, Env env, Typ[] genericTypeParams)
            : base(genericTyp.Name, env)
        {
            GenericType = genericTyp;
            GenericTypeParams = genericTypeParams;

            IsGeneric = true;
            IsGenericInstance = true;
            AssemblyName = genericTyp.AssemblyName;
            _FullName = genericTyp._FullName;
            IsValueType = genericTyp.IsValueType;
            RefType = genericTyp.RefType;
            IsReferencing = true;

            Name = genericTyp._FullName + "<" + string.Join(","
            , new List<Typ>(genericTypeParams).ConvertAll<string>(
            delegate(Typ t_) { return t_._FullName; })
            .ToArray()
            ) + ">";

            //  map generic arguments to actual types
            GenericArgument[] gargs = genericTyp.GetGenericArguments();
            Debug.Assert(gargs.Length == genericTypeParams.Length);
            GenericDic = new Dictionary<string, Typ>();
            for (int i = 0; i < gargs.Length; ++i)
            {
                GenericDic[gargs[i].Name] = genericTypeParams[i];
            }

            EnsureMembersList.AddLast(EnsureGenericMembers);
        }

        public Prop FindProp(string name)
        {
            DoEnsureMembers();
            return Props.Find(GetNamePredicate<Prop>(name));
        }

        public Prop NewProp(PropertyInfo p)
        {
            Prop prp = new Prop(p, E);
            Props.Add(prp);
            BeAMember<Prop>(new Prop(p, E));
            return prp;
        }

        public Evnt NewEvnt(EventInfo ei)
        {
            Evnt ev = new Evnt(this, ei, E);
            Evnts.Add(ev);
            BeAMember<Evnt>(ev);
            return ev;
        }

        public void NewOpeFunByFun(string ope, string funname, Typ[] argtyps)
        {
            Ovld actualovld = FindOvld(funname);
            if (null == actualovld)
            { throw new InternalError(string.Format("No Fun in this overload. Fun:{0}", new object[] { funname })); }
            Fun actual = FindOvld(funname).GetFunOf(this, argtyps, E.BTY.Void);
            if (null == actual)
            { throw new InternalError(string.Format("No Fun in this overload. Fun:{0}", new object[] { funname })); }
            Ovld o = FindOrNewOvld(ope);
            o.NewOpeFunByFun(this, actual);
        }

        public bool IsAssignableFrom(Typ y)
        {
            Typ bty = y;
            while (bty != null)
            {
                if (bty == this)
                { return true; }
                bty = bty.BaseTyp;
            }
            return false;
        }

        public void EnsureMembers()
        {
            if (RefType == null) { return; }
            Type ty = RefType;
            BindingFlags flags_ = BindingFlags.Instance
                                | BindingFlags.Static
                                | BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.FlattenHierarchy
                                ;
            List<MethodBase> ms = new List<MethodBase>();

            ms.AddRange(ty.GetConstructors(flags_));
            ms.AddRange(ty.GetMethods(flags_));
            foreach (MethodBase m in ms)
            { FindOrNewOvld(m.Name).NewFun(m); }

            new List<PropertyInfo>(ty.GetProperties(flags_))
                .ConvertAll<Ovld>(NewProp);

            foreach (EventInfo ei in ty.GetEvents(flags_))
            { NewEvnt(ei); }
        }

        public void EnsureGenericMembers()
        {
            Debug.Assert(this != null && IsGeneric && IsGenericInstance && GenericType != null);

            Typ gt = GenericType;
            gt.DoEnsureMembers();

            List<Variable> nvs = new List<Variable>();
            Action<Ovld, Fun> newfun = delegate(Ovld newovld_, Fun gfun_)
            {
                Variable nv; Typ nvty; int genericIndex;
                nvs.Clear();
                foreach (Variable gv in gfun_.Params)
                {
                    if (gv.VarKind != Variable.VariableKind.Param)
                    { continue; }
                    nvty = TransGenericType(this, gv.Att.TypGet);
                    nv = new Variable(gv.Name, nvty, Variable.VariableKind.Param);
                    genericIndex = GenericDic.ContainsKey(gv.Att.TypGet.Name)
                        ? Array.IndexOf(GenericTypeParams, GenericDic[gv.Att.TypGet.Name])
                        : -1;
                    nv.GenericIndex = genericIndex;
                    if (genericIndex >= 0)
                    { nv.VarKind = Variable.VariableKind.ParamGeneric; }
                    nvs.Add(nv);
                }

                {
                    Typ rettyp = TransGenericType(this, gfun_.ReturnTyp);
                    Fun f = newovld_.NewFun(gfun_.Name, nvs, rettyp);
                    f.MthdAttrs = gfun_.MthdAttrs;
                }
            };

            foreach (Ovld govld in gt.Ovlds)
            {
                Ovld newovld = FindOrNewOvld(govld.Name);
                foreach (Fun gfun in govld.Funs)
                { newfun(newovld, gfun); }
            }

            foreach (Prop p in gt.Props)
            {
                BeAMember<Prop>(p);
            }

        }

        static public Typ TransGenericType(Typ instance, Typ generic)
        {
            Debug.Assert(instance != null && generic != null);

            if (instance.GenericType == generic)
            { return instance; }

            if (instance.GenericDic.ContainsKey(generic.Name))
            { return instance.GenericDic[generic.Name]; }

            return generic;
        }

        public bool IsReferencingOf(Type t)
        {
            return RefType != null && RefType == t;
        }

        public Nmd FindMemeber(string name)
        {
            Typ typ = this;
            if (typ.IsReferencing && typ.RefType.IsEnum)
            {
                Type rty = typ.RefType;
                if (false == Enum.IsDefined(rty, name))
                { return null; }
                object v = Enum.Parse(rty, name);
                return new Enu(name, typ);
            }

            Nmd mem = typ.Find(name);
            while (typ != null && mem == null)
            {
                if (typ.IsReferencing && typ.RefType == typeof(object))
                { break; }
                typ = typ.BaseTyp;
                mem = typ.Find(name);
            }
            return mem;
        }

        public bool IsSubclassOf(Typ t)
        {
            Typ baseType = this;
            do
            {
                if (baseType == t) { return true; }
                baseType = baseType.BaseTyp;
            } while (baseType != null);

            return false;
        }
    }

    public class App : Typ
    {
        /// <summary>Holds all Funs under the this App</summary>
        public List<Fun> AllFuns = new List<Fun>();

        public string ModuleName;

        public App(string name, Env env)
            : base(name, env)
        {
            if (E != null)
            {
                Name = Path.GetFileName(E.OutPath);
                AssemblyName = Path.GetFileNameWithoutExtension(E.OutPath);
                ModuleName = Path.GetFileName(E.OutPath);
            }
        }

        public virtual Blk NewNsp(string name)
        {
            Blk n = new Blk(name, E);
            BeAMember(n);
            return n;
        }

        public Typ NewTyp(string name)
        {
            Typ ty = new Typ(name, E);
            ty.AssemblyName = AssemblyName;
            return BeAMember(ty);
        }

        override public Variable NewVar(string name, Typ typ)
        {
            Variable v = new Variable(name, typ, Variable.VariableKind.Field);
            v.Att.IsStatic = true;
            Vars.Add(v);
            return BeAMember(v);
        }

    }

    public class Prop : Ovld
    {
        public PropertyInfo P;
        public Fun Setter;
        public Fun Getter;

        public Prop(PropertyInfo p, Env env)
            : base(p.Name, env)
        {
            P = p;
            MethodInfo m;
            m = p.GetGetMethod(/* nonPublic:*/ true);
            Getter = null;
            if (m != null)
            {
                Getter = BeAMember<Fun>(new Fun(m, env));
                Att.TypGet = env.FindOrNewRefType(p.PropertyType);
            }
            Setter = null;
            m = p.GetSetMethod(/* nonPublic:*/ true);
            if (m != null)
            {
                Setter = BeAMember<Fun>(new Fun(m, env));
                Att.TypSet = Setter.Signature[0];
            }
        }
    }

    public class Evnt : Typ
    {
        public Typ Holder;
        public EventInfo Evn;
        public Fun Add;
        public Fun Rmv;

        public Evnt(Typ holder, EventInfo evn, Env env)
            : base(evn.EventHandlerType, env)
        {
            Holder = holder;
            Evn = evn;
            Name = evn.Name;
            Att.TypGet = this;
            MethodInfo mi; Fun f;
            mi = evn.GetAddMethod(/*nonPublic*/ true);
            if (null != mi)
            {
                f = new Fun(mi, env);
                BeAMember<Fun>(f);
                Add = f;
                NewOpeFunForEH(holder, f, "+=");
            }
            mi = evn.GetRemoveMethod(/*nonPublic*/ true);
            if (null != mi)
            {
                f = new Fun(mi, env);
                BeAMember<Fun>(f);
                Rmv = f;
                NewOpeFunForEH(holder, f, "-=");
            }
        }

        private void NewOpeFunForEH(Typ holder, Fun f, string ope)
        {
            string funname = f.Name;
            Ovld actualovld = holder.FindOvld(funname);
            if (null == actualovld)
            { throw new InternalError(string.Format("No Fun in this overload. Fun:{0}", new object[] { funname })); }
            Typ[] argtyps = f.Signature;
            Fun actual = actualovld.GetFunOf(holder, argtyps, holder);
            if (null == actual)
            { throw new InternalError(string.Format("No Fun in this overload. Fun:{0}", new object[] { funname })); }

            Ovld o = FindOrNewOvld(ope);
            Fun newfun = o.NewFun(actual.Name, actual.Params, actual.ReturnTyp);
            newfun.MthdAttrs = actual.MthdAttrs;
            newfun.IsOperatorLikeFun = true;
            newfun.CalleeTypOfOperatorLikeFun = holder;
            o.Members.Add(newfun);
            o.Funs.Add(newfun);
        }
    }

    /// <summary>
    /// ACCess information
    /// </summary>
    interface IAcc
    {
        Typ OwnerTyp { get;}
        Sema OwnerIns { get;}
    }

    public class AccEvnt : Sema, IAcc
    {
        public Typ HoldingTyp;
        public Sema Instance;
        public Evnt Evn;

        public AccEvnt(Typ fieldHoldingTyp, Sema instance, Evnt evn)
        {
            HoldingTyp = fieldHoldingTyp;
            Evn = evn;
            Instance = instance;
            Att.TypGet = Att.TypSet = evn.Att.TypGet;
        }

        public Typ OwnerTyp
        {
            get { return Evn.Att.TypGet; }
        }

        public Sema OwnerIns { get { return Instance; } }
    }

    //  Return Determinacy State (It's doubtful to make sense in English.)
    //  ???????K?????^?[????????????????????????????????????????
    public interface IReturnDeterminacyState
    {
        bool RDS { get;}
    }

    public class DoNothing : Sema
    {
        public DoNothing()
        {
            Att.CanExec_ = true;
        }

        public override void Exec(IMRGenerator gen)
        {
            //  do nothing
        }
    }

    public class Box : Sema
    {
        public Sema Actual;

        public Box(Sema actual)
        {
            Actual = actual;
            Att.TypGet = actual.Att.TypGet;
        }

        public override void Give(IMRGenerator gen)
        {
            Actual.Give(gen);
            gen.Box(Att.TypGet);
        }

        public override void Take(IMRGenerator gen)
        {
            throw new InternalError("Boxing cannot take");
        }

        public override void Addr(IMRGenerator gen)
        {
            throw new InternalError("Boxing has no addr");
        }

    }

    public class Literal : Sema
    {
        public object Value;
        public TmpVarGenerator TmpVarGen;

        public Literal(object value, Typ typ)
            : this(value, typ, null)
        {
        }

        public Literal(object value, Typ typ, TmpVarGenerator tmpVarGen)
        {
            Value = value;
            TmpVarGen = tmpVarGen;
            Att.TypGet = typ;
        }

        public override void Give(IMRGenerator gen)
        {
            gen.LoadLiteral(this);
        }

        public override void Addr(IMRGenerator gen)
        {
            Give(gen);
        }

        public override void Exec(IMRGenerator gen)
        {
            Give(gen);
            gen.Pop();
        }

    }

    public class RalLiteral
    {
        public static string[] Separate(string v)
        {
            string up = v.ToUpper();

            bool hassig = up.StartsWith("-") || up.StartsWith("+");
            int dotidx = up.IndexOf('.');
            int expidx = up.IndexOf('E');
            bool hassfx = up.EndsWith("D") || up.EndsWith("F");

            string sfx = "";
            if (hassfx)
            {
                sfx = up[up.Length - 1].ToString();
                up = up.Substring(0, up.Length - 1);
            }

            string expsig = "";
            string expval = "";
            if (0 <= expidx)
            {
                string exp = up.Substring(expidx + 1);
                expsig = "";
                if (exp.StartsWith("+") || exp.StartsWith("-"))
                {
                    expsig = exp[0].ToString();
                    exp = exp.Substring(1);
                }
                expval = exp;
                up = up.Substring(0, expidx);
            }

            string frac = "";
            if (0 <= dotidx)
            {
                frac = up.Substring(dotidx + 1);
                up = up.Substring(0, dotidx);
            }

            string sig = "";
            if (hassig)
            {
                sig = up[0].ToString();
                up = up.Substring(1);
            }

            string integ = up;

            string[] r = new string[6];
            r[0] = sig;
            r[1] = integ;
            r[2] = frac;
            r[3] = expsig;
            r[4] = expval;
            r[5] = sfx;

            return r;
        }
    }

    public class IntLiteral
    {
        public static int UlongMaxValueStringLength;

        public static decimal LongMinValue;
        public static decimal IntMinValue;
        
        public static decimal IntMaxValue;
        public static decimal LongMaxValue;

        public static decimal UintMaxValue;
        public static decimal UlongMaxValue;

        public static List<Type> SignedIntegerTypes;
        public static List<Type> UnsignedIntegerTypes;
        public static List<Type> IntegerTypes;

        static IntLiteral()
        {
            LongMinValue = new decimal(long.MinValue);
            IntMinValue = new decimal(int.MinValue);

            IntMaxValue = new decimal(int.MaxValue);
            LongMaxValue = new decimal(long.MaxValue);

            UintMaxValue = new decimal(uint.MaxValue);
            UlongMaxValue = new decimal(ulong.MaxValue);

            UlongMaxValueStringLength = ulong.MaxValue.ToString().Length;

            SignedIntegerTypes = new List<Type>(new Type[] { typeof(int), typeof(long) });
            UnsignedIntegerTypes = new List<Type>(new Type[] { typeof(uint), typeof(ulong) });
            IntegerTypes = new List<Type>();
            IntegerTypes.AddRange(SignedIntegerTypes);
            IntegerTypes.AddRange(UnsignedIntegerTypes);
        }

        public static bool IsLessThanOrEqualUlongMaxValueStringLength(string val)
        {
            return val.Length <= UlongMaxValueStringLength;
        }

        public static bool IsGreaterThanThanOrEqualLongMinValue(decimal vd)
        {
            return vd >= LongMinValue;
        }

        public static bool IsLessThanOrEqualUlongMaxValue(decimal vd)
        {
            return vd <= UlongMaxValue;
        }

        public static Typ ParseToValueAndTyp(Env e, decimal vd, string sfx)
        {
            Typ ty;
            string sfxup = sfx.ToUpper();
            if ("UL" == sfxup || "LU" == sfxup) /**/ { ty = e.BTY.Ulong; }
            else if ("L" == sfxup)
            {
                if (vd <= LongMaxValue)         /**/ { ty = e.BTY.Long; }
                else                            /**/ { ty = e.BTY.Ulong; }
            }
            else if ("U" == sfxup)
            {
                if (vd <= UintMaxValue)         /**/ { ty = e.BTY.Uint; }
                else                            /**/ { ty = e.BTY.Ulong; }
            }
            else
            {
                if (vd < IntMinValue)           /**/ { ty = e.BTY.Long; }
                else if (vd <= IntMaxValue)     /**/ { ty = e.BTY.Int; }
                else if (vd <= UintMaxValue)    /**/ { ty = e.BTY.Uint; }
                else if (vd <= LongMaxValue)    /**/ { ty = e.BTY.Long; }
                else                            /**/ { ty = e.BTY.Ulong; }
            }
            return ty;
        }

        public static Tuple3<string, string, string> SeparateSignValueSuffix(string v)
        {
            string sig = "";
            if      /**/ (v.StartsWith("-")) { sig = "-"; v = v.Substring(1); }
            else if /**/ (v.StartsWith("+")) { sig = "+"; v = v.Substring(1); }

            string up = v.ToUpper();

            bool ul = up.EndsWith("UL") || up.EndsWith("LU");
            bool u = false == ul && up.EndsWith("U");
            bool l = false == ul && up.EndsWith("L");

            int sublen = ul ? 2 : (u || l ? 1 : 0);

            Tuple3<string, string, string> r = new Tuple3<string, string, string>();
            r.F1 = sig;
            r.F2 = v.Substring(0, v.Length - sublen);
            r.F3 = v.Substring(v.Length - sublen);

            return r;
        }
    }

    public class Cast : Sema
    {
        public Sema Instance;
        public Typ FromTyp;
        public Typ ToTyp;
        public bool DoThrowInvalidCast;

        public Cast(Sema instance, Typ fromTyp, Typ totyp, bool doThrowInvalidCast)
        {
            Instance = instance;
            FromTyp = fromTyp;
            ToTyp = totyp;
            DoThrowInvalidCast = doThrowInvalidCast;
            Att.TypGet = totyp;
        }

        public override void Give(IMRGenerator gen)
        {
            Instance.Give(gen);
            if (Instance.Att.TypGet.IsValueType)
            { gen.Box(Instance.Att.TypGet); }
            if (DoThrowInvalidCast)
            { gen.CastNoisy(FromTyp, ToTyp); }
            else
            { gen.CastSilent(FromTyp, ToTyp); }
        }
    }

    public class Conv : Sema
    {
        public Sema Instance;
        public Typ FromTyp;
        public Typ ToTyp;
        public bool DoThrowInvalidCast;

        public Conv(Sema instance, Typ fromTyp, Typ totyp, bool doThrowInvalidCast)
        {
            Instance = instance;
            FromTyp = fromTyp;
            ToTyp = totyp;
            DoThrowInvalidCast = doThrowInvalidCast;
            Att.TypGet = totyp;
        }

        public override void Give(IMRGenerator gen)
        {
            Instance.Give(gen);
            if (DoThrowInvalidCast)
            { gen.ConvNoisy(FromTyp, ToTyp); }
            else
            { gen.ConvSilent(FromTyp, ToTyp); }
        }
    }

    public class Ret : Sema, IReturnDeterminacyState
    {
        public ReturnPoint RP;
        
        public Ret(ReturnPoint rp)
        {
            Att.CanExec_ = true;
            RP = rp;
        }

        public override void Exec(IMRGenerator gen)
        {
            gen.Br(RP.Lbl.Value.ToString());
        }

        public bool RDS { get { return true; } }
    }

    public class GenericArgument : Nmd
    {
        public Type RefType = null;

        public GenericArgument(Type refType)
        {
            RefType = refType;
            Name = refType.Name;
        }
    }

    public class Enu : Nmd
    {
        public string Val;
        public Enu(string val, Typ typ)
            : base()
        {
            Val = val;
            Att.TypGet = typ;
        }

        public override void Give(IMRGenerator gen)
        {
            if (false == Att.TypGet.IsReferencing)
            { throw new NotImplementedException(); }

            Literal lt = new Literal((int)Enum.Parse(Att.TypGet.RefType, Val), Att.TypGet.E.BTY.Int);
            gen.LoadLiteral(lt);
        }

        public override void Addr(IMRGenerator gen)
        {
            Give(gen);
        }

        public override void Exec(IMRGenerator gen)
        {
            Give(gen);
            gen.Pop();
        }
    }

    public class Variable : Nmd
    {
        //TODO  remove vector? and array because of unuse
        public enum VariableKind
        {
            Param, This, Local,
            Field,
            Vector, Array,
            ParamGeneric
        }
        public VariableKind VarKind;
        public int GenericIndex = -1;

        public Variable(string name, Typ typ, VariableKind varKind)
            : base()
        {
            Name = name;
            VarKind = varKind;

            Att.TypGet = typ;
            Att.TypSet = typ;
        }

        public override void Exec(IMRGenerator gen) { }

        public override void Give(IMRGenerator gen)
        {
            if (VarKind == VariableKind.Field)
            {
                gen.LoadField(/*Typ is*/ null, this);
            }
            else
            {
                gen.LoadVariable(this);
            }
        }

        public override void Take(IMRGenerator gen)
        {
            if (VarKind == VariableKind.Field)
            {
                gen.StoreField(/*Typ is*/ null, this);
            }
            else
            {
                gen.StoreVariable(this);
            }
        }

        public override void Addr(IMRGenerator gen)
        {
            if (Att.TypGet.IsValueType) { gen.LoadAVariable(this); }
            else { Give(gen); }
        }

        public override string ToString()
        {
            Func<string, object, string> nv = Sty.Nv;
            return Sty.Curly(Sty.Csv(nv("Name", Name), nv("Typ", Att.TypGet), nv("VarKind", VarKind))) + ":" + GetType().Name;
        }
    }

    public class ReturnPoint : Sema, IReturnDeterminacyState
    {
        public Literal Lbl;
        public Variable Var;

        public ReturnPoint()
        {
            Att.CanExec_ = true;
        }

        public override void Exec(IMRGenerator gen)
        {
            gen.PutLabel(Lbl.Value.ToString());
            if (null != Var) { Var.Give(gen); }
            gen.Ret();
        }

        public bool RDS { get { return true; } }
    }

    public class ReturnValue : Sema, IReturnDeterminacyState
    {
        public ReturnPoint RP;
        public Sema GiveVal = null;

        public ReturnValue(ReturnPoint rp)
        {
            Att.CanExec_ = true;
            RP = rp;
        }

        public override void Exec(IMRGenerator gen)
        {
            GiveVal.Give(gen);
            RP.Var.Take(gen);
            gen.Br(RP.Lbl.Value.ToString());
        }

        public bool RDS { get { return true; } }
    }

    public class AccFun : Sema, IAcc
    {
        public Typ CalleeTy;
        public Fun Callee;
        public Sema Instance;

        public Typ OwnerTyp
        {
            get { return CalleeTy; }
        }

        public Sema OwnerIns { get { return Instance; } }

        public AccFun(Typ calleety, Fun callee, Sema instance)
        {
            CalleeTy = calleety;
            Callee = callee;
            Instance = instance;
            Att.CanExec_ = true;
            if (false == callee.IsConstructor)
            {
                Att.TypGet = callee.ReturnTyp;
                Att.TakeReturnTyp = callee.ReturnTyp;
            }
        }

        public override void Prepare(IMRGenerator gen)
        {
            LoadInstance(gen);
        }

        public void LoadInstance(IMRGenerator gen)
        {
            if (Instance == null) { return; }

            Instance.Addr(gen);
            if (Instance is Literal && Instance.Att.TypGet.IsValueType)
            {
                Variable v = (Instance as Literal).TmpVarGen.Substitute(Instance.Att.TypGet, gen);
                v.Addr(gen);
            }
        }

        public override void Take(IMRGenerator gen)
        {
            if (Callee.IsOperator)
            {
                gen.Ope(Callee.Name, Callee.Att.TypGet);
            }
            else
            {
                Typ calleeTy = Callee.IsOperatorLikeFun
                    ? Callee.CalleeTypOfOperatorLikeFun
                    : CalleeTy;
                gen.CallFunction(calleeTy, Callee);
            }
        }
    }

    /// <summary>
    /// Constructor access information
    /// </summary>
    public class AccNew : Sema, IAcc
    {
        public Typ CalleeTy;
        public Fun Callee;
        
        public Typ OwnerTyp
        {
            get { return CalleeTy; }
        }

        public Sema OwnerIns { get { return null; } }

        public AccNew(Typ calleety, Fun callee)
        {
            CalleeTy = calleety;
            Callee = callee;
            Att.CanExec_ = true;
            Att.TypGet = callee.ReturnTyp;
            Att.TakeReturnTyp = callee.ReturnTyp;
        }

        public override void Take(IMRGenerator gen)
        {
            gen.NewObject(CalleeTy, Callee);
        }
    }

    public class Semas : Sema
    {
        public Sema[] Ss;

        public Semas(Sema[] ss)
        {
            Ss = ss;
        }

        public override void Give(IMRGenerator gen)
        {
            foreach (Sema s in Ss)
            { s.Give(gen); }
        }

        public static readonly Semas Empty = new Semas(new Sema[0]);
        public static Semas S0()                    /**/ { return Empty; }
        public static Semas S1(Sema s1)             /**/ { return new Semas(new Sema[] { s1 }); }
        public static Semas S2(Sema s1, Sema s2)    /**/ { return new Semas(new Sema[] { s1, s2 }); }
    }

    public class CallFun : Sema
    {
        public Semas Giver;
        public Sema Taker;

        public CallFun(Semas giver, Sema taker)
        {
            Giver = giver;
            Taker = taker;
            Att.CanExec_ = true;
            Att.TypGet = taker.Att.TypGet;
        }

        public override void Prepare(IMRGenerator gen)
        {
            Taker.Prepare(gen);
        }

        public override void Give(IMRGenerator gen)
        {
            if (false == Att.CanGet)
            { throw new SemanticError("Cannot give a value."); }
            Core(gen);
            if (false == Taker.Att.TakeReturns)
            { Taker.Give(gen); }
        }

        public override void Take(IMRGenerator gen)
        {
            Exec(gen);
            Taker.Take(gen);
        }

        public override void Exec(IMRGenerator gen)
        {
            Core(gen);
            if (Taker.Att.TakeReturns)
            { gen.Pop(); }
        }

        private void Core(IMRGenerator gen)
        {
            Taker.Prepare(gen);
            Giver.Give(gen);
            Taker.Take(gen);
        }
    }

    public class AccProp : Sema, IAcc
    {
        public Typ CalleeTy;
        public Prop Prop;
        public Sema Instance;

        public Typ OwnerTyp
        {
            get { return CalleeTy; }
        }

        public Sema OwnerIns { get { return Instance; } }

        public AccProp(Typ calleety, Prop prop, Sema instance)
        {
            CalleeTy = calleety;
            Prop = prop;
            Instance = instance;

            Att.TypGet = prop.Att.TypGet;
            Att.TypSet = prop.Att.TypSet;
        }

        public override void Prepare(IMRGenerator gen)
        {
            LoadInstance(gen);
        }

        public void LoadInstance(IMRGenerator gen)
        {
            if (Instance == null) { return; }

            Instance.Addr(gen);
            if (Instance is Literal && Instance.Att.TypGet.IsValueType)
            {
                Variable v = (Instance as Literal).TmpVarGen.Substitute(Instance.Att.TypGet, gen);
                v.Addr(gen);
            }
        }

        public override void Give(IMRGenerator gen)
        {
            if (Prop.Getter == null)
            { throw new SyntaxError("Cannot get value from the property"); }
            AccFun af = new AccFun(CalleeTy, Prop.Getter, Instance);
            CallFun cf = new CallFun(Semas.S0(), af);
            cf.Give(gen);
        }

        public override void Take(IMRGenerator gen)
        {
            Fun setter = Prop.Setter;
            if (setter == null)
            { throw new SyntaxError("Cannot set value to the property"); }
            gen.CallFunction(CalleeTy, setter);
        }

        public override void Addr(IMRGenerator gen)
        {
            Give(gen);
        }

        public override void Exec(IMRGenerator gen)
        {
            Give(gen);
            gen.Pop();
        }
    }

    public class ThrowStmt : Sema, IReturnDeterminacyState
    {
        public bool RDS_;
        public bool RDS { get { return RDS_; } }
        public Sema S;

        public ThrowStmt(Sema s)
        {
            RDS_ = true;
            S = s;
            Att.CanExec_ = true;
        }

        public override void Exec(IMRGenerator gen)
        {
            S.Give(gen);
            gen.Throw();
        }
    }

    public class TryStmt : Sema, IReturnDeterminacyState
    {
        public class CatchStmt
        {
            public Typ ExcpTyp;
            public Variable ExcpVar;
            public Sema[] Block;
        }

        public string Fix;
        public Sema[] Try;
        public CatchStmt[] Catches;
        public Sema[] Finally;
        public bool RDS_;
        public bool RDS { get { return RDS_; } }

        public TryStmt(string uniqFix, Sema[] try_, CatchStmt[] catches, Sema[] finally_, bool rds)
        {
            Att.CanExec_ = true;
            Fix = uniqFix;
            Try = try_;
            Catches = catches;
            Finally = finally_;
            RDS_ = rds;
        }

        public override void Exec(IMRGenerator gen)
        {
            string exitcatch = "exitcatch" + Fix;
            string exitfinally = "exitfinally" + Fix;

            bool isFinally = Finally != null;
            if (isFinally)
            { gen.Try(); }
            
            gen.Try();
            foreach (Sema s in Try) { s.Exec(gen); }
            gen.Leave(exitcatch);
            foreach (CatchStmt ca in Catches)
            {
                gen.Catch(ca.ExcpTyp);
                if (ca.ExcpVar != null)
                { ca.ExcpVar.Take(gen); }
                else
                { gen.Pop(); }
                foreach (Sema s in ca.Block) { s.Exec(gen); }
                gen.Leave(exitcatch);
            }
            gen.CloseTry();
            gen.PutLabel(exitcatch);
            
            if (isFinally)
            {
                gen.Leave(exitfinally);

                gen.Finally();
                foreach (Sema s in Finally) { s.Exec(gen); }
                gen.EndFinally();
                gen.CloseTry();
                gen.PutLabel(exitfinally);
            }
        }

    }

    public class IfInfo : Sema, IReturnDeterminacyState
    {
        public class Component
        {
            public Sema Condition;
            public Sema[] Lines;
        }

        public string Fix;
        public Component IfThen;
        public Component[] ElifThen;
        public Sema[] Else;
        public bool RDS_;
        public bool RDS { get { return RDS_; } }

        public IfInfo(string uniqFix, Component ifThen, Component[] elifThen, Sema[] else_, bool rds)
        {
            Att.CanExec_ = true;
            Fix = uniqFix;
            IfThen = ifThen;
            ElifThen = elifThen;
            Else = else_;
            RDS_ = rds;
        }

        public override void Exec(IMRGenerator gen)
        {
            string endlbl;
            Stack<string> elselbls;
            Action gotoend, brelse, putelse;

            endlbl = "endif" + Fix;
            elselbls = new Stack<string>();
            elselbls.Push(endlbl);
            if (Else != null) { elselbls.Push("else" + Fix); };
            for (int i = ElifThen.Length; i > 0; i--)
            { elselbls.Push("elif" + Fix + "_" + i.ToString()); }

            gotoend = delegate() { gen.Br("endif" + Fix); };
            putelse = delegate() { gen.PutLabel(elselbls.Pop()); };
            brelse = delegate() { gen.BrFalse(elselbls.Peek()); };

            IfThen.Condition.Give(gen);
            brelse();
            foreach (Sema  s in IfThen.Lines) { s.Exec(gen); }
            gotoend();

            foreach (Component elt_ in ElifThen)
            {
                putelse();
                elt_.Condition.Give(gen);
                brelse();
                foreach (Sema  l_ in elt_.Lines) { l_.Exec(gen); }
                gotoend();
            }

            if (Else != null)
            {
                putelse();
                foreach (Sema  l_ in Else) { l_.Exec(gen); }
            }

            gen.PutLabel(endlbl);
        }

    }

    public class WhileInfo : Sema
    {
        public Literal Dolbl;
        public Literal Endlbl;
        public Sema Condition;
        public Sema[] Lines;

        public WhileInfo(Literal dolbl, Literal endlbl, Sema condition, Sema[] lines)
        {
            Att.CanExec_ = true;
            Dolbl = dolbl;
            Endlbl = endlbl;
            Condition = condition;
            Lines = lines;
        }

        public override void Exec(IMRGenerator gen)
        {
            gen.PutLabel(Dolbl.Value.ToString());
            Condition.Give(gen);
            gen.BrFalse(Endlbl.Value.ToString());
            foreach (Sema  u_ in Lines) { u_.Exec(gen); }
            gen.Br(Dolbl.Value.ToString());
            gen.PutLabel(Endlbl.Value.ToString());
        }

    }

    public class BranchInfo : Sema
    {
        public Literal Label;
        public BranchInfo(Literal label)
        {
            Att.CanExec_ = true;
            Label = label;
        }

        public override void Exec(IMRGenerator gen)
        {
            gen.Br(Label.Value.ToString());
        }
    }

    public class ArrayInstatiation : Sema
    {
        public Sema[] Lens;
        public TmpVarGenerator TmpVarGen;
        // prepare for over twice referenced instatication
        public IMR PlaceHolder;
        public Variable TmpVar;

        public ArrayInstatiation(Typ typ, Sema[] lens, TmpVarGenerator tmpVarGen)
        {
            Lens = lens;
            TmpVarGen = tmpVarGen;
            Att.TypGet = typ;
        }

        public override void Exec(IMRGenerator gen)
        {
            Give(gen);
            gen.Pop();
        }
        public override void Give(IMRGenerator imrs)
        {
            if (PlaceHolder == null) { Give1st(imrs); }
            else { GiveSubsequent(imrs); }
        }

        public override void Addr(IMRGenerator gen)
        {
            Give(gen);
        }

        public void Give1st(IMRGenerator gen)
        {
            foreach (Sema v in Lens)
            { v.Give(gen); }

            PlaceHolder = gen.NewArray(Att.TypGet);
        }

        public void GiveSubsequent(IMRGenerator gen)
        {
            if (TmpVar == null)
            { TmpVar = TmpVarGen.Insert(Att.TypGet, gen, PlaceHolder); }
            TmpVar.Give(gen);
        }

    }

    public class TmpVarGenerator
    {
        public Func<string> GetTempName;
        public Func<string, Typ, Variable> DeclareVariable;

        public TmpVarGenerator(Func<string> getTempName, Func<string, Typ, Variable> declareVariable)
        {
            GetTempName = getTempName;
            DeclareVariable = declareVariable;
        }

        public Variable Generate(Typ typ, IMRGenerator gen)
        {
            return DeclareVariable(GetTempName(), typ);
        }

        public Variable Substitute(Typ typ, IMRGenerator gen)
        {
            Variable v = Generate(typ, gen);
            v.Take(gen);
            return v;
        }

        public Variable Insert(Typ typ, IMRGenerator gen, IMR PlaceHolder)
        {
            Debug.Assert(PlaceHolder != null);
            Debug.Assert(GetTempName != null);
            Debug.Assert(DeclareVariable != null);

            IMRGenerator tmpgen = new IMRGenerator();
            Variable v = Substitute(typ, tmpgen);
            v.Give(tmpgen);

            int idx = gen.IndexOf(PlaceHolder);
            Debug.Assert(idx >= 0);
            idx += 1;
            if (idx < gen.Count) { gen.InsertRange(idx, tmpgen); }
            else { gen.AddRange(tmpgen); }

            return v;
        }
    }

    public class AccArr : Sema
    {
        public Sema Val;
        public Sema[] Indices;

        public AccArr(Sema val, Sema[] indices)
        {
            Val = val;
            Indices = indices;

            Att.TypSet = Att.TypGet = val.Att.TypGet.ArrayType;
        }

        public override void Prepare(IMRGenerator gen)
        {
            Val.Give(gen);
            foreach (Sema idx in Indices)
            { idx.Give(gen); }
        }

        public override void Give(IMRGenerator gen)
        {
            Val.Give(gen);
            Array.ForEach<Sema>(Indices,
                delegate(Sema v_) { v_.Give(gen); });

            gen.LdArrayElement(Val.Att.TypGet);
        }

        public override void Take(IMRGenerator gen)
        {
            Typ arty = Val.Att.TypGet;
            Typ consistedty;
            if (arty.IsVector)
            { consistedty = Att.TypGet; }
            else if (arty.IsArray)
            { consistedty = Val.Att.TypGet; }
            else
            { throw new InternalError("The type is not vector nor array"); }
            gen.StArrayElement(arty, consistedty);
        }

        public override void Addr(IMRGenerator gen)
        {
            Give(gen);
        }
        public override void Exec(IMRGenerator gen)
        {
            Give(gen);
            gen.Pop();
        }
        public override string ToString()
        {
            return Val.ToString() + ":" + GetType();
        }
    }

    public class AccFld : Sema, IAcc
    {
        public Typ HoldingTyp;
        public Sema Instance;
        public Variable Field;

        public Typ OwnerTyp
        {
            get { return HoldingTyp; }
        }

        public Sema OwnerIns { get { return Instance; } }

        public AccFld(Typ fieldHoldingTyp, Sema instance, Variable field)
        {
            HoldingTyp = fieldHoldingTyp;
            Field = field;
            Instance = instance;
            Att.TypGet = Att.TypSet = field.Att.TypGet;
        }

        public override void Prepare(IMRGenerator gen)
        {
            if (false == Field.Att.IsStatic)
            { Instance.Give(gen); }
        }

        public override void Give(IMRGenerator gen)
        {
            Prepare(gen);
            gen.LoadField(HoldingTyp, Field);
        }

        public override void Take(IMRGenerator gen)
        {
            gen.StoreField(HoldingTyp, Field);
        }

        public override void Addr(IMRGenerator gen)
        {
            Give(gen);
        }

        public override void Exec(IMRGenerator gen)
        {
            Give(gen);
            gen.Pop();
        }
    }

    public class AccLoc : Sema, IAcc
    {
        public Fun HoldingFun;
        public Variable Var;

        public Typ OwnerTyp
        {
            get { return Att.TypGet; }
        }

        public Sema OwnerIns { get { return Var; } }

        public AccLoc(Fun vriableHoldingFun, Variable var)
        {
            HoldingFun = vriableHoldingFun;
            Var = var;
            Att.TypGet = Att.TypSet = var.Att.TypGet;
        }

        public override void Give(IMRGenerator gen)
        {
            Var.Give(gen);
        }

        public override void Take(IMRGenerator gen)
        {
            Var.Take(gen);
        }

        public override void Addr(IMRGenerator gen)
        {
            Var.Addr(gen);
        }

        public override void Exec(IMRGenerator gen)
        {
            Var.Exec(gen);
        }
    }

    public class ClosureConstruction : Sema
    {
        public CallFun CtorCall;
        public TmpVarGenerator TmpVarGen;
        public Tuple2<Sema, Variable>[] SrcAndDsts;
        public Variable TmpVar;

        public ClosureConstruction(CallFun ctorcall, TmpVarGenerator tmpVarGen, Tuple2<Sema, Variable>[] srcanddsts)
        {
            CtorCall = ctorcall;
            TmpVarGen = tmpVarGen;
            SrcAndDsts = srcanddsts;
            Att.TypGet = ctorcall.Att.TypGet;
        }

        public override void Give(IMRGenerator gen)
        {
            if (null == TmpVar)
            {
                Typ ty = CtorCall.Att.TypGet; 
                TmpVar = TmpVarGen.Generate(ty, gen);
                CallFun tmpcf = new CallFun(Semas.S1(CtorCall), TmpVar);
                tmpcf.Exec(gen);
                foreach (Tuple2<Sema, Variable> sandd in SrcAndDsts)
                {
                    AccFld af = new AccFld(ty, TmpVar, sandd.F2);
                    CallFun cf = new CallFun(Semas.S1(sandd.F1), af);
                    cf.Exec(gen);
                }
            }
            TmpVar.Give(gen);
        }
    }

    public class Custom : Sema
    {
        public Typ CalleeTy;
        public Fun Callee;
        public Sema[] CtorArgs;
        public FieldOrProp[] ForPs;

        public class FieldOrProp
        {
            public char Kind;
            public Typ Ty;
            public Literal Val;
        }

        public Custom(Typ calleety, Fun callee, Sema[] ctorargs, FieldOrProp[] forps)
        {
            CalleeTy = calleety;
            Callee = callee;
            CtorArgs = ctorargs;
            ForPs = forps;
            Att.CanExec_ = true;
            Att.TypGet = callee.Att.TypGet;
        }

    }

    public class LoadFun : Sema
    {
        public Typ Ty;
        public Fun Fu;

        public LoadFun(Typ ty, Fun fu)
        {
            Ty = ty; Fu = fu;
            Att.TypGet = ty.E.BTY.IntPtr;
        }

        public override void Give(IMRGenerator gen)
        {
            gen.LoadFunction(Ty, Fu);
        }
    }

    public enum Accessibility
    {
        None,
        Public,         //  public
        FamOrAssem,     //  protected internal
        Assembly,       //  internal
        Family,         //  protected
        FamAndAssem,    //  not in C#
        Private         //  private
    }

    public class AccessibilityControl
    {
        public static bool CanAccess(Accessibility calleeacc, Typ calleetyp, Typ callertyp)
        {
            bool isSameClass = calleetyp == callertyp;
            bool isSameFamily = false;
            {
                Typ basetyp = callertyp;
                while (basetyp != null)
                {
                    isSameFamily = calleetyp == basetyp;
                    if (isSameFamily)
                    { break; }
                    basetyp = basetyp.BaseTyp;
                }
            }
            bool isSameAssembly = calleetyp.AssemblyName == callertyp.AssemblyName;
            return CanAccess(calleeacc, isSameClass, isSameFamily, isSameAssembly);
        }

        public static bool CanAccess(Accessibility calleeacc, bool isSameClass, bool isSameFamily, bool isSameAssembly)
        {
            Accessibility acc = calleeacc;
            if (acc == Accessibility.Public) { return true; }
            if (acc == Accessibility.Private && isSameClass) { return true; }

            if (isSameFamily)
            {
                if (acc == Accessibility.Family) { return true; }
                if (acc == Accessibility.FamOrAssem) { return true; }
            }

            if (isSameAssembly)
            {
                if (acc == Accessibility.Assembly) { return true; }
                if (acc == Accessibility.FamOrAssem) { return true; }
            }

            if (isSameFamily && isSameAssembly)
            {
                if (acc == Accessibility.FamAndAssem) { return true; }
            }

            return false;
        }

        static public bool Contains(MethodAttributes flag, MethodAttributes isIn)
        {
            return (isIn & flag) == flag;
        }

        static public Accessibility FromMethodAttributes(MethodAttributes a)
        {
            if (Contains(MethodAttributes.Public        /*0x06*/, a)) return Accessibility.Public;
            if (Contains(MethodAttributes.FamORAssem    /*0x05*/, a)) return Accessibility.FamOrAssem;
            if (Contains(MethodAttributes.Family        /*0x04*/, a)) return Accessibility.Family;
            if (Contains(MethodAttributes.Assembly      /*0x03*/, a)) return Accessibility.Assembly;
            if (Contains(MethodAttributes.FamANDAssem   /*0x02*/, a)) return Accessibility.FamAndAssem;
            if (Contains(MethodAttributes.Private       /*0x01*/, a)) return Accessibility.Private;
            return Accessibility.None;
        }

    }

}
