using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Nana.Delegates;
using UnitTest.Util;
using System.Text.RegularExpressions;
using Nana.Infr;
using Nana.Syntaxes;
using Nana;
using Nana.Tokens;

namespace UnitTest.Syntaxes.SyntaxAnalyzerFxts
{
    public class SyntaxAnalyzerFxt
    {
        public string Inp, Epc;
        public SyntaxAnalyzer Analyzer;

        [SetUp]
        public void SetUp() { Inp = Epc = ""; }

        public SyntaxAnalyzerFxt()
        {
            Analyzer = new SyntaxAnalyzer();
        }

        public void Test()
        {
            Func<TestCase, string> f = delegate(TestCase c)
            {
                IEnumerator<Token> tks; EnumeratorAdapter tenm; Token t;
                tks = SFList.FromText(c.Input).Map<Token>(Token.FromVG).NotNulls().GetEnumerator();
                tenm = new EnumeratorAdapter(tks);
                Analyzer.Init(tenm);
                t = Analyzer.Analyze();
                return TokenEx.ToTree(t);
            };
            new TestCase("", Inp, Epc, f).Run();
        }
    }

    [TestFixture]
    public class Typ_arr_and_misc : SyntaxAnalyzerFxt
    {
        [Test]
        public void T101_0Source()
        {
            Inp = @"
0   Num
";
            Epc =
@"0Source
+---[0]0
";
            Test();
        }

        [Test]
        public void T101_Typ()
        {
            Inp = @"
a           Id
:           Typ
t           Id
";
            Epc =
@"0Source
+---[0]:
    +---[F]a
    +---[S]t
";
            Test();
        }

        [Test]
        public void T102_Typ_gen()
        {
            Inp = @"
a           Id
:           Typ
t           Id
{
gt          Id
}
";
            Epc =
@"0Source
+---[0]:
    +---[F]a
    +---[S]{
        +---[F]t
        +---[S]gt
        +---[T]}
";
            Test();
        }

        [Test]
        public void T103_Typ_arr()
        {
            Inp = @"
a           Id
:           Typ
t           Id
[
]
";
            Epc =
@"0Source
+---[0]:
    +---[F]a
    +---[S][
        +---[F]t
        +---[T]]
";
            Test();
        }

        [Test]
        public void T103_Typ_arr2()
        {
            Inp = @"
a           Id
:           Typ
t           Id
[
,           _End_Cma_
]
";
            Epc =
@"0Source
+---[0]:
    +---[F]a
    +---[S][
        +---[F]t
        +---[S],
        +---[T]]
";
            Test();
        }

        [Test]
        public void T103_Typ_arr_arr()
        {
            Inp = @"
a           Id
:           Typ
t           Id
[
]
[
]
";
            Epc =
@"0Source
+---[0]:
    +---[F]a
    +---[S][
        +---[F][
        |   +---[F]t
        |   +---[T]]
        +---[T]]
";
            Test();
        }

        [Test]
        public void T103_Typ_arr_arr_arr()
        {
            Inp = @"
a           Id
:           Typ
t           Id
[
]
[
]
[
]
";
            Epc =
@"0Source
+---[0]:
    +---[F]a
    +---[S][
        +---[F][
        |   +---[F][
        |   |   +---[F]t
        |   |   +---[T]]
        |   +---[T]]
        +---[T]]
";
            Test();
        }

        [Test]
        public void T104_Typ()
        {
            Inp = @"
a           Id
:           Typ
t           Id
{
gt          Id
}
[
]
";
            Epc =
@"0Source
+---[0]:
    +---[F]a
    +---[S][
        +---[F]{
        |   +---[F]t
        |   +---[S]gt
        |   +---[T]}
        +---[T]]
";
            Test();
        }
    }
}

namespace UnitTest.Syntaxes.InfixAnalyzerFxts
{
    [TestFixture]
    public class InfixAnalyzerFxt
    {
        public string Inp, Epc;
        public InfixAnalyzer Analyzer;

        [SetUp]
        public void SetUp() { Inp = Epc = ""; }

        [Test]
        public void T101()
        {
            Inp = @"
0   Num
";
            Epc =
@"0
";
            Test();
        }

        [Test]
        public void T102()
        {
            Inp = @"
1   Num
+   Sig
2   Num
";
            Epc =
@"+
+---[F]1
+---[S]2
";
            Test();
        }

