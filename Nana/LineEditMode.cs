using System;
using System.Collections.Generic;
using System.Text;
using Nana.Delegates;
using System.IO;
using Nana.Syntaxes;
using System.Text.RegularExpressions;
using Nana.Infr;

namespace Nana
{
    public class LineEditMode
    {
        public void On()
        {
            Init();
            this.CW.GoLoop();
            SaveDefaultSrc();
        }

        // ref http://msdn.microsoft.com/ja-jp/library/system.console_members%28v=VS.80%29.aspx

        public void Init()
        {
            this.IsEdit = false;
            this.EditLn = "";
            this.Row = 1;
            this.Col = 1;

            if (File.Exists(DefaultSrcPath))
            {
                this.Lines = new List<string>(File.ReadAllLines(this.DefaultSrcPath, Encoding.UTF8));
                this.Row = this.Lines.Count + 1;
            }

            SetEditMode();
        }

        public void SaveDefaultSrc()
        {
            File.WriteAllLines(this.DefaultSrcPath, this.Lines.ToArray());
        }

        public static readonly ConsoleKeyInfo Space = new ConsoleKeyInfo(' ', ConsoleKey.Spacebar, false, false, false);

        public ConsoleWrapper CW;

        public Dictionary<ConsoleKey, Action<ConsoleWrapper, ConsoleKeyInfo, Box<bool>>> OnReads;
        public Dictionary<ConsoleKey, Action<ConsoleWrapper, ConsoleKeyInfo, Box<bool>>> OnReadsAlt;

        public Func<string> EditPrompt;
        public Func<string> CmdPrompt;

        public string EditLn;

        public int Row;
        public int Col;
        public List<string> Lines;

        public string DefaultSrcPath = "";

        public bool IsEdit;

        public ConsoleLineEdit LE = new ConsoleLineEdit();
        public Commands Cmds;

        public LineEditMode()
        {
            this.DefaultSrcPath = "lem_default.nana";

            this.Lines = new List<string>();
            this.CW = new ConsoleWrapper();
            this.CW.OnRead = OnRead;
            Cmds = new Commands(this);

            this.EditPrompt = delegate() {
                return string.Format("{0:D"
                 + Lines.Count.ToString().Length
                 + "}>", Row);
                //return this.Row.ToString() + ">"; 
            };
            this.CmdPrompt = delegate() { return ":"; };

            // OnReads
            Dictionary<ConsoleKey, Action<ConsoleWrapper, ConsoleKeyInfo, Box<bool>>> d;
            d = new Dictionary<ConsoleKey, Action<ConsoleWrapper, ConsoleKeyInfo, Box<bool>>>();
            this.OnReads = d;

            d.Add(ConsoleKey.Enter      /**/, delegate(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit)
            {
                if (IsEdit) EditEnter(c, inf, quit); else CmdEnter(c, inf, quit);
            });

            d.Add(ConsoleKey.Escape     /**/, delegate(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit)
            {
                c.N().Home();
                if (IsEdit) SetCmdMode(); else SetEditMode();
            });

            d.Add(ConsoleKey.Backspace  /**/, delegate(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit) { LE.Bs(); });
            d.Add(ConsoleKey.Tab        /**/, delegate(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit) { LE.Tab(); });
            d.Add(ConsoleKey.LeftArrow  /**/, delegate(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit) { LE.L(); });
            d.Add(ConsoleKey.RightArrow /**/, delegate(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit) { LE.R(); });
            d.Add(ConsoleKey.UpArrow    /**/, delegate(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit) { });
            d.Add(ConsoleKey.DownArrow  /**/, delegate(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit) { });

            d = new Dictionary<ConsoleKey, Action<ConsoleWrapper, ConsoleKeyInfo, Box<bool>>>();
            this.OnReadsAlt = d;
            d.Add(ConsoleKey.G          /**/, delegate(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit) { SimCmd("go"); });
            d.Add(ConsoleKey.L          /**/, delegate(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit) { SimCmd("list"); });
            d.Add(ConsoleKey.P          /**/, delegate(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit) { SimCmd("parse"); });
        }

