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
            if (0 == root.Find("@Root").Length) { throw new ArgumentException("No @Root Token"); }
            if (1 < root.Find("@Root").Length) { throw new ArgumentException("Too many @Root Token"); }

            if (0 == root.Find("@Root/@CompileOptions").Length) { throw new ArgumentException("No @CompileOptions Token"); }
            if (1 < root.Find("@Root/@CompileOptions").Length) { throw new ArgumentException("Too many @CompileOptions Token"); }

            if (0 == root.Find("@Root/@Sources").Length || 0 == root.Find("@Root/@Sources")[0].Follows.Length)
            { throw new ArgumentException("No @Sources Token"); }

            if (1 < root.Find("@Root/@Sources").Length) { throw new ArgumentException("Too many @Sources Token"); }

            if (0 == root.Find("@Root/@CompileOptions/@out").Length)
            {
                if (0 == root.Find("@Root/@Sources").Length
                    || 0 == root.Find("@Root/@Sources")[0].Follows.Length)
                {
                    throw new ArgumentException("Cannot omit source path when out option was omitted");
                }
            }

            if (1 < root.Find("@Root/@CompileOptions/@out").Length) { throw new ArgumentException("Too many out option"); }

            if (1 == root.Find("@Root/@Sources").Length)
            {
                foreach (Token p in root.Find("@Root/@Sources")[0].Find("@SourcePath"))
                {
                    if (false == File.Exists(p.Value)) { throw new FileNotFoundException("Source file was not found", p.Value); }
                }
            }

        }

        public void Compile(Token root)
        {
            //  append SourceText if it's SourcePath
            Token srcs = root.Find("@Root/@Sources")[0];
            {
                UTF8Encoding utf8 = new UTF8Encoding(false /* no byte order mark */);
                List<Token> srcsflw = new List<Token>();
                foreach (Token f in srcs.Follows)
                {
                    srcsflw.Add(f);
                    if (f.Group == "SourceText") { continue; }
                    if (f.Group == "SourcePath")
                    {
                        string text = File.ReadAllText(f.Value, utf8);
                        srcsflw.Add(new Token(text, "SourceText"));
                    }
                }
                srcs.Follows = srcsflw.ToArray();
            }

            {
                if (false == root.Contains("@Root/@Syntax"))
                { root.FlwsAdd(new Token("", "Syntax")); }

                Token syntax = root.Find("@Root/@Syntax")[0];
                List<Token> synflw = new List<Token>();
                foreach (Token f in srcs.Follows)
                {
                    if (f.Group == "SourcePath") { continue; }
                    if (f.Group == "SourceText") { synflw.Add(AnalyzeSyntax(f.Value)); }
                }
                syntax.Follows = synflw.ToArray();
            }

            //TODO
            srcs.Group = "Ignore";

            Env env = AnalyzeSemantic(root);

            IMRGenerator imrgen = new IMRGenerator();
            imrgen.GenerateIMR(env.FindInTypeOf<App>());


            if (false == root.Contains("@Root/@Code"))
            { root.FlwsAdd(new Token("", "Code")); }

            Token code = root.Find("@Root/@Code")[0];
            CodeGenerator codegen = new CodeGenerator();
            code.Value = codegen.GenerateCode(env);
        }

        public static void StartCompile(string[] args)
        {
            UTF8Encoding utf8 = new UTF8Encoding(false /* no byte order mark */);
            string ilpath;
            string code;
            Token root = null;
            try
            {
                root = CmdLnArgs.GetCmdLnArgs(args);
                root.Group = "Root";
                Ctrl.Check(root);
                Ctrl c = new Ctrl();
                c.Compile(root);

                if (root.Contains("@Root/@CompileOptions/@xxxsyntax"))
                {
                    foreach (Token t in root.Find("@Root/@Syntax/0Source"))
                    {
                        Console.Write(TokenEx.ToTree(t));
                    }
                }

                if (root.Contains("@Root/@CompileOptions/@xxxil"))
                {
                    Console.Write(root.Find("@Root/@Code")[0].Value);
                }

                ilpath = root.Find("@Root/@CompileOptions/@out")[0].Value;
                ilpath = Path.ChangeExtension(ilpath, ".il");
                code = root.Find("@Root/@Code")[0].Value;
                File.WriteAllText(ilpath, code, utf8);

                ILASMRunner r = new ILASMRunner();
                r.DetectILASM();
                r.Run(ilpath);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                if (null != root && root.Contains("@Root/@CompileOptions/@xxxtrace"))
                { Console.Error.Write(e.StackTrace); }
            }
        }
        
        public Token AnalyzeSyntax(string src)
        {
            SyntaxAnalyzer p = new SyntaxAnalyzer();
            p.Init(src);
            return p.Analyze();
        }

        public static Env AnalyzeSemantic(Token roottk)
        {
            EnvAnalyzer azr = new EnvAnalyzer(roottk);
            azr.Analyze();
            return azr.Env;
        }

    }

}
