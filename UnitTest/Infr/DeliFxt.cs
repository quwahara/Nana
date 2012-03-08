/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using Nana.Infr;
using Nana.Delegates;
using UnitTest.Util;

namespace UnitTest.Infr
{
    [TestFixture]
    public class DeliLoadFxt
    {
        [Test]
        public void T001()
        {
            string label, inpandepc;

            label = "Load depth 0, section 0, line 0";
            inpandepc = "";
            Run(label, inpandepc);
        }

        [Test]
        public void T002()
        {
            string label, inpandepc;

            label = "Load depth 0, section 0, line 1";
            inpandepc =
@"
Dp:0-Sc:0-Ln:1
";
            Run(label, inpandepc);
        }

        [Test]
        public void T003()
        {
            string label, inpandepc;

            label = "Load depth 0, section 0, line n";
            inpandepc =
@"
Dp:0-Sc:0-Ln:1
Dp:0-Sc:0-Ln:2
";
            Run(label, inpandepc);
        }

        [Test]
        public void T004()
        {
            string label, inpandepc;

            label = "Load depth 1, section 1, line 0";
            inpandepc =
@"
#Dp:1-Sc:1
";
            Run(label, inpandepc);
        }

        [Test]
        public void T005()
        {
            string label, inpandepc;

            label = "Load depth 1, section 1, line 1";
            inpandepc =
@"
#Dp:1-Sc:1
Dp:1-Sc:1-Ln:1
";
            Run(label, inpandepc);
        }

        [Test]
        public void T006()
        {
            string label, inpandepc;

            label = "Load depth 1, section 1, line n";
            inpandepc =
@"
#Dp:1-Sc:1
Dp:1-Sc:1-Ln:1
Dp:1-Sc:1-Ln:2
";
            Run(label, inpandepc);
        }

        [Test]
        public void T007()
        {
            string label, inpandepc;

            label = "Load depth 1, section n, line 0";
            inpandepc =
@"
#Dp:1-Sc:1
#Dp:1-Sc:2
";
            Run(label, inpandepc);
        }

        [Test]
        public void T008()
        {
            string label, inpandepc;

            label = "Load depth 1, section n, line 1";
            inpandepc =
@"
#Dp:1-Sc:1
Dp:1-Sc:1-Ln:1
#Dp:1-Sc:2
Dp:1-Sc:2-Ln:1
";
            Run(label, inpandepc);
        }

        [Test]
        public void T009()
        {
            string label, inpandepc;

            label = "Load depth 1, section n, line n";
            inpandepc =
@"
#Dp:1-Sc:1
Dp:1-Sc:1-Ln:1
Dp:1-Sc:1-Ln:2
#Dp:1-Sc:2
Dp:1-Sc:2-Ln:1
Dp:1-Sc:2-Ln:2
";
            Run(label, inpandepc);
        }

        [Test]
        public void T010()
        {
            string label, inpandepc;

            label = "Load depth n, section 1, line 0";
            inpandepc =
@"
#Dp:1-Sc:1
##Dp:2-Sc:1
#Dp:1-Sc:2
##Dp:2-Sc:3
";
            Run(label, inpandepc);
        }

        [Test]
        public void T011()
        {
            string label, inpandepc;

            label = "Load depth 2, section 1, line 1";
            inpandepc =
@"
#Dp:1-Sc:1
##Dp:2-Sc:1
Dp:2-Sc:1-Ln:1
#Dp:1-Sc:2
##Dp:2-Sc:3
Dp:2-Sc:3-Ln:1
";
            Run(label, inpandepc);
        }

        [Test]
        public void T012()
        {
            string label, inpandepc;

            label = "Load depth n, section 1, line n";
            inpandepc =
@"
#Dp:1-Sc:1
##Dp:2-Sc:1
Dp:2-Sc:1-Ln:1
Dp:2-Sc:1-Ln:2
#Dp:1-Sc:2
##Dp:2-Sc:3
Dp:2-Sc:3-Ln:1
Dp:2-Sc:3-Ln:2
";
            Run(label, inpandepc);
        }

        [Test]
        public void T013()
        {
            string label, inpandepc;

            label = "Load depth n, section n, line 0";
            inpandepc =
@"
#Dp:1-Sc:1
##Dp:2-Sc:1
##Dp:2-Sc:2
#Dp:1-Sc:2
##Dp:2-Sc:3
##Dp:2-Sc:4
";
            Run(label, inpandepc);
        }

        [Test]
        public void T014()
        {
            string label, inpandepc;

            label = "Load depth n, section n, line 1";
            inpandepc =
@"
#Dp:1-Sc:1
##Dp:2-Sc:1
Dp:2-Sc:1-Ln:1
##Dp:2-Sc:2
Dp:2-Sc:2-Ln:1
#Dp:1-Sc:2
##Dp:2-Sc:3
Dp:2-Sc:3-Ln:1
##Dp:2-Sc:4
Dp:2-Sc:4-Ln:1
";
            Run(label, inpandepc);
        }