        [Test]
        public void T103()
        {
            Inp = @"
1   Num
*   Sig
2   Num
";
            Epc =
@"*
+---[F]1
+---[S]2
";
            Test();
        }

        [Test]
        public void T104()
        {
            Inp = @"
1   Num
*   Sig
2   Num
+   Sig
3   Num
";
            Epc =
@"+
+---[F]*
|   +---[F]1
|   +---[S]2
+---[S]3
";
            Test();
        }

        [Test]
        public void T105()
        {
            Inp = @"
1   Num
+   Sig
2   Num
*   Sig
3   Num
";
            Epc =
@"+
+---[F]1
+---[S]*
    +---[F]2
    +---[S]3
";
            Test();
        }

        [Test]
        public void T201()
        {
            Inp = @"
a   Id
=   Sig
1   Num
";
            Epc =
@"=
+---[F]a
+---[S]1
";
            Test();
        }

        [Test]
        public void T202()
        {
            Inp = @"
a   Id
=   Sig
1   Num
+   Sig
2   Num
";
            Epc =
@"=
+---[F]a
+---[S]+
    +---[F]1
    +---[S]2
";
            Test();
        }

        [Test]
        public void T301()
        {
            Inp = @"
f   Id
(   Circumfix
)
";
            Epc =
@"(
+---[F]f
+---[T])
";
            Test();
        }

        [Test]
        public void T302()
        {
            Inp = @"
f   Id
(   Circumfix
1   Num
)
";
            Epc =
@"(
+---[F]f
+---[S]1
+---[T])
";
            Test();
        }

        [Test]
        public void T303()
        {
            Inp = @"
int Id
[   Sig
]   Sig
";
            Epc =
@"[
+---[F]int
+---[T]]
";
            Test();
        }

        [Test]
        public void T304()
        {
            Inp = @"
int Id
[   Sig
]   Sig
[   Sig
]   Sig
";
            Epc =
@"[
+---[F][
|   +---[F]int
|   +---[T]]
+---[T]]
";
            Test();
        }

        [Test]
        public void X101()
        {
            Inp = @"
*   Ope
*   Ope
";
            Epc = @"Cannot place '*' at there.";
            Test();
        }

        [Test]
        public void X102()
        {
            Inp = @"
f   Id
(   Sig
a   Id
b   Id
";
            Epc = @"'(' was not closed. 'b' was there.";
            Test();
        }

        public InfixAnalyzerFxt()
        {
            Analyzer = new InfixAnalyzer();
        }

        public void Test()
        {
            Func<TestCase, string> f = delegate(TestCase c)
            {
                try
                {
                    IEnumerator<Token> tks; EnumeratorAdapter tenm; Token t;
                    tks = SFList.FromText(c.Input + "0End\r\n").Map<Token>(Token.FromVG).NotNulls().GetEnumerator();
                    tenm = new EnumeratorAdapter(tks);
                    Analyzer.Init(tenm);
                    t = Analyzer.Analyze();
                    return TokenEx.ToTree(t);
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            };
            new TestCase("", Inp, Epc, f).Run();
        }
    }

}

namespace UnitTest.Syntaxes.PrefixAnalyzerFxts
{
    public class PrefixAnalyzerFxt
    {
        public string Inp, Epc;
        public PrefixAnalyzer Analyzer;

        [SetUp]
        public void SetUp() { Inp = Epc = ""; }

        public PrefixAnalyzerFxt()
        {
            Analyzer = new PrefixAnalyzer();
        }

        public void Test()
        {
            Func<TestCase, string> f = delegate(TestCase c)
            {
                try
                {
                    IEnumerator<Token> tks; ITokenEnumerator tkz; Token t;
                    tks = Sty.ToStringListAndClean(c.Input).ConvertAll<Token>(Token.FromVG).GetEnumerator();
                    tkz = new Append(new EnumeratorAdapter(tks), Token.ZEnd);
                    Analyzer.Init(tkz);
                    t = Analyzer.Analyze();
                    return TokenEx.ToTree(t);
                }
                catch (Exception e)
                {
                    return e.Message;
                }
            };
            new TestCase("", Inp, Epc, f).Run();
        }
    }

    [TestFixture]
    public class AllFxt : PrefixAnalyzerFxt
    {
        [Test]
        public void T101_0Source()
        {
            Inp = @"
0Source
(rbp)
";
            Epc =
@"0Source
+---[0](rbp)
";
            Test();
        }

