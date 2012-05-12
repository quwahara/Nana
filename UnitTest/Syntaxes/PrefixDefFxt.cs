/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnitTest.Util;
using Nana.Delegates;
using Nana.Syntaxes;
using Nana.Infr;
using Nana;

namespace UnitTest.Syntaxes
{
    [TestFixture]
    public class SentenceDefFxt
    {
        public string Inp;
        public string Epc;

        [SetUp]
        public void SetUp()
        {
            string asm = GetType().Name;

            Inp = "";
            Epc = "";
        }

        static public string GetDef(string key)
        {
            return SFList.FromText(PrefixAnalyzer.DefsText).Find(delegate(string ln) { return ln.Trim().StartsWith(key); }).Trim();
        }

        [Test]
        public void T101_Source()
        {
            Inp = GetDef(@"0Source");
            Epc = @"0Source:ValueClause:1:Source:(Expr:Expr:*:(0End), 0End:Value:1)";
            Test();
        }
        [Test]
        public void T102_IdDotDecDef()
        {
            Inp = GetDef(@"0iddotdec");
            Epc = @"0iddotdec:ValueClause:1:(Id:GroupClause:1:Id:(.:ValueClause:*:(Id:Group:1:Id)))";
            Test();
        }

        [Test]
        public void T103_UsingDef()
        {
            Inp = GetDef(@"using");
            Epc = @"using:ValueClause:1:Using:(Expr:Expr:1)";
            Test();
        }

        [Test]
        public void T104_TypeDecDef()
        {
            Inp = GetDef(@"0typedec");
            Epc = @"0typedec:ValueClause:1:(\::ValueClause:?:TypeSpec:(Expr:Expr:1))";
            Test();
        }

        [Test]
        public void T106_AttrDecDef()
        {
            Inp = GetDef(@"0attrdec");
            Epc = @"0attrdec:ValueClause:1:(@:ValueClause:*:Attr:(Expr:Expr:1))";
            Test();
        }

        [Test]
        public void T107_BodyDecDef()
        {
            Inp = GetDef(@"0bodydec");
            Epc = @"0bodydec:ValueClause:1:(Bgn:GroupClause:1:Block:(Expr:Expr:*:(End)), End:Group:1:End)";

            Test();
        }

        [Test]
        public void T108_FuncDecDef()
        {
            Inp = GetDef(@"0funcdec");
            Epc = @"0funcdec:ValueClause:1:(\(:ValueClause:1:PrmDef:(Expr:Expr:?:())), \):Value:1, 0typedec:Refer:1, 0attrdec:Refer:1, 0bodydec:Refer:1)";
            Test();
        }

        [Test]
        public void T109_FunDef()
        {
            Inp = GetDef(@"Fun");
            Epc = @"Fun:GroupClause:1:Fun:(Id:Group:1:Id, 0funcdec:Refer:1)";
            Test();
        }

        public void Test()
        {
            Func<TestCase, string> f = delegate(TestCase c)
            {
                PrefixDef p;
                string s;
                p = PrefixDef.FromInline(Inp);
                s = p.ToString();
                return s;
            };

            new TestCase("", Inp, Epc, f).Run();
        }

    }
}
