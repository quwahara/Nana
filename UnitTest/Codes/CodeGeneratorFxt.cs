using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Nana.CodeGeneration;

namespace UnitTest.Codes
{
    [TestFixture]
    public class CodeGeneratorFxt
    {
        [Test]
        public void QkTest()
        {
            Assert.That(CodeGenerator.Qk("sub"), Is.EqualTo("'sub'"));
            Assert.That(CodeGenerator.Qk(".sub"), Is.EqualTo("'.sub'"));
            Assert.That(CodeGenerator.Qk("x.sub"), Is.EqualTo("'x.sub'"));
            Assert.That(CodeGenerator.Qk("sub.y"), Is.EqualTo("'sub.y'"));
            Assert.That(CodeGenerator.Qk("x.sub.y"), Is.EqualTo("'x.sub.y'"));
            Assert.That(CodeGenerator.Qk("xsub"), Is.EqualTo("xsub"));
            Assert.That(CodeGenerator.Qk("x.xsub"), Is.EqualTo("x.xsub"));
            Assert.That(CodeGenerator.Qk("xsub.y"), Is.EqualTo("xsub.y"));
            Assert.That(CodeGenerator.Qk("x.xsub.y"), Is.EqualTo("x.xsub.y"));
        }
    }
}
