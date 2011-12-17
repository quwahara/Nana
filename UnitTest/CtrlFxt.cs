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
        public string EpcIL;





        [Test]
        public void HelloWorld()
        {

        }

        public void Test()
        {
            //Token root = Ctrl.CreateRootTemplate();
            
        }
    }
}
