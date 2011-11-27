using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnitTest.Util;
using Nana.Syntaxes;
using Nana;
using Nana.Tokens;

namespace UnitTest.Syntaxes.CmdLnArgsFxt
{
    [TestFixture]
    public class PickOpt
    {
        [Test]
        public void T001()
        {
            string label, input, expected;

            label = "";
            input = "/reference:System.Windows.Forms.dll";
            expected =
@"opt[reference]
val[System.Windows.Forms.dll]
";

            Run(label, input, expected);
        }

        [Test]
        public void T002()
        {
            string label, input, expected;

            label = "";
            input = "-reference:System.Windows.Forms.dll";
            expected =
@"opt[reference]
val[System.Windows.Forms.dll]
";

            Run(label, input, expected);
        }

        [Test]
        public void T003()
        {
            string label, input, expected;

            label = "supply not supported option";
            input = "/unknown:System.Windows.Forms.dll";
            expected =
@"(null)";

            Run(label, input, expected);
        }

        [Test]
        public void T004()
        {
            string label, input, expected;

            label = "supply no option";
            input = "";
            expected =
@"(null)";

            Run(label, input, expected);
        }

        public void Run(string lable, string input, string expected)
        {
            new TestCase(lable, input, expected, Test).Run();
        }

        public string Test(TestCase c)
        {
            Token act = CmdLnArgs.PickOpt(c.Input);
            return act == null
                ? "(null)"
                : string.Format("opt[{0}]\r\nval[{1}]\r\n", act.Group, act.Value)
                ;
        }
    }

    [TestFixture]
    public class GetCmdLnArgs
    {
        [Test]
        public void T001()
        {
            string label, inp, epc;

            label = "";
            inp = @"c:\dir\a.nana";
            epc = @"/out:c:\dir\a.exe c:\dir\a.nana";

            Run(label, inp, epc);
        }

        [Test]
        public void T002()
        {
            string label, inp, epc;

            label = "";
            inp = "a.nana b.nana";
            epc = "/out:a.exe a.nana b.nana";

            Run(label, inp, epc);
        }

        [Test]
        public void T003()
        {
            string label, inp, epc;

            label = "";
            inp = "/out:a.exe a.nana";
            epc = "/out:a.exe a.nana";

            Run(label, inp, epc);
        }

        [Test]
        public void T004()
        {
            string label, inp, epc;

            label = "";
            inp = "/out:a.exe a.nana b.nana";
            epc = "/out:a.exe a.nana b.nana";

            Run(label, inp, epc);
        }

        [Test]
        public void T005()
        {
            string label, inp, epc;

            label = "";
            inp = "/out:a.exe /reference:refa a.nana b.nana";
            epc = "/out:a.exe /reference:refa a.nana b.nana";

            Run(label, inp, epc);
        }

        public void Run(string lable, string input, string expected)
        {
            new TestCase(lable, input, expected, Test).Run();
        }

        public string Test(TestCase c)
        {
            string[] args = c.Input.Split(new char[] { ' ' });
            Token act = CmdLnArgs.GetCmdLnArgs(args);
            if (act == null)                    /**/ return "(act == null)";

            Token[] cmpopts = act.Find("@Arguments/@CompileOptions");
            if (cmpopts == null || cmpopts.Length == 0) { return "(No CompileOptions token)"; }
            if (cmpopts.Length > 1) { return "(Too many CompileOptions tokens)"; }

            Token[] srcpaths = act.Find("@Arguments/@SourcePaths");
            if (srcpaths == null || srcpaths.Length == 0) { return "(No SourcePaths token)"; }
            if (srcpaths.Length > 1) { return "(Too many SourcePaths tokens)"; }

            StringBuilder b = new StringBuilder();
            string spl;

            // opts
            spl = "";
            foreach (Token t in  cmpopts[0].Follows)
            {
                b.Append(spl);
                b.Append("/");
                b.Append(t.Group);
                b.Append(":");
                b.Append(t.Value);
                spl = " ";
            }

            // srcs
            foreach (Token t in srcpaths[0].Follows)
            {
                b.Append(spl);
                b.Append(t.Value);
                spl = " ";
            }

            return b.ToString();
        }
    }

    [TestFixture]
    public class GetCmdLnArgs_Exceptions
    {
        [Test]
        public void T001()
        {
            string label, inp, epc;

            label = "";
            inp = "(null)";
            epc = @"ArgumentNullException";

            Run(label, inp, epc);
        }

        //[Test]
        //public void T002()
        //{
        //    string label, inp, epc;

        //    label = "";
        //    inp = "";
        //    epc = "No source file specified: ";

        //    Run(label, inp, epc);
        //}

        [Test]
        public void T003()
        {
            string label, inp, epc;

            label = "";
            inp = "/unknown:xxx";
            epc = "Not supported option: /unknown:xxx";

            Run(label, inp, epc);
        }
        public void Run(string lable, string input, string expected)
        {
            new TestCase(lable, input, expected, Test).Run();
        }

        public string Test(TestCase c)
        {
            string[] args = c.Input.Split(new char[] { ' ' });
            if (args[0] == "(null)") args = null;
            try
            {
                Token act = CmdLnArgs.GetCmdLnArgs(args);
                return "(no exception)";
            }
            catch (ArgumentNullException ane)
            {
                return ane.GetType().Name;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
