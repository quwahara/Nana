/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

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
using Nana.Generations;

/// <summary>
/// Begins the invoke.
/// </summary>
/// <returns>
/// The invoke.
/// </returns>
/// <param name='xxx'>
/// Xxx.
/// </param>
/// <param name='yyy'>
/// Yyy.
/// </param>
/// <param name='callback'>
/// Callback.
/// </param>
/// <param name='object'>
/// Object.
/// </param>
/// <summary>
/// Invoke the specified xxx and yyy.
/// </summary>
/// <param name='xxx'>
/// Xxx.
/// </param>
/// <param name='yyy'>
/// Yyy.
/// </param>
/// <summary>
/// Initializes a new instance of the <see cref="DDD"/> delegate.
/// </summary>
/// <param name='object'>
/// Object.
/// </param>
/// <param name='method'>
/// Method.
/// </param>
/// <summary>
/// Ends the invoke.
/// </summary>
/// <returns>
/// The invoke.
/// </returns>
/// <param name='result'>
/// Result.
/// </param>
public delegate string DDD(int xxx, int yyy);

namespace UnitTest
{

    public class Class0
    {
        /// <summary>
        /// Sss this instance.
        /// </summary>
        public string Sss() { return "ssss"; }
        /// <summary>
        /// The FF.
        /// </summary>
        public int FFF;

        private string Prv0()
        {
            FFF = 999;
            int jjj = FFF;

            string vvv = "www";
            Func<string, string> f = delegate(string ppp) { return Sss() + ppp + vvv + "xxx"; };
            string yyy = f("qqq");

            List<List<string>> lls = new List<List<string>>();
            List<string> sls = new List<string>();
            string sss = sls[99];
            try
            {
            }
            catch (Exception ex)
            {
                string ss = ex.ToString();
            }
            finally
            {
                string t = "x";
            }

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
        /// <summary>
        /// Vs the pub0.
        /// </summary>
        /// <returns>
        /// The pub0.
        /// </returns>
        public virtual string VPub0()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        /// <summary>
        /// Pub0 this instance.
        /// </summary>
        public string Pub0()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
    }

    public class Class1 : Class0
    {
        /// <summary>
        /// Vs the pub0.
        /// </summary>
        /// <returns>
        /// The pub0.
        /// </returns>
        public override string VPub0()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        /// <summary>
        /// Vs the pub1.
        /// </summary>
        /// <returns>
        /// The pub1.
        /// </returns>
        public virtual string VPub1()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        /// <summary>
        /// Pub1 this instance.
        /// </summary>
        public string Pub1()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
    }

    public class Class2 : Class1
    {
        /// <summary>
        /// Vs the pub0.
        /// </summary>
        /// <returns>
        /// The pub0.
        /// </returns>
        public override string VPub0()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        /// <summary>
        /// Vs the pub1.
        /// </summary>
        /// <returns>
        /// The pub1.
        /// </returns>
        public override string VPub1()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        /// <summary>
        /// Vs the pub2.
        /// </summary>
        /// <returns>
        /// The pub2.
        /// </returns>
        public virtual string VPub2()
        {
            string s = GetType().FullName + "." + MethodBase.GetCurrentMethod().Name;
            Console.WriteLine(s);
            return s;
        }
        /// <summary>
        /// Pub2 this instance.
        /// </summary>
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
        /// <summary>
        /// The root.
        /// </summary>
        public Token Root;

        /// <summary>
        /// The references.
        /// </summary>
        public List<string> References = new List<string>();
        /// <summary>
        /// The inp.
        /// </summary>
        public string Inp;
        /// <summary>
        /// The epc syn.
        /// </summary>
        public string EpcSyn;
        /// <summary>
        /// The epc I.
        /// </summary>
        public string EpcIL;

        /// <summary>
        /// Sets up.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            References.Clear();
            string asm = GetType().Name;
            Inp = "";
            EpcSyn = "";
            EpcIL = "";
        }

        /// <summary>
        /// Ts the b1120_ hello world.
        /// </summary>
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

            EpcIL = @".method public static void .cctor() {
    ldstr ""Hello, World!""
    call void [mscorlib]System.Console::WriteLine(string)
rp$000001:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000002:
    ret
}
";

            Test();
        }

        /// <summary>
        /// Ts the b1217_ call method of literal.
        /// </summary>
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
.field static int32 $000003
.method public static void .cctor() {
    ldc.i4 1
    stsfld int32 $000003
    ldsflda int32 $000003
    callvirt instance string int32::ToString()
    stsfld string s
rp$000001:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000002:
    ret
}
";

            Test();
        }

        /// <summary>
        /// Ts the b1218_ comments.
        /// </summary>
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

            EpcIL = @".method public static void .cctor() {
    ldstr ""Hello, World!""
    call void [mscorlib]System.Console::WriteLine(string)
