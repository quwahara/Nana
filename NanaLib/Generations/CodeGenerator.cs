/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.IO;
using Nana.Delegates;
using Nana.Semantics;
using Nana.IMRs;
using System.Text.RegularExpressions;
using Nana.Infr;

namespace Nana.Generations
{
    public class CodeGenerator
    {
        public int IndentLength = 4;
        public char IndentChar = ' ';
        public string Indent { get { return "".PadRight(IndentLength, IndentChar); } }
        public int IndentDepth;

        public static List<string> ILASMKeywords = new List<FieldInfo>(typeof(OpCodes).GetFields())
                .ConvertAll<string>(delegate(FieldInfo f) { return f.Name.ToLower(); })
                .FindAll(delegate(string n) { return n.Contains("_") == false; })
                ;

        /// <summary>
        /// Quote ILASM keyword
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static string Qk(string n)
        {
            return ILASMKeywords.Exists(delegate(string s)
            {
                return Regex.IsMatch(n, @"(^|\.)" + s + @"($|\.)");
            })
                ? "'" + n + "'" : n;
        }

        public string GetCurrentIndent() { return GetCurrentIndent(0); }

        public string GetCurrentIndent(int more)
        {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < (IndentDepth + more); i++) b.Append(Indent);
            return b.ToString();
        }

        static public string FromTypeAttributes(TypeAttributes atrs)
        {
            List<string> ls = new List<string>(0);
            
            // Visibility flags
            switch (atrs & TypeAttributes.VisibilityMask)
            {
                case TypeAttributes.Public: ls.Add("public"); break;
                case TypeAttributes.NestedPublic: ls.Add("nested public"); break;
                case TypeAttributes.NestedPrivate: ls.Add("nested private"); break;
                case TypeAttributes.NestedFamily: ls.Add("nested family"); break;
                case TypeAttributes.NestedAssembly: ls.Add("nested assembly"); break;
                case TypeAttributes.NestedFamANDAssem: ls.Add("nested famandassem"); break;
                case TypeAttributes.NestedFamORAssem: ls.Add("nested famorassem"); break;
            }

            // Layout flags
            switch (atrs & TypeAttributes.LayoutMask)
            {
                case TypeAttributes.AutoLayout: /* default for the flag */ break;
                case TypeAttributes.SequentialLayout: ls.Add("sequential"); break;
                case TypeAttributes.ExplicitLayout: ls.Add("explicit"); break;
            }

            // Type semantics flags
            switch (atrs & ((TypeAttributes)0x000005A0L))
            {
                case TypeAttributes.Interface: ls.Add("interface"); break;
                case TypeAttributes.Abstract: ls.Add("abstract"); break;
                case TypeAttributes.Sealed: ls.Add("sealed"); break;
                case TypeAttributes.SpecialName: ls.Add("specialname"); break;
            }

            // Type implmentation flags
            switch (atrs & ((TypeAttributes)0x00103000L))
            {
                case TypeAttributes.Import: ls.Add("import"); break;
                case TypeAttributes.Serializable: ls.Add("serializable"); break;
                case TypeAttributes.BeforeFieldInit: ls.Add("beforefieldinit"); break;
            }

            // string formatting flags
            switch (atrs & TypeAttributes.StringFormatMask)
            {
                case TypeAttributes.AnsiClass: /* default for the flag */ break;
                case TypeAttributes.UnicodeClass: ls.Add("unicode"); break;
                case TypeAttributes.AutoClass: ls.Add("autochar"); break;
            }

            // reserved flags
            switch (atrs & TypeAttributes.ReservedMask)
            {
                case TypeAttributes.RTSpecialName: ls.Add("rtspecialname"); break;
            }

            //  remainders
            //Class
            //CustomFormatClass
            //CustomFormatMask
            //HasSecurity
            //NotPublic

            return string.Join(" ", ls.ToArray());
        }