        [Test]
        public void T105_using()
        {
            Inp = @"
using
System      Id
";
            Epc =
@"using
+---[0]System
";
            Test();
        }

        [Test]
        public void T106_using()
        {
            Inp = @"
using
System      Id
.
Windows     Id
";
            Epc =
@"using
+---[0]System
    +---[0].
        +---[0]Windows
";
            Test();
        }

        [Test]
        public void T107_using()
        {
            Inp = @"
using
System      Id
.
Windows     Id
.
Forms       Id
";
            Epc =
@"using
+---[0]System
    +---[0].
    |   +---[0]Windows
    +---[1].
        +---[0]Forms
";
            Test();
        }

        [Test]
        public void T108_0typedec()
        {
            Inp = @"
0typedec
:
void        Id
";
            Epc =
@"0typedec
+---[0]:
    +---[0]void
";
            Test();
        }

        [Test]
        public void T113_0attrdec()
        {
            Inp = @"
0attrdec
@
(expr)
";
            Epc =
@"0attrdec
+---[0]@
    +---[0](expr)
";
            Test();
        }

        [Test]
        public void T114_0attrdec()
        {
            Inp = @"
0attrdec
@
(expr)
@
(expr)
";
            Epc =
@"0attrdec
+---[0]@
|   +---[0](expr)
+---[1]@
    +---[0](expr)
";
            Test();
        }

        [Test]
        public void T115_0bodydec()
        {
            Inp = @"
0bodydec
begin       Bgn
end         End
";
            Epc =
@"0bodydec
+---[0]begin
+---[1]end
";
            Test();
        }

        [Test]
        public void T116_0bodydec()
        {
            Inp = @"
0bodydec
begin       Bgn
(expr)
end         End
";
            Epc =
@"0bodydec
+---[0]begin
|   +---[0](expr)
+---[1]end
";
            Test();
        }

        [Test]
        public void T117_0bodydec()
        {
            Inp = @"
0bodydec
begin       Bgn
(expr)
(expr)
end         End
";
            Epc =
@"0bodydec
+---[0]begin
|   +---[0](expr)
|   +---[1](expr)
+---[1]end
";
            Test();
        }

        [Test]
        public void T118_0funcdec()
        {
            Inp = @"
0funcdec
(
)
:
void        Id
begin       Bgn
end         End
";
            Epc =
@"0funcdec
+---[0](
+---[1])
+---[2]:
|   +---[0]void
+---[3]begin
+---[4]end
";
            Test();
        }

        [Test]
        public void T119_sfunc()
        {
            Inp = @"
sfunc   Fnc
name    Id
(
)
:
void    Id
begin   Bgn
(rbp)
end     End
";
            Epc =
@"sfunc
+---[0]name
+---[1](
+---[2])
+---[3]:
|   +---[0]void
+---[4]begin
|   +---[0](rbp)
+---[5]end
";
            Test();
        }

        [Test]
        public void T120_class()
        {
            Inp = @"
class
Nana        Id
begin       Bgn
end         End
";
            Epc =
@"class
+---[0]Nana
+---[1]begin
+---[2]end
";
            Test();
        }

        [Test]
        public void T121_class()
        {
            Inp = @"
class
Nana    Id
->
Object  Id
begin   Bgn
end     End
";
            Epc =
@"class
+---[0]Nana
+---[1]->
|   +---[0]Object
+---[2]begin
+---[3]end
";
            Test();
        }

        [Test]
        public void T131_0funcdec_NoType()
        {
            Inp = @"
0funcdec
(
)
begin       Bgn
end         End
";
            Epc =
@"0funcdec
+---[0](
+---[1])
+---[2]begin
+---[3]end
";
            Test();
        }

        [Test]
        public void T132_sfunc()
        {
            Inp = @"
sfunc   Fnc
name    Id
(
a       Id
)
:
void    Id
begin   Bgn
(rbp)
end     End
";
            Epc =
@"sfunc
+---[0]name
+---[1](
|   +---[0]a
+---[2])
+---[3]:
|   +---[0]void
+---[4]begin
|   +---[0](rbp)
+---[5]end
";
            Test();
        }

        [Test]
        public void T134_cons_Prms0()
        {
            Inp = @"
cons
(
)
begin   Bgn
(rbp)
end     End
";
            Epc =
@"cons
+---[0](
+---[1])
+---[2]begin
|   +---[0](rbp)
+---[3]end
";
            Test();
        }

