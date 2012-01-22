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

        Ope,

        __SENTINEL__
    }

    public class IMR
    {
        public C C = C.None;
        public IMR() { }
        public IMR(C c) { C = c; }
        public IMR(C c, string v) : this(c) { StringV = v; }
        public IMR(C c, Typ vt, Actn va) : this(c) { TypV = vt; ActnV = va; }
        public IMR(C c, Literal v) : this(c) { LiteralV = v; }
        public IMR(C c, Variable v) : this(c) { VariableV = v; }
        public IMR(C c, Typ v) : this(c) { TypV = v; }
        public IMR(C c, Typ v, Typ v2) : this(c) { TypV = v; TypV2 = v2; }
        public IMR(C c, string s, Typ t) : this(c) { StringV = s; TypV = t; }

        public string StringV;
        public Actn ActnV;
        public Literal LiteralV;
        public Variable VariableV;
        public Typ TypV;
        public Typ TypV2;
    }

    public class IMRGenerator : List<IMR>
    {
        static public readonly string InstCons = ".ctor";
        static public readonly string StatCons = ".cctor";

        static public bool IsInstCons(string name) { return name == InstCons; }
        static public bool IsStatCons(string name) { return name == StatCons; }
        static public bool IsAnyCons(string name) { return IsInstCons(name) || IsStatCons(name); }

        public void GenerateIMR(App app)
        {
            Predicate<Nmd> pred = delegate(Nmd n)
            { return n.GetType() == typeof(Actn); };

            foreach (Actn a in app.FindDownAll(pred))
            {
                foreach (Sema x in a.Exes)
                { x.Exec(this); }
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

        public IMR NewObject(Typ t, Actn a) { return Append(new IMR(C.NewObject, t, a)); }

        public IMR CallAction(Typ t, Actn a) { return Append(new IMR(C.CallAction, t, a)); }

        public IMR Br(string label) { return Append(new IMR(C.Br, label)); }
        public IMR BrFalse(string label) { return Append(new IMR(C.BrFalse, label)); }
        public IMR PutLabel(string label) { return Append(new IMR(C.PutLabel, label)); }

        public IMR Ope(string sign, Typ t) { return Append(new IMR(C.Ope, sign, t)); }

    }

}
