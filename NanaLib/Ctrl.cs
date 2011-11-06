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



//  �P�����Z�q����������
//  SyntaxAnalyzer �̒P�̃e�X�g���A�eAnalyzer�N���X�̃��\�b�h�P�ʂŎ��{�ł���悤�ɂ���
//  Token�̊ȒP�Ȑ������@��͍�����B�eAnalyzer�N���X�̃��\�b�h�P�ʂŎ��{�ł���悤�ɂ��邽�߂̏���
//  �G���[��\����O�� SemanticError��ǉ�����B����͂��ׂ�Syntax�G���[�ɂ��Ă��܂��Ă��邽��
//  �d�l�𕶏�������B���J��ڎw������
//  Cty�Ŏԗւ̍Ĕ����ȏ������Ȃ���
//  TODO don't match '`' only.

namespace Nana
{
    public class Ctrl
    {
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