        [Test]
        public void T135_0conscall_P0()
        {
            Inp = @"
0conscall
base
(
)
";
            Epc =
@"0conscall
+---[0]base
    +---[0](
    +---[1])
";
            Test();
        }

        [Test]
        public void T136_0conscall_P1()
        {
            Inp = @"
0conscall
base
(
p1
)
";
            Epc =
@"0conscall
+---[0]base
    +---[0](
    |   +---[0]p1
    +---[1])
";
            Test();
        }

        [Test]
        public void T139_0conscall_Nothing()
        {
            Inp = @"
0conscall
";
            Epc =
@"0conscall
";
            Test();
        }

        [Test]
        public void T140_cons_callcons_P0()
        {
            Inp = @"
cons
(
)
base
(
)
begin   Bgn
(rbp)
end     End
";
            Epc =
@"cons
+---[0](
+---[1])
+---[2]base
|   +---[0](
|   +---[1])
+---[3]begin
|   +---[0](rbp)
+---[4]end
";
            Test();
        }

        [Test]
        public void T141_cons_callcons_P1()
        {
            Inp = @"
cons
(
)
base
(
p1
)
begin   Bgn
(rbp)
end     End
";
            Epc =
@"cons
+---[0](
+---[1])
+---[2]base
|   +---[0](
|   |   +---[0]p1
|   +---[1])
+---[3]begin
|   +---[0](rbp)
+---[4]end
";
            Test();
        }

        [Test]
        public void T143_if_elif0_else0()
        {
            Inp = @"
if
(cond)
then
(exprs)
end
";
            Epc =
@"if
+---[0](cond)
+---[1]then
|   +---[0](exprs)
+---[2]end
";
            Test();
        }

        [Test]
        public void T144_if_elif1_else0()
        {
            Inp = @"
if
(cond)
then
(exprs)
elif
(cond)
then
(exprs)
end
";
            Epc =
@"if
+---[0](cond)
+---[1]then
|   +---[0](exprs)
+---[2]elif
|   +---[0](cond)
|   +---[1]then
|       +---[0](exprs)
+---[3]end
";
            Test();
        }

        [Test]
        public void T145_if_elif2_else0()
        {
            Inp = @"
if
(cond0)
then
(exprs0)
elif
(cond1)
then
(exprs1)
elif
(cond2)
then
(exprs2)
end
";
            Epc =
@"if
+---[0](cond0)
+---[1]then
|   +---[0](exprs0)
+---[2]elif
|   +---[0](cond1)
|   +---[1]then
|       +---[0](exprs1)
+---[3]elif
|   +---[0](cond2)
|   +---[1]then
|       +---[0](exprs2)
+---[4]end
";
            Test();
        }

        [Test]
        public void T146_if_elif0_else1()
        {
            Inp = @"
if
(cond0)
then
(exprs0)
else
(exprse)
end
";
            Epc =
@"if
+---[0](cond0)
+---[1]then
|   +---[0](exprs0)
+---[2]else
|   +---[0](exprse)
+---[3]end
";
            Test();
        }

        [Test]
        public void T147_if_elif1_else1()
        {
            Inp = @"
if
(cond0)
then
(exprs0)
elif
(cond1)
then
(exprs1)
else
(exprse)
end
";
            Epc =
@"if
+---[0](cond0)
+---[1]then
|   +---[0](exprs0)
+---[2]elif
|   +---[0](cond1)
|   +---[1]then
|       +---[0](exprs1)
+---[3]else
|   +---[0](exprse)
+---[4]end
";
            Test();
        }

        [Test]
        public void T148_if_elif2_else1()
        {
            Inp = @"
if
(cond0)
then
(exprs0)
elif
(cond1)
then
(exprs1)
elif
(cond2)
then
(exprs2)
else
(exprse)
end
";
            Epc =
@"if
+---[0](cond0)
+---[1]then
|   +---[0](exprs0)
+---[2]elif
|   +---[0](cond1)
|   +---[1]then
|       +---[0](exprs1)
+---[3]elif
|   +---[0](cond2)
|   +---[1]then
|       +---[0](exprs2)
+---[4]else
|   +---[0](exprse)
+---[5]end
";
            Test();
        }

        [Test]
        public void T149_while()
        {
            Inp = @"
while
(cond0)
do          Bgn
(exprs0)
end         End
";
            Epc =
@"while
+---[0](cond0)
+---[1]do
|   +---[0](exprs0)
+---[2]end
";
            Test();
        }

