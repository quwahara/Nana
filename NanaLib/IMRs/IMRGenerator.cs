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
        None,

        // Arithmetical Operations (p270)
        Add, Sub, Mul, Div,    /* DivUn, */    Rem,    /* RemUn, */    Neg,

        // Bitwise Operations (p272)
        And, Or, Xor, Not,

        // Logical Condition Check Instructions (p275)
        Ceq, Cgt,    /* Cgt_Un, */   Clt,    /* Clt_Un, */   /* Ckfinite, */

        // Branching Instructions
        Br,
        BrFalse,

        Call,
        Load,
        LoadA,
        LdElem,
        LoadArrayArray,     // call Array.Get
        LoadElemRef,
        NewArraryVector,    // newarr
        NewArraryArray,     // newobj
        NewObj,
        StoreArrayArray,    // call Array.Set
        StElem,
        Store,
        Pop,
        PutLabel,
        PlaceHolder
    }

    public class IMRGenerator : List<Func<string>>
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

        static public string AssemblyExtern(Assembly a)
        {
            AssemblyName an = a.GetName();
            StringBuilder b = new StringBuilder();
            byte[] pkt;
            b.Append(".assembly extern ").Append(an.Name).Append(" {");

            if (an.Version != null)
                b.Append(".ver ").Append(an.Version.ToString().Replace(".", ":"));

            if ((pkt = an.GetPublicKeyToken()) != null && pkt.Length == 8)
            {
                b.Append(" .publickeytoken = (");

                b.Append(
                    string.Join(" "
                    , new List<byte>(pkt)
                        .ConvertAll<string>(delegate(byte by) { return by.ToString("X"); })
                        .ToArray()));

                //b.Append(SFList.Cast(FList<byte>.Parse(pkt)
                //    .Map<string>(delegate(byte by) { return by.ToString("X"); })
                //    ).Xsv(" "));
                b.Append(")");
            }

            b.Append("}");
            return b.ToString();
        }

        public Func<string> CallActn(Actn t)
        {
            return delegate()
            {
                if (t.IsConstructor) return S(OpCodes.Call) + " instance " + Body(t); ;
                if (t.IsStatic) return S(OpCodes.Call) + " " + Body(t);
                return S(OpCodes.Callvirt) + " instance " + Body(t);
            };
        }

        public static string Body(Actn fi)
        {
            StringBuilder b = new StringBuilder();
            //TypeInfo retti = fi.IsConstructor ? TypeInfo.Void : fi.ThisType;
            //Typ retti = fi is IValuable && fi.IsConstructor == false ? (fi as IValuable).Typ : null;
            Typ retti = fi is ITyped && fi.IsConstructor == false ? (fi as ITyped).Typ : null;
            b.Append(TypeFullName(retti));

            Typ m = fi.FindUpTypeOf<Typ>();
            //Mdl m = fi.GetActionOvrldHolder();
            //Mdl m = fi.FindUp(Mdl.IsMdl) as Mdl;
            if (m.GetType() == typeof(Typ))
            {
                Typ ti = m as Typ;
                //if (fi.IsConstructor && fi.NmdAbove.Name == "0base")
                //{ ti = ti.BaseTyp; }
                if (ti.IsGeneric)
                { b.Append(ti.IsValueType ? " value" : " class"); }
                b.Append(" ");
                b.Append(TypeFullName(ti));
                b.Append("::");
            }
            else
            {
                b.Append(" ");
            }

            //b.Append(fi._Name);
            if (MethodAttributes.SpecialName != (fi.MthdAttrs & MethodAttributes.SpecialName))
            {
                b.Append(fi.Family.Name);
            }
            else
            {
                b.Append(fi.SpecialName);
            }
            //if (fi.Family.GetType() == typeof(ActnOvld))
            //{
            //    b.Append(fi.Family.Name);
            //}
            //else if (fi.Family.GetType() == typeof(Prop2))
            //{
            //    b.Append(fi.SpecialName);
            //}
            //else
            //{
            //    throw new NotImplementedException();
            //}
            //b.Append(fi.FindUpTypeOf<ActnOvld>().Name);
            b.Append("(");
            Variable v;
            string s;
            List<Variable> Params = fi.FindAllTypeOf<Variable>();
            Params.RemoveAll(delegate(Variable vv)
            {
                return vv.VarKind != Variable.VariableKind.Param
                    && vv.VarKind != Variable.VariableKind.ParamGeneric;
            });
            if (Params.Count > 0)
            {
                v = Params[0];
                s = v.VarKind == Variable.VariableKind.ParamGeneric
                    ? "!" + v.GenericIndex : TypeLongForm(v.Typ);
                //s= TypeLongForm(v.Typ);
                b.Append(s);
                for (int i = 1; i < Params.Count; i++)
                {
                    b.Append(", ");
                    v = Params[i];
                    s = v.VarKind == Variable.VariableKind.ParamGeneric
                        ? "!" + v.GenericIndex : TypeLongForm(v.Typ);
                    //s = TypeLongForm(v.Typ);
                    b.Append(s);
                }
            }


            //List<Variable> Params = fi.GetParams();
            //if (Params.Count > 0)
            //{
            //    v = Params[0];
            //    s = v.VarKind == Variable.VariableKind.ParamGeneric
            //        ? "!" + v.GenericIndex : TypeLongForm(v.Typ);
            //    //s= TypeLongForm(v.Typ);
            //    b.Append(s);
            //    for (int i = 1; i < Params.Count; i++)
            //    {
            //        b.Append(", ");
            //        v = Params[i];
            //        s = v.VarKind == Variable.VariableKind.ParamGeneric
            //            ? "!" + v.GenericIndex : TypeLongForm(v.Typ);
            //        //s = TypeLongForm(v.Typ);
            //        b.Append(s);
            //    }
            //}
            b.Append(")");

            return b.ToString();
        }

        static public string S(OpCode c) { return c.ToString(); }
        static public string S(OpCode c, object opRnd) { return S(c) + " " + opRnd.ToString(); }
        static public bool IsSupportedType(Typ t)
        {
            return string.IsNullOrEmpty(SupportedTypeName(t)) == false;
        }

        static public string SupportedTypeName(Typ t)
        {
            if (t == null) { return "void"; }

            string brk = "";
            while (t.IsVectorOrArray)
            {
                brk = Typ.ToBracket(t.Dimension) + brk;
                t = t.ArrayType;
            }

            string nm = null;

            if (t.IsReferencing == false) { return null; }

            if (t.RefType == typeof(void)) nm = "void";
            if (t.RefType == typeof(bool)) nm = "bool";
            if (t.RefType == typeof(int)) nm = "int32";
            if (t.RefType == typeof(object)) nm = "object";
            if (t.RefType == typeof(string)) nm = "string";

            return nm + brk;
        }

        static public string TypeCharacter(Typ t)
        {
            if (IsSupportedType(t)) return "";
            return "class";
            //if (t.IsValueType()) return "valuetype";
            //else if (t.IsClass()) return "class";
            //else
            //{
            //    throw new InternalError("Can not specify the tyep character: " + t._Name);
            //}
        }

        static public string TypeLongForm(Typ t)
        {
            if (IsSupportedType(t)) return SupportedTypeName(t);
            return TypeCharacter(t) + " " + TypeFullName(t);
        }

        static public string TypeFullName(Typ t)
        {
            if (IsSupportedType(t)) return SupportedTypeName(t);

            StringBuilder b = new StringBuilder();
            b.Append("[").Append(t.AssemblyName).Append("]");
            b.Append(t._FullName);
            if (t.IsGeneric)
            {
                b.Append("<");
                b.Append(string.Join(", "
                    , new List<Typ>(t.GenericTypeParams)
                    .ConvertAll<string>(TypeFullName)
                    .ToArray()
                    ));
                b.Append(">");
            }

            return b.ToString();
        }

        public Func<string> Append(Func<string> f)
        {
            Add(f); return f;
        }

        public Func<string> Ret()
        {
            return Append(delegate() { return OpCodes.Ret.ToString(); });
        }

        public Func<string> Pop()
        {
            return Append(delegate() { return OpCodes.Pop.ToString(); });
        }

        public Func<string> Box(Typ typ)
        {
            return Append(delegate() { return S(OpCodes.Box, TypeLongForm(typ)); });
        }

        public Func<string> LoadLiteral(Literal l)
        {
            Debug.Assert(l != null && l.Typ != null && l.Typ.IsReferencing);
            Func<string> f = null;
            Typ t = l.Typ;
            if (t.RefType == typeof(bool))      /**/ f = delegate() { return ((bool)l.Value) ? S(OpCodes.Ldc_I4_1) : S(OpCodes.Ldc_I4_0); };
            if (t.RefType == typeof(string))    /**/ f = delegate() { return S(OpCodes.Ldstr, @"""" + l.Value + @""""); };
            if (t.RefType == typeof(int))       /**/ f = delegate() { return S(OpCodes.Ldc_I4, l.Value); };
            Debug.Assert(f != null);
            Add(f);
            return f;
        }

        public Func<string> LoadVariable(Variable v)
        {
            Func<string> f = null;
            Variable.VariableKind k = v.VarKind;
            switch (k)
            {
                case Variable.VariableKind.This: f = delegate() { return OpCodes.Ldarg_0.ToString(); }; break;
                case Variable.VariableKind.Param: f = delegate() { return S(OpCodes.Ldarg, v.Name); }; break;
                case Variable.VariableKind.Local: f = delegate() { return S(OpCodes.Ldloc, v.Name); }; break;
                case Variable.VariableKind.StaticField: f = delegate() { return S(OpCodes.Ldsfld, TypeLongForm(v.Typ) + " " + v.Name); }; break;
                case Variable.VariableKind.Vector: f = delegate() { return S(OpCodes.Ldelem, TypeLongForm(v.Typ)); }; break;
                default: Debug.Fail(""); break;
            }
            Add(f);
            return f;
        }

        public Func<string> NewArrayVector(Typ t)
        {
            Func<string> f = delegate() { return S(OpCodes.Newarr) + " " + TypeFullName(t); };
            Add(f);
            return f;
        }

        public Func<string> NewArrayArray(Typ t)
        {
            Func<string> f = delegate()
            {
                StringBuilder b = new StringBuilder();
                b.Append(S(OpCodes.Newobj))
                    .Append(" instance void ").Append(TypeFullName(t))
                    .Append("::.ctor(int32");
                for (int i = 1; i < t.Dimension; i++)
                { b.Append(", int32"); }
                b.Append(")");
                return b.ToString();
            };
            Add(f);
            return f;
        }

        public Func<string> LoadAVariable(Variable v)
        {
            Func<string> f = null;
            Variable.VariableKind k = v.VarKind;
            switch (k)
            {
                case Variable.VariableKind.Param:
                    //TODO  check 
                    if (v.Name == "0")
                    {
                        throw new NotImplementedException("cannot load callee site instance pointer");
                    }
                    f = delegate() { return S(OpCodes.Ldarga, v.Name); }; break;
                case Variable.VariableKind.Local: f = delegate() { return S(OpCodes.Ldloca, v.Name); }; break;
                case Variable.VariableKind.StaticField: f = delegate() { return S(OpCodes.Ldsflda, TypeLongForm(v.Typ) + " " + v.Name); }; break;
                default: Debug.Fail(""); break;
            }
            Add(f);
            return f;
        }

        public Func<string> StoreVariable(Variable v)
        {
            Func<string> f = null;
            Nana.Semantics.Variable.VariableKind k = v.VarKind;
            switch (k)
            {
                case Nana.Semantics.Variable.VariableKind.Param: f = delegate() { return S(OpCodes.Starg, v.Name); }; break;
                case Nana.Semantics.Variable.VariableKind.Local: f = delegate() { return S(OpCodes.Stloc, v.Name); }; break;
                case Nana.Semantics.Variable.VariableKind.StaticField: f = delegate() { return S(OpCodes.Stsfld, TypeLongForm(v.Typ) + " " + v.Name); }; break;
            }
            Debug.Assert(f != null);
            Add(f);
            return f;
        }

        public Func<string> LdElemTyp(Typ t)
        {
            Func<string> f = null;
            f = delegate() { return S(OpCodes.Ldelem) + " " + TypeLongForm(t); };
            Add(f);
            return f;
        }

        public Func<string> LoadArrayArray(Typ tp)
        {
            Func<string> f = null;
            f = delegate()
            {
                //call instance int32 int32[0...,0...]::Get(int32, int32)
                //TypeInfo tp = imr.OpRnd.ThisType;
                Debug.Assert(tp.IsArray);
                StringBuilder b = new StringBuilder();
                b.Append(S(OpCodes.Call))
                    .Append(" instance ")
                    .Append(TypeFullName(tp.ArrayType))
                    .Append(" ").Append(TypeFullName(tp))
                    .Append("::Get(int32")
                    ;
                for (int i = 1; i < tp.Dimension; i++)
                { b.Append(", int32"); }
                b.Append(")");
                return b.ToString();
            };
            Add(f);
            return f;
        }

        public Func<string> StoreArrayArray(Typ tp)
        {
            Func<string> f = null;
            f = delegate()
            {
                //call instance void int32[0...,0...]::Set(int32, int32, int32)
                //TypeInfo tp = imr.TypeArg;
                Debug.Assert(tp.IsArray);
                StringBuilder b = new StringBuilder();
                b.Append(S(OpCodes.Call))
                    .Append(" instance void ")
                    .Append(TypeFullName(tp))
                    .Append("::Set(int32")
                    ;
                for (int i = 1; i < tp.Dimension; i++)
                { b.Append(", int32"); }
                b.Append(", ").Append(TypeFullName(tp.ArrayType)).Append(")");
                return b.ToString();
            };
            Add(f);
            return f;
        }

        public Func<string> StElemTyp(Typ t)
        {
            Func<string> f = null;
            f = delegate() { return S(OpCodes.Stelem) + " " + TypeLongForm(t); };
            Add(f);
            return f;
        }

        public Func<string> NewObjActionSig(Actn t)
        {
            Func<string> f = null;
            f = delegate()
            {
                return S(OpCodes.Newobj) + " instance " + Body(t);
            };
            Add(f);
            return f;
        }

        public Func<string> CallActionSig(Actn t)
        {
            Func<string> f = null;
            f = delegate()
            {
                if (t.IsConstructor) return S(OpCodes.Call) + " instance " + Body(t); ;
                if (t.IsStatic) return S(OpCodes.Call) + " " + Body(t);
                return S(OpCodes.Callvirt) + " instance " + Body(t);
            };
            Add(f);
            return f;
        }

        public Func<string> Br(string label)
        {
            Func<string> f = delegate() { return S(OpCodes.Br) + " " + label; }; Add(f); return f;
        }
        public Func<string> BrFalse(string label)
        {
            Func<string> f = delegate() { return S(OpCodes.Brfalse) + " " + label; }; Add(f); return f;
        }
        public Func<string> PutLabel(string label)
        {
            Func<string> f = delegate() { return label + ":"; }; Add(f); return f;
        }

        public Func<string> Add() { Func<string> f = delegate() { return S(OpCodes.Add); }; Add(f); return f; }
        public Func<string> Sub() { Func<string> f = delegate() { return S(OpCodes.Sub); }; Add(f); return f; }
        public Func<string> Mul() { Func<string> f = delegate() { return S(OpCodes.Mul); }; Add(f); return f; }
        public Func<string> Div() { Func<string> f = delegate() { return S(OpCodes.Div); }; Add(f); return f; }
        public Func<string> Rem() { Func<string> f = delegate() { return S(OpCodes.Rem); }; Add(f); return f; }
        public Func<string> Neg() { Func<string> f = delegate() { return S(OpCodes.Neg); }; Add(f); return f; }
        public Func<string> And() { Func<string> f = delegate() { return S(OpCodes.And); }; Add(f); return f; }
        public Func<string> Or() { Func<string> f = delegate() { return S(OpCodes.Or); }; Add(f); return f; }
        public Func<string> Xor() { Func<string> f = delegate() { return S(OpCodes.Xor); }; Add(f); return f; }
        public Func<string> Not() { Func<string> f = delegate() { return S(OpCodes.Not); }; Add(f); return f; }
        public Func<string> Ceq() { Func<string> f = delegate() { return S(OpCodes.Ceq); }; Add(f); return f; }
        public Func<string> Cgt() { Func<string> f = delegate() { return S(OpCodes.Cgt); }; Add(f); return f; }
        public Func<string> Clt() { Func<string> f = delegate() { return S(OpCodes.Clt); }; Add(f); return f; }
    }

}