rp$000001:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000002:
    ret
}
";

            Test();
        }

        /// <summary>
        /// Ts the b1218_ string operations.
        /// </summary>
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
.method public static void .cctor() {
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
rp$000001:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000002:
    ret
}
";

            Test();
        }

        /// <summary>
        /// Ts the b1218_ keywords.
        /// </summary>
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
    .method public static void Main() {
        .entrypoint
        call void [NanaFxt]'sub'::'pop'()
rp$000001:
        ret
    }
    .method public static void 'pop'() {
rp$000002:
        ret
    }
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
rp$000003:
        ret
    }
}
";

            Test();
        }

        /// <summary>
        /// Ts the b1228_ return_ normal_1.
        /// </summary>
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
    .method public static int32 Main() {
        .entrypoint
        .locals (
            int32 '0rv$000001'
        )
        ldc.i4 1
        stloc '0rv$000001'
        br rp$000001
rp$000001:
        ldloc '0rv$000001'
        ret
    }
    .method public static bool IfElse() {
        .locals (
            bool '0rv$000002'
        )
        ldc.i4.1
        brfalse else$000006
        ldc.i4.1
        stloc '0rv$000002'
        br rp$000002
        br endif$000006
else$000006:
        ldc.i4.0
        stloc '0rv$000002'
        br rp$000002
endif$000006:
rp$000002:
        ldloc '0rv$000002'
        ret
    }
    .method public static bool IfElifElse() {
        .locals (
            bool '0rv$000003'
        )
        ldc.i4.1
        brfalse elif$000007_1
        ldc.i4.1
        stloc '0rv$000003'
        br rp$000003
        br endif$000007
elif$000007_1:
        ldc.i4.1
        brfalse else$000007
        ldc.i4.1
        stloc '0rv$000003'
        br rp$000003
        br endif$000007
else$000007:
        ldc.i4.0
        stloc '0rv$000003'
        br rp$000003
endif$000007:
rp$000003:
        ldloc '0rv$000003'
        ret
    }
    .method public static bool IfElifElifElse() {
        .locals (
            bool '0rv$000004'
        )
        ldc.i4.1
        brfalse elif$000008_1
        ldc.i4.1
        stloc '0rv$000004'
        br rp$000004
        br endif$000008
elif$000008_1:
        ldc.i4.1
        brfalse elif$000008_2
        ldc.i4.1
        stloc '0rv$000004'
        br rp$000004
        br endif$000008
elif$000008_2:
        ldc.i4.1
        brfalse else$000008
        ldc.i4.1
        stloc '0rv$000004'
        br rp$000004
        br endif$000008
else$000008:
        ldc.i4.0
        stloc '0rv$000004'
        br rp$000004
endif$000008:
rp$000004:
        ldloc '0rv$000004'
        ret
    }
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
rp$000005:
        ret
    }
}
";

            Test();
        }

        /// <summary>
        /// Ts the b1228_ return_ normal_2_ if if.
        /// </summary>
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
    .method public static int32 Main() {
        .entrypoint
        .locals (
            int32 '0rv$000001'
        )
        ldc.i4.1
        brfalse else$000003
        ldc.i4.1
        brfalse else$000004
        ldc.i4 21
        stloc '0rv$000001'
        br rp$000001
        br endif$000004
else$000004:
        ldc.i4 22
        stloc '0rv$000001'
        br rp$000001
endif$000004:
        br endif$000003
else$000003:
        ldc.i4.1
        brfalse else$000005
        ldc.i4 31
        stloc '0rv$000001'
        br rp$000001
        br endif$000005
else$000005:
        ldc.i4 32
        stloc '0rv$000001'
        br rp$000001
endif$000005:
endif$000003:
rp$000001:
        ldloc '0rv$000001'
        ret
    }
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
rp$000002:
        ret
    }
}
";
            Test();
        }

        /// <summary>
        /// Ts the b1228_ return_ error_1_ if.
        /// </summary>
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

        /// <summary>
        /// Ts the b1228_ return_ error_2_ if then ret else.
        /// </summary>
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

        /// <summary>
        /// Ts the b1228_ return_ error_3_ if then else ret.
        /// </summary>
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

        /// <summary>
        /// Ts the b1228_ return_ error_4_ if then ret elif ret.
        /// </summary>
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

        /// <summary>
        /// Ts the b1228_ return_ error_5_ if then ret elif ret else.
        /// </summary>
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

        /// <summary>
        /// Ts the b1228_ return_ error_6_ if then ret elif else ret.
        /// </summary>
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

        /// <summary>
        /// Ts the b1228_ return_ error_7_ if then elif ret else ret.
        /// </summary>
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

        /// <summary>
        /// Ts the c0109_ error_1_ the_variable_is_already_defined.
        /// </summary>
        [Test]
        public void TC0109_Error_1_The_variable_is_already_defined()
        {
            Inp = @"
a:int
a:int
";
            EpcIL = @"(ERROR)The a is already defined in NanaFxt.exe";
            Test();
        }

        /// <summary>
        /// Ts the c0109_ error_2_ the_type_is_already_defined.
        /// </summary>
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

        /// <summary>
        /// Ts the c0109_ error_3_ the_function_is_already_defined.
        /// </summary>
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

        /// <summary>
        /// Ts the c0211_ RR.
        /// </summary>
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

        /// <summary>
        /// Ts the c0227_ windows form.
        /// </summary>
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
            EpcIL = @".method public static void Main() {
    .custom instance void [mscorlib]System.STAThreadAttribute::.ctor()
    .entrypoint
    call void [System.Windows.Forms]System.Windows.Forms.Application::EnableVisualStyles()
    ldc.i4.0
    call void [System.Windows.Forms]System.Windows.Forms.Application::SetCompatibleTextRenderingDefault(bool)
    newobj instance void [System.Windows.Forms]System.Windows.Forms.Form::.ctor()
    call void [System.Windows.Forms]System.Windows.Forms.Application::Run(class [System.Windows.Forms]System.Windows.Forms.Form)