        [Test]
        public void X101()
        {
            Inp = @"
xxx
";
            Epc = @"'xxx' is not a first word for a sentence.";
            Test();
        }

        [Test]
        public void X102()
        {
            Inp = @"
sfunc   Fnc
f       Id
xxx     Id
";
            Epc = @"Not expected word: 'xxx'";
            Test();
        }
    }

    [TestFixture]
    public class Z0type : PrefixAnalyzerFxt
    {
        [Test]
        public void T101()
        {
            Inp = @"
0type
a           Id
";
            Epc =
@"0type
+---[0]a
";
            Test();
        }

        [Test]
        public void T111()
        {
            Inp = @"
0type
a           Id
{
ga          Id
}
";
            Epc =
@"0type
+---[0]a
    +---[0]{
        +---[0]ga
        +---[1]}
";
            Test();
        }

        [Test]
        public void T112()
        {
            Inp = @"
0type
a           Id
{
ga          Id
,
gb          Id
}
";
            Epc =
@"0type
+---[0]a
    +---[0]{
        +---[0]ga
        +---[1],
        |   +---[0]gb
        +---[2]}
";
            Test();
        }

        [Test]
        public void T113()
        {
            Inp = @"
0type
a           Id
{
ga          Id
,
gb          Id
,
gc          Id
}
";
            Epc =
@"0type
+---[0]a
    +---[0]{
        +---[0]ga
        +---[1],
        |   +---[0]gb
        +---[2],
        |   +---[0]gc
        +---[3]}
";
            Test();
        }

        [Test]
        public void T121()
        {
            Inp = @"
0type
a           Id
{
ga          Id
{
gga         Id
}
}
";
            Epc =
@"0type
+---[0]a
    +---[0]{
        +---[0]ga
        |   +---[0]{
        |       +---[0]gga
        |       +---[1]}
        +---[1]}
";
            Test();
        }

        [Test]
        public void T122()
        {
            Inp = @"
0type
a           Id
{
ga          Id
{
gga         Id
,
ggb         Id
}
}
";
            Epc =
@"0type
+---[0]a
    +---[0]{
        +---[0]ga
        |   +---[0]{
        |       +---[0]gga
        |       +---[1],
        |       |   +---[0]ggb
        |       +---[2]}
        +---[1]}
";
            Test();
        }

        [Test]
        public void T123()
        {
            Inp = @"
0type
a           Id
{
ga          Id
{
gga         Id
,
ggb         Id
,
ggc         Id
}
}
";
            Epc =
@"0type
+---[0]a
    +---[0]{
        +---[0]ga
        |   +---[0]{
        |       +---[0]gga
        |       +---[1],
        |       |   +---[0]ggb
        |       +---[2],
        |       |   +---[0]ggc
        |       +---[3]}
        +---[1]}
";
            Test();
        }

        [Test]
        public void T131()
        {
            Inp = @"
0type
a           Id
{
ga          Id
{
gga         Id
{
ggga        Id
}
}
}
";
            Epc =
@"0type
+---[0]a
    +---[0]{
        +---[0]ga
        |   +---[0]{
        |       +---[0]gga
        |       |   +---[0]{
        |       |       +---[0]ggga
        |       |       +---[1]}
        |       +---[1]}
        +---[1]}
";
            Test();
        }

        [Test]
        public void T132()
        {
            Inp = @"
0type
a           Id
{
ga          Id
{
gga         Id
{
ggga        Id
,
gggb        Id
}
}
}
";
            Epc =
@"0type
+---[0]a
    +---[0]{
        +---[0]ga
        |   +---[0]{
        |       +---[0]gga
        |       |   +---[0]{
        |       |       +---[0]ggga
        |       |       +---[1],
        |       |       |   +---[0]gggb
        |       |       +---[2]}
        |       +---[1]}
        +---[1]}
";
            Test();
        }

        [Test]
        public void T133()
        {
            Inp = @"
0type
a           Id
{
ga          Id
{
gga         Id
{
ggga        Id
,
gggb        Id
,
gggc        Id
}
}
}
";
            Epc =
@"0type
+---[0]a
    +---[0]{
        +---[0]ga
        |   +---[0]{
        |       +---[0]gga
        |       |   +---[0]{
        |       |       +---[0]ggga
        |       |       +---[1],
        |       |       |   +---[0]gggb
        |       |       +---[2],
        |       |       |   +---[0]gggc
        |       |       +---[3]}
        |       +---[1]}
        +---[1]}
";
            Test();
        }