        static public string FromMethodAttributes(MethodAttributes atrs)
        {
            List<string> ls = new List<string>(0);

            //Accessibility flags
            switch (atrs & MethodAttributes.MemberAccessMask)
            {
                case MethodAttributes.PrivateScope: ls.Add("privatescope"); break;
                case MethodAttributes.Private: ls.Add("private"); break;
                case MethodAttributes.FamANDAssem: ls.Add("famandassem"); break;
                case MethodAttributes.Assembly: ls.Add("assembly"); break;
                case MethodAttributes.Family: ls.Add("family"); break;
                case MethodAttributes.FamORAssem: ls.Add("famorassem"); break;
                case MethodAttributes.Public: ls.Add("public"); break;
            }

            //Contruct flags
            switch (atrs & ((MethodAttributes)0x00F0))
            {
                case MethodAttributes.Static: ls.Add("static"); break;
                case MethodAttributes.Final: ls.Add("final"); break;
                case MethodAttributes.Virtual: ls.Add("virtual"); break;
                case MethodAttributes.HideBySig: ls.Add("hidebysig"); break;
            }

            //Virtual method table (v-table) cotrol flags
            switch (atrs & ((MethodAttributes)0x0300))
            {
                case MethodAttributes.NewSlot: ls.Add("newslot"); break;
                case MethodAttributes.CheckAccessOnOverride: ls.Add("strict"); break;
            }

            //Implimentation flags
            switch (atrs & ((MethodAttributes)0x2C08))
            {
                case MethodAttributes.Abstract: ls.Add("abstract"); break;
                case MethodAttributes.SpecialName: ls.Add("specialname"); break;
                case MethodAttributes.PinvokeImpl: ls.Add("pinvokeimpl"); break;
                case MethodAttributes.UnmanagedExport: ls.Add("unmanagedexp"); break;
            }

            //Reserved flags
            switch (atrs & ((MethodAttributes)0xD000))
            {
                case MethodAttributes.RTSpecialName: ls.Add("rtspecialname"); break;
                case MethodAttributes.RequireSecObject: ls.Add("reqsecobj"); break;
            }

            return string.Join(" ", ls.ToArray());
        }

        static public string FromMethodImplAttributes(MethodImplAttributes atrs)
        {
            List<string> ls = new List<string>(0);

            //Code Implementation Masks
            switch (atrs & MethodImplAttributes.CodeTypeMask)
            {
                case MethodImplAttributes.IL: /* default for the flag */ break;
                case MethodImplAttributes.Native: ls.Add("native"); break;
                case MethodImplAttributes.OPTIL: ls.Add("optil"); break;
                case MethodImplAttributes.Runtime: ls.Add("runtime"); break;
            }
            //Managed Masks
            switch (atrs & MethodImplAttributes.ManagedMask)
            {
                case MethodImplAttributes.Unmanaged: ls.Add("unmanaged"); break;
                case MethodImplAttributes.Managed: /* default for the flag */ break;
            }
            //Implementation Information and Interop Masks
            switch (atrs & ((MethodImplAttributes)0x10D8))
            {
                case MethodImplAttributes.ForwardRef: ls.Add("forwardref"); break;
                case MethodImplAttributes.PreserveSig: ls.Add("preservesig"); break;
                case MethodImplAttributes.InternalCall: ls.Add("internalcall"); break;
                case MethodImplAttributes.Synchronized: ls.Add("synchronized"); break;
                case MethodImplAttributes.NoInlining: ls.Add("noinlining"); break;
            }

            return string.Join(" ", ls.ToArray());
        }

        static public string TypeNameInSig(Typ t)
        {
            return TypeNameILSupported(t)
                ?? TypeCharacter(t) + TypeNameGeneral(t)
                ;
        }

        static public string TypeCharacter(Typ t)
        {
            if (t.IsValueType) { return "valuetype "; }
            return "class ";
        }

        static public string TypeFullName(Typ t)
        {
            return TypeNameILSupported(t)
                ?? TypeNameGeneral(t)
                ;
        }

