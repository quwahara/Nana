using System;
using System.Collections.Generic;
using System.Text;
using Nana.Semantics;
using Nana.Delegates;
//using Nana.ILs;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Reflection.Emit;
using Nana.Infr;

//  IMR:    intermediate representation

namespace Nana.IMRs
{
    public enum C /*'C'ode*/
    {
        //Add,
        //Sub,
        //Mul,
        //Div,
        //Rem,
        //Neg,
        //And,
        //Or,
        //Xor,
        //Not,
        //Ceq,
        //Cgt,
        //Clt,


        None,

        // Branching Instructions
        //Br,
        //BrFalse,

        //Call,
        //Load,
        //LoadA,
        //LdElem,
        //LoadArrayArray,     // call Array.Get
        //LoadElemRef,
        //NewArraryVector,    // newarr
        //NewArraryArray,     // newobj
        //NewObj,
        //StoreArrayArray,    // call Array.Set
        //StElem,
        //Store,
        ////PutLabel,


        //PlaceHolder,

        //  --- for restructured of IMR ---

        // Branching Instructions
        Br,
        BrFalse,
        PutLabel,

        Pop,
        Ret,

        LdArrayElement,
        LdLiteral,
        LdVariable,
        LdVariableA,

        StArrayElement,
        StVariable,
        
        NewObject,
        NewArray,

        CallAction,


        // Arithmetical Operations (p270)
        Add, Sub, Mul, Div,    /* DivUn, */    Rem,    /* RemUn, */    Neg,

        // Bitwise Operations (p272)
        And, Or, Xor, Not,

        // Logical Condition Check Instructions (p275)
        Ceq, Cgt,    /* Cgt_Un, */   Clt,    /* Clt_Un, */   /* Ckfinite, */



        __SENTINEL__

    }

    public class IMR
    {
        public C C = C.None;
        public IMR() { }
        public IMR(C c) { C = c; }
        public IMR(C c, string v) : this(c) { StringV = v; }
        public IMR(C c, Actn v) : this(c) { ActnV = v; }
        public IMR(C c, Literal v) : this(c) { LiteralV = v; }
        public IMR(C c, Variable v) : this(c) { VariableV = v; }
        public IMR(C c, Typ v) : this(c) { TypV = v; }
        public IMR(C c, Typ v, Typ v2) : this(c) { TypV = v; TypV2 = v2; }
        public IMR(C c, IValuable v) : this(c) { ValuableV = v; }

        public string StringV;
        public Actn ActnV;
        public Literal LiteralV;
        public Variable VariableV;
        public Typ TypV;
        public Typ TypV2;
        public IValuable ValuableV;
    }

