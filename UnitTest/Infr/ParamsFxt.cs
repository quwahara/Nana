/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Nana.Infr;
using UnitTest.Util;

namespace UnitTest.Infr
{
    [TestFixture]
    public class ParamsSplitFxt
    {
        public Deli QuotesTest;

        [SetUp]
        public void SetUp()
        {
            QuotesTest = Deli.Load(TestsText).Sub("QuotesTest");
        }

        [Test]
        public void Run()
        {
            foreach (string casename in Deli.SubNames(QuotesTest))
            {
                Deli c = QuotesTest.Sub(casename);
                Dictionary<string, string> cdic = Cty.ToDic(c.List);
                if (cdic.ContainsKey("inp") == false) { continue; };
                if (cdic.ContainsKey("epc") == false) { continue; };
                new TestCase(c.Name, cdic["inp"], cdic["epc"], Test).Run();
            }
        }

        public string Test(TestCase c)
        {
            string[] spl;
            string inp;
            StringBuilder act;
            inp = c.Input;
            if (inp == "(null)") inp = null;
            if (inp == "(0len)") inp = "";
            spl = Params.Split(inp, @"\s+", "<<", ">>", @"\");
            act = new StringBuilder();
            act.Append("`");
            if (spl.Length >= 1) act.Append(spl[0]);
            for (int i = 1; i < spl.Length; i++)
            {
                act.Append("-").Append(spl[i]);
            }
            act.Append("`");


            return act.ToString();
        }

        #region TestsText
        static public string TestsText = @"

--------------------
#QuotesTest
--------------------
## <\>a>
inp <<\>>a>>
epc `<<\>>a>>`

## <\<a\b>
inp <<\>>a\>>b>>
epc `<<\>>a\>>b>>`

## <a\>>
inp <<a\>>>>
epc `<<a\>>>>`

## <a\>b\>>
inp <<a\>>b\>>>>
epc `<<a\>>b\>>>>`

## \<-<a>
inp \<< <<a>>
epc `\<<-<<a>>`

## \<a\<-<b>
inp \<<a\<< <<b>>
epc `\<<a\<<-<<b>>`

## \
inp \
epc `\`

## \a
inp \a
epc `\a`

## a\
inp a\
epc `a\`

## <1>-<1>
inp <<a>>  <<b>>
epc `<<a>>-<<b>>`

## <2>-2-<2>
inp <<aa>> bb <<cc>>
epc `<<aa>>-bb-<<cc>>`

## null
inp (null)
epc ``

## 0-Length
inp (0len)
epc ``

## 1
inp a
epc `a`

## 2
inp ab
epc `ab`

## 3
inp abc
epc `abc`

## 1-1
inp a b
epc `a-b`

## 2-1
inp aa b
epc `aa-b`

## 1-2
inp a bb
epc `a-bb`

## 2-2
inp aa bb
epc `aa-bb`

## 3-3-3
inp aaa   bbb   ccc
epc `aaa-bbb-ccc`

## <1>
inp <<a>>
epc `<<a>>`

## <2>
inp <<aa>>
epc `<<aa>>`

## <3>
inp <<aaa>>
epc `<<aaa>>`

## <2>-<1>
inp <<aa>>  <<b>>
epc `<<aa>>-<<b>>`

## <1>-<2>
inp <<a>>  <<bb>>
epc `<<a>>-<<bb>>`

## <2>-<2>
inp <<aa>>  <<bb>>
epc `<<aa>>-<<bb>>`

## <3>-<3>-<3>
inp <<aaa>>  <<bbb>>  <<ccc>>
epc `<<aaa>>-<<bbb>>-<<ccc>>`

## 2-<2>
inp aa <<bb>>
epc `aa-<<bb>>`

## <2>-2
inp <<aa>> bb
epc `<<aa>>-bb`

## 2-2-<2>
inp aa bb <<cc>>
epc `aa-bb-<<cc>>`

## 2-<2>-2
inp aa <<bb>> cc
epc `aa-<<bb>>-cc`

## <2>-2-<2>
inp <<aa>> bb <<cc>>
epc `<<aa>>-bb-<<cc>>`

";
        #endregion
    }
}
