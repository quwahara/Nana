/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Nana.Infr;
using Nana.Syntaxes;
using UnitTest.Util;
using Nana.Delegates;
using System.IO;
using System.Text.RegularExpressions;
using Nana;
using Nana.Tokens;

namespace UnitTest.Tokens
{
    [TestFixture]
    public class IEnumeratorAdapterFxt
    {
        public string Inp, Epc;

        [Test]
        public void T101()
        {
            Inp = @"
";
            Epc =
@"(EOF)";
            Test();
        }

        [Test]
        public void T102()
        {
            Inp = @"
2   Factor
";
            Epc =
@"V:2 G:Factor B:0
";
            Test();
        }

        [Test]
        public void T103()
        {
            Inp = @"
3   Factor
3   Factor
";
            Epc =
@"V:3 G:Factor B:0
V:3 G:Factor B:0
";
            Test();
        }

        [Test]
        public void T104()
        {
            Inp = @"
4   Factor
4   Factor
4   Factor
";
            Epc =
@"V:4 G:Factor B:0
V:4 G:Factor B:0
V:4 G:Factor B:0
";
            Test();
        }

        [SetUp]
        public void SetUp() { Inp = Epc = ""; }

        public void Test()
        {
            Func<TestCase, string> f = delegate(TestCase c)
            {
                IEnumerator<Token> tks;
                EnumeratorAdapter tkz;
                tks = SFList.FromText(c.Input).Map<Token>(Token.FromVG).NotNulls().GetEnumerator();
                tkz = new EnumeratorAdapter(tks);

                if (tkz.EOF) return "(EOF)";

                StringBuilder b;
                b = new StringBuilder();
                while (tkz.EOF == false)
                {
                    b.Append(TokenEx.ToUTStr(tkz.Cur)).AppendLine();
                    //b.Append(tkz.Cur).AppendLine();
                    tkz.Next();
                }

                return b.ToString();
            };
            new TestCase("", Inp, Epc, f).Run();
        }
    }

    [TestFixture]
    public class InlineTokenizerFxt
    {
        [Test]
        public void T101()
        {
            string inp, epc;
            inp = @"//";
            epc = @"V:// G:Cmt B:0/Pos=2";
            Run(inp, epc);
        }

        [Test]
        public void T102()
        {
            string inp, epc;
            inp = @"//a";
            epc = @"V://a G:Cmt B:0/Pos=3";
            Run(inp, epc);
        }

        [Test]
        public void T103()
        {
            string inp, epc;
            inp = @"//abc";
            epc = @"V://abc G:Cmt B:0/Pos=5";
            Run(inp, epc);
        }


        [Test]
        public void T201()
        {
            string inp, epc;
            inp = @"...";
            epc = @"V:... G:Id B:0/Pos=3";
            Run(inp, epc);
        }

        [Test]
        public void T202()
        {
            string inp, epc;
            inp = @"==";
            epc = @"V:== G:Ope B:0/Pos=2";
            Run(inp, epc);
        }

        public void Run(string inp, string epc)
        {
            new TestCase("", inp, epc, TestNext).Run();
        }