        private void SetEditMode()
        {
            IsEdit = true;
            
            if (Row < 1) Row = 1;
            if (Row > (Lines.Count + 1)) Row = Lines.Count + 1;
            LE.Prompt = EditPrompt;
            LE.Init();
            LE.Ins(EditLn);
        }

        private void SetCmdMode()
        {
            EditLn = LE.Line;
            IsEdit = false;
            LE.Prompt = CmdPrompt;
            LE.Init();
        }

        public void EditEnter(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit)
        {
            CW.N();
            if (Row > Lines.Count)
            {
                Lines.Add(LE.Line);
            }
            else
            {
                Lines.Insert(Row - 1, LE.Line);
            }

            Row += 1;
            LE.Init();
        }
        
        public void CmdEnter(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit)
        {
            CW.N();
            string ln = LE.Line;
            try
            {
                string[] args;
                args = Regex.Split(ln.Trim(), @"\s+");
                if (args.Length == 0) args = new string[] { "" };
                Cmds.Execute(args, quit);
                if (quit.Value == false) SetEditMode();
            }
            catch (Exception e)
            {
                if (e.InnerException != null) e = e.InnerException;

                if (e is Error)
                {
                    Error er_ = e as Error;
                    string s = FormatError(er_);
                    c.WN(s);
                }
                else
                {
                    c.WN(e.ToString());
                }
                LE.Init();
                LE.Ins(ln);
            }
        }

