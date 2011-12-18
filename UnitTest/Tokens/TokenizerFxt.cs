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
@"V:2 G:Factor B:0 T:
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
@"V:3 G:Factor B:0 T:
V:3 G:Factor B:0 T:
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
@"V:4 G:Factor B:0 T:
V:4 G:Factor B:0 T:
V:4 G:Factor B:0 T:
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
            epc = @"V:// G:Cmt B:0 T:/Pos=2";
            Run(inp, epc);
        }

        [Test]
        public void T102()
        {
            string inp, epc;
            inp = @"//a";
            epc = @"V://a G:Cmt B:0 T:/Pos=3";
            Run(inp, epc);
        }

        [Test]
        public void T103()
        {
            string inp, epc;
            inp = @"//abc";
            epc = @"V://abc G:Cmt B:0 T:/Pos=5";
            Run(inp, epc);
        }


        [Test]
        public void T201()
        {
            string inp, epc;
            inp = @"...";
            epc = @"V:... G:Id B:0 T:/Pos=3";
            Run(inp, epc);
        }

        [Test]
        public void T202()
        {
            string inp, epc;
            inp = @"==";
            epc = @"V:== G:Ope B:0 T:/Pos=2";
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
            using (LineBufferedReader r = LineBufferedReader.GetInstanceWithText(c.Input))
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
            epc = @"V:"""" G:Str B:0 T:/Pos=2";
            Run(inp, epc);
        }

        [Test]
        public void T002()
        {
            string inp, epc;
            inp = @"""a""";
            epc = @"V:""a"" G:Str B:0 T:/Pos=3";
            Run(inp, epc);
        }

        [Test]
        public void T003()
        {
            string inp, epc;
            inp = @"""abc""";
            epc = @"V:""abc"" G:Str B:0 T:/Pos=5";
            Run(inp, epc);
        }

        [Test]
        public void T201()
        {
            string inp, epc;
            inp = @"""\\""";
            epc = @"V:""\\"" G:Str B:0 T:/Pos=4";
            Run(inp, epc);
        }

        [Test]
        public void T202()
        {
            string inp, epc;
            inp = @"""\\\\""";
            epc = @"V:""\\\\"" G:Str B:0 T:/Pos=6";
            Run(inp, epc);
        }

        [Test]
        public void T301()
        {
            string inp, epc;
            inp = @"""\""""";
            epc = @"V:""\"""" G:Str B:0 T:/Pos=4";
            Run(inp, epc);
        }

        [Test]
        public void T302()
        {
            string inp, epc;
            inp = @"""\""\""\""""";
            epc = @"V:""\""\""\"""" G:Str B:0 T:/Pos=8";
            Run(inp, epc);
        }

        [Test]
        public void T401()
        {
            string inp, epc;
            inp = @"""
""";
            epc = @"V:""
"" G:Str B:0 T:/Pos=1";
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
            using (LineBufferedReader r = LineBufferedReader.GetInstanceWithText(c.Input))
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
            epc = @"V:/.,/ G:Cmt B:0 T:/Pos=4";
            Run(inp, epc);
        }

        [Test]
        public void T102()
        {
            string inp, epc;
            inp = @"/.abc,/";
            epc = @"V:/.abc,/ G:Cmt B:0 T:/Pos=7";
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
,/ G:Cmt B:0 T:/Pos=2";
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
hij,/ G:Cmt B:0 T:/Pos=5";
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
            using (LineBufferedReader r = LineBufferedReader.GetInstanceWithText(c.Input))
            {
                tkz.Init(r);
                t = tkz.Cur;
            }
            return TokenEx.ToUTStr(t) + "/Pos=" + tkz.Pos.Value.ToString();
            //return t.ToString() + "/Pos=" + tkz.Pos.Value.ToString();
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
            using (LineBufferedReader r = LineBufferedReader.GetInstanceWithText(c.Input))
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
}
