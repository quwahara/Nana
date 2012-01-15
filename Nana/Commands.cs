using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Nana.Delegates;
using Nana.Infr;
using Nana.Syntaxes;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using Nana.Tokens;

namespace Nana
{
    public class Commands
    {
        public LineEditMode LEM;
        public Dictionary<string, Action<string[], Box<bool>, LineEditMode>> Cmds;
        public Dictionary<string, string> Usages;
        public List<string> Briefs;

        public Commands(LineEditMode lem)
        {
            LEM = lem;
            Cmds = new Dictionary<string, Action<string[], Box<bool>, LineEditMode>>();
            Usages = new Dictionary<string, string>();
            Briefs = new List<string>();

            MethodInfo[] mis = GetType().GetMethods();
            object[] objs;
            CommandNameAttribute nameatr;
            CommandUsageAttribute usageatr;
            CommandBriefAttribute briefatr;
            string brief;
            Action<string[], Box<bool>, LineEditMode> act;
            
            foreach (MethodInfo mi in mis)
            {
                if ((objs = mi.GetCustomAttributes(typeof(CommandNameAttribute), false)).Length == 0) continue;

                nameatr = objs[0] as CommandNameAttribute;
                usageatr = ((objs = mi.GetCustomAttributes(typeof(CommandUsageAttribute), false)).Length != 0)
                    ? objs[0] as CommandUsageAttribute : null;
                briefatr = ((objs = mi.GetCustomAttributes(typeof(CommandBriefAttribute), false)).Length != 0)
                    ? objs[0] as CommandBriefAttribute : null;

                act = GenerateCommand(mi);
                    
                Cmds.Add(nameatr.Name, act);
                if (usageatr != null) Usages.Add(nameatr.Name, usageatr.Usage);

                foreach (string c in nameatr.Short.Replace(" ", "").Split(','))
                {
                    if (string.IsNullOrEmpty(c)) continue;
                    Cmds.Add(c, act);
                    if (usageatr != null) Usages.Add(c, usageatr.Usage);
                }

                brief = (nameatr.Name + (nameatr.Short != "" ? " (" + nameatr.Short + ")" : "")).PadRight(14, ' ');
                Briefs.Add(brief + (briefatr != null ? " -- " + briefatr.Brief : ""));
            }
        }

        public Action<string[], Box<bool>, LineEditMode> GenerateCommand(MethodInfo mi)
        {
            return delegate(string[] args, Box<bool> quit, LineEditMode lem)
            {
                mi.Invoke(null, new object[] { args, quit, lem });
            };
        }

        public void Execute(string[] args, Box<bool> quit)
        {
            if (args == null || args.Length == 0) return;
            string cmd = args[0];
            if (Cmds.ContainsKey(cmd) == false)
            {
                LEM.CW.N().WN(string.Format("The command is unkown: {0}", cmd));
                return;
            }

            try
            {
                Cmds[cmd](args, quit, LEM);
            }
            catch (Error /*er*/)
            {
                LEM.CW.N().WN(string.Format("The command is unkown: {0}", "yyy"));
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null
                    && ex.InnerException is UsageException
                    && Usages.ContainsKey(cmd)
                    ) throw new Exception("usage: " + Usages[cmd]);

                throw ex;
            }
        }

        static public void Usage() { throw new UsageException(); }

        [CommandName("delete", "d")]
        [CommandBrief("delete lines")]
        [CommandUsage("delete|d <line no> [[- <line no to>]|[: [-]<lenght>]]")]
        static public void Delete(string[] args, Box<bool> quit, LineEditMode lem)
        {
            if (lem.Lines.Count == 0) throw new Exception("No lines to delete");
            int no = 0, n2 = 0, len;
            
            if (args.Length != 2 && args.Length != 4) Usage();
            if (int.TryParse(args[1], out no) == false) Usage();
            if (args.Length == 4 && Regex.IsMatch(args[2], @"(-|:)") == false) Usage();
            if (args.Length == 4 && int.TryParse(args[3], out n2) == false) Usage();

            if (args.Length == 4 && args[2] == "-")
            {
                if (n2 < no) { int tmp = no; no = n2; n2 = tmp; }
                len = n2 - no + 1;
            }
            else if (args.Length == 4 && args[2] == ":")
            {
                if (n2 < 0)
                {
                    no = no + n2;
                    n2 = Math.Abs(n2) + 1;
                }
                len = n2;
            }
            else
            {
                len = 1;
            }

            VerifyLineNo(no, lem.Lines.Count,/* extra = */ 0);
            VerifyLineNo(no + len - 1, lem.Lines.Count,/* extra = */ 0);
            lem.Lines.RemoveRange(no - 1, len);
        }

        [CommandName("insert", "i")]
        [CommandBrief("set current line no")]
        [CommandUsage("insert [<line no>]")]
        static public void Insert(string[] args, Box<bool> quit, LineEditMode lem)
        {
            int no = 0;
            if (args.Length != 1 && args.Length != 2) Usage();
            if (args.Length == 2 && int.TryParse(args[1], out no) == false) Usage();
            
            if (args.Length == 1) no = lem.Lines.Count + 1;
            VerifyLineNo(no, lem.Lines.Count,/* extra = */ 1);

            if (no > 1) WriteLineFormat(no - 1, lem);
            
            lem.Row = no;
            lem.Col = 1;
            lem.EditLn = "";
        }

