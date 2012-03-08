/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Nana.ILASM
{
    public class ILASMRunner
    {
        public string ILASMpath;

        public void DetectILASM()
        {
            if (ILASMpath == null)
            {
                ILASMpath = Environment.GetEnvironmentVariable(@"NANA_ILASM_PATH");
            }

            if (ILASMpath == null)
            {
                string l = Path.DirectorySeparatorChar.ToString();
                string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
                string netfwdir = systemRoot + l + @"Microsoft.NET\Framework\v2.0.50727";
                ILASMpath = netfwdir + l + @"ilasm.exe";
            }

            if (false == File.Exists(ILASMpath))
            {
                throw new Exception("Could not detect ilasm.exe. You can set ilasm.exe path to environment variable 'NANA_ILASM_PATH'. Detected path:" + ILASMpath);
            }
        }

        public int Run(string srcpath)
        {
            Process p;

            p = new Process();
            p.StartInfo.FileName = ILASMpath;
            p.StartInfo.Arguments = srcpath;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            
            p.OutputDataReceived += new DataReceivedEventHandler(OnOutputDataReceived);
            p.ErrorDataReceived += new DataReceivedEventHandler(OnErrorDataReceived);

            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            p.CancelErrorRead();
            p.CancelOutputRead();

            return p.ExitCode;
        }

        void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Error.WriteLine(e.Data);
        }
    }
}