        [Test]
        public void T015()
        {
            string label, inpandepc;

            label = "Load depth n, section n, line n";
            inpandepc =
@"
#Dp:1-Sc:1
##Dp:2-Sc:1
Dp:2-Sc:1-Ln:1
Dp:2-Sc:1-Ln:2
##Dp:2-Sc:2
Dp:2-Sc:2-Ln:1
Dp:2-Sc:2-Ln:2
#Dp:1-Sc:2
##Dp:2-Sc:3
Dp:2-Sc:3-Ln:1
Dp:2-Sc:3-Ln:2
##Dp:2-Sc:4
Dp:2-Sc:4-Ln:1
Dp:2-Sc:4-Ln:2
";
            Run(label, inpandepc);
        }

        public void Run(string lable, string inpandepc)
        {
            new TestCase(lable, inpandepc, inpandepc, Test).Run();
        }

        public string Test(TestCase c)
        {
            return Deli.Load(c.Input).ToString();
        }
    }

    [TestFixture]
    public class DeliSubFxt1
    {
        public Deli Master;

        [SetUp]
        public void SetUp()
        {
            #region master data
            Master = Deli.Load(@"
#a
a
##1
a-1
###1
a-1-1
###2
a-1-2
###3
a-1-3

##2
a-2
###1
a-2-1
###2
a-2-2
###3
a-2-3

##3
a-3
###1
a-3-1
###2
a-3-2
###3
a-3-3

#b
b
##1
b-1
###1
b-1-1
###2
b-1-2
###3
b-1-3

##2
b-2
###1
b-2-1
###2
b-2-2
###3
b-2-3

##3
b-3
###1
b-3-1
###2
b-3-2
###3
b-3-3

#c
c
##1
c-1
###1
c-1-1
###2
c-1-2
###3
c-1-3

##2
c-2
###1
c-2-1
###2
c-2-2
###3
c-2-3

##3
c-3
###1
c-3-1
###2
c-3-2
###3
c-3-3

");
            #endregion
        }

        [Test]
        public void T001()
        {
            string label, input, expected;

            label = "a: Get depth 1 by absolute.";
            input = "#a";
            expected =
@"#a
a
##1
a-1
###1
a-1-1
###2
a-1-2
###3
a-1-3

##2
a-2
###1
a-2-1
###2
a-2-2
###3
a-2-3

##3
a-3
###1
a-3-1
###2
a-3-2
###3
a-3-3

";
            Run(label, input, expected);
        }

        [Test]
        public void T002()
        {
            string label, input, expected;

            label = "a: Get depth 1 by relative.";
            input = "a";
            expected =
@"#a
a
##1
a-1
###1
a-1-1
###2
a-1-2
###3
a-1-3

##2
a-2
###1
a-2-1
###2
a-2-2
###3
a-2-3

##3
a-3
###1
a-3-1
###2
a-3-2
###3
a-3-3

";
            Run(label, input, expected);
        }

        [Test]
        public void T003()
        {
            string label, input, expected;

            label = "a-1: Get depth 2 by absolute.";
            input = "#a#1";
            expected =
@"##1
a-1
###1
a-1-1
###2
a-1-2
###3
a-1-3

";
            Run(label, input, expected);
        }

        [Test]
        public void T004()
        {
            string label, input, expected;

            label = "a-1-1: Get depth 3 by absolute.";
            input = "#a#1#1";
            expected =
@"###1
a-1-1
";
            Run(label, input, expected);
        }

        [Test]
        public void T005()
        {
            string label, input, expected;

            label = "b: Get depth 1 by absolute.";
            input = "#b";
            expected =
@"#b
b
##1
b-1
###1
b-1-1
###2
b-1-2
###3
b-1-3

##2
b-2
###1
b-2-1
###2
b-2-2
###3
b-2-3

##3
b-3
###1
b-3-1
###2
b-3-2
###3
b-3-3

";
            Run(label, input, expected);
        }

        [Test]
        public void T006()
        {
            string label, input, expected;

            label = "b: Get depth 1 by relative.";
            input = "b";
            expected =
@"#b
b
##1
b-1
###1
b-1-1
###2
b-1-2
###3
b-1-3

##2
b-2
###1
b-2-1
###2
b-2-2
###3
b-2-3

##3
b-3
###1
b-3-1
###2
b-3-2
###3
b-3-3

";
            Run(label, input, expected);
        }

        [Test]
        public void T007()
        {
            string label, input, expected;

            label = "b-2: Get depth 2 by absolute.";
            input = "#b#2";
            expected =
@"##2
b-2
###1
b-2-1
###2
b-2-2
###3
b-2-3

";
            Run(label, input, expected);
        }

        [Test]
        public void T008()
        {
            string label, input, expected;

            label = "b-2-2: Get depth 3 by absolute.";
            input = "#b#2#2";
            expected =
@"###2
b-2-2
";
            Run(label, input, expected);
        }

        [Test]
        public void T009()
        {
            string label, input, expected;

            label = "c: Get depth 1 by absolute.";
            input = "#c";
            expected =
@"#c
c
##1
c-1
###1
c-1-1
###2
c-1-2
###3
c-1-3

##2
c-2
###1
c-2-1
###2
c-2-2
###3
c-2-3

##3
c-3
###1
c-3-1
###2
c-3-2
###3
c-3-3

";
            Run(label, input, expected);
        }

        [Test]
        public void T010()
        {
            string label, input, expected;

            label = "c: Get depth 1 by relative.";
            input = "c";
            expected =
@"#c
c
##1
c-1
###1
c-1-1
###2
c-1-2
###3
c-1-3

##2
c-2
###1
c-2-1
###2
c-2-2
###3
c-2-3

##3
c-3
###1
c-3-1
###2
c-3-2
###3
c-3-3

";
            Run(label, input, expected);
        }

        [Test]
        public void T011()
        {
            string label, input, expected;

            label = "c-3: Get depth 2 by absolute.";
            input = "#c#3";
            expected =
@"##3
c-3
###1
c-3-1
###2
c-3-2
###3
c-3-3

";
            Run(label, input, expected);
        }

        [Test]
        public void T012()
        {
            string label, input, expected;

            label = "c-3-3: Get depth 3 by absolute.";
            input = "#c#3#3";
            expected =
@"###3
c-3-3

";
            Run(label, input, expected);
        }

        public void Run(string lable, string input, string expected)
        {
            new TestCase(lable, input, expected, Test).Run();
        }

        public string Test(TestCase c)
        {
            return Master.Sub(c.Input).ToString();
        }
    }