        public static string FormatError(Error e)
        {
            //return string.Format("Path:{0}, Row:{1}, Col:{2}\n{3}:{4}", e.Path, e.Row, e.Col, e.GetType().Name, e.Message);
            return string.Format("Path:{0}, Row:{1}, Col:{2}\n{3}:{4}", e.Path, e.Row, e.Col, e.GetType().Name, 
                e.ToString().Replace(@"C:\Documents and Settings\user1\My Documents\Visual Studio 2005\Projects\Nana\","")
                );
        }

        public void OnRead(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit)
        {
            if (inf.Modifiers == ConsoleModifiers.Alt && OnReadsAlt.ContainsKey(inf.Key))
            {
                OnReadsAlt[inf.Key](c, inf, quit);
                return;
            }

            if (OnReads.ContainsKey(inf.Key))
            {
                OnReads[inf.Key](c, inf, quit);
            }
            else
            {
                LE.Ins(inf.KeyChar.ToString());
            }
        }

        public void SimCmd(string args)
        {
            if (string.IsNullOrEmpty(args)) return;

            ConsoleKey key;
            ConsoleKeyInfo inf;
            Box<bool> quit = new Box<bool>(false);

            key = ConsoleKey.Escape;
            inf = new ConsoleKeyInfo((char)ConsoleKey.Escape, key, false, false, false);
            OnRead(CW, inf, quit);

            for (int i = 0; i < args.Length; i++)
            {
                key = (ConsoleKey)Enum.Parse(typeof(ConsoleKey), args[i].ToString().ToUpper());
                inf = new ConsoleKeyInfo(args[i], key, false, false, false);
                OnRead(CW, inf, quit);
            }

            key = ConsoleKey.Enter;
            inf = new ConsoleKeyInfo((char)ConsoleKey.Enter, key, false, false, false);
            OnRead(CW, inf, quit);
        }

        static public void List(LineEditMode lem)
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
                + "}: {1}", no, lem.Lines[no - 1]));
        }

    }

    public interface ILineEdit
    {
        void Init();
        string Line { get; set; }
        int Pos { get; }
        bool EOL { get; }
        bool HOL { get; }
        ILineEdit L();
        ILineEdit R();
        ILineEdit Ins(string value);
        ILineEdit Bs();
        ILineEdit Tab();
    }

    public class StringLineEdit : ILineEdit
    {
        public string TabSpace;

        public string _Line;

        public string Line { get { return _Line; } set { _Line = value; } }

        public int _Pos;

        public int Pos { get { return _Pos; } set { _Pos = value; } }
        /// <summary>Head Of Line</summary>
        public bool HOL { get { return Pos == 1; } }
        /// <summary>End Of Line</summary>
        public bool EOL { get { return Pos == (Line.Length + 1); } }

        public StringLineEdit()
        {
            TabSpace = "    ";
            Init();
        }
        
        public virtual void Init()
        {
            Line = "";
            Pos = 1;
        }

        /// <summary>Left</summary>
        public virtual ILineEdit L()
        {
            if (HOL) return this;
            Pos -= 1;
            return this;
        }

        /// <summary>Right</summary>
        public virtual ILineEdit R()
        {
            if (EOL) return this;
            Pos += 1;
            return this;
        }

        /// <summary>Insert</summary>
        public virtual ILineEdit Ins(string value)
        {
            value = value ?? "";
            if (EOL)
            {
                Line += value;
            }
            else
            {
                Line.Insert(Pos - 1, value);
            }
            Pos += value.Length;
            return this;
        }

        /// <summary>Backspace</summary>
        public virtual ILineEdit Bs()
        {
            if (HOL) return this;
            Line = Line.Remove(Pos - 2);
            Pos -= 1;
            return this;
        }

        /// <summary>Tab</summary>
        public virtual ILineEdit Tab()
        {
            string tab = GetTab();
            Ins(tab);
            return this;
        }

        public string GetTab()
        {
            int tabSize = TabSpace.Length;
            int d = tabSize - ((Pos - 1) % tabSize);
            string tab = TabSpace.Substring(0, d);
            return tab;
        }
    }

    public class ConsoleLineEdit : ILineEdit
    {
        StringLineEdit SLE;

        public string Line { get { return SLE.Line; } set { SLE.Line = value; } }
        public int Pos { get { return SLE.Pos; } }
        public bool HOL { get { return SLE.HOL; } }
        public bool EOL { get { return SLE.EOL; } }

        public Func<string> Prompt = delegate() { return ""; };

        public ConsoleLineEdit()
        {
            SLE = new StringLineEdit();
            Init();
        }

        public void Init()
        {
            SLE.Init();
            Console.Write(Prompt());
        }

        public virtual ILineEdit L()
        {
            if (SLE.HOL) return this;
            Console.CursorLeft -= 1;
            SLE.L();
            return this;
        }

        public virtual ILineEdit R()
        {
            if (SLE.EOL) return this;
            Console.CursorLeft += 1;
            SLE.R();
            return this;
        }

        public virtual ILineEdit Ins(string value)
        {
            Console.Write(value);
            SLE.Ins(value);
            return this;
        }

        /// <summary>Backspace</summary>
        public virtual ILineEdit Bs()
        {
            if (SLE.HOL) return this;
            Console.CursorLeft -= 1;
            Console.Write(" ");
            Console.CursorLeft -= 1;
            SLE.Bs();
            return this;
        }

        /// <summary>Tab</summary>
        public virtual ILineEdit Tab()
        {
            Ins(SLE.GetTab());
            return this;
        }
    }
    
    public class ConsoleWrapper
    {
        public bool Suppress = true;

        public Action<ConsoleWrapper, ConsoleKeyInfo, Box<bool>> OnRead
            = delegate(ConsoleWrapper c, ConsoleKeyInfo inf, Box<bool> quit) { };

        public void GoLoop()
        {
            int left, top;
            ConsoleKeyInfo inf;
            Box<bool> quit = new Box<bool>(false);

            while (quit.Value == false)
            {
                left = Console.CursorLeft;
                top = Console.CursorTop;

                inf = Console.ReadKey(Suppress);

                if (Console.CursorLeft != left) Console.CursorLeft = left;
                if (Console.CursorTop != top) Console.CursorTop = top;

                OnRead(this, inf, quit);
            }
        }

        public ConsoleWrapper Home()
        {
            Console.CursorLeft = 0;
            return this;
        }

        public ConsoleWrapper D()
        {
            Console.CursorTop = Console.CursorTop + 1;
            return this;
        }

        public void Ins(string value)
        {
            Console.Write(value);
        }

        public ConsoleWrapper N()
        {
            Console.WriteLine();
            return this;
        }

        public ConsoleWrapper W(char value)
        {
            Console.Write(value);
            return this;
        }

        public ConsoleWrapper W(string value)
        {
            Console.Write(value);
            return this;
        }

        public ConsoleWrapper WN(string value)
        {
            W(value); N();
            return this;
        }

        public bool IsOnLeft(int offset)
        {
            return Console.CursorLeft <= (0 + offset);
        }
    }
}
