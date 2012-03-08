/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnitTest.Util;
using System.Text.RegularExpressions;
using Nana.Infr;

namespace UnitTest.Infr
{
    [TestFixture]
    public class NodeScanPatternFxt
    {
        [Test]
        public void T001() { Run("SCAN_PATTERN:-001", @"", ""); }
        [Test]
        public void T002() { Run("SCAN_PATTERN:-002", @" ", ""); }
        [Test]
        public void T003() { Run("SCAN_PATTERN:-003", @"(", "L0:1"); }
        [Test]
        public void T004() { Run("SCAN_PATTERN:-004", @")", "R0:1"); }
        [Test]
        public void T005() { Run("SCAN_PATTERN:-005", @"a", "V0:1"); }
        [Test]
        public void T006() { Run("SCAN_PATTERN:-006", @"\\", "V0:2"); }
        [Test]
        public void T007() { Run("SCAN_PATTERN:-007", @"\(", "V0:2"); }
        [Test]
        public void T008() { Run("SCAN_PATTERN:-008", @"\)", "V0:2"); }
        [Test]
        public void T009() { Run("SCAN_PATTERN:-009", @"\( a (b c) \)", "V0:2 V3:1 L5:1 V6:1 V8:1 R9:1 V11:2"); }
        [Test]
        public void T010() { Run("SCAN_PATTERN:-010", @"\\\(((\((\\", "V0:4 L4:1 L5:1 V6:2 L8:1 V9:2"); }

        public void Run(string lable, string input, string expected)
        {
            new TestCase(lable, input, expected, Test).Run();
        }

        public string Test(TestCase c)
        {
            StringBuilder b;
            Match m;
            //int idx = -1;
            //bool idxng = false;

            b = new StringBuilder();
            m = Regex.Match(c.Input, Node.SCAN_PATTERN);
            while (m.Success)
            {
                //if (idx < m.Index) idx = m.Index; else idxng = true;
                foreach (string n in new string[] { "V", "L", "R" })
                {
                    Group g = m.Groups[n];
                    if (g.Success)
                    {
                        b.Append(n);
                        b.Append(g.Index);
                        b.Append(":");
                        b.Append(g.Length);
                    }
                }
                m = m.NextMatch();
                if (m.Success) b.Append(" ");
            }
            return b.ToString();
        }
    }

    [TestFixture]
    public class NodeParseFxt
    {
        [Test]
        public void T001() { Run("PARSE:-001", "", ""); }
        [Test]
        public void T002() { Run("PARSE:-002", " ", ""); }
        [Test]
        public void T003() { Run("PARSE:-003", "()", "()"); }
        [Test]
        public void T004() { Run("PARSE:-004", "(a(b(c(d))))", "(a (b (c (d))))"); }
        [Test]
        public void T005() { Run("PARSE:-005", "((((a)b)c)d)", "((((a) b) c) d)"); }
        [Test]
        public void T006() { Run("PARSE:-006", "(a b (c d (e f) g h) i j)", "(a b (c d (e f) g h) i j)"); }
        [Test]
        public void T007() { Run("", "a", "a"); }
        [Test]
        public void T008() { Run("", "a b", "a b"); }

        public void Run(string lable, string input, string expected)
        {
            new TestCase(lable, input, expected, Test).Run();
        }

        public string Test(TestCase c)
        {
            List<Node> lst = Node.Parse(c.Input);
            string act = Cty.ToText<Node>(lst).Replace("\r\n", " ").TrimEnd();
            return act;
        }
    }

    [TestFixture]
    public class NodeParseFromIndentFxt
    {
        [Test]
        public void T001()
        {
            string inp, epc;
            inp = @"";
            epc = "()";
            Run(inp, epc);
        }

        [Test]
        public void T101()
        {
            string inp, epc;
            inp = @"
1
";
            epc = "(1)";
            Run(inp, epc);
        }

        [Test]
        public void T102()
        {
            string inp, epc;
            inp = @"
1
2
";
            epc = "(1 2)";
            Run(inp, epc);
        }

        [Test]
        public void T103()
        {
            string inp, epc;
            inp = @"
1
2
3
";
            epc = "(1 2 3)";
            Run(inp, epc);
        }

        [Test]
        public void T151()
        {
            string inp, epc;
            inp = @"
    1
";
            epc = "((1))";
            Run(inp, epc);
        }

        [Test]
        public void T152()
        {
            string inp, epc;
            inp = @"
    1
    
    2
";
            epc = "((1) (2))";
            Run(inp, epc);
        }

