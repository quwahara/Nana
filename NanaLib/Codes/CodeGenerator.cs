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

namespace Nana.CodeGeneration
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

        static public string FromMethodAttributes(MethodAttributes atrs)
        {
            if (atrs == MethodAttributes.PrivateScope) return "PrivateScope";

            Array vs = Enum.GetValues(typeof(MethodAttributes));
            string[] ns = Enum.GetNames(typeof(MethodAttributes));
            MethodAttributes v = MethodAttributes.PrivateScope, vv;
            List<string> ls = new List<string>();
            for (int i = vs.Length - 1; i >= 0; i--)
            {
                vv = v;
                v = (MethodAttributes)vs.GetValue(i);
                if (v == MethodAttributes.PrivateScope || v == vv || (atrs & v) != v) continue;
                ls.Add(ns[i]);
                if ((int)v <= 7) break;
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
            if (t.RefType == typeof(object)) { return "object" + brk; }
            if (t.RefType == typeof(string)) { return "string" + brk; }

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
                    .ConvertAll<string>(TypeFullName)
                    .ToArray()
                    ));
                b.Append(">");
            }

            return b.ToString();
        }

        public string GenerateCode(Nsp d)
        {
            StringBuilder b = new StringBuilder();
            
            b.Append(CallBegin(d));
            
            if (d is Fun)
            { b.Append(CallMiddle(d as Fun)); }

            d.Members
                .FindAll(delegate(Nmd n)
                { return n is Nsp; })
                .ConvertAll<Nsp>(delegate(Nmd n)
                { return n as Nsp; })
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

        public string CallBegin(Nsp n)
        {
            StringBuilder b = new StringBuilder();
            Type t = n.GetType();
            if (t == typeof(Env)) { b.Append(BeginEnv(n as Env)); }
            if (t == typeof(App)) { b.Append(BeginApp(n as App)); }
            if (t == typeof(Nsp)) { b.Append(BeginNsp(n)); }
            if (t == typeof(Typ)) { b.Append(BeginTyp(n as Typ)); }
            if (t == typeof(Fun)) { b.Append(BeginActn(n as Fun)); }
            return b.ToString();
        }

        public string CallEnd(Nsp n)
        {
            StringBuilder b = new StringBuilder();
            Type t = n.GetType();
            if (t == typeof(Env)) { b.Append(EndEnv(n as Env)); }
            if (t == typeof(App)) { b.Append(EndApp(n as App)); }
            if (t == typeof(Nsp)) { b.Append(EndNsp(n)); }
            if (t == typeof(Typ)) { b.Append(EndTyp(n as Typ)); }
            if (t == typeof(Fun)) { b.Append(EndActn(n as Fun)); }
            return b.ToString();
        }

        public string BeginNsp(Nsp d)
        {
            if (d.IsReferencing) { return ""; }
            return "";
        }

        public string EndNsp(Nsp d)
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
            string name = "";
            if (ap.E.Seed.Contains("@CompileOptions/@out"))
            {
                name = ap.E.Seed.Find("@CompileOptions/@out").Value;
            }
            else
            {
                name = ap.Name;
            }

            StringBuilder b = new StringBuilder();
            b.Append(".assembly ")
                .Append(Qk(Path.GetFileNameWithoutExtension(name)))
                .Append(" { }")
                .AppendLine()
                .Append(".module ")
                .Append(name)
                .AppendLine();

            foreach (Variable v in ap.Vars)
            { b.Append(DeclareField(v)); }

            return b.ToString();
        }

        public string DeclareField(Variable v)
        {
            StringBuilder b = new StringBuilder();
            b.Append(".field static ")
                .Append(TypeNameInSig(v.Att.TypGet))
                .Append(" ")
                .Append(Qk(v.Name))
                .AppendLine();
            return b.ToString();
        }

        public string BeginTyp(Typ d)
        {
            if (d.IsReferencing || d.IsVectorOrArray) { return ""; }

            StringBuilder b = new StringBuilder();

            string accessibility = null;
            if ((d.TypAttributes & TypeAttributes.Public) == TypeAttributes.Public) accessibility = "public";

            b.Append(GetCurrentIndent());
            b.Append(".class");
            b.Append(" ").Append(accessibility);
            b.Append(" ").Append(Qk(d.Name));

            Typ bty;
            if ((bty = d.BaseTyp) != null && bty.RefType != typeof(object))
            { b.Append(" extends ").Append(TypeFullName(bty)); }

            b.Append(" {").AppendLine();

            IndentDepth += 1;

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

            Typ returnType = f.IsConstructor == false && f.Att.CanGet ? f.Att.TypGet : null;
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
                { return TypeFullName(v.Att.TypGet) + " " + Qk(v.Name); })
                .ToArray()
                )
                );

            b.Append(") {").AppendLine();

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
            }
            throw new NotSupportedException();
        }

        static public string S(OpCode c) { return c.ToString(); }
        static public string S(OpCode c, object opRnd) { return S(c) + " " + opRnd.ToString(); }

        public static string LoadLiteral(Literal l)
        {
            Typ t = l.Att.TypGet;
            if (t.RefType == typeof(bool))      /**/ return ((bool)l.Value) ? S(OpCodes.Ldc_I4_1) : S(OpCodes.Ldc_I4_0);
            if (t.RefType == typeof(string))    /**/ return S(OpCodes.Ldstr, @"""" + l.Value + @"""");
            if (t.RefType == typeof(int))       /**/ return S(OpCodes.Ldc_I4, l.Value);
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
                case Variable.VariableKind.StaticField: return S(OpCodes.Ldsfld, TypeNameInSig(v.Att.TypGet) + " " + Qk(v.Name));
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
                case Variable.VariableKind.StaticField: return S(OpCodes.Ldsflda, TypeNameInSig(v.Att.TypGet) + " " + Qk(v.Name));
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
                case Nana.Semantics.Variable.VariableKind.StaticField: return S(OpCodes.Stsfld, TypeNameInSig(v.Att.TypGet) + " " + Qk(v.Name));
            }
            throw new NotSupportedException();
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
            Typ retti = false == f.IsConstructor && f.Att.CanGet ? f.Att.TypGet : null;
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

            string nm;
            if (MethodAttributes.SpecialName != (f.MthdAttrs & MethodAttributes.SpecialName))
            {
                nm = f.Name;
            }
            else
            {
                nm = f.SpecialName;
            }
            b.Append(Qk(nm));

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

        public static string Ope(IMR imr, out string[] extra)
        {
            extra = null;
            switch (imr.StringV)
            {
                case "+": return S(OpCodes.Add);
                case "-": return S(OpCodes.Sub);
                case "*": return S(OpCodes.Mul);
                case "/": return S(OpCodes.Div);
                case "%": return S(OpCodes.Rem);
                case "==": return S(OpCodes.Ceq);
                case "!=": extra = new string[] { S(OpCodes.Ceq), S(OpCodes.Neg) }; return null;
                case "<":
                case "<_": return S(OpCodes.Clt);
                case ">":
                case ">_": return S(OpCodes.Cgt);
                case "<=": extra = new string[] { S(OpCodes.Cgt), S(OpCodes.Neg) }; return null;
                case ">=": extra = new string[] { S(OpCodes.Clt), S(OpCodes.Neg) }; return null;
                case "and": return S(OpCodes.And);
                case "or": return S(OpCodes.Or);
                case "xor": return S(OpCodes.Xor);
            }

            throw new NotSupportedException();
        }

    }


}