        [Test]
        public void T201()
        {
            Inp = @"
0type
a           Id
.
b           Id
";
            Epc =
@"0type
+---[0]a
    +---[0].
        +---[0]b
";
            Test();
        }

        [Test]
        public void T211()
        {
            Inp = @"
0type
a           Id
.
b           Id
{
ga          Id
.
gb          Id
}
";
            Epc =
@"0type
+---[0]a
    +---[0].
        +---[0]b
            +---[0]{
                +---[0]ga
                |   +---[0].
                |       +---[0]gb
                +---[1]}
";
            Test();
        }

        [Test]
        public void T212()
        {
            Inp = @"
0type
a           Id
.
b           Id
{
ga          Id
.
gb          Id
,
gi          Id
.
gj          Id
}
";
            Epc =
@"0type
+---[0]a
    +---[0].
        +---[0]b
            +---[0]{
                +---[0]ga
                |   +---[0].
                |       +---[0]gb
                +---[1],
                |   +---[0]gi
                |       +---[0].
                |           +---[0]gj
                +---[2]}
";
            Test();
        }

        [Test]
        public void T213()
        {
            Inp = @"
0type
a           Id
.
b           Id
{
ga          Id
.
gb          Id
,
gi          Id
.
gj          Id
,
gl          Id
.
gm          Id
}
";
            Epc =
@"0type
+---[0]a
    +---[0].
        +---[0]b
            +---[0]{
                +---[0]ga
                |   +---[0].
                |       +---[0]gb
                +---[1],
                |   +---[0]gi
                |       +---[0].
                |           +---[0]gj
                +---[2],
                |   +---[0]gl
                |       +---[0].
                |           +---[0]gm
                +---[3]}
";
            Test();
        }

        [Test]
        public void T221()
        {
            Inp = @"
0type
a           Id
.
b           Id
{
ga          Id
{
gga         Id
.
ggb         Id
}
.
gb          Id
}
";
            Epc =
@"0type
+---[0]a
    +---[0].
        +---[0]b
            +---[0]{
                +---[0]ga
                |   +---[0]{
                |   |   +---[0]gga
                |   |   |   +---[0].
                |   |   |       +---[0]ggb
                |   |   +---[1]}
                |   +---[1].
                |       +---[0]gb
                +---[1]}
";
            Test();
        }

        [Test]
        public void T222()
        {
            Inp = @"
0type
a           Id
.
b           Id
{
ga          Id
.
gb          Id
{
gga         Id
.
ggb         Id
}
}
";
            Epc =
@"0type
+---[0]a
    +---[0].
        +---[0]b
            +---[0]{
                +---[0]ga
                |   +---[0].
                |       +---[0]gb
                |           +---[0]{
                |               +---[0]gga
                |               |   +---[0].
                |               |       +---[0]ggb
                |               +---[1]}
                +---[1]}
";
            Test();
        }

        [Test]
        public void T223()
        {
            Inp = @"
0type
a           Id
.
b           Id
{
ga          Id
{
gga         Id
.
ggb         Id
}
.
gb          Id
{
gga         Id
.
ggb         Id
}
}
";
            Epc =
@"0type
+---[0]a
    +---[0].
        +---[0]b
            +---[0]{
                +---[0]ga
                |   +---[0]{
                |   |   +---[0]gga
                |   |   |   +---[0].
                |   |   |       +---[0]ggb
                |   |   +---[1]}
                |   +---[1].
                |       +---[0]gb
                |           +---[0]{
                |               +---[0]gga
                |               |   +---[0].
                |               |       +---[0]ggb
                |               +---[1]}
                +---[1]}
";
            Test();
        }

        [Test]
        public void T301()
        {
            Inp = @"
0type
a           Id
.
b           Id
.
c           Id
";
            Epc =
@"0type
+---[0]a
    +---[0].
        +---[0]b
            +---[0].
                +---[0]c
";
            Test();
        }

    }

    [TestFixture]
    public class Z0typegenprm : PrefixAnalyzerFxt
    {
        [Test]
        public void T001()
        {
            Inp = @"
0typegenprm
";
            Epc =
@"0typegenprm
";
            Test();
        }

        [Test]
        public void T101()
        {
            Inp = @"
0typegenprm
{
a           Id
}
";
            Epc =
@"0typegenprm
+---[0]{
    +---[0]a
    +---[1]}
";
            Test();
        }