        public string TestNext(TestCase c)
        {
            Regex startRx;

            startRx = new Regex(@"(?<Id>(\.\.\.|\-\-\-|\.\.|\-\-))
|(?<Cmt>(//.*))
|(?<Ope>(==|!=|\<=|\>=))
|(?<Sig>(\<\<|\>\>|\::|\-\>))
|(?<Ope>(\+|\-|\*|/|\<|\>))
|(?<Sig>(=|:|\(|\)|,|\.|@))
|(?<Num>(\d+)(\.(\d)+)?)
|(?<Bol>(true|false))
|(?<Id>[_a-zA-Z][_a-zA-Z0-9]*)
|(?<Unk>[^\s$]+)
", RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            TokenizerBase tkz = new InlineTokenizer(startRx);
            Token t;
            using (LineBufferedReader r = LineBufferedReader.GetInstanceWithText(c.Input, ""))
            {
                tkz.Init(r);
                t = tkz.Cur;
            }
            return TokenEx.ToUTStr(t) + "/Pos=" + tkz.Pos.Value.ToString();
            //return t.ToString() + "/Pos=" + tkz.Pos.Value.ToString();
        }
    }

    [TestFixture]
    public class BlockTokenizerFxt1
    {
        [Test]
        public void T001()
        {
            string inp, epc;
            inp = @"""""";
            epc = @"V:"""" G:Str B:0/Pos=2";
            Run(inp, epc);
        }

        [Test]
        public void T002()
        {
            string inp, epc;
            inp = @"""a""";
            epc = @"V:""a"" G:Str B:0/Pos=3";
            Run(inp, epc);
        }

        [Test]
        public void T003()
        {
            string inp, epc;
            inp = @"""abc""";
            epc = @"V:""abc"" G:Str B:0/Pos=5";
            Run(inp, epc);
        }

        [Test]
        public void T201()
        {
            string inp, epc;
            inp = @"""\\""";
            epc = @"V:""\\"" G:Str B:0/Pos=4";
            Run(inp, epc);
        }

        [Test]
        public void T202()
        {
            string inp, epc;
            inp = @"""\\\\""";
            epc = @"V:""\\\\"" G:Str B:0/Pos=6";
            Run(inp, epc);
        }

        [Test]
        public void T301()
        {
            string inp, epc;
            inp = @"""\""""";
            epc = @"V:""\"""" G:Str B:0/Pos=4";
            Run(inp, epc);
        }

        [Test]
        public void T302()
        {
            string inp, epc;
            inp = @"""\""\""\""""";
            epc = @"V:""\""\""\"""" G:Str B:0/Pos=8";
            Run(inp, epc);
        }

        [Test]
        public void T401()
        {
            string inp, epc;
            inp = @"""
""";
            epc = @"V:""
"" G:Str B:0/Pos=1";
            Run(inp, epc);
        }

        public void Run(string inp, string epc)
        {
            new TestCase("", inp, epc, TestNext).Run();
        }

        public string TestNext(TestCase c)
        {
            Regex startRx, escRx, endRx;
            TokenizerBase tkz;
            startRx = new Regex(@"^""");
            escRx = new Regex(@"\\.");
            endRx = new Regex(@"""");
            tkz = new BlockTokenizer(startRx, escRx, endRx, "Str");

            Token t;
            using (LineBufferedReader r = LineBufferedReader.GetInstanceWithText(c.Input, ""))
            {
                tkz.Init(r);
                t = tkz.Cur;
            }
            return TokenEx.ToUTStr(t) + "/Pos=" + tkz.Pos.Value.ToString();
            //return t.ToString() + "/Pos=" + tkz.Pos.Value.ToString();
        }
    }

    [TestFixture]
    public class BlockTokenizerFxt2
    {
        [Test]
        public void T101()
        {
            string inp, epc;
            inp = @"/.,/";
            epc = @"V:/.,/ G:Cmt B:0/Pos=4";
            Run(inp, epc);
        }

        [Test]
        public void T102()
        {
            string inp, epc;
            inp = @"/.abc,/";
            epc = @"V:/.abc,/ G:Cmt B:0/Pos=7";
            Run(inp, epc);
        }

        [Test]
        public void T201()
        {
            string inp, epc;
            inp = @"/.
,/
";
            epc = @"V:/.
,/ G:Cmt B:0/Pos=2";
            Run(inp, epc);
        }

        [Test]
        public void T202()
        {
            string inp, epc;
            inp = @"/.
abcd
efg
hij,/
";
            epc = @"V:/.
abcd
efg
hij,/ G:Cmt B:0/Pos=5";
            Run(inp, epc);
        }

        public void Run(string inp, string epc)
        {
            new TestCase("", inp, epc, TestNext).Run();
        }

        public string TestNext(TestCase c)
        {
            Regex startRx, escRx, endRx;
            TokenizerBase tkz;
            startRx = new Regex(@"^/\.");
            escRx = new Regex(@"$^");
            endRx = new Regex(@",/");
            tkz = new BlockTokenizer(startRx, escRx, endRx, "Cmt");

            Token t;
            using (LineBufferedReader r = LineBufferedReader.GetInstanceWithText(c.Input, ""))
            {
                tkz.Init(r);
                t = tkz.Cur;
            }
            return TokenEx.ToUTStr(t) + "/Pos=" + tkz.Pos.Value.ToString();
            //return t.ToString() + "/Pos=" + tkz.Pos.Value.ToString();
        }
    }

    [TestFixture]
    public class ScriptTokenizerInlineRxPatternFxt
    {
        public string Inp;
        public string Epc;
        public Dictionary<string, List<string>> Ptns = new Dictionary<string, List<string>>();

        public ScriptTokenizerInlineRxPatternFxt()
        {
            List<string> ls = Sty.ToStringList(ScriptTokenizer.InlineRxPattern);
            foreach (string ln in ls)
            {
                int bgn = ln.IndexOf("<");
                int end = ln.IndexOf(">");
                if (end <= bgn) { continue; }
                string key = ln.Substring(bgn + 1, end - bgn -1);
                int baridx = ln.Length > 0 && '|' == ln[0] ?
                    0 : -1;
                string ptn = ln.Substring(baridx + 1);
                List<string> ptnls;
                if (false == Ptns.TryGetValue(key, out ptnls))
                {
                    ptnls = new List<string>();
                    Ptns.Add(key, ptnls);
                }
                ptnls.Add(ptn);
            }
        }

        [SetUp]
        public void SetUp()
        {
            Inp = ""; Epc = "";
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_01()
        {
            Inp = "Int  :0  #9";
            Epc = "(same as input)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_02()
        {
            Inp = "Int  :0  #99";
            Epc = "(same as input)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_03()
        {
            Inp = "Int  :0  #9_";
            Epc = "(same as input)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_04()
        {
            Inp = "Int  :0  #_9";
            Epc = "9";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_05()
        {
            Inp = "Int  :0  #__";
            Epc = "(fail)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_06()
        {
            Inp = "Int  :0  #999";
            Epc = "(same as input)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_07()
        {
            Inp = "Int  :0  #99_";
            Epc = "(same as input)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_08()
        {
            Inp = "Int  :0  #9_9";
            Epc = "(same as input)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_09()
        {
            Inp = "Int  :0  #9__";
            Epc = "(same as input)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_10()
        {
            Inp = "Int  :0  #_99";
            Epc = "99";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_11()
        {
            Inp = "Int  :0  #_9_";
            Epc = "9_";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_12()
        {
            Inp = "Int  :0  #__9";
            Epc = "9";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_13()
        {
            Inp = "Int  :0  #___";
            Epc = "(fail)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_14()
        {
            Inp = "Int  :0  #9u";
            Epc = "(same as input)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_15()
        {
            Inp = "Int  :0  #9l";
            Epc = "(same as input)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_16()
        {
            Inp = "Int  :0  #9ul";
            Epc = "(same as input)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_17()
        {
            Inp = "Int  :0  #9lu";
            Epc = "9lu";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_18()
        {
            Inp = "Int  :0  #9UL";
            Epc = "(same as input)";
            Test();
        }

        [Test]
        public void TC0714_DecimalIntegerLiteral_19()
        {
            Inp = "Int  :0  #9LU";
            Epc = "(same as input)";
            Test();
        }

        public void Test()
        {
            new TestCase("", Inp, Epc, delegate(TestCase c)
            {
                string inp = c.Input;
                string[] inpspl = inp.Split(new char[] { ':', '#' });
                string key = inpspl[0].Trim();
                int index = int.Parse(inpspl[1].Trim());
                string input = inp.Substring(inp.IndexOf("#") + 1);
                if ("(same as input)" == c.Expected)
                { c.Expected = input; }
                string ptn = Ptns[key][index];
                RegexOptions inlineRxOptions = RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture;
                Match m = Regex.Match(input, ptn, inlineRxOptions);
                string result;
                if (false == m.Success)
                { result = "(fail)"; }
                else
                { result = m.Groups[key].Value; }
                return result;
            })
            .Run();
        }
    }

    [TestFixture]
    public class ScriptTokenizerFxt
    {
        [Test]
        public void T001()
        {
            string inp, epc;
            inp = @"
class Nana    
begin
	sfun Main():void
    begin
        //  a line comment
        ""string literal"" -> a
        /*  block comment 1 */
        factor1
        /*  block comment 2
        */
        factor2
        /*
            block comment 3 */
        factor3
        /*
            block comment 4
        */
        factor4
        /*  /*  block comment 5 */
        factor5
        /*  block comment 6
        //  */  factor6
    end
end
";
            epc = @"class
Nana
begin
sfun
Main
(
)
:
void
begin
""string literal""
->
a
factor1
factor2
factor3
factor4
factor5
factor6
end
end
";
            Run(inp, epc);
        }

        public void Run(string inp, string epc)
        {
            new TestCase("", inp, epc, TestNext).Run();
        }

        public string TestNext(TestCase c)
        {
            TokenizerBase tkz = new ScriptTokenizer();
            Token t;
            StringBuilder b = new StringBuilder();
            using (LineBufferedReader r = LineBufferedReader.GetInstanceWithText(c.Input, ""))
            {
                tkz.Init(r);
                while (tkz.EOF == false)
                {
                    t = tkz.Cur;
                    Console.WriteLine(t.Value);
                    b.AppendLine(t.Value);
                    tkz.Next();
                }
            }
            return b.ToString();
        }
    }

    [TestFixture]
    public class PrependAndAppendFxt
    {
        public string Inp;
        public string Epc;

        public ITokenEnumerator Tokens;

        [SetUp]
        public void SetUp()
        {
            Inp = "";
            Epc = "";
        }

        [Test]
        public void TC0520_SrcIsEmpty()
        {
            Inp = "";
            Epc = new Token(Token.ZSourceValue).ToString();
            Test();
        }

        public void Test()
        {
            Func<TestCase, string> f = delegate(TestCase c)
            {
                TokenizerBase tkz = new ScriptTokenizer();
                LineBufferedReader r = LineBufferedReader.GetInstanceWithText(c.Input, /*path*/ "");
                tkz.Init(r);

                ITokenEnumerator tokens = tkz;

                Token src;
                src = new Token();
                src.Value = Token.ZSourceValue;
                Tokens = new Prepend(tokens, src);
                Tokens = new Append(Tokens, Token.ZEnd);
                string cur = Tokens.Cur.ToString();
                return cur;
            };

            new TestCase("", Inp, Epc, f).Run();
        }

    }
     
}
