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

namespace Nana.CodeGeneration
{
    public class CodeGenerator
    {
        public int IndentLength = 4;
        public char IndentChar = ' ';
        public string Indent { get { return "".PadRight(IndentLength, IndentChar); } }
        public int IndentDepth;

        //public static List<string> ILASMKeywords = new List<FieldInfo>(typeof(OpCodes).GetFields())
        //        .ConvertAll<string>(delegate(FieldInfo f) { return f.Name.ToLower(); })
        //        .FindAll(delegate(string n) { return n.Contains("_") == false; })
        //        ;

        ///// <summary>
        ///// Quote ILASM keyword
        ///// </summary>
        ///// <param name="n"></param>
        ///// <returns></returns>
        //public static string Qk(string n)
        //{
        //    return ILASMKeywords.Contains(n)
        //        ? "'" + n + "'" : n;
        //}

        public string GetCurrentIndent(int more)
        {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < (IndentDepth + more); i++) b.Append(Indent);
            return b.ToString();
        }

        public string GetCurrentIndent()
        {
            return GetCurrentIndent(0);
        }

        public string GenerateCode(Nsp d)
        {
            StringBuilder b = new StringBuilder();
            
            b.Append(CallBegin(d));
            
            if (d is IInstructionsHolder)
            { b.Append(CallMiddle(d as IInstructionsHolder)); }
            
            d.FindAllTypeIs<Nsp>()
                .ConvertAll<string>(GenerateCode)
                .ConvertAll<StringBuilder>(b.Append);
            
            b.Append(CallEnd(d));
            return b.ToString();
        }

        public string CallMiddle(IInstructionsHolder h)
        {
            StringBuilder b = new StringBuilder();

            string[] extra;
            string s;
            foreach (IMR imr in h.Instructions)
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
            if (t == typeof(Actn)) { b.Append(BeginActn(n as Actn)); }
            if (t == typeof(Fctn)) { b.Append(BeginFctn(n as Fctn)); }
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
            if (t == typeof(Actn)) { b.Append(EndActn(n as Actn)); }
            if (t == typeof(Fctn)) { b.Append(EndFctn(n as Fctn)); }
            return b.ToString();
        }

        public string BeginNsp(Nsp d)
        {
            if (d.IsReferencing_) { return ""; }
            return "";
        }

