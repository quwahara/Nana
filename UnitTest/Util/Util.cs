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
            if (Actual == Expected) Result = "OK";
            Debug.WriteLine(ToString());
            Assert.That(Actual, Is.EqualTo(Expected), Label);
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