        static public string TypeNameILSupported(Typ t)
        {
            if (t == null) { return "void"; }

            string brk = "";
            while (t.IsVectorOrArray)
            {
                brk = ToArrayBracket(t.Dimension) + brk;
                t = t.ArrayType;
            }

            if (t.IsReferencing == false) { return null; }

            if (t.RefType == typeof(void)) { return "void" + brk; }
            if (t.RefType == typeof(bool)) { return "bool" + brk; }
            if (t.RefType == typeof(int)) { return "int32" + brk; }
            if (t.RefType == typeof(uint)) { return "uint32" + brk; }
            if (t.RefType == typeof(long)) { return "int64" + brk; }
            if (t.RefType == typeof(ulong)) { return "uint64" + brk; }
            if (t.RefType == typeof(object)) { return "object" + brk; }
            if (t.RefType == typeof(string)) { return "string" + brk; }
            if (t.RefType == typeof(IntPtr)) { return "native int" + brk; }
            if (t.RefType == typeof(UIntPtr)) { return "native unsigned int" + brk; }

            return null;
        }

        static public string ToArrayBracket(int dimension)
        {
            string s = "[";
            if (dimension >= 2) s += "0...";
            for (int i = 2; i <= dimension; i++) s += ",0...";
            s += "]";
            return s;
        }

        static public string TypeNameGeneral(Typ t)
        {
            StringBuilder b = new StringBuilder();
            
            b.Append("[").Append(t.AssemblyName).Append("]");
            b.Append(Qk(t._FullName));
            if (t.IsGeneric)
            {
                b.Append("<");
                b.Append(string.Join(", "
                    , new List<Typ>(t.GenericTypeParams)
                    .ConvertAll<string>(TypeNameInSig)
                    //.ConvertAll<string>(TypeFullName)
                    .ToArray()
                    ));
                b.Append(">");
            }

            return b.ToString();
        }

        public string GenerateCode(Blk d)
        {
            StringBuilder b = new StringBuilder();
            
            b.Append(CallBegin(d));
            
            if (d is Fun)
            { b.Append(CallMiddle(d as Fun)); }

            d.Members
                .FindAll(delegate(Nmd n)
                { return n is Blk; })
                .ConvertAll<Blk>(delegate(Nmd n)
                { return n as Blk; })
                .ConvertAll<string>(GenerateCode)
                .ConvertAll<StringBuilder>(b.Append);
            
            b.Append(CallEnd(d));
            return b.ToString();
        }

        public string CallMiddle(Fun f)
        {
            StringBuilder b = new StringBuilder();

            string[] extra;
            string s;
            foreach (IMR imr in f.IMRs)
            {
                s = FromIMR(imr, out extra);
                if (false == string.IsNullOrEmpty(s))
                {
                    b.Append(GenLine(s));
                }
                if (extra != null)
                {
                    foreach (string t in extra)
                    {
                        b.Append(GenLine(t));
                    }
                }
            }

            return b.ToString();
        }

        public string GenLine(string s)
        {
            StringBuilder b = new StringBuilder();
            string ind = s.EndsWith(":") ? "" : GetCurrentIndent();
            b.Append(ind).Append(s).AppendLine();
            return b.ToString();
        }

        public string CallBegin(Blk n)
        {
            StringBuilder b = new StringBuilder();
            Type t = n.GetType();
            if (t == typeof(Env)) { b.Append(BeginEnv(n as Env)); }
            if (t == typeof(App)) { b.Append(BeginApp(n as App)); }
            if (t == typeof(Blk)) { b.Append(BeginNsp(n)); }
            if (t == typeof(Typ)) { b.Append(BeginTyp(n as Typ)); }
            if (t == typeof(Fun)) { b.Append(BeginActn(n as Fun)); }
            return b.ToString();
        }

        public string CallEnd(Blk n)
        {
            StringBuilder b = new StringBuilder();
            Type t = n.GetType();
            if (t == typeof(Env)) { b.Append(EndEnv(n as Env)); }
            if (t == typeof(App)) { b.Append(EndApp(n as App)); }
            if (t == typeof(Blk)) { b.Append(EndNsp(n)); }
            if (t == typeof(Typ)) { b.Append(EndTyp(n as Typ)); }
            if (t == typeof(Fun)) { b.Append(EndActn(n as Fun)); }
            return b.ToString();
        }

