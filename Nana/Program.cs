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
            if (args.Length == 0)
            {
                StartLineEditMode();
            }
            else
            {
                Ctrl.StartCompile(args);
            }
        }

        public static void StartLineEditMode()
        {
            LineEditMode lem;
            lem = new LineEditMode();
            lem.On();
        }
    }
}
