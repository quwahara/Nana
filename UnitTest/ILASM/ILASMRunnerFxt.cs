using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnitTest.Util;
using Nana.ILASM;
using System.IO;

namespace UnitTest.ILASM
{
    [TestFixture]
    public class ILASMRunnerFxt
    {
        public string Inp;
        public string Epc;

        [SetUp]
        public void SetUp()
        {
            Inp = Epc = "";
        }

        [Test]
        public void DetectILASM_MacOSX()
        {
            Inp = PlatformID.MacOSX.ToString();
            Epc = "/usr/bin/ilasm";
            Test();
        }
        [Test]
        public void DetectILASM_UnixX()
        {
            Inp = PlatformID.Unix.ToString();
            Epc = "/usr/bin/ilasm";
            Test();
        }
        [Test]
        public void DetectILASM_Win32NT()
        {
            Inp = PlatformID.Win32NT.ToString();
            string l = Path.DirectorySeparatorChar.ToString();
            string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            string netfwdir = systemRoot + l + @"Microsoft.NET\Framework\v2.0.50727";
            Epc = netfwdir + l + @"ilasm.exe";
            Test();
        }

        public void Test()
        {
            new TestCase("", Inp, Epc, delegate(TestCase c_)
            {
                PlatformID pfm = (PlatformID)Enum.Parse(typeof(PlatformID), c_.Input);
                Version ver;
                OperatingSystem os;
                try
                {
                    ver = new Version();
                    os = new OperatingSystem(pfm, ver);
                }
                catch (Exception exver)
                {
                    c_.Expected = exver.Message;
                    return exver.Message;
                }

                ILASMRunner r = new ILASMRunner();
                string act = "";
                try
                {
                    r.DetectILASM(os);
                    act = r.ILASMpath;
                }
                catch (Exception ex)
                {
                    c_.Expected = ex.Message;
                    act = ex.Message;
                }
                return act;
            })
            .Run();
        }
    }
}
