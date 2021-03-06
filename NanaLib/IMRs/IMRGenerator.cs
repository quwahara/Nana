/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

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
        LdField,
        LdFunction,

        StArrayElement,
        StVariable,
        StField,
        
        NewObject,
        NewArray,

        CallFunction,

        Ope,

        Box,
        Unbox,

        CastNoisy,
        CastSilent,

        ConvNoisy,
        ConvSilent,

        Throw,
        Try,
        Catch,
        Finally,
        Leave,
        EndFinally,
        CloseTry,

        __SENTINEL__
    }

    public class IMR
    {
        public C C = C.None;
        public IMR() { }
        public IMR(C c) { C = c; }
        public IMR(C c, string v) : this(c) { StringV = v; }
        public IMR(C c, Typ vt, Fun vf) : this(c) { TypV = vt; FunV = vf; }
        public IMR(C c, Typ vt, Variable v) : this(c) { TypV = vt; VariableV = v; }
        public IMR(C c, Literal v) : this(c) { LiteralV = v; }
        public IMR(C c, Variable v) : this(c) { VariableV = v; }
        public IMR(C c, Typ v) : this(c) { TypV = v; }
        public IMR(C c, Typ v, Typ v2) : this(c) { TypV = v; TypV2 = v2; }
        public IMR(C c, string s, Typ t) : this(c) { StringV = s; TypV = t; }

        public string StringV;
        public Fun FunV;
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
            { return n.GetType() == typeof(Fun); };

            foreach (Fun f in app.AllFuns)
            {
                foreach (Sema x in f.Exes)
                { x.Exec(this); }
                f.IMRs.AddRange(this);
                Clear();
            }
        }

        public IMR Append(IMR imr) { Add(imr); return imr; }

        public static readonly IMR IMR_Ret = new IMR(C.Ret);
        public static readonly IMR IMR_Pop = new IMR(C.Pop);
        public static readonly IMR IMR_Throw = new IMR(C.Throw);
        public static readonly IMR IMR_Try = new IMR(C.Try);
        public static readonly IMR IMR_Finally = new IMR(C.Finally);
        public static readonly IMR IMR_EndFinally = new IMR(C.EndFinally);
        public static readonly IMR IMR_CloseTry = new IMR(C.CloseTry);

        public IMR Ret() { return Append(IMR_Ret); }
        public IMR Pop() { return Append(IMR_Pop); }
        public IMR Throw() { return Append(IMR_Throw); }
        public IMR Try() { return Append(IMR_Try); }
        public IMR Finally() { return Append(IMR_Finally); }
        public IMR EndFinally() { return Append(IMR_EndFinally); }
        public IMR CloseTry() { return Append(IMR_CloseTry); }

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

        public IMR NewObject(Typ t, Fun f) { return Append(new IMR(C.NewObject, t, f)); }

        public IMR CallFunction(Typ t, Fun f) { return Append(new IMR(C.CallFunction, t, f)); }

        public IMR Br(string label) { return Append(new IMR(C.Br, label)); }
        public IMR BrFalse(string label) { return Append(new IMR(C.BrFalse, label)); }
        public IMR PutLabel(string label) { return Append(new IMR(C.PutLabel, label)); }

        public IMR Ope(string sign, Typ t) { return Append(new IMR(C.Ope, sign, t)); }

        public IMR Box(Typ t) { return Append(new IMR(C.Box, t)); }

        public IMR CastNoisy(Typ fromTyp, Typ toTyp) { return Append(new IMR(C.CastNoisy, fromTyp, toTyp)); }
        public IMR CastSilent(Typ fromTyp, Typ toTyp) { return Append(new IMR(C.CastSilent, fromTyp, toTyp)); }

        public IMR ConvNoisy(Typ fromTyp, Typ toTyp) { return Append(new IMR(C.ConvNoisy, fromTyp, toTyp)); }
        public IMR ConvSilent(Typ fromTyp, Typ toTyp) { return Append(new IMR(C.ConvSilent, fromTyp, toTyp)); }

        public IMR Catch(Typ t) { return Append(new IMR(C.Catch, t)); }
        public IMR Leave(string label) { return Append(new IMR(C.Leave, label)); }


        public IMR LoadField(Typ t, Variable v) { return Append(new IMR(C.LdField, t, v)); }
        public IMR StoreField(Typ t, Variable v) { return Append(new IMR(C.StField, t, v)); }


        public IMR LoadFunction(Typ t, Fun f) { return Append(new IMR(C.LdFunction, t, f)); }


    }

}