    [TestFixture]
    public class DeliSubFxt2
    {
        public Deli Master;

        [SetUp]
        public void SetUp()
        {
            #region master data
            Deli tmp = Deli.Load(@"
#a
a
##1
a-1
###1
a-1-1
###2
a-1-2
###3
a-1-3

##2
a-2
###1
a-2-1
###2
a-2-2
###3
a-2-3

##3
a-3
###1
a-3-1
###2
a-3-2
###3
a-3-3

#b
b
##1
b-1
###1
b-1-1
###2
b-1-2
###3
b-1-3

##2
b-2
###1
b-2-1
###2
b-2-2
###3
b-2-3

##3
b-3
###1
b-3-1
###2
b-3-2
###3
b-3-3

#c
c
##1
c-1
###1
c-1-1
###2
c-1-2
###3
c-1-3

##2
c-2
###1
c-2-1
###2
c-2-2
###3
c-2-3

##3
c-3
###1
c-3-1
###2
c-3-2
###3
c-3-3

");
            Master = tmp.Sub("#a#1");
            #endregion
        }

        [Test]
        public void T001()
        {
            string label, input, expected;

            label = "a: Get depth 1 by absolute.";
            input = "#a";
            expected =
@"#a
a
##1
a-1
###1
a-1-1
###2
a-1-2
###3
a-1-3

##2
a-2
###1
a-2-1
###2
a-2-2
###3
a-2-3

##3
a-3
###1
a-3-1
###2
a-3-2
###3
a-3-3

";
            Run(label, input, expected);
        }

        [Test]
        public void T002()
        {
            string label, input, expected;

            label = "a-1: Get depth 2 by absolute.";
            input = "#a#1";
            expected =
@"##1
a-1
###1
a-1-1
###2
a-1-2
###3
a-1-3

";
            Run(label, input, expected);
        }

        [Test]
        public void T003()
        {
            string label, input, expected;

            label = "a-1-1: Get depth 3 by absolute.";
            input = "#a#1#1";
            expected =
@"###1
a-1-1
";
            Run(label, input, expected);
        }

        [Test]
        public void T004()
        {
            string label, input, expected;

            label = "a-1-1: Get depth 3 by relative.";
            input = "1";
            expected =
@"###1
a-1-1
";
            Run(label, input, expected);
        }

        [Test]
        public void T005()
        {
            string label, input, expected;

            label = "a-1-2: Get depth 3 by relative.";
            input = "2";
            expected =
@"###2
a-1-2
";
            Run(label, input, expected);
        }

        [Test]
        public void T006()
        {
            string label, input, expected;

            label = "a-1-3: Get depth 3 by relative.";
            input = "3";
            expected =
@"###3
a-1-3

";
            Run(label, input, expected);
        }

        [Test]
        public void T007()
        {
            string label, input, expected;

            label = "b: Get depth 1 by absolute.";
            input = "#b";
            expected =
@"#b
b
##1
b-1
###1
b-1-1
###2
b-1-2
###3
b-1-3

##2
b-2
###1
b-2-1
###2
b-2-2
###3
b-2-3

##3
b-3
###1
b-3-1
###2
b-3-2
###3
b-3-3

";
            Run(label, input, expected);
        }

        public void Run(string lable, string input, string expected)
        {
            new TestCase(lable, input, expected, Test).Run();
        }

        public string Test(TestCase c)
        {
            return Master.Sub(c.Input).ToString();
        }
    }
}
