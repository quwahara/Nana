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
        public static void Check(Token args)
        {
            if (args == null) { throw new ArgumentNullException("args"); }
            if (0 == args.Find("@Arguments").Length) { throw new ArgumentException("No @Arguments Token"); }
            if (1 < args.Find("@Arguments").Length) { throw new ArgumentException("Too many @Arguments Token"); }

            if (0 == args.Find("@Arguments/@CompileOptions").Length) { throw new ArgumentException("No @CompileOptions Token"); }
            if (1 < args.Find("@Arguments/@CompileOptions").Length) { throw new ArgumentException("Too many @CompileOptions Token"); }

            if ((0 == args.Find("@Arguments/@SourcePaths").Length || 0 == args.Find("@Arguments/@SourcePaths")[0].Follows.Length)
                && (0 == args.Find("@Arguments/@SourceTexts").Length || 0 == args.Find("@Arguments/@SourceTexts")[0].Follows.Length))
            { throw new ArgumentException("No @SourcePaths nor @SourceTexts Token"); }

            if (1 < args.Find("@Arguments/@SourcePaths").Length) { throw new ArgumentException("Too many @SourcePaths Token"); }
            if (1 < args.Find("@Arguments/@SourceTexts").Length) { throw new ArgumentException("Too many @SourceTexts Token"); }

            if (0 == args.Find("@Arguments/@CompileOptions/@out").Length)
            {
                if (0 == args.Find("@Arguments/@SourcePaths").Length
                    || 0 == args.Find("@Arguments/@SourcePaths")[0].Follows.Length)
                {
                    throw new ArgumentException("Cannot omit source path when out option was omitted");
                }
            }

            if (1 < args.Find("@Arguments/@CompileOptions/@out").Length) { throw new ArgumentException("Too many out option"); }

            if (1 == args.Find("@Arguments/@SourcePaths").Length)
            {
                foreach (Token p in args.Find("@Arguments/@SourcePaths")[0].Follows)
                {
                    if (false == File.Exists(p.Value)) { throw new FileNotFoundException("Source file was not found", p.Value); }
                }
            }

        }

        public void Compile(Token args, Action<string> outWriteAct)
        {
            Token opt;
            opt = args.Follows[0];

            string src;
            string outfn = "";
            Token t;
            List<Token> srcs = new List<Token>();

            foreach (Token sf in args.Follows[1].Follows)
            {
                switch (sf.Group)
                {
                    case "SourcePath":
                        src = File.ReadAllText(sf.Value, Encoding.UTF8);
                        if (outfn == "") { outfn = Path.ChangeExtension(sf.Value, ".exe"); }
                        break;
                    case "SourceText":
                        src = sf.Value; break;

                    default:
                        {
                            throw new InternalError("It is not a source text: " + sf.Value);
                        }
                }

                t = AnalyzeSyntax(src);

                srcs.Add(t);
            }

            foreach (Token co in args.Follows[0].Follows)
            {
                if (co.Value == "out") { outfn = co.First.Value; }
            }

            string name = Path.GetFileNameWithoutExtension(outfn);

            Token srcstkn;
            srcstkn = new Token(outfn, "Sources");
            srcstkn.Follows = srcs.ToArray();

            Token roottk = new Token(name, "SemanticRoot");
            roottk.FlwsAdd(srcstkn);

            Env env = AnalyzeSemantic(roottk);
            
            IMRGenerator imrgen = new IMRGenerator();
            imrgen.GenerateIMR(env.FindInTypeOf<App>());

            CodeGenerator codegen = new CodeGenerator();
            outWriteAct(codegen.GenerateCode(env));
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
