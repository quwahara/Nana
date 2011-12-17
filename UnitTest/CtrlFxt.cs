using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Nana.Delegates;
using Nana.Syntaxes;
using UnitTest.Util;
using System.Reflection;
using System.IO;
using Nana;
using Nana.Tokens;

namespace UnitTest
{
    public class Class0
    {
        private string Prv0()
        {
            int[,] a = new int[3,5];
            int b = a.Length;

            int[] d = new int[7];
            int e = d.Length;

            //int[] ar;
            //int len;
            //ar = null;
            //len = ar.Length;
            //len = ar.GetLength(0);
            //int[][] a;
            //int b;

            ////a:int[][]
            ////a       = int[9][]
            ////a[0]    = int[7]
            ////a[0][1] = 5
            ////b       = a[0][1]

            //a = new int[9][];
            //a[0] = new int[7];
            //a[0][1] = 5;
            //b = a[0][1];



            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        protected string Prt0()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        internal string Intr0()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        protected internal string PrtIntr0()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        public virtual string VPub0()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        public string Pub0()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
    }

    public class Class1 : Class0
    {
        public override string VPub0()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        public virtual string VPub1()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        public string Pub1()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
    }

    public class Class2 : Class1
    {
        public override string VPub0()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        public override string VPub1()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        public virtual string VPub2()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        public string Pub2()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
    }

    public class Aaa<T1>
    {
        public class AaaSub<T2>
        {
        }
    }

    public class Bbb
    {
        Aaa<int>.AaaSub<string> aaasub = new Aaa<int>.AaaSub<string>();
    }
}

namespace UnitTest
{
    [TestFixture]
    public class NanaFxt
    {
        public Token Root;

        public string Inp;
        public string EpcSyn;
        public string EpcILHeader;
        public string EpcIL;


        [SetUp]
        public void SetUp()
        {
            string asm = GetType().Name;
            Inp = "";
            EpcSyn = "";
            EpcILHeader = @".assembly extern mscorlib {.ver 2:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89)}
.assembly extern UnitTest {.ver 1:0:0:0}
.assembly " + asm + @" { }
.module " + asm + @".exe
";
            EpcIL = "";
        }

        [Test]
        public void HelloWorld()
        {
            Inp = @"`p(""Hello, World!"")";
            
            EpcSyn = @"0Source
+---[0](
    +---[F]`p
    +---[S]
    |   +---[0]""Hello, World!""
    +---[T])
";

            EpcIL = @".method static public void .cctor() {
    ldstr ""Hello, World!""
    call void [mscorlib]System.Console::WriteLine(string)
    ret
}
.method static public void '0'() {
    .entrypoint
    ret
}
";

            Test();
        }

        public void Test()
        {
            Func<TestCase, string> f = delegate(TestCase c)
            {
                Token root = Ctrl.CreateRootTemplate();

                Assembly exeasmb = Assembly.GetExecutingAssembly();
                string name = GetType().Name;
                root.Find("@Root/@CompileOptions")[0]
                    .FlwsAdd(Path.GetDirectoryName(exeasmb.Location), "include")
                    .FlwsAdd(Path.GetFileNameWithoutExtension(exeasmb.Location), "reference")
                    .FlwsAdd(name + ".exe", "out")
                    ;
                root.Find("@Root/@Sources")[0].FlwsAdd(c.Input, "SourceText");

                Ctrl.Check(root);
                Ctrl ctrl = new Ctrl();

                StringBuilder b = new StringBuilder();
                Action<string> trace = delegate(string s_) { b.Append(s_); };
                try
                {
                    ctrl.Compile(root);
                    trace(root.Find("@Root/@Code")[0].Value);
                }
                catch (Nana.Infr.Error e)
                {
                    trace(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
                catch (Exception ex)
                {
                    trace(ex.ToString());
                }

                return TokenEx.ToTree(root.Find("@Root/@Syntax")[0].Follows[0]) + b.ToString();
            };

            new TestCase("", Inp, EpcSyn + EpcILHeader + EpcIL, f).Run();
            
        }
    }
}