        public string BeginNsp(Blk d)
        {
            if (d.IsReferencing) { return ""; }
            return "";
        }

        public string EndNsp(Blk d)
        {
            if (d.IsReferencing) { return ""; }
            return "";
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

                b.Append(")");
            }

            b.Append("}");
            return b.ToString();
        }

        public string BeginEnv(Env d)
        {
            StringBuilder b = new StringBuilder();
            d.TypeLdr.InAssembly.Assemblies
                .ConvertAll<string>(AssemblyExtern)
                .ConvertAll<StringBuilder>(b.AppendLine);
            return b.ToString();
        }

        public string BeginApp(App ap)
        {
            StringBuilder b = new StringBuilder();
            b.Append(".assembly ")
                .Append(Qk(ap.AssemblyName))
                .Append(" { }")
                .AppendLine()
                .Append(".module ")
                .Append(Qk(ap.ModuleName))
                .AppendLine();

            foreach (Variable v in ap.Vars)
            { b.Append(DeclareFieldStatic(v)); }

            return b.ToString();
        }

        public string DeclareFieldStatic(Variable v)
        {
            return DeclareField(v, /*isStatic=*/ true);
        }

        public string DeclareFieldInstance(Variable v)
        {
            return DeclareField(v, /*isStatic=*/ false);
        }

        public string DeclareField(Variable v, bool isStatic)
        {
            StringBuilder b = new StringBuilder();
            b.Append(".field ");
            if (isStatic)
            { b.Append("static "); }
            b.Append(TypeNameInSig(v.Att.TypGet))
                .Append(" ")
                .Append(Qk(v.Name))
                .AppendLine();
            return b.ToString();
        }

        public string BeginTyp(Typ d)
        {
            if (d.IsReferencing || d.IsVectorOrArray) { return ""; }

            StringBuilder b = new StringBuilder();

            b.Append(GetCurrentIndent());
            b.Append(".class");
            b.Append(" ").Append(FromTypeAttributes(d.TypAttributes));
            b.Append(" ").Append(Qk(d.Name));

            Typ bty;
            if ((bty = d.BaseTyp) != null && bty.RefType != typeof(object))
            { b.Append(" extends ").Append(TypeFullName(bty)); }

            b.Append(" {").AppendLine();

            IndentDepth += 1;

            string ind = GetCurrentIndent();
            foreach (Variable v in d.Flds)
            { b.Append(ind).Append(DeclareFieldInstance(v)); }

            return b.ToString();
        }

        public string EndEnv(Env d) { return ""; }

        public string EndApp(App d) { return ""; }

        public string EndTyp(Typ d)
        {
            if (d.IsReferencing || d.IsVectorOrArray) { return ""; }
            IndentDepth -= 1;
            return GetCurrentIndent() + "}" + Environment.NewLine;
        }

        public string BeginActn(Fun f)
        {
            if (f.IsReferencing) { return ""; }

            StringBuilder b = new StringBuilder();

            string ind0 = GetCurrentIndent();
            string ind1 = GetCurrentIndent(1);
            string ind2 = GetCurrentIndent(2);

            Typ returnType = f.IsConstructor ? null : f.ReturnTyp;
            b.Append(ind0);
            b.Append(".method ");
            b.Append(FromMethodAttributes(f.MthdAttrs).ToLower());
            b.Append(" ").Append(TypeFullName(returnType));
            b.Append(" ").Append(Qk(f.Name));
            b.Append("(");

            b.Append(
                string.Join(", "
                , f.Params
                .ConvertAll<string>(delegate(Variable v)
                { return TypeNameInSig(v.Att.TypGet) + " " + Qk(v.Name); })
                .ToArray()
                )
                );

            b.Append(")");
            string impattrs = FromMethodImplAttributes(f.ImplAttrs);
            if (impattrs.Length > 0)
            { b.Append(' ').Append(impattrs); }
            b.Append(" {").AppendLine();

            if (f.Customs != null)
            {
                foreach (Custom c in f.Customs)
                { b.Append(ind1).Append(DeclCustom(c)).AppendLine(); }
            }

            if (f.IsEntryPoint) { b.Append(ind1).Append(".entrypoint").AppendLine(); }

            List<Variable> vars = new List<Variable>(f.Vars)
                .FindAll(delegate(Variable v)
                { return v.VarKind != Variable.VariableKind.This; })
                ;

            if (vars.Count > 0)
            {
                b.Append(ind1).Append(".locals (").AppendLine();
                Func<Typ, string> lf = TypeNameInSig;
                b.Append(ind2);
                b.Append(
                    string.Join(Environment.NewLine  + ind2 + ", "
                    , vars
                    .ConvertAll<string>(delegate(Variable v)
                    { return lf(v.Att.TypGet) + " " + v.Name; })
                    .ToArray()
                    )
                    );
                b.AppendLine();
                b.Append(ind1).Append(")").AppendLine();
            }

            IndentDepth += 1;

            return b.ToString();
        }

        public string DeclCustom(Custom c)
        {
            Fun ctor = c.Callee;
            StringBuilder b = new StringBuilder();
            b.Append(".custom ").Append("instance void ")
                .Append(TypeFullName(c.CalleeTy))
                .Append("::").Append(ctor.Name)
                .Append("(")
                ;
            b.Append(
                string.Join(", "
                , ctor.Params
                .ConvertAll<string>(delegate(Variable p)
                { return TypeFullName(p.Att.TypGet); }).ToArray()
                ));
            b.Append(")");

            //TODO  implement value part

            return b.ToString();
        }

        public string EndActn(Fun f)
        {
            if (f.IsReferencing) { return ""; }
            IndentDepth -= 1;
            return GetCurrentIndent() + "}" + Environment.NewLine;
        }

        public static string FromIMR(IMR imr, out string[] extra)
        {
            extra = null;
            switch (imr.C)
            {
                case C.Ret: return OpCodes.Ret.ToString();
                case C.Pop: return OpCodes.Pop.ToString();
                case C.LdLiteral: return LoadLiteral(imr.LiteralV);
                case C.LdVariable: return LoadVariable(imr.VariableV);
                case C.NewArray: return NewArray(imr.TypV);
                case C.LdVariableA: return LoadAVariable(imr.VariableV);
                case C.StVariable: return StoreVariable(imr.VariableV);
                case C.LdArrayElement: return LdArrayElement(imr);
                case C.StArrayElement: return StArrayElement(imr);
                case C.NewObject: return NewObject(imr.TypV, imr.FunV);
                case C.CallFunction: return CallAction(imr.TypV, imr.FunV);
                case C.Br: return Br(imr);
                case C.BrFalse: return BrFalse(imr);
                case C.PutLabel: return PutLabel(imr);
                case C.Ope: return Ope(imr, out extra);
                case C.Box: return Box(imr);
                case C.Throw: return OpCodes.Throw.ToString();
                case C.Try: return ".try {";
                case C.Catch: return Catch(imr.TypV);
                case C.Finally: return "} finally {";
                case C.CloseTry: return "}";
                case C.Leave: return OpCodes.Leave.ToString() + " " + imr.StringV;
                case C.EndFinally: return OpCodes.Endfinally.ToString();
                case C.LdField: return LoadField(imr.TypV, imr.VariableV);
                case C.StField: return StoreField(imr.TypV, imr.VariableV);
                case C.LdFunction: return LoadFunction(imr);
                case C.CastNoisy: return CastNoisy(imr);
                case C.CastSilent: return CastSilent(imr);
            }
            throw new NotSupportedException();
        }

        public static string LoadFunction(IMR imr)
        {
            return S(OpCodes.Ldftn) + " instance " + Body(imr.TypV, imr.FunV);
        }

        static public string S(OpCode c) { return c.ToString(); }
        static public string S(OpCode c, object opRnd) { return S(c) + " " + opRnd.ToString(); }

        public static string Catch(Typ t)
        {
            return "} catch " + TypeFullName(t) + " {";
        }

        public static string LoadLiteral(Literal l)
        {
            if (null == l.Value)                /**/ { return S(OpCodes.Ldnull); }

            Typ t = l.Att.TypGet;
            if (t.RefType == typeof(bool))      /**/ return ((bool)l.Value) ? S(OpCodes.Ldc_I4_1) : S(OpCodes.Ldc_I4_0);
            if (t.RefType == typeof(string))    /**/ return S(OpCodes.Ldstr, @"""" + l.Value + @"""");
            if (t.RefType == typeof(int))       /**/ return S(OpCodes.Ldc_I4, l.Value);
            if (t.RefType == typeof(uint))      /**/ return S(OpCodes.Ldc_I4, l.Value);
            if (t.RefType == typeof(long))      /**/ return S(OpCodes.Ldc_I8, l.Value);
            if (t.RefType == typeof(ulong))     /**/ return S(OpCodes.Ldc_I8, l.Value);
            throw new NotSupportedException();
        }

        public static string LoadVariable(Variable v)
        {
            Variable.VariableKind k = v.VarKind;
            switch (k)
            {
                case Variable.VariableKind.This: return OpCodes.Ldarg_0.ToString();
                case Variable.VariableKind.Param: return S(OpCodes.Ldarg, Qk(v.Name));
                case Variable.VariableKind.Local: return S(OpCodes.Ldloc, Qk(v.Name));
                case Variable.VariableKind.Vector: return S(OpCodes.Ldelem, TypeNameInSig(v.Att.TypGet));
            }
            throw new NotSupportedException();
        }

        public static string NewArray(Typ t)
        {
            if (t.IsVector)
            {
                return S(OpCodes.Newarr) + " " + TypeFullName(t.ArrayType);
            }
            else
            {
                StringBuilder b = new StringBuilder();
                b.Append(S(OpCodes.Newobj))
                    .Append(" instance void ").Append(TypeFullName(t))
                    .Append("::.ctor(int32");
                for (int i = 1; i < t.Dimension; i++)
                { b.Append(", int32"); }
                b.Append(")");
                return b.ToString();
            }
        }

        public static string LoadAVariable(Variable v)
        {
            Variable.VariableKind k = v.VarKind;
            switch (k)
            {
                case Variable.VariableKind.Param:
                    //TODO  check 
                    if (v.Name == "0")
                    {
                        throw new NotImplementedException("cannot load callee site instance pointer");
                    }
                    return S(OpCodes.Ldarga, Qk(v.Name));
                case Variable.VariableKind.Local: return S(OpCodes.Ldloca, Qk(v.Name));
                case Variable.VariableKind.Field: return S(v.Att.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, TypeNameInSig(v.Att.TypGet) + " " + Qk(v.Name));
            }
            throw new NotSupportedException();
        }

        public static string StoreVariable(Variable v)
        {
            Nana.Semantics.Variable.VariableKind k = v.VarKind;
            switch (k)
            {
                case Nana.Semantics.Variable.VariableKind.Param: return S(OpCodes.Starg, Qk(v.Name));
                case Nana.Semantics.Variable.VariableKind.Local: return S(OpCodes.Stloc, Qk(v.Name));
            }
            throw new NotSupportedException();
        }

        public static string StoreField(Typ t, Variable v)
        {
            Attr att = v.Att;
            OpCode op = att.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld;
            string pre = t == null ? "" : (TypeFullName(t) + "::");
            return S(op, TypeNameInSig(att.TypGet) + " " + pre + Qk(v.Name));
        }

        public static string LoadField(Typ t, Variable v)
        {
            Attr att = v.Att;
            OpCode op = att.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld;
            string pre = t == null ? "" : (TypeFullName(t) + "::");
            return S(op, TypeNameInSig(att.TypGet) + " " + pre + Qk(v.Name));
        }

        public static string LdArrayElement(IMR imr)
        {
            Typ tp = imr.TypV;

            if (tp.IsVector)
            {
                return S(OpCodes.Ldelem) + " " + TypeNameInSig(tp.ArrayType);
            }
            else if (tp.IsArray)
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
            }
            throw new NotSupportedException();
        }

        public static string StArrayElement(IMR imr)
        {
            if (imr.TypV.IsVector)
            {
                return S(OpCodes.Stelem) + " " + TypeNameInSig(imr.TypV2);
            }
            else if (imr.TypV.IsArray)
            {
                Typ tp = imr.TypV2;
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
            }
            throw new NotSupportedException();
        }

        public static string NewObject(Typ t, Fun f)
        {
            return S(OpCodes.Newobj) + " instance " + Body(t, f);
        }

        public static string CallAction(Typ t, Fun f)
        {
            if (f.IsConstructor) return S(OpCodes.Call) + " instance " + Body(t, f); ;
            if (f.IsStatic) return S(OpCodes.Call) + " " + Body(t, f);
            return S(OpCodes.Callvirt) + " instance " + Body(t, f);
        }

        public static string Body(Typ t, Fun f)
        {
            StringBuilder b = new StringBuilder();
            Typ retti = false == f.IsConstructor ? f.ReturnTyp : null;
            b.Append(TypeNameInSig(retti));

            if (t != null && t.GetType() == typeof(Typ))
            {
                Typ ti = t as Typ;
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

            b.Append(Qk(f.Name));
            b.Append("(");
            List<Variable> prms = f.Params;
            if (prms.Count > 0)
            {
                b.Append(
                    string.Join(", "
                    , prms.ConvertAll<string>(delegate(Variable v)
                    {
                        return v.VarKind == Variable.VariableKind.ParamGeneric
                            ? "!" + v.GenericIndex : TypeNameInSig(v.Att.TypGet);
                    })
                    .ToArray()
                    ));
            }
            b.Append(")");

            return b.ToString();
        }

        public static string Br(IMR imr)
        {
            string label = imr.StringV;
            return S(OpCodes.Br) + " " + label;
        }

        public static string BrFalse(IMR imr)
        {
            string label = imr.StringV;
            return S(OpCodes.Brfalse) + " " + label;
        }

        public static string PutLabel(IMR imr)
        {
            string label = imr.StringV;
            return label + ":";
        }

        public static string CastNoisy(IMR imr)
        {
            string s = S(OpCodes.Castclass) + " " + TypeFullName(imr.TypV);
            return s;
        }

        public static string CastSilent(IMR imr)
        {
            string s = "// silent cast to " + TypeFullName(imr.TypV);
            return s;
        }

        public static string Box(IMR imr)
        {
            string s = S(OpCodes.Box) + " " + TypeFullName(imr.TypV);
            return s;
        }

        public static string Ope(IMR imr, out string[] extra)
        {
            extra = null;
            switch (imr.StringV)
            {
                case "op_Addition":             /**/ return S(OpCodes.Add);
                case "op_Subtraction":          /**/ return S(OpCodes.Sub);
                case "op_Multiply":             /**/ return S(OpCodes.Mul);
                case "op_Division":             /**/ return S(OpCodes.Div);
                case "op_Modulus":              /**/ return S(OpCodes.Rem);
                case "op_Equality":             /**/ return S(OpCodes.Ceq);
                case "op_Inequality":           /**/ extra = new string[] { S(OpCodes.Ceq), S(OpCodes.Ldc_I4_0), S(OpCodes.Ceq) }; return null;
                case "op_LessThan":             /**/ return S(OpCodes.Clt);
                case "op_GreaterThan":          /**/ return S(OpCodes.Cgt);
                case "op_LessThanOrEqual":      /**/ extra = new string[] { S(OpCodes.Cgt), S(OpCodes.Ldc_I4_0), S(OpCodes.Ceq) }; return null;
                case "op_GreaterThanOrEqual":   /**/ extra = new string[] { S(OpCodes.Clt), S(OpCodes.Ldc_I4_0), S(OpCodes.Ceq) }; return null;
                case "op_And":                  /**/ return S(OpCodes.And);
                case "op_Or":                   /**/ return S(OpCodes.Or);
                case "op_Xor":                  /**/ return S(OpCodes.Xor);
                case "op_UnaryNegation":        /**/ return S(OpCodes.Neg);
            }

            throw new InternalError(string.Format("The IMR operator '{0}' is not supported", new object[] { imr.StringV }));
        }

    }


}