        [Test]
        public void T102()
        {
            Inp = @"
0typegenprm
{
a           Id
.
b           Id
}
";
            Epc =
@"0typegenprm
+---[0]{
    +---[0]a
    |   +---[0].
    |       +---[0]b
    +---[1]}
";
            Test();
        }

        [Test]
        public void T111()
        {
            Inp = @"
0typegenprm
{
a           Id
{
aa          Id
}
}
";
            Epc =
@"0typegenprm
+---[0]{
    +---[0]a
    |   +---[0]{
    |       +---[0]aa
    |       +---[1]}
    +---[1]}
";
            Test();
        }

        [Test]
        public void T112()
        {
            Inp = @"
0typegenprm
{
a           Id
.
b           Id
{
bb          Id
}
}
";
            Epc =
@"0typegenprm
+---[0]{
    +---[0]a
    |   +---[0].
    |       +---[0]b
    |           +---[0]{
    |               +---[0]bb
    |               +---[1]}
    +---[1]}
";
            Test();
        }

        [Test]
        public void T113()
        {
            Inp = @"
0typegenprm
{
a           Id
{
aa          Id
}
.
b           Id
{
bb          Id
}
}
";
            Epc =
@"0typegenprm
+---[0]{
    +---[0]a
    |   +---[0]{
    |   |   +---[0]aa
    |   |   +---[1]}
    |   +---[1].
    |       +---[0]b
    |           +---[0]{
    |               +---[0]bb
    |               +---[1]}
    +---[1]}
";
            Test();
        }

        [Test]
        public void T201()
        {
            Inp = @"
0typegenprm
{
a           Id
,
i           Id
}
";
            Epc =
@"0typegenprm
+---[0]{
    +---[0]a
    +---[1],
    |   +---[0]i
    +---[2]}
";
            Test();
        }

        [Test]
        public void T202()
        {
            Inp = @"
0typegenprm
{
a           Id
,
i           Id
.
j           Id
}
";
            Epc =
@"0typegenprm
+---[0]{
    +---[0]a
    +---[1],
    |   +---[0]i
    |       +---[0].
    |           +---[0]j
    +---[2]}
";
            Test();
        }

        [Test]
        public void T301()
        {
            Inp = @"
0typegenprm
{
a           Id
,
i           Id
,
l           Id
}
";
            Epc =
@"0typegenprm
+---[0]{
    +---[0]a
    +---[1],
    |   +---[0]i
    +---[2],
    |   +---[0]l
    +---[3]}
";
            Test();
        }

        [Test]
        public void T302()
        {
            Inp = @"
0typegenprm
{
a           Id
,
i           Id
,
l           Id
.
m           Id
}
";
            Epc =
@"0typegenprm
+---[0]{
    +---[0]a
    +---[1],
    |   +---[0]i
    +---[2],
    |   +---[0]l
    |       +---[0].
    |           +---[0]m
    +---[3]}
";
            Test();
        }

