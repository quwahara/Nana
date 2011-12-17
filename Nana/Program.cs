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
            UTF8Encoding utf8 = new UTF8Encoding(false /* no byte order mark */);
            string ilpath;
            string code;
            Token root = null;
            try
            {
                root = CmdLnArgs.GetCmdLnArgs(args);
                root.Group = "Root";
                Ctrl.Check(root);
                Ctrl c = new Ctrl();
                c.Compile(root);

                if (root.Contains("@Root/@CompileOptions/@xxxsyntax"))
                {
                    foreach (Token t in root.Find("@Root/@Syntax/0Source"))
                    {
                        Console.Write(TokenEx.ToTree(t));
                    }
                }

                if (root.Contains("@Root/@CompileOptions/@xxxil"))
                {
                    Console.Write(root.Find("@Root/@Code")[0].Value);
                }

                ilpath = root.Find("@Root/@CompileOptions/@out")[0].Value;
                ilpath = Path.ChangeExtension(ilpath, ".il");
                code = root.Find("@Root/@Code")[0].Value;
                File.WriteAllText(ilpath, code, utf8);

                ILASMRunner r = new ILASMRunner();
                r.DetectILASM();
                r.Run(ilpath);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                if (null != root && root.Contains("@Root/@CompileOptions/@xxxtrace"))
                { Console.Error.Write(e.StackTrace); }
            }
        }

    }
}