    public class IMRGenerator : List<object>
    //public class IMRGenerator : List<Func<string>>
    {
        static public readonly string InstCons = ".ctor";
        static public readonly string StatCons = ".cctor";

        static public bool IsInstCons(string name) { return name == InstCons; }
        static public bool IsStatCons(string name) { return name == StatCons; }
        static public bool IsAnyCons(string name) { return IsInstCons(name) || IsStatCons(name); }

        public void GenerateIMR(App app)
        {
            Predicate<INmd> pred = delegate(INmd n)
            { return n.GetType() == typeof(Actn) || n.GetType() == typeof(Fctn); };

            foreach (Actn a in app.FindDownAll(pred))
            {
                a.Exes.ForEach(delegate(IExecutable x) { x.Exec(this); });
                Ret();
                a.Instructions.AddRange(this);
                Clear();
            }
        }

        //static public string AssemblyExtern(Assembly a)
        //{
        //    AssemblyName an = a.GetName();
        //    StringBuilder b = new StringBuilder();
        //    byte[] pkt;
        //    b.Append(".assembly extern ").Append(an.Name).Append(" {");

        //    if (an.Version != null)
        //        b.Append(".ver ").Append(an.Version.ToString().Replace(".", ":"));

        //    if ((pkt = an.GetPublicKeyToken()) != null && pkt.Length == 8)
        //    {
        //        b.Append(" .publickeytoken = (");

        //        b.Append(
        //            string.Join(" "
        //            , new List<byte>(pkt)
        //                .ConvertAll<string>(delegate(byte by) { return by.ToString("X"); })
        //                .ToArray()));

        //        //b.Append(SFList.Cast(FList<byte>.Parse(pkt)
        //        //    .Map<string>(delegate(byte by) { return by.ToString("X"); })
        //        //    ).Xsv(" "));
        //        b.Append(")");
        //    }

        //    b.Append("}");
        //    return b.ToString();
        //}

        //public Func<string> CallActn(Actn t)
        //{
        //    return delegate()
        //    {
        //        if (t.IsConstructor) return S(OpCodes.Call) + " instance " + Body(t); ;
        //        if (t.IsStatic) return S(OpCodes.Call) + " " + Body(t);
        //        return S(OpCodes.Callvirt) + " instance " + Body(t);
        //    };
        //}

        //public static string Body(Actn fi)
        //{
        //    StringBuilder b = new StringBuilder();
        //    //TypeInfo retti = fi.IsConstructor ? TypeInfo.Void : fi.ThisType;
        //    //Typ retti = fi is IValuable && fi.IsConstructor == false ? (fi as IValuable).Typ : null;
        //    Typ retti = fi is ITyped && fi.IsConstructor == false ? (fi as ITyped).Typ : null;
        //    b.Append(TypeFullName(retti));

        //    Typ m = fi.FindUpTypeOf<Typ>();
        //    //Mdl m = fi.GetActionOvrldHolder();
        //    //Mdl m = fi.FindUp(Mdl.IsMdl) as Mdl;
        //    if (m.GetType() == typeof(Typ))
        //    {
        //        Typ ti = m as Typ;
        //        //if (fi.IsConstructor && fi.NmdAbove.Name == "0base")
        //        //{ ti = ti.BaseTyp; }
        //        if (ti.IsGeneric)
        //        { b.Append(ti.IsValueType ? " value" : " class"); }
        //        b.Append(" ");
        //        b.Append(TypeFullName(ti));
        //        b.Append("::");
        //    }
        //    else
        //    {
        //        b.Append(" ");
        //    }

        //    //b.Append(fi._Name);
        //    if (MethodAttributes.SpecialName != (fi.MthdAttrs & MethodAttributes.SpecialName))
        //    {
        //        b.Append(fi.Family.Name);
        //    }
        //    else
        //    {
        //        b.Append(fi.SpecialName);
        //    }
        //    //if (fi.Family.GetType() == typeof(ActnOvld))
        //    //{
        //    //    b.Append(fi.Family.Name);
        //    //}
        //    //else if (fi.Family.GetType() == typeof(Prop2))
        //    //{
        //    //    b.Append(fi.SpecialName);
        //    //}
        //    //else
        //    //{
        //    //    throw new NotImplementedException();
        //    //}
        //    //b.Append(fi.FindUpTypeOf<ActnOvld>().Name);
        //    b.Append("(");
        //    Variable v;
        //    string s;
        //    List<Variable> Params = fi.FindAllTypeOf<Variable>();
        //    Params.RemoveAll(delegate(Variable vv)
        //    {
        //        return vv.VarKind != Variable.VariableKind.Param
        //            && vv.VarKind != Variable.VariableKind.ParamGeneric;
        //    });
        //    if (Params.Count > 0)
        //    {
        //        v = Params[0];
        //        s = v.VarKind == Variable.VariableKind.ParamGeneric
        //            ? "!" + v.GenericIndex : TypeLongForm(v.Typ);
        //        //s= TypeLongForm(v.Typ);
        //        b.Append(s);
        //        for (int i = 1; i < Params.Count; i++)
        //        {
        //            b.Append(", ");
        //            v = Params[i];
        //            s = v.VarKind == Variable.VariableKind.ParamGeneric
        //                ? "!" + v.GenericIndex : TypeLongForm(v.Typ);
        //            //s = TypeLongForm(v.Typ);
        //            b.Append(s);
        //        }
        //    }


        //    //List<Variable> Params = fi.GetParams();
        //    //if (Params.Count > 0)
        //    //{
        //    //    v = Params[0];
        //    //    s = v.VarKind == Variable.VariableKind.ParamGeneric
        //    //        ? "!" + v.GenericIndex : TypeLongForm(v.Typ);
        //    //    //s= TypeLongForm(v.Typ);
        //    //    b.Append(s);
        //    //    for (int i = 1; i < Params.Count; i++)
        //    //    {
        //    //        b.Append(", ");
        //    //        v = Params[i];
        //    //        s = v.VarKind == Variable.VariableKind.ParamGeneric
        //    //            ? "!" + v.GenericIndex : TypeLongForm(v.Typ);
        //    //        //s = TypeLongForm(v.Typ);
        //    //        b.Append(s);
        //    //    }
        //    //}
        //    b.Append(")");

        //    return b.ToString();
        //}

        //static public string S(OpCode c) { return c.ToString(); }
        //static public string S(OpCode c, object opRnd) { return S(c) + " " + opRnd.ToString(); }
        //static public bool IsSupportedType(Typ t)
        //{
        //    return string.IsNullOrEmpty(SupportedTypeName(t)) == false;
        //}

        //static public string SupportedTypeName(Typ t)
        //{
        //    if (t == null) { return "void"; }

        //    string brk = "";
        //    while (t.IsVectorOrArray)
        //    {
        //        brk = Typ.ToBracket(t.Dimension) + brk;
        //        t = t.ArrayType;
        //    }

        //    string nm = null;

        //    if (t.IsReferencing == false) { return null; }

        //    if (t.RefType == typeof(void)) nm = "void";
        //    if (t.RefType == typeof(bool)) nm = "bool";
        //    if (t.RefType == typeof(int)) nm = "int32";
        //    if (t.RefType == typeof(object)) nm = "object";
        //    if (t.RefType == typeof(string)) nm = "string";

        //    return nm + brk;
        //}

        //static public string TypeCharacter(Typ t)
        //{
        //    if (IsSupportedType(t)) return "";
        //    return "class";
        //    //if (t.IsValueType()) return "valuetype";
        //    //else if (t.IsClass()) return "class";
        //    //else
        //    //{
        //    //    throw new InternalError("Can not specify the tyep character: " + t._Name);
        //    //}
        //}

        //static public string TypeLongForm(Typ t)
        //{
        //    if (IsSupportedType(t)) return SupportedTypeName(t);
        //    return TypeCharacter(t) + " " + TypeFullName(t);
        //}

        //static public string TypeFullName(Typ t)
        //{
        //    if (IsSupportedType(t)) return SupportedTypeName(t);

        //    StringBuilder b = new StringBuilder();
        //    b.Append("[").Append(t.AssemblyName).Append("]");
        //    b.Append(t._FullName);
        //    if (t.IsGeneric)
        //    {
        //        b.Append("<");
        //        b.Append(string.Join(", "
        //            , new List<Typ>(t.GenericTypeParams)
        //            .ConvertAll<string>(TypeFullName)
        //            .ToArray()
        //            ));
        //        b.Append(">");
        //    }

        //    return b.ToString();
        //}

        //public Func<string> Append(Func<string> f)
        //{
        //    Add(f); return f;
        //}

        public IMR Append(IMR imr) { Add(imr); return imr; }

        public static readonly IMR IMR_Ret = new IMR(C.Ret);
        public static readonly IMR IMR_Pop = new IMR(C.Pop);

        public IMR Ret() { return Append(IMR_Ret); }
        public IMR Pop() { return Append(IMR_Pop); }

        //public Func<string> Ret()
        //{
        //    return Append(delegate() { return OpCodes.Ret.ToString(); });
        //}

        //public Func<string> Pop()
        //{
        //    return Append(delegate() { return OpCodes.Pop.ToString(); });
        //}

        //public Func<string> Box(Typ typ)
        //{
        //    return Append(delegate() { return S(OpCodes.Box, TypeLongForm(typ)); });
        //}

        //public Func<string> LoadLiteral(Literal l)
        //{
        //    Debug.Assert(l != null && l.Typ != null && l.Typ.IsReferencing);
        //    Func<string> f = null;
        //    Typ t = l.Typ;
        //    if (t.RefType == typeof(bool))      /**/ f = delegate() { return ((bool)l.Value) ? S(OpCodes.Ldc_I4_1) : S(OpCodes.Ldc_I4_0); };
        //    if (t.RefType == typeof(string))    /**/ f = delegate() { return S(OpCodes.Ldstr, @"""" + l.Value + @""""); };
        //    if (t.RefType == typeof(int))       /**/ f = delegate() { return S(OpCodes.Ldc_I4, l.Value); };
        //    Debug.Assert(f != null);
        //    Add(f);
        //    return f;
        //}

        public IMR LoadLiteral(Literal l) { return Append(new IMR(C.LdLiteral, l)); }

        //public Func<string> LoadVariable(Variable v)
        //{
        //    Func<string> f = null;
        //    Variable.VariableKind k = v.VarKind;
        //    switch (k)
        //    {
        //        case Variable.VariableKind.This: f = delegate() { return OpCodes.Ldarg_0.ToString(); }; break;
        //        case Variable.VariableKind.Param: f = delegate() { return S(OpCodes.Ldarg, v.Name); }; break;
        //        case Variable.VariableKind.Local: f = delegate() { return S(OpCodes.Ldloc, v.Name); }; break;
        //        case Variable.VariableKind.StaticField: f = delegate() { return S(OpCodes.Ldsfld, TypeLongForm(v.Typ) + " " + v.Name); }; break;
        //        case Variable.VariableKind.Vector: f = delegate() { return S(OpCodes.Ldelem, TypeLongForm(v.Typ)); }; break;
        //        default: Debug.Fail(""); break;
        //    }
        //    Add(f);
        //    return f;
        //}

        public IMR LoadVariable(Variable v)
        {
            IMR imr = new IMR(C.LdVariable);
            imr.VariableV = v;
            return Append(imr);
        }

        //public Func<string> NewArrayVector(Typ t)
        //{
        //    Func<string> f = delegate() { return S(OpCodes.Newarr) + " " + TypeFullName(t); };
        //    Add(f);
        //    return f;
        //}

        //public Func<string> NewArrayArray(Typ t)
        //{
        //    Func<string> f = delegate()
        //    {
        //        StringBuilder b = new StringBuilder();
        //        b.Append(S(OpCodes.Newobj))
        //            .Append(" instance void ").Append(TypeFullName(t))
        //            .Append("::.ctor(int32");
        //        for (int i = 1; i < t.Dimension; i++)
        //        { b.Append(", int32"); }
        //        b.Append(")");
        //        return b.ToString();
        //    };
        //    Add(f);
        //    return f;
        //}

        public IMR NewArray(Typ t){ return Append(new IMR(C.NewArray, t)); }

        //public Func<string> LoadAVariable(Variable v)
        //{
        //    Func<string> f = null;
        //    Variable.VariableKind k = v.VarKind;
        //    switch (k)
        //    {
        //        case Variable.VariableKind.Param:
        //            //TODO  check 
        //            if (v.Name == "0")
        //            {
        //                throw new NotImplementedException("cannot load callee site instance pointer");
        //            }
        //            f = delegate() { return S(OpCodes.Ldarga, v.Name); }; break;
        //        case Variable.VariableKind.Local: f = delegate() { return S(OpCodes.Ldloca, v.Name); }; break;
        //        case Variable.VariableKind.StaticField: f = delegate() { return S(OpCodes.Ldsflda, TypeLongForm(v.Typ) + " " + v.Name); }; break;
        //        default: Debug.Fail(""); break;
        //    }
        //    Add(f);
        //    return f;
        //}

        public IMR LoadAVariable(Variable v) { return Append(new IMR(C.LdVariableA, v)); }

        //public Func<string> StoreVariable(Variable v)
        //{
        //    Func<string> f = null;
        //    Nana.Semantics.Variable.VariableKind k = v.VarKind;
        //    switch (k)
        //    {
        //        case Nana.Semantics.Variable.VariableKind.Param: f = delegate() { return S(OpCodes.Starg, v.Name); }; break;
        //        case Nana.Semantics.Variable.VariableKind.Local: f = delegate() { return S(OpCodes.Stloc, v.Name); }; break;
        //        case Nana.Semantics.Variable.VariableKind.StaticField: f = delegate() { return S(OpCodes.Stsfld, TypeLongForm(v.Typ) + " " + v.Name); }; break;
        //    }
        //    Debug.Assert(f != null);
        //    Add(f);
        //    return f;
        //}

        public IMR StoreVariable(Variable v) { return Append(new IMR(C.StVariable, v)); }

        //public Func<string> LdElemTyp(Typ t)
        //{
        //    Func<string> f = null;
        //    f = delegate() { return S(OpCodes.Ldelem) + " " + TypeLongForm(t); };
        //    Add(f);
        //    return f;
        //}

        //public Func<string> LoadArrayArray(Typ tp)
        //{
        //    Func<string> f = null;
        //    f = delegate()
        //    {
        //        //call instance int32 int32[0...,0...]::Get(int32, int32)
        //        //TypeInfo tp = imr.OpRnd.ThisType;
        //        Debug.Assert(tp.IsArray);
        //        StringBuilder b = new StringBuilder();
        //        b.Append(S(OpCodes.Call))
        //            .Append(" instance ")
        //            .Append(TypeFullName(tp.ArrayType))
        //            .Append(" ").Append(TypeFullName(tp))
        //            .Append("::Get(int32")
        //            ;
        //        for (int i = 1; i < tp.Dimension; i++)
        //        { b.Append(", int32"); }
        //        b.Append(")");
        //        return b.ToString();
        //    };
        //    Add(f);
        //    return f;
        //}

        public IMR LdArrayElement(Typ t){ return Append(new IMR(C.LdArrayElement, t)); }

        //public Func<string> StoreArrayArray(Typ tp)
        //{
        //    Func<string> f = null;
        //    f = delegate()
        //    {
        //        //call instance void int32[0...,0...]::Set(int32, int32, int32)
        //        //TypeInfo tp = imr.TypeArg;
        //        Debug.Assert(tp.IsArray);
        //        StringBuilder b = new StringBuilder();
        //        b.Append(S(OpCodes.Call))
        //            .Append(" instance void ")
        //            .Append(TypeFullName(tp))
        //            .Append("::Set(int32")
        //            ;
        //        for (int i = 1; i < tp.Dimension; i++)
        //        { b.Append(", int32"); }
        //        b.Append(", ").Append(TypeFullName(tp.ArrayType)).Append(")");
        //        return b.ToString();
        //    };
        //    Add(f);
        //    return f;
        //}

        //public Func<string> StElemTyp(Typ t)
        //{
        //    Func<string> f = null;
        //    f = delegate() { return S(OpCodes.Stelem) + " " + TypeLongForm(t); };
        //    Add(f);
        //    return f;
        //}

        public IMR StArrayElement(Typ t, Typ t2) { return Append(new IMR(C.StArrayElement, t, t2)); }

        //public Func<string> NewObjActionSig(Actn t)
        //{
        //    Func<string> f = null;
        //    f = delegate()
        //    {
        //        return S(OpCodes.Newobj) + " instance " + Body(t);
        //    };
        //    Add(f);
        //    return f;
        //}

        public IMR NewObject(Actn a) { return Append(new IMR(C.NewObject, a)); }        

        //public Func<string> CallActionSig(Actn t)
        //{
        //    Func<string> f = null;
        //    f = delegate()
        //    {
        //        if (t.IsConstructor) return S(OpCodes.Call) + " instance " + Body(t); ;
        //        if (t.IsStatic) return S(OpCodes.Call) + " " + Body(t);
        //        return S(OpCodes.Callvirt) + " instance " + Body(t);
        //    };
        //    Add(f);
        //    return f;
        //}

        public IMR CallAction(Actn a) { return Append(new IMR(C.CallAction, a)); }

        //public Func<string> Br(string label)
        //{
        //    Func<string> f = delegate() { return S(OpCodes.Br) + " " + label; }; Add(f); return f;
        //}
        //public Func<string> BrFalse(string label)
        //{
        //    Func<string> f = delegate() { return S(OpCodes.Brfalse) + " " + label; }; Add(f); return f;
        //}
        //public Func<string> PutLabel(string label)
        //{
        //    Func<string> f = delegate() { return label + ":"; }; Add(f); return f;
        //}

        public IMR Br(string label) { return Append(new IMR(C.Br, label)); }
        public IMR BrFalse(string label) { return Append(new IMR(C.BrFalse, label)); }
        public IMR PutLabel(string label) { return Append(new IMR(C.PutLabel, label)); }

        public static readonly IMR IMR_Add = new IMR(C.Add);
        public static readonly IMR IMR_Sub = new IMR(C.Sub);
        public static readonly IMR IMR_Mul = new IMR(C.Mul);
        public static readonly IMR IMR_Div = new IMR(C.Div);
        public static readonly IMR IMR_Rem = new IMR(C.Rem);
        public static readonly IMR IMR_Neg = new IMR(C.Neg);
        public static readonly IMR IMR_And = new IMR(C.And);
        public static readonly IMR IMR_Or = new IMR(C.Or);
        public static readonly IMR IMR_Xor = new IMR(C.Xor);
        public static readonly IMR IMR_Not = new IMR(C.Not);
        public static readonly IMR IMR_Ceq = new IMR(C.Ceq);
        public static readonly IMR IMR_Cgt = new IMR(C.Cgt);
        public static readonly IMR IMR_Clt = new IMR(C.Clt);

        public IMR Add() { return Append(IMR_Add); }
        public IMR Sub() { return Append(IMR_Sub); }
        public IMR Mul() { return Append(IMR_Mul); }
        public IMR Div() { return Append(IMR_Div); }
        public IMR Rem() { return Append(IMR_Rem); }
        public IMR Neg() { return Append(IMR_Neg); }
        public IMR And() { return Append(IMR_And); }
        public IMR Or() { return Append(IMR_Or); }
        public IMR Xor() { return Append(IMR_Xor); }
        public IMR Not() { return Append(IMR_Not); }
        public IMR Ceq() { return Append(IMR_Ceq); }
        public IMR Cgt() { return Append(IMR_Cgt); }
        public IMR Clt() { return Append(IMR_Clt); }

        //public Func<string> Add() { Func<string> f = delegate() { return S(OpCodes.Add); }; Add(f); return f; }
        //public Func<string> Sub() { Func<string> f = delegate() { return S(OpCodes.Sub); }; Add(f); return f; }
        //public Func<string> Mul() { Func<string> f = delegate() { return S(OpCodes.Mul); }; Add(f); return f; }
        //public Func<string> Div() { Func<string> f = delegate() { return S(OpCodes.Div); }; Add(f); return f; }
        //public Func<string> Rem() { Func<string> f = delegate() { return S(OpCodes.Rem); }; Add(f); return f; }
        //public Func<string> Neg() { Func<string> f = delegate() { return S(OpCodes.Neg); }; Add(f); return f; }
        //public Func<string> And() { Func<string> f = delegate() { return S(OpCodes.And); }; Add(f); return f; }
        //public Func<string> Or() { Func<string> f = delegate() { return S(OpCodes.Or); }; Add(f); return f; }
        //public Func<string> Xor() { Func<string> f = delegate() { return S(OpCodes.Xor); }; Add(f); return f; }
        //public Func<string> Not() { Func<string> f = delegate() { return S(OpCodes.Not); }; Add(f); return f; }
        //public Func<string> Ceq() { Func<string> f = delegate() { return S(OpCodes.Ceq); }; Add(f); return f; }
        //public Func<string> Cgt() { Func<string> f = delegate() { return S(OpCodes.Cgt); }; Add(f); return f; }
        //public Func<string> Clt() { Func<string> f = delegate() { return S(OpCodes.Clt); }; Add(f); return f; }
    }

}