        [Test]
        public void X101()
        {
            Inp = @"
0typegenprm
{
";
            Epc =
@"Not expected end"
;
            Test();
        }

        [Test]
        public void X102()
        {
            Inp = @"
0typegenprm
{
,
";
            Epc =
@"Not expected word: ','"
;
            Test();
        }

        [Test]
        public void X103()
        {
            Inp = @"
0typegenprm
{
a           Id
,
";
            Epc =
@"Not expected end"
;
            Test();
        }

        [Test]
        public void X104()
        {
            Inp = @"
0typegenprm
{
a           Id
,
,
";
            Epc =
@"Not expected word: ','"
;
            Test();
        }

        [Test]
        public void X105()
        {
            Inp = @"
0typegenprm
{
a           Id
,
b           Id
";
            Epc =
@"Not expected end"
;
            Test();
        }

    }

    [TestFixture]
    public class Z0iddotdec : PrefixAnalyzerFxt
    {
        [Test]
        public void T102_0iddotdec()
        {
            Inp = @"
0iddotdec
System      Id
";
            Epc =
@"0iddotdec
+---[0]System
";
            Test();
        }

        [Test]
        public void T103_0iddotdec()
        {
            Inp = @"
0iddotdec
System      Id
.
Windows     Id
";
            Epc =
@"0iddotdec
+---[0]System
    +---[0].
        +---[0]Windows
";
            Test();
        }

        [Test]
        public void T104_0iddotdec()
        {
            Inp = @"
0iddotdec
System      Id
.
Windows     Id
.
Forms       Id
";
            Epc =
@"0iddotdec
+---[0]System
    +---[0].
    |   +---[0]Windows
    +---[1].
        +---[0]Forms
";
            Test();
        }
    }

    [TestFixture]
    public class Z0arrFxt : PrefixAnalyzerFxt
    {
        [Test]
        public void T100_0arrdec()
        {
            Inp = @"
0arrdec
[
]
";
            Epc =
@"0arrdec
+---[0][
    +---[0]]
";
            Test();
        }

        [Test]
        public void T101_0arrdec()
        {
            Inp = @"
0arrdec
[
,           _End_Cma_
]
";
            Epc =
@"0arrdec
+---[0][
    +---[0],
    +---[1]]
";
            Test();
        }

        [Test]
        public void T102_0arrdec()
        {
            Inp = @"
0arrdec
[
,           _End_Cma_
,           _End_Cma_
]
";
            Epc =
@"0arrdec
+---[0][
    +---[0],
    +---[1],
    +---[2]]
";
            Test();
        }

        [Test]
        public void T103_0arrdec()
        {
            Inp = @"
0arrdec
[
,           _End_Cma_
,           _End_Cma_
,           _End_Cma_
]
";
            Epc =
@"0arrdec
+---[0][
    +---[0],
    +---[1],
    +---[2],
    +---[3]]
";
            Test();
        }

        [Test]
        public void T104_0arrdec()
        {
            Inp = @"
0arrdec
[
]
[
]
";
            Epc =
@"0arrdec
+---[0][
    +---[0]]
    +---[1][
        +---[0]]
";
            Test();
        }

        [Test]
        public void T105_0arrdec()
        {
            Inp = @"
0arrdec
[
]
[
]
[
]
";
            Epc =
@"0arrdec
+---[0][
    +---[0]]
    +---[1][
        +---[0]]
        +---[1][
            +---[0]]
";
            Test();
        }

        [Test]
        public void T106_0arrdec()
        {
            Inp = @"
0arrdec
[
]
[
,           _End_Cma_
]
";
            Epc =
@"0arrdec
+---[0][
    +---[0]]
    +---[1][
        +---[0],
        +---[1]]
";
            Test();
        }

        [Test]
        public void T107_0arrdec()
        {
            Inp = @"
0arrdec
[
]
[
,           _End_Cma_
,           _End_Cma_
]
";
            Epc =
@"0arrdec
+---[0][
    +---[0]]
    +---[1][
        +---[0],
        +---[1],
        +---[2]]
";
            Test();
        }

        [Test]
        public void T108_0arrdec()
        {
            Inp = @"
0arrdec
[
]
[
,           _End_Cma_
,           _End_Cma_
,           _End_Cma_
]
";
            Epc =
@"0arrdec
+---[0][
    +---[0]]
    +---[1][
        +---[0],
        +---[1],
        +---[2],
        +---[3]]
";
            Test();
        }

        [Test]
        public void T109_0arrdec()
        {
            Inp = @"
0arrdec
[
]
[
]
[
,           _End_Cma_
]
";
            Epc =
@"0arrdec
+---[0][
    +---[0]]
    +---[1][
        +---[0]]
        +---[1][
            +---[0],
            +---[1]]
";
            Test();
        }

        [Test]
        public void T110_0arrdec()
        {
            Inp = @"
0arrdec
[
]
[
]
[
,           _End_Cma_
,           _End_Cma_
]
";
            Epc =
@"0arrdec
+---[0][
    +---[0]]
    +---[1][
        +---[0]]
        +---[1][
            +---[0],
            +---[1],
            +---[2]]
";
            Test();
        }

        [Test]
        public void T111_0arrdec()
        {
            Inp = @"
0arrdec
[
]
[
]
[
,           _End_Cma_
,           _End_Cma_
,           _End_Cma_
]
";
            Epc =
@"0arrdec
+---[0][
    +---[0]]
    +---[1][
        +---[0]]
        +---[1][
            +---[0],
            +---[1],
            +---[2],
            +---[3]]
";
            Test();
        }

        [Test]
        public void X101_0arrdec()
        {
            Inp = @"
0arrdec
[
";
            Epc =
@"Not expected end";
            Test();
        }

        [Test]
        public void X102_0arrdec()
        {
            Inp = @"
0arrdec
[
a
";
            Epc =
@"Not expected word: 'a'";
            Test();
        }

    }
}

