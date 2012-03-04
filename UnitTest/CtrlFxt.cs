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
using Nana.Infr;

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

        public List<string> References = new List<string>();
        public string Inp;
        public string EpcSyn;
        public string EpcIL;

        [SetUp]
        public void SetUp()
        {
            References.Clear();
            string asm = GetType().Name;
            Inp = "";
            EpcSyn = "";
            EpcIL = "";
        }

        [Test]
        //  T:  20, B:  11
        public void TB1120_HelloWorld()
        {
            Inp = @"`p(""Hello, World!"")";
            
            EpcSyn = @"0Source
+---[0](
    +---[F]`p
    +---[S]""Hello, World!""
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

        [Test]
        public void TB1217_CallMethodOfLiteral()
        {
            Inp = @"1.ToString() -> s";

            EpcSyn = @"0Source
+---[0]->
    +---[F](
    |   +---[F].
    |   |   +---[F]1
    |   |   +---[S]ToString
    |   +---[T])
    +---[S]s
";

            EpcIL = @".field static string s
.field static int32 $000001
.method static public void .cctor() {
    ldc.i4 1
    stsfld int32 $000001
    ldsflda int32 $000001
    callvirt instance string int32::ToString()
    stsfld string s
    ret
}
.method static public void '0'() {
    .entrypoint
    ret
}
";

            Test();
        }

        [Test]
        public void TB1218_Comments()
        {
            Inp = @"
`p(
//  a line comment
""Hello, World!""
/*  
    a block comment
 */
)
";

            EpcSyn = @"0Source
+---[0](
    +---[F]`p
    +---[S]""Hello, World!""
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

        [Test]
        public void TB1218_StringOperations()
        {
            Inp = @"
""abcde"".Substring(1, 3)   -> sss  //  sss is ""bcd""
`p(sss)
""abcde"".Length            -> len  //  len is 5
`p(len)
";

            EpcSyn = @"0Source
+---[0]->
|   +---[F](
|   |   +---[F].
|   |   |   +---[F]""abcde""
|   |   |   +---[S]Substring
|   |   +---[S],
|   |   |   +---[F]1
|   |   |   +---[S]3
|   |   +---[T])
|   +---[S]sss
+---[1](
|   +---[F]`p
|   +---[S]sss
|   +---[T])
+---[2]->
|   +---[F].
|   |   +---[F]""abcde""
|   |   +---[S]Length
|   +---[S]len
+---[3](
    +---[F]`p
    +---[S]len
    +---[T])
";

            EpcIL = @".field static string sss
.field static int32 len
.method static public void .cctor() {
    ldstr ""abcde""
    ldc.i4 1
    ldc.i4 3
    callvirt instance string string::Substring(int32, int32)
    stsfld string sss
    ldsfld string sss
    call void [mscorlib]System.Console::WriteLine(string)
    ldstr ""abcde""
    callvirt instance int32 string::get_Length()
    stsfld int32 len
    ldsfld int32 len
    call void [mscorlib]System.Console::WriteLine(int32)
    ret
}
.method static public void '0'() {
    .entrypoint
    ret
}
";

            Test();
        }

        [Test]
        public void TB1218_Keywords()
        {
            Inp = @"
class sub
....
    sfun Main():void
    ..
        pop()
    ,,
    sfun pop():void
    ..
    ,,
,,,,
";

            EpcSyn = @"0Source
+---[0]class
    +---[0]sub
    +---[1]....
    |   +---[0]sfun
    |   |   +---[0]Main
    |   |   +---[1](
    |   |   +---[2])
    |   |   +---[3]:
    |   |   |   +---[0]void
    |   |   +---[4]..
    |   |   |   +---[0](
    |   |   |       +---[F]pop
    |   |   |       +---[T])
    |   |   +---[5],,
    |   +---[1]sfun
    |       +---[0]pop
    |       +---[1](
    |       +---[2])
    |       +---[3]:
    |       |   +---[0]void
    |       +---[4]..
    |       +---[5],,
    +---[2],,,,
";

            EpcIL = @".class public 'sub' {
    .method static public void Main() {
        .entrypoint
        call void [NanaFxt]'sub'::'pop'()
        ret
    }
    .method static public void 'pop'() {
        ret
    }
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
        ret
    }
}
";

            Test();
        }

        [Test]
        public void TB1228_Return_Normal_1()
        {
            Inp = @"
class A
....
    sfun Main():int
    ..
        return  1
    ,,
    sfun IfElse():bool
    ..
        if true then
            return  true
        else
            return  false
        end
    ,,
    sfun IfElifElse():bool
    ..
        if true then
            return  true
        elif true then
            return  true
        else
            return  false
        end
    ,,
    sfun IfElifElifElse():bool
    ..
        if true then
            return  true
        elif true then
            return  true
        elif true then
            return  true
        else
            return  false
        end
    ,,
,,,,
";

            EpcIL = @".class public A {
    .method static public int32 Main() {
        .entrypoint
        ldc.i4 1
        ret
    }
    .method static public bool IfElse() {
        ldc.i4.1
        brfalse else$000001
        ldc.i4.1
        ret
        br endif$000001
else$000001:
        ldc.i4.0
        ret
endif$000001:
    }
    .method static public bool IfElifElse() {
        ldc.i4.1
        brfalse elif$000002_1
        ldc.i4.1
        ret
        br endif$000002
elif$000002_1:
        ldc.i4.1
        brfalse else$000002
        ldc.i4.1
        ret
        br endif$000002
else$000002:
        ldc.i4.0
        ret
endif$000002:
    }
    .method static public bool IfElifElifElse() {
        ldc.i4.1
        brfalse elif$000003_1
        ldc.i4.1
        ret
        br endif$000003
elif$000003_1:
        ldc.i4.1
        brfalse elif$000003_2
        ldc.i4.1
        ret
        br endif$000003
elif$000003_2:
        ldc.i4.1
        brfalse else$000003
        ldc.i4.1
        ret
        br endif$000003
else$000003:
        ldc.i4.0
        ret
endif$000003:
    }
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
        ret
    }
}
";

            Test();
        }

        [Test]
        public void TB1228_Return_Normal_2_IfIf()
        {
            Inp = @"
class A
....
    sfun Main():int
    ..
        if true then
            if true then
                return  21
            else
                return  22
            end
        else
            if true then
                return  31
            else
                return  32
            end
        end
    ,,
,,,,
";

            EpcIL = @".class public A {
    .method static public int32 Main() {
        .entrypoint
        ldc.i4.1
        brfalse else$000001
        ldc.i4.1
        brfalse else$000002
        ldc.i4 21
        ret
        br endif$000002
else$000002:
        ldc.i4 22
        ret
endif$000002:
        br endif$000001
else$000001:
        ldc.i4.1
        brfalse else$000003
        ldc.i4 31
        ret
        br endif$000003
else$000003:
        ldc.i4 32
        ret
endif$000003:
endif$000001:
    }
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
        ret
    }
}
";

            Test();
        }

        [Test]
        public void TB1228_Return_Error_1_If()
        {
            Inp = @"
class A
....
    sfun Main():int
    ..
        if true then
            return  1
        end
    ,,
,,,,
";
            EpcIL = @"(ERROR)Function doesn't return value";
            Test();
        }

        [Test]
        public void TB1228_Return_Error_2_IfThenRetElse()
        {
            Inp = @"
class A
....
    sfun Main():int
    ..
        if true then
            return  1
        else
        end
    ,,
,,,,
";
            EpcIL = @"(ERROR)Function doesn't return value";
            Test();
        }

        [Test]
        public void TB1228_Return_Error_3_IfThenElseRet()
        {
            Inp = @"
class A
....
    sfun Main():int
    ..
        if true then
        else
            return  1
        end
    ,,
,,,,
";
            EpcIL = @"(ERROR)Function doesn't return value";
            Test();
        }

        [Test]
        public void TB1228_Return_Error_4_IfThenRetElifRet()
        {
            Inp = @"
class A
....
    sfun Main():int
    ..
        if      true    then
            return  1
        elif    true    then
            return  1
        end
    ,,
,,,,
";
            EpcIL = @"(ERROR)Function doesn't return value";
            Test();
        }

        [Test]
        public void TB1228_Return_Error_5_IfThenRetElifRetElse()
        {
            Inp = @"
class A
....
    sfun Main():int
    ..
        if      true    then
            return  1
        elif    true    then
            return  1
        else
        end
    ,,
,,,,
";
            EpcIL = @"(ERROR)Function doesn't return value";
            Test();
        }

        [Test]
        public void TB1228_Return_Error_6_IfThenRetElifElseRet()
        {
            Inp = @"
class A
....
    sfun Main():int
    ..
        if      true    then
            return  1
        elif    true    then
        else
            return  1
        end
    ,,
,,,,
";
            EpcIL = @"(ERROR)Function doesn't return value";
            Test();
        }

        [Test]
        public void TB1228_Return_Error_7_IfThenElifRetElseRet()
        {
            Inp = @"
class A
....
    sfun Main():int
    ..
        if      true    then
        elif    true    then
            return  1
        else
            return  1
        end
    ,,
,,,,
";
            EpcIL = @"(ERROR)Function doesn't return value";
            Test();
        }

        [Test]
        public void TC0109_Error_1_The_variable_is_already_defined()
        {
            Inp = @"
a:int
a:int
";
            EpcIL = @"(ERROR)The variable is already defined. Variable name:a";
            Test();
        }

        [Test]
        public void TC0109_Error_2_The_type_is_already_defined()
        {
            Inp = @"
class a ... ,,,
class a ... ,,,
";
            EpcIL = @"(ERROR)The type is already defined. Type name:a";
            Test();
        }

        [Test]
        public void TC0109_Error_3_The_function_is_already_defined()
        {
            Inp = @"
fun a() .. ,,
fun a() .. ,,
";
            EpcIL = @"(ERROR)The function is already defined. Function name:a";
            Test();
        }

        [Test]
        public void TC0211_RRR()
        {
            Inp = @"
a = b = c = 1
";
            EpcSyn = @"0Source
+---[0]=
    +---[F]a
    +---[S]=
        +---[F]b
        +---[S]=
            +---[F]c
            +---[S]1
";
            Test();
        }

        [Test]
        public void TC0227_WindowsForm()
        {
            References.Add("system.windows.forms.dll");

            Inp = @"
@System.STAThreadAttribute
fun Main():void
..
    System.Windows.Forms.Application.EnableVisualStyles()
    System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false)
    System.Windows.Forms.Application.Run(System.Windows.Forms.Form())
,,
        ";
            EpcSyn = @"";
            EpcIL = @".method static public void Main() {
    .custom instance void [mscorlib]System.STAThreadAttribute::.ctor()
    .entrypoint
    call void [System.Windows.Forms]System.Windows.Forms.Application::EnableVisualStyles()
    ldc.i4.0
    call void [System.Windows.Forms]System.Windows.Forms.Application::SetCompatibleTextRenderingDefault(bool)
    newobj instance void [System.Windows.Forms]System.Windows.Forms.Form::.ctor()
    call void [System.Windows.Forms]System.Windows.Forms.Application::Run(class [System.Windows.Forms]System.Windows.Forms.Form)
    ret
}
";
            Test();
        }

        //[Test]
        public void ZZZ()
        {

            Inp = @"
dir = ""..\\..\\..\\LICENSE""
fs = System.IO.Directory.GetFiles(dir, ""*.cs"", System.IO.SearchOption.AllDirectories)
//System.IO.SearchOption.AllDirectories   -> opt
//fs = System.IO.Directory.GetFiles(dir, ""*.cs"", opt)

//fun Main(argv:string[]):void
//..
////    cont = System.IO.File.ReadAll(""li.nana"")
////    cont = System.IO.File.ReadAllText(""li.nana"")
//    `p(cont)
//,,
";
            EpcIL = @"x";

            Test();
        }


//        [Test]
//        public void ZZZ_a()
//        {
//            Inp = @"
//a < b >
//a < b , d > d
//
////a < b.c >
////a < b . c , d . e >
//
////c < d
////f > g
////Substring(1, 3)
//";
//            EpcSyn = @"aa";
//            EpcIL = @"";
//            Test();
//        }

        public void Test()
        {
            Func<TestCase, string> f = delegate(TestCase c)
            {
                Token root = Ctrl.CreateRootTemplate();

                Assembly exeasmb = Assembly.GetExecutingAssembly();
                string name = GetType().Name;
                Token opt = root.Find("@CompileOptions");
                opt.FlwsAdd(Path.GetDirectoryName(exeasmb.Location), "include")
                    .FlwsAdd(name + ".exe", "out")
                    ;
                References.ForEach(delegate(string s) { opt.FlwsAdd(s, "reference"); });
                root.Find("@Sources").FlwsAdd(c.Input, "SourceText");

                Ctrl.Check(root);
                Ctrl ctrl = new Ctrl();

                StringBuilder b = new StringBuilder();
                Action<string> trace = delegate(string s_) { b.Append(s_); };
                try
                {
                    if (false == string.IsNullOrEmpty(EpcSyn))
                    {
                        ctrl.AfterSyntaxAnalyze = delegate(Token root_)
                        {
                            trace(TokenEx.ToTree(root_.Find("@Syntax").Follows[0]));
                        };
                    }
                    ctrl.Compile(root);
                    if (EpcIL != "")
                    { trace(root.Find("@Code").Value); }
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

                return b.ToString();
            };

            string epc;
            if (false == EpcIL.StartsWith("(ERROR)"))
            {
                epc = EpcSyn;
                if (EpcIL != "")
                {
                    string asm = GetType().Name;
                    StringBuilder b = new StringBuilder();
                    b.Append(".assembly extern mscorlib {.ver 2:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89)}").AppendLine();
                    if (References.Count > 0)
                    {
                        TypeInAssemblyLoader ldr = new TypeInAssemblyLoader();
                        References.ForEach(delegate(string s) { b.AppendLine(Nana.CodeGeneration.CodeGenerator.AssemblyExtern(ldr.LoadFrameworkClassLibrarie(s))); });
                    }
                    b.Append(".assembly ").Append(asm).Append(" { }").AppendLine();
                    b.Append(".module ").Append(asm).Append(".exe").AppendLine();
                    epc += b.ToString() + EpcIL;
                }
            }
            else
            {
                epc = EpcSyn + EpcIL.Substring("(ERROR)".Length);
            }

            new TestCase("", Inp, epc, f).Run();
        }
    }
}
