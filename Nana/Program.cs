using System;
using System.Collections.Generic;
using System.Text;
using Nana.Syntaxes;
using Nana.Tokens;

namespace Nana
{
    public class Program
    {
        static public void Main(string[] args)
        {
            LineEditMode im;
            im = new LineEditMode();
            im.On();
            return;


            Token a;
            //Ctrl c;
            Ctrl c;
            try
            {
                a = CmdLnArgs.GetCmdLnArgs(args);
                c = new Ctrl();
                //c = new Ctrl();
                //c.Init();
                //c.Compile(a, null);
                c.Compile(a, null);
                //c.Compile(a);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }
    }
}
