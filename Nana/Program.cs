using System;
using System.Collections.Generic;
using System.Text;
using Nana.Syntaxes;
using Nana.Tokens;
using System.IO;
using Nana.ILASM;

namespace Nana
{
    public class Program
    {
        static public void Main(string[] args)
        {
            if (args.Length == 0) { StartLineEditMode(); }
            else { StartCompile(args); }
        }

        public static void StartLineEditMode()
        {
            LineEditMode lem;
            lem = new LineEditMode();
            lem.On();
        }

        public static void StartCompile(string[] args)
        {
            Token a; Ctrl c;
            StringBuilder b = new StringBuilder();
            Action<string> write = delegate(string s) { b.Append(s); };
            UTF8Encoding utf8 = new UTF8Encoding(false /* no byte order mark */);
            string ilpath;
            try
            {
                a = CmdLnArgs.GetCmdLnArgs(args);
                c = new Ctrl();
                c.Compile(a, write);

                ilpath = new List<Token>(a.Find(@"SemanticRoot").Follows).Find(delegate(Token t) { return t.Value == @"out"; }).First.Value;
                ilpath = Path.ChangeExtension(ilpath, ".il");
                File.WriteAllText(ilpath, b.ToString(), utf8);

                ILASMRunner r = new ILASMRunner();
                r.DetectILASM();
                r.Run(ilpath);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }

    }
}