        [CommandName("delins", "di")]
        [CommandBrief("delete the line no and set as a current line no")]
        [CommandUsage("delins [<line no>]")]
        static public void DelIns(string[] args, Box<bool> quit, LineEditMode lem)
        {
            Delete(args, quit, lem);
            Insert(args, quit, lem);
        }

        [CommandName("list", "ls")]
        [CommandBrief("show list")]
        [CommandUsage("list")]
        static public void List(string[] args, Box<bool> quit, LineEditMode lem)
        {
            for (int i = 1; i <= lem.Lines.Count; i++)
            {
                WriteLineFormat(i, lem);
            }
        }

        static public void WriteLineFormat(int no, LineEditMode lem)
        {
            if (no <= 0 || lem.Lines.Count < no) return;

            lem.CW.WN(string.Format("{0:D"
                + lem.Lines.Count.ToString().Length
                + "}:{1}", no, lem.Lines[no - 1]));
        }

        //static public Ctrl genCtrl() { Ctrl c = new Ctrl(); c.Init(); return c; }

        [CommandName("tokenize", "tk")]
        [CommandBrief("show tokens")]
        [CommandUsage("tokenize")]
        static public void Tokenize(string[] args, Box<bool> quit, LineEditMode lem)
        {
            string src;
            src = Cty.ToText(lem.Lines);
            SyntaxAnalyzer p = new SyntaxAnalyzer();
            p.Init(src);

            ITokenEnumerator tkz = p.Tokens;
            while (tkz.EOF == false)
            {
                lem.CW.WN(tkz.Cur.ToString());
                tkz.Next();
            }
        }

        [CommandName("parse", "p")]
        [CommandBrief("show parse tree")]
        [CommandUsage("parse")]
        static public void Parse(string[] args, Box<bool> quit, LineEditMode lem)
        {
            string src = Cty.ToText(lem.Lines);
            Token t = (new SyntaxAnalyzer()).Run(src);
            lem.CW.W(TokenEx.ToTree(t));
        }

        [CommandName("compile", "c")]
        [CommandBrief("compile list")]
        [CommandUsage("compile")]
        static public void Compile(string[] args, Box<bool> quit, LineEditMode lem)
        {
            string output = Compile(lem);
            lem.CW.WN(output);
        }

        static public string Compile(LineEditMode lem)
        {
            Ctrl c = new Ctrl();
            StringBuilder b = new StringBuilder();
            Action<string> trace = delegate(string s) { b.Append(s); };
            c.StdOut = trace;
            c.StdErr = trace;
            
            c.StartCompile(new string[] { lem.DefaultSrcPath
                , "/xxxil"
                , "/xxxtrace"
            });

            return b.ToString();
        }

        [CommandName("go", "")]
        [CommandBrief("compile list and go the executable file")]
        [CommandUsage("go")]
        static public void Go(string[] args, Box<bool> quit, LineEditMode lem)
        {
            StringBuilder output = new StringBuilder();
            output.Append(Compile(lem));

            // go
            output.AppendLine("--- go ---");
            Process p;
            string stdoutput;
            p = new Process();
            p.StartInfo.FileName = @"lem_default.exe";
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true; // 標準出力をリダイレクト

            p.Start(); // アプリの実行開始
            
            stdoutput = p.StandardOutput.ReadToEnd(); // 標準出力の読み取り
            stdoutput = stdoutput.Replace("\r\r\n", "\n"); // 改行コードの修正

            output.Append(stdoutput);
            
            lem.CW.WN(output.ToString()); // ［出力］ウィンドウに出力
        }

        [CommandName("quit", "q")]
        [CommandBrief("quit line edit mode")]
        [CommandUsage("quit")]
        static public void Quit(string[] args, Box<bool> quit, LineEditMode lem)
        {
            quit.Value = true;
        }

        [CommandName("help", "h,?")]
        [CommandBrief("show brief help")]
        [CommandUsage("help")]
        static public void Help(string[] args, Box<bool> quit, LineEditMode lem)
        {
            if (args.Length >= 2 && lem.Cmds.Usages.ContainsKey(args[1]))
            {
                string s = lem.Cmds.Usages[args[1]];
                lem.CW.WN(s);
                return;
            }

            foreach (string b in lem.Cmds.Briefs)
            {
                lem.CW.WN(b);
            }
        }

        static public void VerifyLineNo(int no, int actual, int extra)
        {
            if (no < 1)
                throw new Exception(string.Format("input line no greater than or equal 1. your input:'{0}'", no));
            if (no > (actual + extra))
                throw new Exception(string.Format("input line no less than or equal {0}. your input:'{1}'", (actual + extra), no));
        }
    }

    public class CommandNameAttribute : Attribute
    {
        public string Name;
        public string Short;
        public CommandNameAttribute(string name, string short_) { Name = name; Short = short_; }
    }

    public class CommandBriefAttribute : Attribute
    {
        public string Brief;
        public CommandBriefAttribute(string brief) { Brief = brief; }
    }

    public class CommandUsageAttribute : Attribute
    {
        public string Usage;
        public CommandUsageAttribute(string usage) { Usage = usage; }
    }

    public class UsageException : Exception { }
}
