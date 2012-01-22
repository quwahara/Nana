using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Nana.Syntaxes;
using Nana.Tokens;
using Nana.Semantics;
using System.Reflection;
using Nana;
using System.IO;
using Nana.IMRs;
using UnitTest.Util;
using Nana.Delegates;
using Nana.CodeGeneration;
using Nana.Infr;

namespace UnitTest.Semantics.Try
{
//    [TestFixture]
//    public class Try
//    {
//        public string Inp, Epc;

//        [SetUp]
//        public void SetUp() { Inp = Epc = ""; }

//        [Test]
//        public void T001()
//        {
//            Inp = @"
//sfunc   Fnc
//name    Id
//(
//)
//:
//void    Id
//begin   Bgn
//(rbp)
//end     End
//";
//            Epc =
//@"sfunc
//+---[0]name
//+---[1](
//+---[2])
//+---[3]:
//|   +---[0]void
//+---[4]begin
//|   +---[0](rbp)
//+---[5]end
//";
//            Test();
//        }

//        public void Test()
//        {
//            Func<TestCase, string> f = delegate(TestCase c)
//            {
//                ITokenEnumerator ten = new EnumeratorAdapter(
//                    Sty.ToStringListAndClean(Inp)
//                        .ConvertAll<Token>(Token.FromVG).GetEnumerator());
//                PrefixAnalyzer pfa = new PrefixAnalyzer();
//                pfa.Init(ten);
//                Token t = pfa.Analyze();


//                ActnAnalyzer actna = new ActnAnalyzer(t, null);
//                actna.Analyze();

//                return TokenEx.ToTree(t);
//            };
//            new TestCase("", Inp, Epc, f).Run();
//        }
//    }

    [TestFixture]
    public class CtrlDecFxt : RootFxt2
    {
        [Test]
        public void T101_Class1Func0()
        {
            Inp = @"
class Class1Func0
begin
end
";
            Epc +=
@".class public Class1Func0 {
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
        ret
    }
}
.method static public void '0'() {
    .entrypoint
    ret
}
";
            Analyze();

            Assert.IsNotNull(Env);
            Assert.AreEqual(1, Env.Members.Count);

            App app = Env.Members.Find(delegate(Nmd n_) { return n_ is App; }) as App;
            Assert.IsNotNull(app);

            Typ typ = Env.FindByNamePath(ModuleFilename + @"/Class1Func0") as Typ;
            Ovld ao = Env.FindByNamePath(ModuleFilename + @"/Class1Func0/.ctor") as Ovld;
            Fun a = ao.GetFunOf(typ, new Typ[] { }, typ, typ);

            Assert.IsTrue(2 == a.Exes.Count);
            string s = a.Exes[0].ToString();

            Assert.AreEqual(Sty.Sieve(
@"{
    Instance={
        Name=this
        , Typ={
            Name=Class1Func0
            }:Typ
        , VarKind=This
        }:Variable
    , Callee={
        Name=.ctor
        }:Fun
    , Args={}
    , IsNewObj=False
    }:CallFunction"
)
                , a.Exes[0].ToString());
        }

        [Test]
        public void T102_Class1Func1P0()
        {
            Inp = @"
class T102_Class1Func1P0
begin
    sfun Main():void
    begin
    end
end
";
            Epc +=
@".class public T102_Class1Func1P0 {
    .method static public void Main() {
        .entrypoint
        ret
    }
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
        ret
    }
}
";
            Analyze();

            Typ typ = App.FindByNamePath("T102_Class1Func1P0") as Typ;
            Ovld ao = typ.FindByNamePath(@".ctor") as Ovld;
            Fun a = ao.GetFunOf(typ, new Typ[] { }, typ, typ);

            Assert.IsTrue(2 == a.Exes.Count);

            List<string> xs = a.Exes.ConvertAll<string>(delegate(Sema x) { return x.ToString(); });
            Assert.IsTrue(xs[0].EndsWith("}:CallFunction"));

            Typ typ2 = App.FindByNamePath("T102_Class1Func1P0") as Typ;

            Fun main = (typ2.FindByNamePath(@"Main") as Ovld).GetFunOf(typ2, new Typ[] { }, typ2, typ2);
            Fun ctor = (typ2.FindByNamePath(@".ctor") as Ovld).GetFunOf(typ2, new Typ[] { }, typ2, typ2);

        }

    }

    public class RootFxt2
    {
        public string Inp;
        public string Epc;
        public string ModuleFilename;
        public Env Env;
        public App App;

        [SetUp]
        public void SetUp()
        {
            string asm = GetType().Name;
            ModuleFilename = asm + ".exe";

            Inp = "";
            Epc = @".assembly extern mscorlib {.ver 2:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89)}
.assembly extern UnitTest {.ver 1:0:0:0}
.assembly " + asm + @" { }
.module " + asm + @".exe
";
            Epc = @".assembly extern mscorlib {.ver 2:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89)}
.assembly extern UnitTest {.ver 1:0:0:0}
.assembly " + asm + @" { }
.module " + ModuleFilename + @"
";
            Env = null;
        }

        public void Analyze()
        {
            Token root = Ctrl.CreateRootTemplate();

            Assembly exeasmb = Assembly.GetExecutingAssembly();
            string name = GetType().Name;
            root.Find("@CompileOptions")
                .FlwsAdd(Path.GetDirectoryName(exeasmb.Location), "include")
                .FlwsAdd(Path.GetFileNameWithoutExtension(exeasmb.Location), "reference")
                .FlwsAdd(name + ".exe", "out")
                ;
            root.Find("@Sources").FlwsAdd(Inp, "SourceText");

            Ctrl.Check(root);
            Ctrl ctrl = new Ctrl();

            StringBuilder b = new StringBuilder();
            Action<string> trace = delegate(string s_) { b.Append(s_); };
            ctrl.AfterSemanticAnalyze = delegate(Token root_, Env env_)
            {
                Env = env_;
                App = env_.Members.Find(delegate(Nmd n_) { return n_ is App; }) as App;

            };
            try
            {
                ctrl.Compile(root);
                trace(root.Find("@Code").Value);
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
        }

    }

}