rp$000001:
    ret
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0311_ set propety.
        /// </summary>
        [Test]
        public void TC0311_SetPropety()
        {
            References.Add("system.windows.forms.dll");

            Inp = @"
using System.Windows.Forms
@STAThreadAttribute
fun Main():void
..
    Application.EnableVisualStyles()
    Application.SetCompatibleTextRenderingDefault(false)
    Form()  -> f
    ""hoge"" -> f.Text
    Application.Run(f)
,,
        ";
            EpcSyn = @"";
            EpcIL = @".method public static void Main() {
    .custom instance void [mscorlib]System.STAThreadAttribute::.ctor()
    .entrypoint
    .locals (
        class [System.Windows.Forms]System.Windows.Forms.Form f
    )
    call void [System.Windows.Forms]System.Windows.Forms.Application::EnableVisualStyles()
    ldc.i4.0
    call void [System.Windows.Forms]System.Windows.Forms.Application::SetCompatibleTextRenderingDefault(bool)
    newobj instance void [System.Windows.Forms]System.Windows.Forms.Form::.ctor()
    stloc f
    ldloc f
    ldstr ""hoge""
    callvirt instance void [System.Windows.Forms]System.Windows.Forms.Form::set_Text(string)
    ldloc f
    call void [System.Windows.Forms]System.Windows.Forms.Application::Run(class [System.Windows.Forms]System.Windows.Forms.Form)
rp$000001:
    ret
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0315_ exception handling.
        /// </summary>
        [Test]
        public void TC0315_ExceptionHandling()
        {
            Inp =
@"
try
    throw Exception()
catch IndexOutOfRangeException do
catch ex:Exception do
finally
end
";
            EpcSyn = @"0Source
+---[0]try
    +---[0]throw
    |   +---[0](
    |       +---[F]Exception
    |       +---[T])
    +---[1]catch
    |   +---[0]IndexOutOfRangeException
    |   +---[1]do
    +---[2]catch
    |   +---[0]:
    |   |   +---[F]ex
    |   |   +---[S]Exception
    |   +---[1]do
    +---[3]finally
    +---[4]end
";

            EpcIL = @".field static class [mscorlib]System.Exception ex
.method public static void .cctor() {
    .try {
    .try {
    newobj instance void [mscorlib]System.Exception::.ctor()
    throw
    leave exitcatch$000001
    } catch [mscorlib]System.IndexOutOfRangeException {
    pop
    leave exitcatch$000001
    } catch [mscorlib]System.Exception {
    stsfld class [mscorlib]System.Exception ex
    leave exitcatch$000001
    }
exitcatch$000001:
    leave exitfinally$000001
    } finally {
    endfinally
    }
exitfinally$000001:
rp$000002:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000003:
    ret
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0315_ list_string.
        /// </summary>
        [Test]
        public void TC0315_List_string()
        {
            Inp =
@"
System.Collections.Generic.List`<string>()
-> ls
ls.IndexOf("""") -> i
System.Console.WriteLine(i)
";
            EpcSyn = @"0Source
+---[0]->
|   +---[F](
|   |   +---[F]`<
|   |   |   +---[F].
|   |   |   |   +---[F].
|   |   |   |   |   +---[F].
|   |   |   |   |   |   +---[F]System
|   |   |   |   |   |   +---[S]Collections
|   |   |   |   |   +---[S]Generic
|   |   |   |   +---[S]List
|   |   |   +---[S]string
|   |   |   +---[T]>
|   |   +---[T])
|   +---[S]ls
+---[1]->
|   +---[F](
|   |   +---[F].
|   |   |   +---[F]ls
|   |   |   +---[S]IndexOf
|   |   +---[S]""""
|   |   +---[T])
|   +---[S]i
+---[2](
    +---[F].
    |   +---[F].
    |   |   +---[F]System
    |   |   +---[S]Console
    |   +---[S]WriteLine
    +---[S]i
    +---[T])
";
            EpcIL = @".field static class [mscorlib]System.Collections.Generic.List`1<string> ls
.field static int32 i
.method public static void .cctor() {
    newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
    stsfld class [mscorlib]System.Collections.Generic.List`1<string> ls
    ldsfld class [mscorlib]System.Collections.Generic.List`1<string> ls
    ldstr """"
    callvirt instance int32 class [mscorlib]System.Collections.Generic.List`1<string>::IndexOf(!0)
    stsfld int32 i
    ldsfld int32 i
    call void [mscorlib]System.Console::WriteLine(int32)
rp$000001:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000002:
    ret
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0415_ list_ list_string.
        /// </summary>
        [Test]
        public void TC0415_List_List_string()
        {
            Inp =
@"
System.Collections.Generic.List`< System.Collections.Generic.List`<string> >()
-> ls
ls.Count -> i
System.Console.WriteLine(i)
";
            EpcSyn = @"0Source
+---[0]->
|   +---[F](
|   |   +---[F]`<
|   |   |   +---[F].
|   |   |   |   +---[F].
|   |   |   |   |   +---[F].
|   |   |   |   |   |   +---[F]System
|   |   |   |   |   |   +---[S]Collections
|   |   |   |   |   +---[S]Generic
|   |   |   |   +---[S]List
|   |   |   +---[S]`<
|   |   |   |   +---[F].
|   |   |   |   |   +---[F].
|   |   |   |   |   |   +---[F].
|   |   |   |   |   |   |   +---[F]System
|   |   |   |   |   |   |   +---[S]Collections
|   |   |   |   |   |   +---[S]Generic
|   |   |   |   |   +---[S]List
|   |   |   |   +---[S]string
|   |   |   |   +---[T]>
|   |   |   +---[T]>
|   |   +---[T])
|   +---[S]ls
+---[1]->
|   +---[F].
|   |   +---[F]ls
|   |   +---[S]Count
|   +---[S]i
+---[2](
    +---[F].
    |   +---[F].
    |   |   +---[F]System
    |   |   +---[S]Console
    |   +---[S]WriteLine
    +---[S]i
    +---[T])
";
            EpcIL = @".field static class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Collections.Generic.List`1<string>> ls
.field static int32 i
.method public static void .cctor() {
    newobj instance void class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Collections.Generic.List`1<string>>::.ctor()
    stsfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Collections.Generic.List`1<string>> ls
    ldsfld class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Collections.Generic.List`1<string>> ls
    callvirt instance int32 class [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Collections.Generic.List`1<string>>::get_Count()
    stsfld int32 i
    ldsfld int32 i
    call void [mscorlib]System.Console::WriteLine(int32)
rp$000001:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000002:
    ret
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0421_ field.
        /// </summary>
        [Test]
        public void TC0421_Field()
        {
            Inp =
@"
class C
...
    Field : int

    sfun Main():void
    ..
        c       = C()
        c.Field = 7
        Console.WriteLine(c.Field);
    ,,
,,,
";
            EpcSyn = @"";
            EpcIL = @".class public C {
    .field int32 Field
    .method public static void Main() {
        .entrypoint
        .locals (
            class [NanaFxt]C c
        )
        newobj instance void [NanaFxt]C::.ctor()
        stloc c
        ldloc c
        ldc.i4 7
        stfld int32 [NanaFxt]C::Field
        ldloc c
        ldfld int32 [NanaFxt]C::Field
        call void [mscorlib]System.Console::WriteLine(int32)
rp$000001:
        ret
    }
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
rp$000002:
        ret
    }
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0504_ multicast delegate inheritance.
        /// </summary>
        [Test]
        public void TC0504_MulticastDelegateInheritance()
        {
            Inp =
@"
class C
...
    fun M()
    ..
        `p(""Hi"")
    ,,
,,,

class MyD -> MulticastDelegate
...
    cons (obj:object, mtd:IntPtr) .. ,,
    fun Invoke() .. ,,
,,,

C() -> c
MyD(c, C.M) -> d
d.Invoke()
d()
";
            EpcSyn = @"";

            EpcIL = @".field static class [NanaFxt]C c
.field static class [NanaFxt]MyD d
.class public C {
    .method public virtual void M() {
        ldstr ""Hi""
        call void [mscorlib]System.Console::WriteLine(string)
rp$000001:
        ret
    }
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
rp$000004:
        ret
    }
}
.class public sealed MyD extends [mscorlib]System.MulticastDelegate {
    .method public hidebysig newslot void .ctor(object obj, native int mtd) runtime {
    }
    .method public newslot void Invoke() runtime {
    }
}
.method public static void .cctor() {
    newobj instance void [NanaFxt]C::.ctor()
    stsfld class [NanaFxt]C c
    ldsfld class [NanaFxt]C c
    ldftn instance void [NanaFxt]C::M()
    newobj instance void [NanaFxt]MyD::.ctor(object, native int)
    stsfld class [NanaFxt]MyD d
    ldsfld class [NanaFxt]MyD d
    callvirt instance void [NanaFxt]MyD::Invoke()
    ldsfld class [NanaFxt]MyD d
    callvirt instance void [NanaFxt]MyD::Invoke()
rp$000005:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000006:
    ret
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0505_ sweep stack on leaving.
        /// </summary>
        [Test]
        public void TC0505_SweepStackOnLeaving()
        {
            Inp =
@"
class C
...
    fun F() ..  object() ,,
,,,
";
            EpcSyn = @"";

            EpcIL =
@".class public C {
    .method public virtual void F() {
        newobj instance void object::.ctor()
        pop
rp$000001:
        ret
    }
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
rp$000002:
        ret
    }
}
.method public static void '0'() {
    .entrypoint
rp$000003:
    ret
}
";
            Test();
        }
        
        /// <summary>
        /// Ts the c0505_ limited support closure.
        /// </summary>
        [Test]
        public void TC0505_LimitedSupportClosure()
        {
            Inp =
@"
gv  = ""global variable""
`() ..  cv1 = gv    ,,

fun Main()
..
    `() ..  cv2 = gv    ,,
,,
";
            EpcSyn = @"";

            EpcIL = 
@".field static string gv
.field static class [NanaFxt]'0clsr$000002' $000014
.method public static void Main() {
    .entrypoint
    .locals (
        class [NanaFxt]'0clsr$000007' $000013
    )
    newobj instance void [NanaFxt]'0clsr$000007'::.ctor()
    stloc $000013
    ldloc $000013
    ldftn instance void [NanaFxt]'0clsr$000007'::'0impl'()
    newobj instance void [NanaFxt]'0dlgt$000007'::.ctor(object, native int)
    pop
rp$000001:
    ret
}
.class public '0clsr$000002' {
    .method public void .ctor() {
rp$000003:
        ret
    }
    .method public void '0impl'() {
        .locals (
            string cv1
        )
        ldsfld string gv
        stloc cv1
rp$000004:
        ret
    }
}
.class public sealed '0dlgt$000002' extends [mscorlib]System.MulticastDelegate {
    .method public hidebysig newslot void .ctor(object obj, native int mth) runtime {
    }
    .method public hidebysig newslot void Invoke() runtime {
    }
}
.class public '0clsr$000007' {
    .method public void .ctor() {
rp$000008:
        ret
    }
    .method public void '0impl'() {
        .locals (
            string cv2
        )
        ldsfld string gv
        stloc cv2
rp$000009:
        ret
    }
}
.class public sealed '0dlgt$000007' extends [mscorlib]System.MulticastDelegate {
    .method public hidebysig newslot void .ctor(object obj, native int mth) runtime {
    }
    .method public hidebysig newslot void Invoke() runtime {
    }
}
.method public static void .cctor() {
    ldstr ""global variable""
    stsfld string gv
    newobj instance void [NanaFxt]'0clsr$000002'::.ctor()
    stsfld class [NanaFxt]'0clsr$000002' $000014
    ldsfld class [NanaFxt]'0clsr$000002' $000014
    ldftn instance void [NanaFxt]'0clsr$000002'::'0impl'()
    newobj instance void [NanaFxt]'0dlgt$000002'::.ctor(object, native int)
    pop
rp$000012:
    ret
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0513_ instance field access.
        /// </summary>
        [Test]
        public void TC0513_InstanceFieldAccess()
        {
            Inp =
@"
class C
...
    F:int
    fun Sub()
    ..
        F   = 777
        v   = F
    ,,
,,,
";
            EpcSyn = @"";

            EpcIL =
@".class public C {
    .field int32 F
    .method public virtual void Sub() {
        .locals (
            int32 v
        )
        ldarg.0
        ldc.i4 777
        stfld int32 [NanaFxt]C::F
        ldarg.0
        ldfld int32 [NanaFxt]C::F
        stloc v
rp$000001:
        ret
    }
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
rp$000002:
        ret
    }
}
.method public static void '0'() {
    .entrypoint
rp$000003:
    ret
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0520_ source is empty.
        /// </summary>
        [Test]
        public void TC0520_SrcIsEmpty()
        {
            Inp =
@"
";
            EpcSyn =
@"0Source
";
            EpcIL =
@".method public static void '0'() {
    .entrypoint
rp$000001:
    ret
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0523_ closure with local variable capture.
        /// </summary>
        [Test]
        public void TC0523_ClosureWithLocalVariableCapture()
        {
            Inp =
@"
fun Main()
..
    0   -> lv
    `() ..  cv = lv ,,
,,
";
            EpcSyn = @"";

            EpcIL =
@".method public static void Main() {
    .entrypoint
    .locals (
        int32 lv
        , class [NanaFxt]'0clsr$000002' $000007
    )
    ldc.i4 0
    stloc lv
    newobj instance void [NanaFxt]'0clsr$000002'::.ctor()
    stloc $000007
    ldloc $000007
    ldloc lv
    stfld int32 [NanaFxt]'0clsr$000002'::lv
    ldloc $000007
    ldftn instance void [NanaFxt]'0clsr$000002'::'0impl'()
    newobj instance void [NanaFxt]'0dlgt$000002'::.ctor(object, native int)
    pop
rp$000001:
    ret
}
.class public '0clsr$000002' {
    .field int32 lv
    .method public void .ctor() {
rp$000003:
        ret
    }
    .method public void '0impl'() {
        .locals (
            int32 cv
        )
        ldarg.0
        ldfld int32 [NanaFxt]'0clsr$000002'::lv
        stloc cv
rp$000004:
        ret
    }
}
.class public sealed '0dlgt$000002' extends [mscorlib]System.MulticastDelegate {
    .method public hidebysig newslot void .ctor(object obj, native int mth) runtime {
    }
    .method public hidebysig newslot void Invoke() runtime {
    }
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0617_ event.
        /// </summary>
        [Test]
        public void TC0617_Event()
        {
            References.Add("system.windows.forms.dll");

            Inp =
@"
using System.Windows.Forms

@STAThreadAttribute
fun Main():void
..
    Application.EnableVisualStyles()
    Application.SetCompatibleTextRenderingDefault(false)

    f   = Form()
    c   = `(sender:object, a:EventArgs)
    ..
        MessageBox.Show(""xxx"")
    ,,
    h   = EventHandler(c)
    f.Load  += h

    Application.Run(f)
,,
";
            EpcSyn = @"";

            EpcIL =
@".method public static void Main() {
    .custom instance void [mscorlib]System.STAThreadAttribute::.ctor()
    .entrypoint
    .locals (
        class [System.Windows.Forms]System.Windows.Forms.Form f
        , class [NanaFxt]'0dlgt$000002' c
        , class [mscorlib]System.EventHandler h
        , class [NanaFxt]'0clsr$000002' $000007
    )
    call void [System.Windows.Forms]System.Windows.Forms.Application::EnableVisualStyles()
    ldc.i4.0
    call void [System.Windows.Forms]System.Windows.Forms.Application::SetCompatibleTextRenderingDefault(bool)
    newobj instance void [System.Windows.Forms]System.Windows.Forms.Form::.ctor()
    stloc f
    newobj instance void [NanaFxt]'0clsr$000002'::.ctor()
    stloc $000007
    ldloc $000007
    ldftn instance void [NanaFxt]'0clsr$000002'::'0impl'(object, class [mscorlib]System.EventArgs)
    newobj instance void [NanaFxt]'0dlgt$000002'::.ctor(object, native int)
    stloc c
    ldloc c
    ldftn instance void [NanaFxt]'0dlgt$000002'::Invoke(object, class [mscorlib]System.EventArgs)
    newobj instance void [mscorlib]System.EventHandler::.ctor(object, native int)
    stloc h
    ldloc f
    ldloc h
    callvirt instance void [System.Windows.Forms]System.Windows.Forms.Form::add_Load(class [mscorlib]System.EventHandler)
    ldloc f
    call void [System.Windows.Forms]System.Windows.Forms.Application::Run(class [System.Windows.Forms]System.Windows.Forms.Form)
rp$000001:
    ret
}
.class public '0clsr$000002' {
    .method public void .ctor() {
rp$000003:
        ret
    }
    .method public void '0impl'(object sender, class [mscorlib]System.EventArgs a) {
        ldstr ""xxx""
        call valuetype [System.Windows.Forms]System.Windows.Forms.DialogResult [System.Windows.Forms]System.Windows.Forms.MessageBox::Show(string)
        pop
rp$000004:
        ret
    }
}
.class public sealed '0dlgt$000002' extends [mscorlib]System.MulticastDelegate {
    .method public hidebysig newslot void .ctor(object obj, native int mth) runtime {
    }
    .method public hidebysig newslot void Invoke(object sender, class [mscorlib]System.EventArgs a) runtime {
    }
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0708_ store return value to temporary variable.
        /// </summary>
        [Test]
        public void TC0708_StoreReturnValueToTemporaryVariable()
        {
            Inp =
@"
fun Fibo(n:int):int
..
    if      0 == n then
        return 0
    elif    1 == n then
        return 1
    else
        return Fibo(n - 2) + Fibo(n - 1)
    end
,,

num = 0
while   17 > num do
    Fibo(num)   -> fi
    `p(fi)
    num = num + 1
end
";
            EpcSyn = @"";
            EpcIL =
@".field static int32 num
.field static int32 fi
.method public static int32 Fibo(int32 n) {
    .locals (
        int32 '0rv$000001'
    )
    ldc.i4 0
    ldarg n
    ceq
    brfalse elif$000003_1
    ldc.i4 0
    stloc '0rv$000001'
    br rp$000001
    br endif$000003
elif$000003_1:
    ldc.i4 1
    ldarg n
    ceq
    brfalse else$000003
    ldc.i4 1
    stloc '0rv$000001'
    br rp$000001
    br endif$000003
else$000003:
    ldarg n
    ldc.i4 2
    sub
    call int32 Fibo(int32)
    ldarg n
    ldc.i4 1
    sub
    call int32 Fibo(int32)
    add
    stloc '0rv$000001'
    br rp$000001
endif$000003:
rp$000001:
    ldloc '0rv$000001'
    ret
}
.method public static void .cctor() {
    ldc.i4 0
    stsfld int32 num
do$000002:
    ldc.i4 17
    ldsfld int32 num
    cgt
    brfalse endwhile$000002
    ldsfld int32 num
    call int32 Fibo(int32)
    stsfld int32 fi
    ldsfld int32 fi
    call void [mscorlib]System.Console::WriteLine(int32)
    ldsfld int32 num
    ldc.i4 1
    add
    stsfld int32 num
    br do$000002
endwhile$000002:
rp$000004:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000005:
    ret
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0712_ cast class.
        /// </summary>
        [Test]
        public void TC0712_CastClass()
        {
            Inp =
@"
o   = null  as object
s   = o     as! string
n   = 1     as object
";
            EpcSyn = @"";

            EpcIL =
@".field static object o
.field static string s
.field static object n
.method public static void .cctor() {
    ldnull
    // silent cast to object
    stsfld object o
    ldsfld object o
    castclass string
    stsfld string s
    ldc.i4 1
    box int32
    // silent cast to object
    stsfld object n
rp$000001:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000002:
    ret
}
";
            Test();
        }

        /// <summary>
        /// Ts the c0714_ operator method.
        /// </summary>
        [Test]
        public void TC0714_OperatorMethod()
        {
            Inp =
@"
Decimal(13) ->  a
Decimal(4)  ->  b
c   =   a   ==  b
c   =   a   !=  b
c   =   a   <   b
c   =   a   <=  b
c   =   a   >   b
c   =   a   >=  b
d   =   a   +   b
d   =   a   -   b
d   =   a   *   b
d   =   a   /   b
d   =   a   %   b

        ";
            EpcSyn = @"";

            EpcIL =
@".field static valuetype [mscorlib]System.Decimal a
.field static valuetype [mscorlib]System.Decimal b
.field static bool c
.field static valuetype [mscorlib]System.Decimal d
.method public static void .cctor() {
    ldc.i4 13
    newobj instance void [mscorlib]System.Decimal::.ctor(int32)
    stsfld valuetype [mscorlib]System.Decimal a
    ldc.i4 4
    newobj instance void [mscorlib]System.Decimal::.ctor(int32)
    stsfld valuetype [mscorlib]System.Decimal b
    ldsfld valuetype [mscorlib]System.Decimal a
    ldsfld valuetype [mscorlib]System.Decimal b
    call bool [mscorlib]System.Decimal::op_Equality(valuetype [mscorlib]System.Decimal, valuetype [mscorlib]System.Decimal)
    stsfld bool c
    ldsfld valuetype [mscorlib]System.Decimal a
    ldsfld valuetype [mscorlib]System.Decimal b
    call bool [mscorlib]System.Decimal::op_Inequality(valuetype [mscorlib]System.Decimal, valuetype [mscorlib]System.Decimal)
    stsfld bool c
    ldsfld valuetype [mscorlib]System.Decimal a
    ldsfld valuetype [mscorlib]System.Decimal b
    call bool [mscorlib]System.Decimal::op_LessThan(valuetype [mscorlib]System.Decimal, valuetype [mscorlib]System.Decimal)
    stsfld bool c
    ldsfld valuetype [mscorlib]System.Decimal a
    ldsfld valuetype [mscorlib]System.Decimal b
    call bool [mscorlib]System.Decimal::op_LessThanOrEqual(valuetype [mscorlib]System.Decimal, valuetype [mscorlib]System.Decimal)
    stsfld bool c
    ldsfld valuetype [mscorlib]System.Decimal a
    ldsfld valuetype [mscorlib]System.Decimal b
    call bool [mscorlib]System.Decimal::op_GreaterThan(valuetype [mscorlib]System.Decimal, valuetype [mscorlib]System.Decimal)
    stsfld bool c
    ldsfld valuetype [mscorlib]System.Decimal a
    ldsfld valuetype [mscorlib]System.Decimal b
    call bool [mscorlib]System.Decimal::op_GreaterThanOrEqual(valuetype [mscorlib]System.Decimal, valuetype [mscorlib]System.Decimal)
    stsfld bool c
    ldsfld valuetype [mscorlib]System.Decimal a
    ldsfld valuetype [mscorlib]System.Decimal b
    call valuetype [mscorlib]System.Decimal [mscorlib]System.Decimal::op_Addition(valuetype [mscorlib]System.Decimal, valuetype [mscorlib]System.Decimal)
    stsfld valuetype [mscorlib]System.Decimal d
    ldsfld valuetype [mscorlib]System.Decimal a
    ldsfld valuetype [mscorlib]System.Decimal b
    call valuetype [mscorlib]System.Decimal [mscorlib]System.Decimal::op_Subtraction(valuetype [mscorlib]System.Decimal, valuetype [mscorlib]System.Decimal)
    stsfld valuetype [mscorlib]System.Decimal d
    ldsfld valuetype [mscorlib]System.Decimal a
    ldsfld valuetype [mscorlib]System.Decimal b
    call valuetype [mscorlib]System.Decimal [mscorlib]System.Decimal::op_Multiply(valuetype [mscorlib]System.Decimal, valuetype [mscorlib]System.Decimal)
    stsfld valuetype [mscorlib]System.Decimal d
    ldsfld valuetype [mscorlib]System.Decimal a
    ldsfld valuetype [mscorlib]System.Decimal b
    call valuetype [mscorlib]System.Decimal [mscorlib]System.Decimal::op_Division(valuetype [mscorlib]System.Decimal, valuetype [mscorlib]System.Decimal)
    stsfld valuetype [mscorlib]System.Decimal d
    ldsfld valuetype [mscorlib]System.Decimal a
    ldsfld valuetype [mscorlib]System.Decimal b
    call valuetype [mscorlib]System.Decimal [mscorlib]System.Decimal::op_Modulus(valuetype [mscorlib]System.Decimal, valuetype [mscorlib]System.Decimal)
    stsfld valuetype [mscorlib]System.Decimal d
rp$000001:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000002:
    ret
}
";
            Test();
        }

        [Test]
        public void TC0715_IntegerLiteral()
        {
            Inp =
@"
i1  = 2147483647                //  int?^???????????Bint.MaxValue???????????????????????B
i2  = 2147483648                //  uint?^???????????Bint.MaxValue?????????????????B
i3  = 4294967295                //  uint?^???????????Buint.MaxValuee???????????????????????B
i4  = 4294967296                //  longt?^???????????Buint.MaxValue?????????????????B
i5  = 9223372036854775807       //  long?^???????????Blong.MaxValue???????????????????????B
i6  = 9223372036854775808       //  ulong?^???????????Blong.MaxValue?????????????????B

im1 = -2147483648               //  int?^???????????Bint.MinValue???????????????????????B
im2 = -2147483649               //  longt?^???????????Bint.MinValue???????????????????B
im3 = -9223372036854775808      //  longt?^???????????B

ul  = 1UL                       //  ulong?^???????????B

l1  = 9223372036854775807L      //  long?^???????????Blong.MaxValue???????????????????????B
l2  = 9223372036854775808L      //  ulong?^???????????Blong.MaxValue?????????????????B

u1  = 4294967295u               //  uint?^???????????Buint.MaxValue??????????????????????
u2  = 4294967296u               //  ulong?^???????????Buint.MaxValue?????????????????B
";
            EpcSyn = @"";

            EpcIL =
@".field static int32 i1
.field static uint32 i2
.field static uint32 i3
.field static int64 i4
.field static int64 i5
.field static uint64 i6
.field static int32 im1
.field static int64 im2
.field static int64 im3
.field static uint64 ul
.field static int64 l1
.field static uint64 l2
.field static uint32 u1
.field static uint64 u2
.method public static void .cctor() {
    ldc.i4 2147483647
    stsfld int32 i1
    ldc.i4 2147483648
    stsfld uint32 i2
    ldc.i4 4294967295
    stsfld uint32 i3
    ldc.i8 4294967296
    stsfld int64 i4
    ldc.i8 9223372036854775807
    stsfld int64 i5
    ldc.i8 9223372036854775808
    stsfld uint64 i6
    ldc.i4 -2147483648
    stsfld int32 im1
    ldc.i8 -2147483649
    stsfld int64 im2
    ldc.i8 -9223372036854775808
    stsfld int64 im3
    ldc.i8 1
    stsfld uint64 ul
    ldc.i8 9223372036854775807
    stsfld int64 l1
    ldc.i8 9223372036854775808
    stsfld uint64 l2
    ldc.i4 4294967295
    stsfld uint32 u1
    ldc.i8 4294967296
    stsfld uint64 u2
rp$000001:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000002:
    ret
}
";
            Test();
        }

        [Test]
        public void TC0721_IntegerLiteralUnary()
        {
            Inp =
@"
iuna1 = + 101
iuna2 = - 102
iuna3 = + + 103
iuna4 = + - 104
iuna5 = - + 105
iuna6 = - - 106
iuna7 = + + + 107
iuna8 = + + - 108
iuna9 = + - + 109
iuna10 = + - - 110
iuna11 = - + + 111
iuna12 = - + - 112
iuna13 = - - + 113
iuna14 = - - - 114
        ";
            EpcSyn = @"";

            EpcIL =
@".field static int32 iuna1
.field static int32 iuna2
.field static int32 iuna3
.field static int32 iuna4
.field static int32 iuna5
.field static int32 iuna6
.field static int32 iuna7
.field static int32 iuna8
.field static int32 iuna9
.field static int32 iuna10
.field static int32 iuna11
.field static int32 iuna12
.field static int32 iuna13
.field static int32 iuna14
.method public static void .cctor() {
    ldc.i4 101
    stsfld int32 iuna1
    ldc.i4 -102
    stsfld int32 iuna2
    ldc.i4 103
    stsfld int32 iuna3
    ldc.i4 -104
    stsfld int32 iuna4
    ldc.i4 -105
    stsfld int32 iuna5
    ldc.i4 106
    stsfld int32 iuna6
    ldc.i4 107
    stsfld int32 iuna7
    ldc.i4 -108
    stsfld int32 iuna8
    ldc.i4 -109
    stsfld int32 iuna9
    ldc.i4 110
    stsfld int32 iuna10
    ldc.i4 -111
    stsfld int32 iuna11
    ldc.i4 112
    stsfld int32 iuna12
    ldc.i4 113
    stsfld int32 iuna13
    ldc.i4 -114
    stsfld int32 iuna14
rp$000001:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000002:
    ret
}
";
            Test();
        }

        [Test]
        public void TC0722_RealLiteral()
        {
            Inp =
@"
d1  = 0.1                       //  ???????w???????? double?^??????????
f1  = 0.1F                      //  suffix 'F' ???w???????? float?^??????????
d2  = 314E-2                    //  'E' 10???K?????w?????w???????????Bdouble?^???????????B
        ";
            EpcSyn = @"";

            EpcIL =
@".field static float64 d1
.field static float32 f1
.field static float64 d2
.method public static void .cctor() {
    ldc.r8 0.1E0
    stsfld float64 d1
    ldc.r4 0.1E0
    stsfld float32 f1
    ldc.r8 314.0E-2
    stsfld float64 d2
rp$000001:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000002:
    ret
}
";
            Test();
        }

        [Test]
        public void TC0729_NumericConversion()
        {
            Inp =
@"
d   = 1.0
sb  = d as sbyte
b   = d as byte
s   = d as short
us  = d as ushort
i   = d as int
ui  = d as uint
l   = d as long
ul  = d as ulong
f   = d as float
d   = d as double
        ";
            EpcSyn = @"";

            EpcIL =
@".field static float64 d
.field static int8 sb
.field static uint8 b
.field static int16 s
.field static uint16 us
.field static int32 i
.field static uint32 ui
.field static int64 l
.field static uint64 ul
.field static float32 f
.method public static void .cctor() {
    ldc.r8 1.0E0
    stsfld float64 d
    ldsfld float64 d
    conv.i1
    stsfld int8 sb
    ldsfld float64 d
    conv.u1
    stsfld uint8 b
    ldsfld float64 d
    conv.i2
    stsfld int16 s
    ldsfld float64 d
    conv.u2
    stsfld uint16 us
    ldsfld float64 d
    conv.i4
    stsfld int32 i
    ldsfld float64 d
    conv.u4
    stsfld uint32 ui
    ldsfld float64 d
    conv.i8
    stsfld int64 l
    ldsfld float64 d
    conv.u8
    stsfld uint64 ul
    ldsfld float64 d
    conv.r4
    stsfld float32 f
    ldsfld float64 d
    conv.r8
    stsfld float64 d
rp$000001:
    ret
}
.method public static void '0'() {
    .entrypoint
rp$000002:
    ret
}
";
            Test();
        }

        //        [Test]
//        public void ZZZ_TestStub()
//        {
//            References.Add("system.windows.forms.dll");

//            Inp =
//@"
//";
//            EpcSyn = @"";

//            EpcIL =
//@"
//";
//            Test();
//        }

        public void Test()
        {
            Func<TestCase, string> f = delegate(TestCase c)
            {
                Token root = Ctrl.CreateRootTemplate();

                Assembly exeasmb = Assembly.GetExecutingAssembly();
                string name = GetType().Name;
                Token opt = root.Find("CompileOptions");
                opt.FlwsAdd(Path.GetDirectoryName(exeasmb.Location), "include")
                    .FlwsAdd(name + ".exe", "out")
                    ;
                References.ForEach(delegate(string s) { opt.FlwsAdd(s, "reference"); });
                root.Find("Sources").FlwsAdd(c.Input, "SourceText");

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
                            trace(TokenEx.ToTree(root_.Find("Syntax").Follows[0]));
                        };
                    }
                    ctrl.Compile(root);
                    if (EpcIL != "")
                    {
                        Token code = root.Find("Code");
                        trace(code.Value);
                    }
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
                    Assembly mscorlib = Assembly.Load("mscorlib.dll");
                    string asmext = CodeGenerator.AssemblyExtern(mscorlib);
                    b.Append(asmext).AppendLine();
                    if (References.Count > 0)
                    {
                        TypeInAssemblyLoader ldr = new TypeInAssemblyLoader();
                        References.ForEach(delegate(string s) { b.AppendLine(Nana.Generations.CodeGenerator.AssemblyExtern(ldr.LoadFrameworkClassLibrarie(s))); });
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
