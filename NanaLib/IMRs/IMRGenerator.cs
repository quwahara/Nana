using System;
using System.Collections.Generic;
using System.Text;
using Nana.Semantics;
using Nana.Delegates;
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

        public IMR Append(IMR imr) { Add(imr); return imr; }

        public static readonly IMR IMR_Ret = new IMR(C.Ret);
        public static readonly IMR IMR_Pop = new IMR(C.Pop);

        public IMR Ret() { return Append(IMR_Ret); }
        public IMR Pop() { return Append(IMR_Pop); }

        public IMR LoadLiteral(Literal l) { return Append(new IMR(C.LdLiteral, l)); }

        public IMR LoadVariable(Variable v)
        {
            IMR imr = new IMR(C.LdVariable);
            imr.VariableV = v;
            return Append(imr);
        }

        public IMR NewArray(Typ t){ return Append(new IMR(C.NewArray, t)); }

        public IMR LoadAVariable(Variable v) { return Append(new IMR(C.LdVariableA, v)); }

        public IMR StoreVariable(Variable v) { return Append(new IMR(C.StVariable, v)); }

        public IMR LdArrayElement(Typ t){ return Append(new IMR(C.LdArrayElement, t)); }

        public IMR StArrayElement(Typ t, Typ t2) { return Append(new IMR(C.StArrayElement, t, t2)); }

        public IMR NewObject(Actn a) { return Append(new IMR(C.NewObject, a)); }        

        public IMR CallAction(Actn a) { return Append(new IMR(C.CallAction, a)); }

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
    }

}