namespace UnitTest.Semantics.Call
{
    [TestFixture]
    public class Call : UnitTest.Semantics.Root.RootFxt
    {
        [Test]
        public void T101_CallRefFuncP0()
        {
            Inp = @"
class CallRefFuncP0
begin
    sfun Main():void
    begin
        System.Console.WriteLine()
    end
end
";
            Epc +=
@".class public CallRefFuncP0 {
    .method static public void Main() {
        .entrypoint
        call void [mscorlib]System.Console::WriteLine()
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
        public void T102_CallRefFuncP1()
        {
            Inp = @"
class CallRefFuncP1
begin
    sfun Main():void
    begin
        System.Console.WriteLine(""P1"")
    end
end
";
            Epc +=
@".class public CallRefFuncP1 {
    .method static public void Main() {
        .entrypoint
        ldstr ""P1""
        call void [mscorlib]System.Console::WriteLine(string)
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
        public void T103_CallDecFuncP0()
        {
            Inp = @"
class CallDecFuncP0
begin
    sfun Main():void
    begin
        Sub()
    end
    sfun Sub():void
    begin
    end
end
";
            Epc +=
@".class public CallDecFuncP0 {
    .method static public void Main() {
        .entrypoint
        call void [Call]CallDecFuncP0::Sub()
        ret
    }
    .method static public void Sub() {
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
        public void T104_CallDecFuncP1()
        {
            Inp = @"
class CallDecFuncP1
begin
    sfun Main():void
    begin
        Sub(""st"")
    end
    sfun Sub(s:string):void
    begin
        System.Console.WriteLine(s)
    end
end
";
            Epc +=
@".class public CallDecFuncP1 {
    .method static public void Main() {
        .entrypoint
        ldstr ""st""
        call void [Call]CallDecFuncP1::Sub(string)
        ret
    }
    .method static public void Sub(string s) {
        ldarg s
        call void [mscorlib]System.Console::WriteLine(string)
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
        public void T105_CallDecFuncP2()
        {
            Inp = @"
class CallDecFuncP2
begin
    sfun Main():void
    begin
        Sub(""st"", ""ring"")
    end
    sfun Sub(s:string, t:string):void
    begin
        System.Console.WriteLine(t)
    end
end
";
            Epc +=
@".class public CallDecFuncP2 {
    .method static public void Main() {
        .entrypoint
        ldstr ""st""
        ldstr ""ring""
        call void [Call]CallDecFuncP2::Sub(string, string)
        ret
    }
    .method static public void Sub(string s, string t) {
        ldarg t
        call void [mscorlib]System.Console::WriteLine(string)
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
        public void T201_CallInstRefP0()
        {
            Inp = @"
object()        ->  o
o.ToString()    ->  s
System.Console.WriteLine(s)
";
            Epc +=
@".field static object o
.field static string s
.method static public void .cctor() {
    newobj instance void object::.ctor()
    stsfld object o
    ldsfld object o
    callvirt instance string object::ToString()
    stsfld string s
    ldsfld string s
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
        public void T202_CallInstRefP0()
        {
            Inp = @"
UnitTest.Class0()   ->  c0
c0.Pub0()           ->  s
System.Console.WriteLine(s)
";
            Epc +=
@".field static class [UnitTest]UnitTest.Class0 c0
.field static string s
.method static public void .cctor() {
    newobj instance void [UnitTest]UnitTest.Class0::.ctor()
    stsfld class [UnitTest]UnitTest.Class0 c0
    ldsfld class [UnitTest]UnitTest.Class0 c0
    callvirt instance string [UnitTest]UnitTest.Class0::Pub0()
    stsfld string s
    ldsfld string s
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
        public void T301_Global()
        {
            Inp = @"
""xxx"" -> a
System.Console.WriteLine(a)
";
            Epc +=
@".field static string a
.method static public void .cctor() {
    ldstr ""xxx""
    stsfld string a
    ldsfld string a
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
        public void T401_Global()
        {
            Inp = @"
a:int <- 10
";
            Epc +=
@".field static int32 a
.method static public void .cctor() {
    ldc.i4 10
    stsfld int32 a
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
        public void T501_CallValueTypeFunc()
        {
            Inp = @"
class CallRefFuncP0
begin
    sfun Main():void
    begin
        333 -> n
        n.ToString()    -> s
        System.Console.WriteLine(s)
    end
end
";
            Epc +=
@".class public CallRefFuncP0 {
    .method static public void Main() {
        .entrypoint
        .locals (
            int32 n
            , string s
        )
        ldc.i4 333
        stloc n
        ldloca n
        callvirt instance string int32::ToString()
        stloc s
        ldloc s
        call void [mscorlib]System.Console::WriteLine(string)
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
    }
}

namespace UnitTest.Semantics.Dec
{
    [TestFixture]
    public class CtrlDecFxt : UnitTest.Semantics.Root.RootFxt
    {
        [Test]
        public void T101_Class1Func0()
        {
            Inp = @"
class Class1Func0
begin
end
";
            Epc +=
@".class public Class1Func0 {
    .method public void .ctor() {
        ldarg.0
        call instance void object::.ctor()
        ret
    }
}
.method static public void '0'() {
    .entrypoint
    ret
}
";
            Test();
        }

        [Test]
        public void T102_Class1Func1P0()
        {
            Inp = @"
class T102_Class1Func1P0
begin
    sfun Main():void
    begin
    end
end
";
            Epc +=
@".class public T102_Class1Func1P0 {
    .method static public void Main() {
        .entrypoint
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
        public void T103_Class1Func2P0()
        {
            Inp = @"
class Class1Func2P0
begin
    sfun Main():void
    begin
    end
    sfun Sub():void
    begin
    end
end
";
            Epc +=
@".class public Class1Func2P0 {
    .method static public void Main() {
        .entrypoint
        ret
    }
    .method static public void Sub() {
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
        public void T104_Class1Func1P1()
        {
            Inp = @"
class Class1Func1P1
begin
    sfun Main(s:string):void
    begin
    end
end
";
            Epc +=
@".class public Class1Func1P1 {
    .method static public void Main(string s) {
        .entrypoint
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
        public void T105_Class1Func1P2()
        {
            Inp = @"
class Class1Func1P2
begin
    sfun Main(s:string, t:string):void
    begin
    end
end
";
            Epc +=
@".class public Class1Func1P2 {
    .method static public void Main(string s, string t) {
        .entrypoint
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
    }
}

namespace UnitTest.Semantics.Calc
{
    public class CalcFxt : UnitTest.Semantics.Root.RootFxt
    {
        [Test]
        public void T_bool()
        {
            Inp = @"
true == true -> v
System.Console.WriteLine(v)
true != true -> v
System.Console.WriteLine(v)
true and true -> v
System.Console.WriteLine(v)
true or true -> v
System.Console.WriteLine(v)
true xor true -> v
System.Console.WriteLine(v)
";
            Epc +=
@".field static bool v
.method static public void .cctor() {
    ldc.i4.1
    ldc.i4.1
    ceq
    stsfld bool v
    ldsfld bool v
    call void [mscorlib]System.Console::WriteLine(bool)
    ldc.i4.1
    ldc.i4.1
    ceq
    neg
    stsfld bool v
    ldsfld bool v
    call void [mscorlib]System.Console::WriteLine(bool)
    ldc.i4.1
    ldc.i4.1
    and
    stsfld bool v
    ldsfld bool v
    call void [mscorlib]System.Console::WriteLine(bool)
    ldc.i4.1
    ldc.i4.1
    or
    stsfld bool v
    ldsfld bool v
    call void [mscorlib]System.Console::WriteLine(bool)
    ldc.i4.1
    ldc.i4.1
    xor
    stsfld bool v
    ldsfld bool v
    call void [mscorlib]System.Console::WriteLine(bool)
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
        public void T_int_Arithmetic()
        {
            Inp = @"
1 + 2 -> i
System.Console.WriteLine(i)
1 - 2 -> i
System.Console.WriteLine(i)
1 * 2 -> i
System.Console.WriteLine(i)
6 / 2 -> i
System.Console.WriteLine(i)
17 % 5 -> i
System.Console.WriteLine(i)
";
            Epc +=
@".field static int32 i
.method static public void .cctor() {
    ldc.i4 1
    ldc.i4 2
    add
    stsfld int32 i
    ldsfld int32 i
    call void [mscorlib]System.Console::WriteLine(int32)
    ldc.i4 1
    ldc.i4 2
    sub
    stsfld int32 i
    ldsfld int32 i
    call void [mscorlib]System.Console::WriteLine(int32)
    ldc.i4 1
    ldc.i4 2
    mul
    stsfld int32 i
    ldsfld int32 i
    call void [mscorlib]System.Console::WriteLine(int32)
    ldc.i4 6
    ldc.i4 2
    div
    stsfld int32 i
    ldsfld int32 i
    call void [mscorlib]System.Console::WriteLine(int32)
    ldc.i4 17
    ldc.i4 5
    rem
    stsfld int32 i
    ldsfld int32 i
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
        public void T_int_LogicalConditionChceck()
        {
            Inp = @"
1 == 1 -> i
System.Console.WriteLine(i)
1 != 1 -> i
System.Console.WriteLine(i)
1 > 1 -> i
System.Console.WriteLine(i)
1 < 1 -> i
System.Console.WriteLine(i)
1 >= 1 -> i
System.Console.WriteLine(i)
1 <= 1 -> i
System.Console.WriteLine(i)
";
            Epc +=
@".field static bool i
.method static public void .cctor() {
    ldc.i4 1
    ldc.i4 1
    ceq
    stsfld bool i
    ldsfld bool i
    call void [mscorlib]System.Console::WriteLine(bool)
    ldc.i4 1
    ldc.i4 1
    ceq
    neg
    stsfld bool i
    ldsfld bool i
    call void [mscorlib]System.Console::WriteLine(bool)
    ldc.i4 1
    ldc.i4 1
    cgt
    stsfld bool i
    ldsfld bool i
    call void [mscorlib]System.Console::WriteLine(bool)
    ldc.i4 1
    ldc.i4 1
    clt
    stsfld bool i
    ldsfld bool i
    call void [mscorlib]System.Console::WriteLine(bool)
    ldc.i4 1
    ldc.i4 1
    clt
    neg
    stsfld bool i
    ldsfld bool i
    call void [mscorlib]System.Console::WriteLine(bool)
    ldc.i4 1
    ldc.i4 1
    cgt
    neg
    stsfld bool i
    ldsfld bool i
    call void [mscorlib]System.Console::WriteLine(bool)
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
        public void T_string()
        {
            Inp = @"
""a"" + ""b"" -> s
System.Console.WriteLine(s)
";
            Epc +=
@".field static string s
.method static public void .cctor() {
    ldstr ""a""
    ldstr ""b""
    call string string::Concat(string, string)
    stsfld string s
    ldsfld string s
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
    }
}

namespace UnitTest.Semantics.Branches
{
    public class BranchesFxt : UnitTest.Semantics.Root.RootFxt
    {
        [Test]
        public void T_If_elif0_else_0()
        {
            Inp = @"
if true then
    System.Console.WriteLine(""true"")
end
";
            Epc +=
@".method static public void .cctor() {
    ldc.i4.1
    brfalse endif$000001
    ldstr ""true""
    call void [mscorlib]System.Console::WriteLine(string)
    br endif$000001
endif$000001:
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
        public void T_If_elif0_else_1()
        {
            Inp = @"
if true then
    System.Console.WriteLine(""then"")
else
    System.Console.WriteLine(""else"")
end
";
            Epc +=
@".method static public void .cctor() {
    ldc.i4.1
    brfalse else$000001
    ldstr ""then""
    call void [mscorlib]System.Console::WriteLine(string)
    br endif$000001
else$000001:
    ldstr ""else""
    call void [mscorlib]System.Console::WriteLine(string)
endif$000001:
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
        public void T_If_elif1_else_0()
        {
            Inp = @"
if true then
    System.Console.WriteLine(""then"")
elif true then
    System.Console.WriteLine(""elif0"")
end
";
            Epc +=
@".method static public void .cctor() {
    ldc.i4.1
    brfalse elif$000001_1
    ldstr ""then""
    call void [mscorlib]System.Console::WriteLine(string)
    br endif$000001
elif$000001_1:
    ldc.i4.1
    brfalse endif$000001
    ldstr ""elif0""
    call void [mscorlib]System.Console::WriteLine(string)
    br endif$000001
endif$000001:
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
        public void T_If_elif1_else_1()
        {
            Inp = @"
if true then
    System.Console.WriteLine(""then"")
elif true then
    System.Console.WriteLine(""elif0"")
else
    System.Console.WriteLine(""else"")
end
";
            Epc +=
@".method static public void .cctor() {
    ldc.i4.1
    brfalse elif$000001_1
    ldstr ""then""
    call void [mscorlib]System.Console::WriteLine(string)
    br endif$000001
elif$000001_1:
    ldc.i4.1
    brfalse else$000001
    ldstr ""elif0""
    call void [mscorlib]System.Console::WriteLine(string)
    br endif$000001
else$000001:
    ldstr ""else""
    call void [mscorlib]System.Console::WriteLine(string)
endif$000001:
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
        public void T_If_elif2_else_0()
        {
            Inp = @"
if true then
    System.Console.WriteLine(""then"")
elif true then
    System.Console.WriteLine(""elif0"")
elif true then
    System.Console.WriteLine(""elif1"")
end
";
            Epc +=
@".method static public void .cctor() {
    ldc.i4.1
    brfalse elif$000001_1
    ldstr ""then""
    call void [mscorlib]System.Console::WriteLine(string)
    br endif$000001
elif$000001_1:
    ldc.i4.1
    brfalse elif$000001_2
    ldstr ""elif0""
    call void [mscorlib]System.Console::WriteLine(string)
    br endif$000001
elif$000001_2:
    ldc.i4.1
    brfalse endif$000001
    ldstr ""elif1""
    call void [mscorlib]System.Console::WriteLine(string)
    br endif$000001
endif$000001:
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
        public void T_If_elif2_else_1()
        {
            Inp = @"
if true then
    System.Console.WriteLine(""then"")
elif true then
    System.Console.WriteLine(""elif0"")
elif true then
    System.Console.WriteLine(""elif1"")
else
    System.Console.WriteLine(""else"")
end
";
            Epc +=
@".method static public void .cctor() {
    ldc.i4.1
    brfalse elif$000001_1
    ldstr ""then""
    call void [mscorlib]System.Console::WriteLine(string)
    br endif$000001
elif$000001_1:
    ldc.i4.1
    brfalse elif$000001_2
    ldstr ""elif0""
    call void [mscorlib]System.Console::WriteLine(string)
    br endif$000001
elif$000001_2:
    ldc.i4.1
    brfalse else$000001
    ldstr ""elif1""
    call void [mscorlib]System.Console::WriteLine(string)
    br endif$000001
else$000001:
    ldstr ""else""
    call void [mscorlib]System.Console::WriteLine(string)
endif$000001:
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
        public void T_While()
        {
            Inp = @"
while false do
    System.Console.WriteLine(""do"")
    break
    System.Console.WriteLine(""middle"")
    continue
    System.Console.WriteLine(""end"")
end
";
            Epc +=
@".method static public void .cctor() {
do$000001:
    ldc.i4.0
    brfalse endwhile$000001
    ldstr ""do""
    call void [mscorlib]System.Console::WriteLine(string)
    br endwhile$000001
    ldstr ""middle""
    call void [mscorlib]System.Console::WriteLine(string)
    br do$000001
    ldstr ""end""
    call void [mscorlib]System.Console::WriteLine(string)
    br do$000001
endwhile$000001:
    ret
}
.method static public void '0'() {
    .entrypoint
    ret
}
";
            Test();
        }
    }
}


namespace UnitTest.Semantics.Array
{
    [TestFixture]
    [Description(
@"[test suit to vector, array and jugged vector]
T101
declaration
instatiation
get and set an element
T102
declaration by instantiation
get and set a vector itself
T103
calculation order when same variable is used as in index scope and assigned value expression
T104
calculation elements each other
T105
calling a method of Array
T106
calling a property of Array
operation for anonymous array
T107
set and get directly an element in instantiate array
")]
    public class VectorFxt : UnitTest.Semantics.Root.RootFxt
    {
        [Test]
        public void T101()
        {
            Inp = @"
a:int[]
a       = int[3]
a[0]    = 5
b       = a[0]
System.Console.WriteLine(b)
";
            Epc +=
@".field static int32[] a
.field static int32 b
.method static public void .cctor() {
    ldc.i4 3
    newarr int32
    stsfld int32[] a
    ldsfld int32[] a
    ldc.i4 0
    ldc.i4 5
    stelem int32
    ldsfld int32[] a
    ldc.i4 0
    ldelem int32
    stsfld int32 b
    ldsfld int32 b
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
        public void T102()
        {
            Inp = @"
a       = int[3]
a[0]    = 1
b       = a
System.Console.WriteLine(b[0])
";
            Epc +=
@".field static int32[] a
.field static int32[] b
.method static public void .cctor() {
    ldc.i4 3
    newarr int32
    stsfld int32[] a
    ldsfld int32[] a
    ldc.i4 0
    ldc.i4 1
    stelem int32
    ldsfld int32[] a
    stsfld int32[] b
    ldsfld int32[] b
    ldc.i4 0
    ldelem int32
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
        public void T103()
        {
            Inp = @"
a       = int[3]
i       = 0
a[i = i + 1]    = (i = i + 2)
System.Console.WriteLine(a[1])
";
            Epc +=
@".field static int32[] a
.field static int32 i
.method static public void .cctor() {
    ldc.i4 3
    newarr int32
    stsfld int32[] a
    ldc.i4 0
    stsfld int32 i
    ldsfld int32[] a
    ldsfld int32 i
    ldc.i4 1
    add
    stsfld int32 i
    ldsfld int32 i
    ldsfld int32 i
    ldc.i4 2
    add
    stsfld int32 i
    ldsfld int32 i
    stelem int32
    ldsfld int32[] a
    ldc.i4 1
    ldelem int32
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
        public void T104()
        {
            Inp = @"
a       = int[4]
a[0]    = 0
a[1]    = 3
a[2]    = 2
a[3]    = 1
a[0]    = a[1] + a[2]
System.Console.WriteLine(a[0])
System.Console.WriteLine(a[0] + a[3])
";
            Epc +=
@".field static int32[] a
.method static public void .cctor() {
    ldc.i4 4
    newarr int32
    stsfld int32[] a
    ldsfld int32[] a
    ldc.i4 0
    ldc.i4 0
    stelem int32
    ldsfld int32[] a
    ldc.i4 1
    ldc.i4 3
    stelem int32
    ldsfld int32[] a
    ldc.i4 2
    ldc.i4 2
    stelem int32
    ldsfld int32[] a
    ldc.i4 3
    ldc.i4 1
    stelem int32
    ldsfld int32[] a
    ldc.i4 0
    ldsfld int32[] a
    ldc.i4 1
    ldelem int32
    ldsfld int32[] a
    ldc.i4 2
    ldelem int32
    add
    stelem int32
    ldsfld int32[] a
    ldc.i4 0
    ldelem int32
    call void [mscorlib]System.Console::WriteLine(int32)
    ldsfld int32[] a
    ldc.i4 0
    ldelem int32
    ldsfld int32[] a
    ldc.i4 3
    ldelem int32
    add
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
        public void T105()
        {
            Inp = @"
a       = int[3]
a[0]    = 1
b       = a.GetValue(0)
System.Console.WriteLine(b)
";
            Epc +=
@".field static int32[] a
.field static object b
.method static public void .cctor() {
    ldc.i4 3
    newarr int32
    stsfld int32[] a
    ldsfld int32[] a
    ldc.i4 0
    ldc.i4 1
    stelem int32
    ldsfld int32[] a
    ldc.i4 0
    callvirt instance object int32[]::GetValue(int32)
    stsfld object b
    ldsfld object b
    call void [mscorlib]System.Console::WriteLine(object)
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
        public void T106()
        {
            Inp = @"
a       = int[3]
System.Console.WriteLine(a.Length)
";
            Epc +=
@".field static int32[] a
.method static public void .cctor() {
    ldc.i4 3
    newarr int32
    stsfld int32[] a
    ldsfld int32[] a
    callvirt instance int32 int32[]::get_Length()
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
        public void T107()
        {
            Inp = @"
b       = ((int[5])[0] = 7)
System.Console.WriteLine(b)
";
            Epc +=
@".field static int32 b
.field static int32[] $000001
.method static public void .cctor() {
    ldc.i4 5
    newarr int32
    stsfld int32[] $000001
    ldsfld int32[] $000001
    ldc.i4 0
    ldc.i4 7
    stelem int32
    ldsfld int32[] $000001
    ldc.i4 0
    ldelem int32
    stsfld int32 b
    ldsfld int32 b
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
    }

    public class ArrayFxt : UnitTest.Semantics.Root.RootFxt
    {
        [Test]
        public void T101()
        {
            Inp = @"
a:int[,]
a       = int[3,5]
a[1,2]  = 7
b       = a[1, 2]
System.Console.WriteLine(b)
";
            Epc +=
@".field static int32[0...,0...] a
.field static int32 b
.method static public void .cctor() {
    ldc.i4 3
    ldc.i4 5
    newobj instance void int32[0...,0...]::.ctor(int32, int32)
    stsfld int32[0...,0...] a
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 2
    ldc.i4 7
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 2
    call instance int32 int32[0...,0...]::Get(int32, int32)
    stsfld int32 b
    ldsfld int32 b
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
        public void T102()
        {
            Inp = @"
a       = int[9, 7]
a[3, 1] = 5
b       = a
System.Console.WriteLine(b[3, 1])
";
            Epc +=
@".field static int32[0...,0...] a
.field static int32[0...,0...] b
.method static public void .cctor() {
    ldc.i4 9
    ldc.i4 7
    newobj instance void int32[0...,0...]::.ctor(int32, int32)
    stsfld int32[0...,0...] a
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 1
    ldc.i4 5
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    stsfld int32[0...,0...] b
    ldsfld int32[0...,0...] b
    ldc.i4 3
    ldc.i4 1
    call instance int32 int32[0...,0...]::Get(int32, int32)
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
        public void T103()
        {
            Inp = @"
a       = int[4,5]
a[0,0]  = 0
a[0,1]  = 1
a[0,2]  = 2
a[0,3]  = 3
a[0,4]  = 4
a[1,0]  = 10
a[1,1]  = 11
a[1,2]  = 12
a[1,3]  = 13
a[1,4]  = 14
a[2,0]  = 20
a[2,1]  = 21
a[2,2]  = 22
a[2,3]  = 23
a[2,4]  = 24
a[3,0]  = 30
a[3,1]  = 31
a[3,2]  = 32
a[3,3]  = 33
a[3,4]  = 34
i       = 0
a[i = i + 1, i = i + 1]  = a[i = i + 1, i = i + 1]
System.Console.WriteLine(i)
System.Console.WriteLine(a[3, 4])
System.Console.WriteLine(a[1, 2])
";
            Epc +=
@".field static int32[0...,0...] a
.field static int32 i
.method static public void .cctor() {
    ldc.i4 4
    ldc.i4 5
    newobj instance void int32[0...,0...]::.ctor(int32, int32)
    stsfld int32[0...,0...] a
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 0
    ldc.i4 0
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 1
    ldc.i4 1
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 2
    ldc.i4 2
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 3
    ldc.i4 3
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 4
    ldc.i4 4
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 0
    ldc.i4 10
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 1
    ldc.i4 11
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 2
    ldc.i4 12
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 3
    ldc.i4 13
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 4
    ldc.i4 14
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 2
    ldc.i4 0
    ldc.i4 20
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 2
    ldc.i4 1
    ldc.i4 21
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 2
    ldc.i4 2
    ldc.i4 22
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 2
    ldc.i4 3
    ldc.i4 23
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 2
    ldc.i4 4
    ldc.i4 24
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 0
    ldc.i4 30
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 1
    ldc.i4 31
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 2
    ldc.i4 32
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 3
    ldc.i4 33
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 4
    ldc.i4 34
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldc.i4 0
    stsfld int32 i
    ldsfld int32[0...,0...] a
    ldsfld int32 i
    ldc.i4 1
    add
    stsfld int32 i
    ldsfld int32 i
    ldsfld int32 i
    ldc.i4 1
    add
    stsfld int32 i
    ldsfld int32 i
    ldsfld int32[0...,0...] a
    ldsfld int32 i
    ldc.i4 1
    add
    stsfld int32 i
    ldsfld int32 i
    ldsfld int32 i
    ldc.i4 1
    add
    stsfld int32 i
    ldsfld int32 i
    call instance int32 int32[0...,0...]::Get(int32, int32)
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32 i
    call void [mscorlib]System.Console::WriteLine(int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 4
    call instance int32 int32[0...,0...]::Get(int32, int32)
    call void [mscorlib]System.Console::WriteLine(int32)
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 2
    call instance int32 int32[0...,0...]::Get(int32, int32)
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
        public void T104()
        {
            Inp = @"
a       = int[4,5]
a[0,0]  = 0
a[0,1]  = 1
a[0,2]  = 2
a[0,3]  = 3
a[0,4]  = 4
a[1,0]  = 10
a[1,1]  = 11
a[1,2]  = 12
a[1,3]  = 13
a[1,4]  = 14
a[2,0]  = 20
a[2,1]  = 21
a[2,2]  = 22
a[2,3]  = 23
a[2,4]  = 24
a[3,0]  = 30
a[3,1]  = 31
a[3,2]  = 32
a[3,3]  = 33
a[3,4]  = 34
System.Console.WriteLine(a[ a[0,0] + a[0, 3], a[0, 1] + a[0, 2]])
System.Console.WriteLine(a[3, 3])
";
            Epc +=
@".field static int32[0...,0...] a
.method static public void .cctor() {
    ldc.i4 4
    ldc.i4 5
    newobj instance void int32[0...,0...]::.ctor(int32, int32)
    stsfld int32[0...,0...] a
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 0
    ldc.i4 0
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 1
    ldc.i4 1
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 2
    ldc.i4 2
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 3
    ldc.i4 3
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 4
    ldc.i4 4
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 0
    ldc.i4 10
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 1
    ldc.i4 11
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 2
    ldc.i4 12
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 3
    ldc.i4 13
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 1
    ldc.i4 4
    ldc.i4 14
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 2
    ldc.i4 0
    ldc.i4 20
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 2
    ldc.i4 1
    ldc.i4 21
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 2
    ldc.i4 2
    ldc.i4 22
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 2
    ldc.i4 3
    ldc.i4 23
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 2
    ldc.i4 4
    ldc.i4 24
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 0
    ldc.i4 30
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 1
    ldc.i4 31
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 2
    ldc.i4 32
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 3
    ldc.i4 33
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 4
    ldc.i4 34
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 0
    call instance int32 int32[0...,0...]::Get(int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 3
    call instance int32 int32[0...,0...]::Get(int32, int32)
    add
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 1
    call instance int32 int32[0...,0...]::Get(int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 0
    ldc.i4 2
    call instance int32 int32[0...,0...]::Get(int32, int32)
    add
    call instance int32 int32[0...,0...]::Get(int32, int32)
    call void [mscorlib]System.Console::WriteLine(int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 3
    call instance int32 int32[0...,0...]::Get(int32, int32)
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
        public void T105()
        {
            Inp = @"
a       = int[7,9]
a[3, 5] = 11
b       = a.GetValue(3, 5)
System.Console.WriteLine(b)
";
            Epc +=
@".field static int32[0...,0...] a
.field static object b
.method static public void .cctor() {
    ldc.i4 7
    ldc.i4 9
    newobj instance void int32[0...,0...]::.ctor(int32, int32)
    stsfld int32[0...,0...] a
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 5
    ldc.i4 11
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] a
    ldc.i4 3
    ldc.i4 5
    callvirt instance object int32[0...,0...]::GetValue(int32, int32)
    stsfld object b
    ldsfld object b
    call void [mscorlib]System.Console::WriteLine(object)
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
        public void T106()
        {
            Inp = @"
a       = int[5,7]
System.Console.WriteLine(a.Length)
";
            Epc +=
@".field static int32[0...,0...] a
.method static public void .cctor() {
    ldc.i4 5
    ldc.i4 7
    newobj instance void int32[0...,0...]::.ctor(int32, int32)
    stsfld int32[0...,0...] a
    ldsfld int32[0...,0...] a
    callvirt instance int32 int32[0...,0...]::get_Length()
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
        public void T107()
        {
            Inp = @"
b       = ((int[7,5])[4,3] = 2)
System.Console.WriteLine(b)
";
            Epc +=
@".field static int32 b
.field static int32[0...,0...] $000001
.method static public void .cctor() {
    ldc.i4 7
    ldc.i4 5
    newobj instance void int32[0...,0...]::.ctor(int32, int32)
    stsfld int32[0...,0...] $000001
    ldsfld int32[0...,0...] $000001
    ldc.i4 4
    ldc.i4 3
    ldc.i4 2
    call instance void int32[0...,0...]::Set(int32, int32, int32)
    ldsfld int32[0...,0...] $000001
    ldc.i4 4
    ldc.i4 3
    call instance int32 int32[0...,0...]::Get(int32, int32)
    stsfld int32 b
    ldsfld int32 b
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
        public void T108()
        {
            Inp = @"
fun Main(args:string[]):void
..
    i:int[,,]
    int[15, 13, 11] -> i
    3               -> i[9, 7, 5]
    `p(i[9, 7, 5])
,,
";
            Epc +=
@".method static public void Main(string[] args) {
    .entrypoint
    .locals (
        int32[0...,0...,0...] i
    )
    ldc.i4 15
    ldc.i4 13
    ldc.i4 11
    newobj instance void int32[0...,0...,0...]::.ctor(int32, int32, int32)
    stloc i
    ldloc i
    ldc.i4 9
    ldc.i4 7
    ldc.i4 5
    ldc.i4 3
    call instance void int32[0...,0...,0...]::Set(int32, int32, int32, int32)
    ldloc i
    ldc.i4 9
    ldc.i4 7
    ldc.i4 5
    call instance int32 int32[0...,0...,0...]::Get(int32, int32, int32)
    call void [mscorlib]System.Console::WriteLine(int32)
    ret
}
";
            Test();
        }
    }

    public class VectorJaggedFxt : UnitTest.Semantics.Root.RootFxt
    {
        [Test]
        public void T101()
        {
            Inp = @"
a:int[][]
a       = int[9][]
a[0]    = int[7]
a[0][1] = 5
b       = a[0][1]
System.Console.WriteLine(b)
";
            Epc +=
@".field static int32[][] a
.field static int32 b
.method static public void .cctor() {
    ldc.i4 9
    newarr int32[]
    stsfld int32[][] a
    ldsfld int32[][] a
    ldc.i4 0
    ldc.i4 7
    newarr int32
    stelem int32[]
    ldsfld int32[][] a
    ldc.i4 0
    ldelem int32[]
    ldc.i4 1
    ldc.i4 5
    stelem int32
    ldsfld int32[][] a
    ldc.i4 0
    ldelem int32[]
    ldc.i4 1
    ldelem int32
    stsfld int32 b
    ldsfld int32 b
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
        public void T102()
        {
            Inp = @"
a       = int[9][]
a[0]    = int[7]
a[0][1] = 5
b       = a
System.Console.WriteLine(b[0][1])
";
            Epc +=
@".field static int32[][] a
.field static int32[][] b
.method static public void .cctor() {
    ldc.i4 9
    newarr int32[]
    stsfld int32[][] a
    ldsfld int32[][] a
    ldc.i4 0
    ldc.i4 7
    newarr int32
    stelem int32[]
    ldsfld int32[][] a
    ldc.i4 0
    ldelem int32[]
    ldc.i4 1
    ldc.i4 5
    stelem int32
    ldsfld int32[][] a
    stsfld int32[][] b
    ldsfld int32[][] b
    ldc.i4 0
    ldelem int32[]
    ldc.i4 1
    ldelem int32
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
        public void T103()
        {
            Inp = @"
a       = int[9][]
a[1]    = int[7]
i       = 0
a[i = i + 1][i = i + 2] = (i = i + 3)
System.Console.WriteLine(a[1][3])
";
            Epc +=
@".field static int32[][] a
.field static int32 i
.method static public void .cctor() {
    ldc.i4 9
    newarr int32[]
    stsfld int32[][] a
    ldsfld int32[][] a
    ldc.i4 1
    ldc.i4 7
    newarr int32
    stelem int32[]
    ldc.i4 0
    stsfld int32 i
    ldsfld int32[][] a
    ldsfld int32 i
    ldc.i4 1
    add
    stsfld int32 i
    ldsfld int32 i
    ldelem int32[]
    ldsfld int32 i
    ldc.i4 2
    add
    stsfld int32 i
    ldsfld int32 i
    ldsfld int32 i
    ldc.i4 3
    add
    stsfld int32 i
    ldsfld int32 i
    stelem int32
    ldsfld int32[][] a
    ldc.i4 1
    ldelem int32[]
    ldc.i4 3
    ldelem int32
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
        public void T104()
        {
            Inp = @"
a       = int[21][]
a[1]    = int[19]
a[2]    = int[17]
a[3]    = int[15]
a[1][9] = 13
a[2][7] = 11
a[3][5] = a[1][9] + a[2][7]
System.Console.WriteLine(a[3][5])
";
            Epc +=
@".field static int32[][] a
.method static public void .cctor() {
    ldc.i4 21
    newarr int32[]
    stsfld int32[][] a
    ldsfld int32[][] a
    ldc.i4 1
    ldc.i4 19
    newarr int32
    stelem int32[]
    ldsfld int32[][] a
    ldc.i4 2
    ldc.i4 17
    newarr int32
    stelem int32[]
    ldsfld int32[][] a
    ldc.i4 3
    ldc.i4 15
    newarr int32
    stelem int32[]
    ldsfld int32[][] a
    ldc.i4 1
    ldelem int32[]
    ldc.i4 9
    ldc.i4 13
    stelem int32
    ldsfld int32[][] a
    ldc.i4 2
    ldelem int32[]
    ldc.i4 7
    ldc.i4 11
    stelem int32
    ldsfld int32[][] a
    ldc.i4 3
    ldelem int32[]
    ldc.i4 5
    ldsfld int32[][] a
    ldc.i4 1
    ldelem int32[]
    ldc.i4 9
    ldelem int32
    ldsfld int32[][] a
    ldc.i4 2
    ldelem int32[]
    ldc.i4 7
    ldelem int32
    add
    stelem int32
    ldsfld int32[][] a
    ldc.i4 3
    ldelem int32[]
    ldc.i4 5
    ldelem int32
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
        public void T105()
        {
            Inp = @"
a       = int[17][]
a[1]    = int[15]
a[5]    = int[13]
a[1][3] = 11
System.Console.WriteLine(a[1].GetValue(3))
";
            Epc +=
@".field static int32[][] a
.method static public void .cctor() {
    ldc.i4 17
    newarr int32[]
    stsfld int32[][] a
    ldsfld int32[][] a
    ldc.i4 1
    ldc.i4 15
    newarr int32
    stelem int32[]
    ldsfld int32[][] a
    ldc.i4 5
    ldc.i4 13
    newarr int32
    stelem int32[]
    ldsfld int32[][] a
    ldc.i4 1
    ldelem int32[]
    ldc.i4 3
    ldc.i4 11
    stelem int32
    ldsfld int32[][] a
    ldc.i4 1
    ldelem int32[]
    ldc.i4 3
    callvirt instance object int32[]::GetValue(int32)
    call void [mscorlib]System.Console::WriteLine(object)
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
        public void T106()
        {
            Inp = @"
a       = int[3][]
a[0]    = int[5]
System.Console.WriteLine(a[0].Length)
";
            Epc +=
@".field static int32[][] a
.method static public void .cctor() {
    ldc.i4 3
    newarr int32[]
    stsfld int32[][] a
    ldsfld int32[][] a
    ldc.i4 0
    ldc.i4 5
    newarr int32
    stelem int32[]
    ldsfld int32[][] a
    ldc.i4 0
    ldelem int32[]
    callvirt instance int32 int32[]::get_Length()
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
        public void T107()
        {
            Inp = @"
b = (((int[11][])[9] = int[7])[5] = 3)
System.Console.WriteLine(b)
";
            Epc +=
@".field static int32 b
.field static int32[][] $000001
.field static int32[] $000002
.method static public void .cctor() {
    ldc.i4 11
    newarr int32[]
    stsfld int32[][] $000001
    ldsfld int32[][] $000001
    ldc.i4 9
    ldc.i4 7
    newarr int32
    stsfld int32[] $000002
    ldsfld int32[] $000002
    stelem int32[]
    ldsfld int32[][] $000001
    ldc.i4 9
    ldelem int32[]
    ldc.i4 5
    ldc.i4 3
    stelem int32
    ldsfld int32[][] $000001
    ldc.i4 9
    ldsfld int32[] $000002
    stelem int32[]
    ldsfld int32[][] $000001
    ldc.i4 9
    ldelem int32[]
    ldc.i4 5
    ldelem int32
    stsfld int32 b
    ldsfld int32 b
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
    }

}

namespace UnitTest.Semantics.AccessModifiers
{
    public class AccessModifiersFxt : UnitTest.Semantics.Root.RootFxt
    {
        [Test]
        public void T202_CallInstRefP0()
        {
            Inp = @"
UnitTest.Class0()   ->  c0
c0.Pub0()           ->  s
System.Console.WriteLine(s)
";
            Epc +=
@".field static class [UnitTest]UnitTest.Class0 c0
.field static string s
.method static public void .cctor() {
    newobj instance void [UnitTest]UnitTest.Class0::.ctor()
    stsfld class [UnitTest]UnitTest.Class0 c0
    ldsfld class [UnitTest]UnitTest.Class0 c0
    callvirt instance string [UnitTest]UnitTest.Class0::Pub0()
    stsfld string s
    ldsfld string s
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
        public void T203_CallPrv0()
        {
            Inp = @"
UnitTest.Class0()   ->  c0
c0.Prv0()           ->  s
System.Console.WriteLine(s)
";
            //Epc = @"Cannot access the function 'Prv0', because of accessibility";
            Epc = @"Can not access the function: Prv0";
            Test();
        }

        [Test]
        public void T204_CallPrt0()
        {
            Inp = @"
UnitTest.Class0()   ->  c0
c0.Prt0()           ->  s
System.Console.WriteLine(s)
";
            Epc = @"Can not access the function: Prt0";
            Test();
        }

        [Test]
        public void T205_CallIntr0()
        {
            Inp = @"
UnitTest.Class0()   ->  c0
c0.Intr0()          ->  s
System.Console.WriteLine(s)
";
            Epc = @"Can not access the function: Intr0";
            Test();
        }

        [Test]
        public void T206_CallPrtIntr0()
        {
            Inp = @"
UnitTest.Class0()   ->  c0
c0.PrtIntr0()       ->  s
System.Console.WriteLine(s)
";
            Epc = @"Can not access the function: PrtIntr0";
            Test();
        }

        [Test]
        public void T301_CallPrtIntr0()
        {
            Inp = @"
class A -> UnitTest.Class0
begin
    cons()
        base()
    begin
    end
    sfun Main():void
    begin
        A() -> a
        a.Pub0()
    end
end
";
            Epc +=
@".class public A extends [UnitTest]UnitTest.Class0 {
    .method public void .ctor() {
        ldarg.0
        call instance void [UnitTest]UnitTest.Class0::.ctor()
        ret
    }
    .method static public void Main() {
        .entrypoint
        .locals (
            class [AccessModifiersFxt]A a
        )
        newobj instance void [AccessModifiersFxt]A::.ctor()
        stloc a
        ldloc a
        callvirt instance string [AccessModifiersFxt]A::Pub0()
        pop
        ret
    }
}
";
            Test();
        }
    }
}

namespace UnitTest.Semantics.Precedings
{
    public class PrecedingsFxt : UnitTest.Semantics.Root.RootFxt
    {
        [Test]
        public void T101()
        {
            Inp = @"
    a           = (1 + 2)
;   (a + 3)     -> a
    `p(a)
            ";
            Epc +=
@".field static int32 a
.method static public void .cctor() {
    ldc.i4 1
    ldc.i4 2
    add
    stsfld int32 a
    ldsfld int32 a
    ldc.i4 3
    add
    stsfld int32 a
    ldsfld int32 a
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
        public void T102()
        {
            Inp = @"
    a           = (1 + 2)
;   (a + 3)     -> a
;   (a + 5)     -> a
    `p(a)
            ";
            Epc +=
@".field static int32 a
.method static public void .cctor() {
    ldc.i4 1
    ldc.i4 2
    add
    stsfld int32 a
    ldsfld int32 a
    ldc.i4 3
    add
    stsfld int32 a
    ldsfld int32 a
    ldc.i4 5
    add
    stsfld int32 a
    ldsfld int32 a
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
    }
}

namespace UnitTest.Semantics.Generics
{
    public class GenericsFxt : UnitTest.Semantics.Root.RootFxt
    {
        [Test]
        public void T_List_string_1()
        {
            Inp = @"
System.Collections.Generic.List{string}()
-> ls
ls.IndexOf("""") -> i
System.Console.WriteLine(i)
            ";
            Epc +=
@".field static class [mscorlib]System.Collections.Generic.List`1<string> ls
.field static int32 i
.method static public void .cctor() {
    newobj instance void class [mscorlib]System.Collections.Generic.List`1<string>::.ctor()
    stsfld class [mscorlib]System.Collections.Generic.List`1<string> ls
    ldsfld class [mscorlib]System.Collections.Generic.List`1<string> ls
    ldstr """"
    callvirt instance int32 class [mscorlib]System.Collections.Generic.List`1<string>::IndexOf(!0)
    stsfld int32 i
    ldsfld int32 i
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
    }
}

namespace UnitTest.Semantics.Tutorial
{
    public class TutorialFxt : UnitTest.Semantics.Root.RootFxt
    {
        [Test]
        public void T_HelloWorld()
        {
            Inp = @"`p(""Hello, world!"")
";
            Epc +=
@".method static public void .cctor() {
    ldstr ""Hello, world!""
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
        public void T_String_operation()
        {
            Inp = @"
""abcde"".Substring(1,3) -> sub
`p(sub)                      // sub is ""bcd""

""abcde"".Length -> len
`p(len)                      // len is 5
";
            Epc +=
@".field static string 'sub'
.field static int32 len
.method static public void .cctor() {
    ldstr ""abcde""
    ldc.i4 1
    ldc.i4 3
    callvirt instance string string::Substring(int32, int32)
    stsfld string 'sub'
    ldsfld string 'sub'
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
    }
}

namespace UnitTest.Semantics.Root
{
    public class RootFxt
    {
        public string Inp;
        public string Epc;

        [SetUp]
        public void SetUp()
        {
            //TraceTree = false;
            string asm = GetType().Name;

            Inp = "";
            Epc = @".assembly extern mscorlib {.ver 2:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89)}
.assembly extern UnitTest {.ver 1:0:0:0}
.assembly " + asm + @" { }
.module " + asm + @".exe
";
        }

        public void Test()
        {
            Func<TestCase, string> f = delegate(TestCase c)
            {
                Token root = Ctrl.CreateRootTemplate();

                Assembly exeasmb = Assembly.GetExecutingAssembly();
                string name = GetType().Name;
                root.Find("@CompileOptions")
                    .FlwsAdd(Path.GetDirectoryName(exeasmb.Location), "include")
                    .FlwsAdd(Path.GetFileNameWithoutExtension(exeasmb.Location), "reference")
                    .FlwsAdd(name + ".exe", "out")
                    ;
                root.Find("@Sources").FlwsAdd(c.Input, "SourceText");

                Ctrl.Check(root);
                Ctrl ctrl = new Ctrl();

                StringBuilder b = new StringBuilder();
                Action<string> trace = delegate(string s_) { b.Append(s_); };
                try
                {
                    ctrl.Compile(root);
                    trace(root.Find("@Code").Value);
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

            new TestCase("", Inp, Epc, f).Run();
        }
    }
}
