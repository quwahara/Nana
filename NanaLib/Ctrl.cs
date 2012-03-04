using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Nana.CodeGeneration;
using Nana.Delegates;
using Nana.Infr;
using Nana.Tokens;
using Nana.IMRs;
using Nana.Semantics;
using Nana.Syntaxes;
using Nana.ILASM;



//  support reserved words by msil as id
//  attribute を実装する
//  関数の戻り値を実装する
//  単項演算子を実装する
//  SyntaxAnalyzer の単体テストを、各Analyzerクラスのメソッド単位で実施できるようにする
//  Tokenの簡単な生成方法を模索する。各Analyzerクラスのメソッド単位で実施できるようにするための助け
//  エラーを表す例外に SemanticErrorを追加する。現状はすべてSyntaxエラーにしてしまっているため
//  仕様を文書化する。公開を目指すため
//  Ctyで車輪の再発明な処理をなくす
//  TODO don't match '`' only.

namespace Nana
{
    public class Ctrl
    {
        public Action<string> StdOut = Console.Write;
        public Action<string> StdErr = Console.Error.Write;

        public Action<Token> AfterSyntaxAnalyze = delegate(Token root) { };                     //  place holder
        public Action<Token, Env> AfterSemanticAnalyze = delegate(Token root, Env env) { };     //  place holder

        public int StartCompile(string[] args)
        {
            UTF8Encoding utf8 = new UTF8Encoding(false /* no byte order mark */);
            Action<string> prt = delegate(string s) { StdOut(s + Environment.NewLine); };
            string nl = Environment.NewLine;
            string ilpath;
            string code;
            Token root = null;
            try
            {
                root = CmdLnArgs.GetCmdLnArgs(args);
                root.Group = "Root";

                if (args.Length == 0 || root.Contains("@CompileOptions/@help"))
                {
                    foreach (string opt in CmdLnArgs.Options)
                    { StdErr(opt + nl); }
                    return 0;
                }

                if (root.Contains("@CompileOptions/@verbose"))
                {
                    prt("Specified options:");
                    foreach (Token t in root.Find("@CompileOptions").Follows)
                    { prt(t.Group + (string.IsNullOrEmpty(t.Value) ? "" : ":" + t.Value)); }
                }

                Ctrl.Check(root);
                Ctrl c = new Ctrl();

                if (root.Contains("@CompileOptions/@xxxsyntax"))
                {
                    c.AfterSyntaxAnalyze = delegate(Token root_)
                    {
                        foreach (Token t in root_.Select("@Syntax/0Source"))
                        {
                            StdOut(TokenEx.ToTree(t));
                        }
                    };
                }

                c.Compile(root);

                if (root.Contains("@CompileOptions/@xxxil"))
                {
                    StdOut(root.Find("@Code").Value);
                }

                ilpath = root.Find("@CompileOptions/@out").Value;
                ilpath = Path.ChangeExtension(ilpath, ".il");
                code = root.Find("@Code").Value;
                File.WriteAllText(ilpath, code, utf8);

                ILASMRunner r = new ILASMRunner();
                r.DetectILASM();
                r.Run(ilpath);
                return 0;
            }
            catch (Exception e)
            {
                StdErr("Error:" + nl);
                StringBuilder b = new StringBuilder();
                if (e is SemanticError)
                {
                    SemanticError se = e as SemanticError;
                    b.Append(se.Path)
                        .Append(":").Append(se.Row).Append(",").Append(se.Col)
                        .Append(":");
                }
                b.Append(e.Message);
                b.AppendLine();
                StdErr(b.ToString());
                if (null != root && root.Contains("@CompileOptions/@xxxtrace"))
                { StdErr(e.StackTrace); }
                return -1;
            }
        }

        public static Token CreateRootTemplate()
        {
            return new Token("", "Root")
                .FlwsAdd("", "CompileOptions")
                .FlwsAdd("", "Sources")
                .FlwsAdd("", "Syntax")
                .FlwsAdd("", "Code")
                ;
        }

        public static void Check(Token root)
        {
            if (root == null) { throw new ArgumentNullException("args"); }

            if (0 == root.Select("@CompileOptions").Length) { throw new ArgumentException("No @CompileOptions Token"); }
            if (1 < root.Select("@CompileOptions").Length) { throw new ArgumentException("Too many @CompileOptions Token"); }

            if (0 == root.Select("@Sources").Length || 0 == root.Find("@Sources").Follows.Length)
            { throw new ArgumentException("No source filename is specified to command line parameter"); }

            if (1 < root.Select("@Sources").Length) { throw new ArgumentException("Too many @Sources Token"); }

            if (0 == root.Select("@CompileOptions/@out").Length)
            {
                if (0 == root.Select("@Sources").Length
                    || 0 == root.Find("@Sources").Follows.Length)
                {
                    throw new ArgumentException("Cannot omit source path when out option was omitted");
                }
            }

            if (1 < root.Select("@CompileOptions/@out").Length) { throw new ArgumentException("Too many out options are specified to command line parameter"); }

            if (1 == root.Select("@Sources").Length)
            {
                foreach (Token p in root.Select("@Sources/@SourcePath"))
                {
                    if (false == File.Exists(p.Value)) { throw new FileNotFoundException("Source file was not found", p.Value); }
                }
            }

        }

        public static void Prepare(Token root)
        {
            if (false == root.Contains("@Syntax"))
            { root.FlwsAdd(new Token("", "Syntax")); }

            if (false == root.Contains("@Code"))
            { root.FlwsAdd(new Token("", "Code")); }
        }

        public static void ReadSourceFiles(Token root)
        {
            Token srcs = root.Find("@Sources");

            UTF8Encoding utf8 = new UTF8Encoding(false /* no byte order mark */);
            List<Token> srcsflw = new List<Token>();
            foreach (Token f in srcs.Follows)
            {
                srcsflw.Add(f);
                if (f.Group == "SourceText")
                {
                    f.First = new Token("");
                    continue;
                }
                if (f.Group == "SourcePath")
                {
                    string text = File.ReadAllText(f.Value, utf8);
                    Token txtt = new Token(text, "SourceText");
                    txtt.First = f;
                    srcsflw.Add(txtt);
                }
            }
            srcs.Follows = srcsflw.ToArray();
        }

        public static void AnalyzeSyntax(Token root)
        {
            SyntaxAnalyzer analyzer = new SyntaxAnalyzer();

            Token srcs = root.Find("@Sources");

            Token syntax = root.Find("@Syntax");
            List<Token> synflw = new List<Token>();
            foreach (Token f in srcs.Follows)
            {
                if (f.Group == "SourcePath") { continue; }
                if (f.Group == "SourceText") { synflw.Add(analyzer.Run(f.Value, f.First.Value)); }
            }
            syntax.Follows = synflw.ToArray();
        }

        public void Compile(Token root)
        {
            Prepare(root);

            Token srcs = root.Find("@Sources");

            //  append SourceText if it's SourcePath
            ReadSourceFiles(root);

            AnalyzeSyntax(root);

            AfterSyntaxAnalyze(root);

            Env env =  AnalyzeSemantic(root);

            AfterSemanticAnalyze(root, env);

            IMRGenerator imrgen = new IMRGenerator();
            imrgen.GenerateIMR(env.Ap);

            Token code = root.Find("@Code");
            CodeGenerator codegen = new CodeGenerator();
            code.Value = codegen.GenerateCode(env);
        }

        public static Env AnalyzeSemantic(Token root)
        {
            return EnvAnalyzer.Run(root);
        }

    }

}
