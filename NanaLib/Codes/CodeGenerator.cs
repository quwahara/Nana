using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.IO;
using Nana.Delegates;
using Nana.Semantics;

namespace Nana.CodeGeneration
{
    public class CodeGenerator
    {
        public int IndentLength = 4;
        public char IndentChar = ' ';
        public string Indent { get { return "".PadRight(IndentLength, IndentChar); } }
        public int IndentDepth;

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
            h.Instructions.ForEach(delegate(Func<string> ff)
            {
                string s = ff();
                string ind = s.EndsWith(":") ? "" : GetCurrentIndent();
                b.Append(ind)
                    .Append(s)
                    .AppendLine();
            });
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

        public string BeginEnv(Env d)
        {
            StringBuilder b = new StringBuilder();
            d.TypeLdr.InAssembly.Assemblies
                .ConvertAll<string>(Nana.IMRs.IMRGenerator.AssemblyExtern)
                .ConvertAll<StringBuilder>(b.AppendLine);
            return b.ToString();
        }

        public string DeclareField(Variable v)
        {
            StringBuilder b = new StringBuilder();
            Func<string, StringBuilder> Tr = b.Append;
            Func<StringBuilder> Nl = b.AppendLine;
            Tr(".field static ");
            Tr(IMRs.IMRGenerator.TypeLongForm(v.Typ));
            Tr(" "); Tr(v.Name); Nl();
            return b.ToString();
        }

        public string BeginApp(App d)
        {
            StringBuilder b = new StringBuilder();
            b.Append(".assembly ")
                .Append(Path.GetFileNameWithoutExtension(d.Name))
                .Append(" { }")
                .AppendLine()
                .Append(".module ")
                .Append(d.Name)
                .AppendLine();
            //return ".assembly " + Path.GetFileNameWithoutExtension(d.Name) + " { }" + Environment.NewLine
            //    + ".module " + d.Name + Environment.NewLine;
            d.FindAllTypeOf<Variable>()
                .FindAll(delegate(Variable v) { return v.VarKind == Variable.VariableKind.StaticField; })
                .ConvertAll<StringBuilder>(delegate(Variable v) { return b.Append(DeclareField(v)); })
                ;
            //if (d.Instructions.Count > 0)
            //{
            //    b.AppendLine(".method static public void .cctor() {");
            //    IndentDepth += 1;
            //}

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
            { b.Append(" extends ").Append(Nana.IMRs.IMRGenerator.TypeFullName(bty)); }

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
            b.Append(" ").Append(Nana.IMRs.IMRGenerator.TypeFullName(returnType));
            b.Append(" ").Append(f.Family.Name);
            b.Append("(");

            b.Append(
                string.Join(", "
                , f.FindAllTypeIs<Variable>()
                .FindAll(delegate(Variable v) { return v.VarKind == Variable.VariableKind.Param; })
                .ConvertAll<string>(delegate(Variable v)
                { return Nana.IMRs.IMRGenerator.TypeFullName(v.Typ) + " " + v.Name; })
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
                Func<Typ, string> lf = Nana.IMRs.IMRGenerator.TypeLongForm;
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

        public Func<string> LoadVariable(Variable v)
        {
            Variable.VariableKind k = v.VarKind;
            switch (k)
            {
                case Variable.VariableKind.Param: return delegate() { return S(OpCodes.Ldarg, v.Name); };
                case Variable.VariableKind.This: return delegate() { return OpCodes.Ldarg_0.ToString(); };
                case Variable.VariableKind.Local: return delegate() { return S(OpCodes.Ldloc, v.Name); };
                //case Variable2.VariableKind.StaticField: return delegate() { return S(OpCodes.Ldsfld, TypeLongForm(v.Typ) + " " + v.Name); };
                //case Variable2.VariableKind.Vector: return delegate() { return S(OpCodes.Ldelem, TypeLongForm(v.Typ)); };
            }
            Debug.Fail("");
            return null;
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

    }

}