        [Test]
        public void T153()
        {
            string inp, epc;
            inp = @"
    1
    
    2
    
    3
";
            epc = "((1) (2) (3))";
            Run(inp, epc);
        }

        [Test]
        public void T201()
        {
            string inp, epc;
            inp = @"
a
    1
";
            epc = "(a (1))";
            Run(inp, epc);
        }

        [Test]
        public void T202()
        {
            string inp, epc;
            inp = @"
a
    1
    2
";
            epc = "(a (1 2))";
            Run(inp, epc);
        }

        [Test]
        public void T203()
        {
            string inp, epc;
            inp = @"
a
    1
    2
    3
";
            epc = "(a (1 2 3))";
            Run(inp, epc);
        }

        [Test]
        public void T210()
        {
            string inp, epc;
            inp = @"
a
    1
b
";
            epc = "(a (1) b)";
            Run(inp, epc);
        }

        [Test]
        public void T212()
        {
            string inp, epc;
            inp = @"
a
    1
b
    2
c
";
            epc = "(a (1) b (2) c)";
            Run(inp, epc);
        }

        [Test]
        public void T213()
        {
            string inp, epc;
            inp = @"
a
    1
b
    2
    3
";
            epc = "(a (1) b (2 3))";
            Run(inp, epc);
        }

        [Test]
        public void T214()
        {
            string inp, epc;
            inp = @"
a
    1
b
    2
    3
c
";
            epc = "(a (1) b (2 3) c)";
            Run(inp, epc);
        }

        [Test]
        public void T251()
        {
            string inp, epc;
            inp = @"
a
    1
        h
        i
    
    2
";
            epc = "(a (1 (h i)) (2))";
            Run(inp, epc);
        }

        [Test]
        public void T301()
        {
            string inp, epc;
            inp = @"
a
    1
        h
";
            epc = "(a (1 (h)))";
            Run(inp, epc);
        }

        [Test]
        public void T302()
        {
            string inp, epc;
            inp = @"
a
    1
        h
        i
";
            epc = "(a (1 (h i)))";
            Run(inp, epc);
        }

        [Test]
        public void T303()
        {
            string inp, epc;
            inp = @"
a
    1
        h
        i
        j
";
            epc = "(a (1 (h i j)))";
            Run(inp, epc);
        }

        [Test]
        public void T311()
        {
            string inp, epc;
            inp = @"
a
    1
        h
    2
";
            epc = "(a (1 (h) 2))";
            Run(inp, epc);
        }

        [Test]
        public void T312()
        {
            string inp, epc;
            inp = @"
a
    1
        h
        i
    2
";
            epc = "(a (1 (h i) 2))";
            Run(inp, epc);
        }

        [Test]
        public void T313()
        {
            string inp, epc;
            inp = @"
a
    1
        h
        i
    2
        j
";
            epc = "(a (1 (h i) 2 (j)))";
            Run(inp, epc);
        }

        [Test]
        public void T314()
        {
            string inp, epc;
            inp = @"
a
    1
        h
        i
    2
        j
        k
";
            epc = "(a (1 (h i) 2 (j k)))";
            Run(inp, epc);
        }

        [Test]
        public void T901()
        {
            string inp;
            inp = @"
    1
2
";
            try
            {
                Node.ParseFromIndent(inp);
                Assert.Fail();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Test]
        [Description("Do not allow to put seprator(= blank line) into the no indent column.")]
        public void T902()
        {
            string inp;
            inp = @"
1

2
";
            try
            {
                Node.ParseFromIndent(inp);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Run(string inp, string epc) { Run("", inp, epc); }
        
        public void Run(string lbl, string inp, string epc)
        {
            RunNormal(lbl, inp, epc);
            RunRecur(lbl, inp);
        }

        private static void RunNormal(string lbl, string inp, string epc)
        {
            new TestCase(lbl, inp, epc,
                delegate(TestCase c)
                {
                    return GenInLine(c.Input);
                }
                ).Run();
        }

        private static string GenInLine(string inp)
        {
            return new Node(Node.ParseFromIndent(inp)).ToString();
        }

        public void RunRecur(string lbl, string inp)
        {
            lbl += " (Recurrence)";
            List<string> ls = Sty.ToStringList(inp);
            int idx = ls.FindIndex(Sty.NotNullOrEmpty);
            if (idx >= 1) ls = ls.GetRange(idx, ls.Count - idx);
            string epc = Cty.ToText(ls);
            inp = GenInLine(inp);
            new TestCase(lbl, inp, epc,
                delegate(TestCase c)
                {
                    return Node.Parse(c.Input)[0].ToIndent();
                }
                ).Run();
        }
    }
}
