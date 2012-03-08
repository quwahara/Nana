/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Reflection;
using System.IO;
using Nana.Infr;

namespace UnitTest.Infr
{
    [TestFixture]
    public class Experiment
    {
        //[Test]
        public void No0001()
        {
            Assembly mscorlib = Assembly.Load("mscorlib.dll");
            string yen = Path.DirectorySeparatorChar.ToString();
            string sysPath = Path.GetDirectoryName(mscorlib.Location) + yen;
            string cscRspPath = sysPath + "csc.rsp";
            if (!File.Exists(cscRspPath)) throw new Exception("No csc.rsp file. Checked path=" + cscRspPath);
            List<string> refList = new List<string>();
            string ln2;
            foreach (string ln in File.ReadAllLines(cscRspPath))
            {
                ln2 = ln.Trim();
                if (string.IsNullOrEmpty(ln2) || ln2.StartsWith("#")) continue;
                if (ln2.StartsWith("/r:")) {
                    ln2 = ln2.Substring(3);
                    ln2 = ln2.Substring(0, ln2.Length - 4);
                    refList.Add(ln2);
                    //refList.Add(ln2.Substring(3));
                }
            }

            Console.WriteLine("== ref list ==");
            refList.Add("mscorlib");
            refList.Sort();
            List<Assembly> refasm = new List<Assembly>();
            foreach (string s in refList)
            {
                Console.WriteLine(s + ".dll");
                Assembly a = Assembly.LoadFile(sysPath + s + ".dll");
                refasm.Add(a);
            }
            Console.WriteLine("== ref asm ==");

            foreach (Assembly a in refasm)
            {
                Console.WriteLine(a.GetName());

            }
            //return;

            Type[] ts;
            List<string> tmps = new List<string>();
            foreach (Assembly a in refasm)
            {
                tmps.Clear();
                Console.WriteLine("== "+ a.GetName() + " ==");
                ts = a.GetExportedTypes();
                Console.WriteLine("-- ExportedTypes[" + ts.Length.ToString("D4") + "] --");
                foreach (Type t in ts)
                {
                    tmps.Add(t.FullName);
                    //Console.WriteLine(t.Name);
                }
                tmps.Sort();
                foreach (string s in tmps)
                {
                    Console.WriteLine(s);
                }

                tmps.Clear();
                ts = a.GetTypes();
                Console.WriteLine("-- Types[" + ts.Length.ToString("D4") + "] --");
                foreach (Type t in ts)
                {
                    tmps.Add(t.FullName);
                    //Console.WriteLine(t.Name);
                }
                tmps.Sort();
                foreach (string s in tmps)
                {
                    Console.WriteLine(s);
                }
            }





        }

        //[Test]
        public void No0002()
        {
            DateTime begin, end;

            begin = DateTime.Now;

            Assembly mscorlib = Assembly.Load("mscorlib.dll");
            string yen = Path.DirectorySeparatorChar.ToString();
            string sysPath = Path.GetDirectoryName(mscorlib.Location) + yen;
            string cscRspPath = sysPath + "csc.rsp";
            if (!File.Exists(cscRspPath)) throw new Exception("No csc.rsp file. Checked path=" + cscRspPath);
            List<string> refList = new List<string>();
            string ln2;
            foreach (string ln in File.ReadAllLines(cscRspPath))
            {
                ln2 = ln.Trim();
                if (string.IsNullOrEmpty(ln2) || ln2.StartsWith("#")) continue;
                if (ln2.StartsWith("/r:"))
                {
                    ln2 = ln2.Substring(3);
                    ln2 = ln2.Substring(0, ln2.Length - 4);
                    refList.Add(ln2);
                    //refList.Add(ln2.Substring(3));
                }
            }

            //Console.WriteLine("== ref list ==");
            refList.Add("mscorlib");
            refList.Sort();
            List<Assembly> refasm = new List<Assembly>();
            foreach (string s in refList)
            {
                //Console.WriteLine(s + ".dll");
                Assembly a = Assembly.LoadFile(sysPath + s + ".dll");
                refasm.Add(a);
            }
            //Console.WriteLine("== ref asm ==");

            foreach (Assembly a in refasm)
            {
                //Console.WriteLine(a.GetName());

            }
            //return;

            Type[] ts;
            List<string> tmps = new List<string>();
            foreach (Assembly a in refasm)
            {
                tmps.Clear();
                //Console.WriteLine("== " + a.GetName() + " ==");
                ts = a.GetExportedTypes();
                //Console.WriteLine("-- ExportedTypes[" + ts.Length.ToString("D4") + "] --");
                foreach (Type t in ts)
                {
                    tmps.Add(t.FullName);
                    //Console.WriteLine(t.Name);
                }
                tmps.Sort();
                foreach (string s in tmps)
                {
                    //Console.WriteLine(s);
                }

                tmps.Clear();
                ts = a.GetTypes();
                //Console.WriteLine("-- Types[" + ts.Length.ToString("D4") + "] --");
                foreach (Type t in ts)
                {
                    tmps.Add(t.FullName);
                    //Console.WriteLine(t.Name);
                }
                tmps.Sort();
                foreach (string s in tmps)
                {
                    //Console.WriteLine(s);
                }
            }

            end = DateTime.Now;
            TimeSpan times = end - begin;
            Console.WriteLine(begin);
            Console.WriteLine(end);
            Console.WriteLine(times);




        }

        //[Test]
        public void E0003_ReadWindowsForms()
        {
            Assembly mscorlib = Assembly.Load("mscorlib.dll");
            string yen = Path.DirectorySeparatorChar.ToString();
            string sysPath = Path.GetDirectoryName(mscorlib.Location) + yen;

            string s;
            s = @"System.Windows.Forms.dll";
            Assembly a = Assembly.LoadFile(sysPath + s);

            string fn = @"System.Windows.Forms.Form";
            Type t;
            t = a.GetType(fn);

            string rsl;
            rsl = t.FullName;
        }

        [Test]
        public void E004_LoadTypeInThisAssembly()
        {
            TypeLoader tl = new TypeLoader();

            string path;
            path = Assembly.GetExecutingAssembly().Location;

            tl.InAssembly.LoadFrameworkClassLibrarie(path);

            Type t = tl.GetTypeByName("UnitTest.Infr.UTA", new string[0]);

            Console.WriteLine(t.ToString());
            Console.WriteLine(MethodBase.GetCurrentMethod().Name);

            //Assembly.GetExecutingAssembly().
        }
    }

    public class UTA
    {
    }
}
