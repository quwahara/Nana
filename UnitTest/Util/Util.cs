/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using NUnit.Framework;
using Nana.Delegates;
using Nana.Infr;
using System.IO;

namespace UnitTest.Util
{
    public class TestCase
    {
        public string Label = "";
        public string Input = "";
        public string Expected = "";
        public Func<TestCase, string> Test = Nana.Delegates.Util.NullFunc<TestCase, string>;
        public string Actual = "";
        public string Result = "NG";

        public TestCase(string label, string input, string expected, Func<TestCase, string> test)
        {
            this.Label = label;
            this.Input = input;
            this.Expected = expected;
            this.Test = test;
        }

        public void Run()
        {
            Actual = Test(this);
            List<string> inp = PutNo(Sty.ToStringList(Input));
            List<string> epc = PutNo(Sty.ToStringList(Expected));
            List<string> act = PutNo(Sty.ToStringList(Actual));
            string epcln = "", actln = "";
            Result = "OK";
            for (int i = 0; i < Math.Max(epc.Count, act.Count); ++i)
            {
                epcln = i < epc.Count ? epc[i] : "(--- End ---)";
                actln = i < act.Count ? act[i] : "(--- End ---)";
                if (epcln != actln)
                {
                    Result = "NG";
                    break;
                }
            }
            string rep = ToReport(Label, Result, inp, epc, act);
            Debug.WriteLine(rep);
            Assert.That(actln, Is.EqualTo(epcln), Label);
        }

        public static string ToReport(string Label, string Result, List<string> inp, List<string> epc, List<string> act)
        {
            StringBuilder b = new StringBuilder();
            if (Sty.NotNullOrEmpty(Label)) b.AppendLine("--- " + Label + " ---");
            b.AppendLine("R: " + Result);
            b.AppendLine("I:");
            b.Append(Cty.ToText(inp));
            b.AppendLine("E:");
            b.Append(Cty.ToText(epc));
            b.AppendLine("A:");
            b.Append(Cty.ToText(act));
            return b.ToString();
        }

        public static List<string> PutNo(List<string> ls)
        {
            int no = 0;
            return ls.ConvertAll<string>(delegate(string ln)
            { ++no; return no.ToString("0000") + ": " + ln; });
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            if (Sty.NotNullOrEmpty(Label)) b.AppendLine("--- " + Label + " ---");
            b.AppendLine("R: " + Result);
            b.AppendLine("I: " + Input);
            b.AppendLine("E: " + Expected);
            b.AppendLine("A: " + Actual);
            return b.ToString();
        }
    }

    /// <summary>Test Utility</summary>
    public class Tty
    {
        static public Func<string> SFunc(object o) { return delegate() { return o.ToString(); }; }
    }
}