        public string EndNsp(Nsp d)
        {
            if (d.IsReferencing_) { return ""; }
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

        public string DeclareField(Variable v)
        {
            StringBuilder b = new StringBuilder();
            Func<string, StringBuilder> Tr = b.Append;
            Func<StringBuilder> Nl = b.AppendLine;
            Tr(".field static ");
            Tr(TypeLongForm(v.Typ));
            //Tr(IMRs.IMRGenerator.TypeLongForm(v.Typ));
            Tr(" "); Tr(v.Name); Nl();
            return b.ToString();
        }

        public string BeginApp(App ap)
        {
            string name = "";
            if (ap.Env.Seed.Contains("@Root/@CompileOptions/@out"))
            {
                name = ap.Env.Seed.Find("@Root/@CompileOptions/@out")[0].Value;
            }
            else
            {
                name = ap.Name;
            }

            StringBuilder b = new StringBuilder();
            b.Append(".assembly ")
                .Append(Path.GetFileNameWithoutExtension(name))
                .Append(" { }")
                .AppendLine()
                .Append(".module ")
                .Append(name)
                .AppendLine();
            ap.FindAllTypeOf<Variable>()
                .FindAll(delegate(Variable v) { return v.VarKind == Variable.VariableKind.StaticField; })
                .ConvertAll<StringBuilder>(delegate(Variable v) { return b.Append(DeclareField(v)); })
                ;

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
            b.Append(" ").Append(d.Name);

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

        public string BeginFctn(Fctn f)
        {
            return BeginActn(f);
        }

        public string BeginActn(Actn f)
        {
            if (f.IsReferencing) { return ""; }

            StringBuilder b = new StringBuilder();

            string ind0 = GetCurrentIndent();
            string ind1 = GetCurrentIndent(1);
            string ind2 = GetCurrentIndent(2);

            Typ returnType = null;
            b.Append(ind0);
            b.Append(".method ");
            b.Append(FromMethodAttributes(f.MthdAttrs).ToLower());
            b.Append(" ").Append(TypeFullName(returnType));
            b.Append(" ").Append(f.Family.Name);
            b.Append("(");

            b.Append(
                string.Join(", "
                , f.FindAllTypeIs<Variable>()
                .FindAll(delegate(Variable v) { return v.VarKind == Variable.VariableKind.Param; })
                .ConvertAll<string>(delegate(Variable v)
                { return TypeFullName(v.Typ) + " " + v.Name; })
                .ToArray()
                )
                );

            b.Append(") {").AppendLine();

            if (f.IsEntryPoint) { b.Append(ind1).Append(".entrypoint").AppendLine(); }

            List<Variable> locals = f.FindAllLocalVariables();

            if (locals.Count >= 1)
            {
                b.Append(ind1).Append(".locals (").AppendLine();
                Variable v;
                Func<Typ, string> lf = TypeLongForm;
                v = locals[0];
                b.Append(ind2).Append(lf(v.Typ)).Append(" ").Append(v.Name).AppendLine();
                for (int i = 1; i < locals.Count; ++i)
                {
                    v = locals[i];
                    b.Append(ind2).Append(", ").Append(lf(v.Typ)).Append(" ").Append(v.Name).AppendLine();
                }
                b.Append(ind1).Append(")").AppendLine();
            }

            IndentDepth += 1;

            return b.ToString();
        }

        public string EndActn(Actn d)
        {
            if (d.IsReferencing) { return ""; }
            IndentDepth -= 1;
            return GetCurrentIndent() + "}" + Environment.NewLine;
        }

        public string EndFctn(Fctn d)
        {
            return EndActn(d);
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
                case C.NewObject: return NewObject(imr.ActnV);
                case C.CallAction: return CallAction(imr.ActnV);
                case C.Br: return Br(imr);
                case C.BrFalse: return BrFalse(imr);
                case C.PutLabel: return PutLabel(imr);
                case C.Ope: return Ope(imr, out extra);
            }
            throw new NotSupportedException();
        }

        public static string LoadLiteral(Literal l)
        {
            Typ t = l.Typ;
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
                case Variable.VariableKind.Param: return S(OpCodes.Ldarg, v.Name);
                case Variable.VariableKind.Local: return S(OpCodes.Ldloc, v.Name);
                case Variable.VariableKind.StaticField: return S(OpCodes.Ldsfld, TypeLongForm(v.Typ) + " " + v.Name);
                case Variable.VariableKind.Vector: return S(OpCodes.Ldelem, TypeLongForm(v.Typ));
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
                    return S(OpCodes.Ldarga, v.Name);
                case Variable.VariableKind.Local: return S(OpCodes.Ldloca, v.Name);
                case Variable.VariableKind.StaticField: return S(OpCodes.Ldsflda, TypeLongForm(v.Typ) + " " + v.Name);
            }
            throw new NotSupportedException();
        }

        public static string StoreVariable(Variable v)
        {
            Nana.Semantics.Variable.VariableKind k = v.VarKind;
            switch (k)
            {
                case Nana.Semantics.Variable.VariableKind.Param: return S(OpCodes.Starg, v.Name);
                case Nana.Semantics.Variable.VariableKind.Local: return S(OpCodes.Stloc, v.Name);
                case Nana.Semantics.Variable.VariableKind.StaticField: return S(OpCodes.Stsfld, TypeLongForm(v.Typ) + " " + v.Name);
            }
            throw new NotSupportedException();
        }

        public static string LdArrayElement(IMR imr)
        {
            Typ tp = imr.TypV;

            if (tp.IsVector)
            {
                return S(OpCodes.Ldelem) + " " + TypeLongForm(tp.ArrayType);
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
                return S(OpCodes.Stelem) + " " + TypeLongForm(imr.TypV2);
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

        public static string NewObject(Actn t)
        {
            return S(OpCodes.Newobj) + " instance " + Body(t);
        }

        public static string Body(Actn fi)
        {
            StringBuilder b = new StringBuilder();
            Typ retti = fi is ITyped && fi.IsConstructor == false ? (fi as ITyped).Typ : null;
            b.Append(TypeFullName(retti));

            Typ m = fi.FindUpTypeOf<Typ>();
            if (m.GetType() == typeof(Typ))
            {
                Typ ti = m as Typ;
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

            if (MethodAttributes.SpecialName != (fi.MthdAttrs & MethodAttributes.SpecialName))
            {
                b.Append(fi.Family.Name);
            }
            else
            {
                b.Append(fi.SpecialName);
            }

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

            b.Append(")");

            return b.ToString();
        }

        public static string CallAction(Actn t)
        {
            if (t.IsConstructor) return S(OpCodes.Call) + " instance " + Body(t); ;
            if (t.IsStatic) return S(OpCodes.Call) + " " + Body(t);
            return S(OpCodes.Callvirt) + " instance " + Body(t);
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
                case "<": return S(OpCodes.Clt);
                case ">": return S(OpCodes.Cgt);
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
