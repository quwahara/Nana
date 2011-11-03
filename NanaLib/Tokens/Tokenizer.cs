using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Nana.Infr;
using Nana.Delegates;

namespace Nana.Tokens
{
    /// <summary>
    /// このインターフェイスの要件
    /// カーソルが現在さすTokenを取得できること     (Cur)
    /// 末尾を越えた状態が取得、表現できること      (EOF)
    /// 初期状態は集合の先頭のTokenを指していること (Cur)
    /// 末尾を越えた状態はnullを指していること      (Cur)
    /// 集合が空のときはnullを指していること        (Cur)
    /// </summary>
    public interface ITokenEnumerator
    {
        bool EOF { get; }
        Token Cur{ get; }
        void Next();
    }

    public class EnumeratorAdapter : ITokenEnumerator
    {
        public IEnumerator<Token> Enm;
        public bool _EOF;

        public EnumeratorAdapter(IEnumerator<Token> enm)
        {
            Enm = enm;
            _EOF = false;
            Next();
        }

        public bool EOF { get { return _EOF; } }
        public Token Cur { get { return EOF ? null : Enm.Current; } }

        public void Next()
        {
            if (EOF) return;
            _EOF = Enm.MoveNext() == false;
        }
    }

    public class Prepend : ITokenEnumerator
    {
        public bool IsFirst;
        public ITokenEnumerator Enm;
        public Token Value;

        public Prepend(ITokenEnumerator enm, Token value)
        {
            IsFirst = true;
            Enm = enm;
            Value = value;
        }

        public bool EOF { get { return Enm.EOF; } }
        public Token Cur { get { return IsFirst ? Value : Enm.Cur; } }

        public void Next()
        {
            if (IsFirst)
            {
                IsFirst = false;
                return;
            }
            Enm.Next();
        }
    }

    public class Append : ITokenEnumerator
    {
        public bool IsInnerEOF;
        public ITokenEnumerator Enm;
        public Token Value;

        public Append(ITokenEnumerator enm, Token value)
        {
            IsInnerEOF = false;
            Enm = enm;
            Value = value;
        }

        public bool EOF { get { return IsInnerEOF; } }
        public Token Cur { get { return IsInnerEOF ? null : (Enm.EOF ? Value : Enm.Cur); } }

        public void Next()
        {
            if (Enm.EOF == false)
            {
                Enm.Next();
                return;
            }
            if (IsInnerEOF == false)
            {
                IsInnerEOF = true;
                return;
            }
        }
    }

    public class TokenizerBase : ITokenEnumerator
    {
        public LineBufferedReader R;
        public Box<int> Pos = new Box<int>(0);
        public Regex StartRx = null;
        public Token _Cur;

        public TokenizerBase(Regex startRx)
        {
            _Cur = null;
            StartRx = startRx;
        }

        public virtual void Init(LineBufferedReader r)
        {
            SetR(r);
            Next();
        }

        public virtual void SetR(LineBufferedReader r)
        {
            if (r == null) throw new ArgumentNullException();
            R = r;
        }

        public bool EOF { get { return R.EOF; } }
        public Token Cur { get { return EOF ? null : _Cur; } }

        public bool EOL { get { return R.EOF == false && R.CurLine.Length <= Pos.Value; } }
        /// <summary>Offset CurLine by Pos</summary>
        public string SubCurLine { get { return R != null ? R.CurLine.Substring(Pos.Value) : null; } }

        public void CallReadLine()
        {
            if (R == null || R.EOF) return;
            R.ReadLine();
            Pos.Value = 0;
        }

        public bool IsTokenStart() { return StartRx.IsMatch(SubCurLine); }

        public void Next()
        {
            if (R.BOF)
            {
                CallReadLine();
                if (R.EOF) return;
            }

            SkipSpace();
            while (EOL)
            {
                CallReadLine();
                if (R.EOF) return;
                SkipSpace();
            }

            _Cur = GetToken();
        }

        public void SkipSpace()
        {
            if (R.CurLine == null) return;
            Match m = Regex.Match(SubCurLine, @"^\s+");
            if (m.Success) Pos.Value += m.Length;
        }

        public virtual Token GetToken() { throw new NotImplementedException(); }
    }

    public class InlineTokenizer : TokenizerBase
    {
        public InlineTokenizer(Regex startRx)
            : base(startRx)
        {
        }

        public override Token GetToken()
        {
            string ln = SubCurLine;
            if (IsTokenStart() == false) throw new Exception();
            string[] groups;
            string g = "";
            Match m = StartRx.Match(ln);
            Token t = new Token();
            groups = StartRx.GetGroupNames();
            for (int i = 1; i < groups.Length; i++)
            {
                if (m.Groups[i].Success)
                {
                    g = groups[i];
                    break;
                }
            }

            t = new Token();
            t.Value = m.Value;
            t.Group = g;
            t.Path = R.Path;
            t.Row = R.ReadCount;
            t.Col = Pos.Value;

            Pos.Value += m.Index + m.Length;

            return t;
        }
    }

    public class BlockTokenizer : TokenizerBase
    {
        public Regex EscRx = null;
        public Regex EndRx = null;
        public string Group = null;

        public BlockTokenizer(Regex startRx, Regex escRx, Regex endRx, string group)
            : base(startRx)
        {
            EscRx = escRx;
            EndRx = endRx;
            Group = group;
        }

        public override Token GetToken()
        {
            string ln = SubCurLine;
            if (IsTokenStart() == false) throw new Exception();
            Match startMt = StartRx.Match(ln);
            Token t = new Token();
            t.Group = Group;

            StringBuilder b = new StringBuilder();
            Match escMt = null, endMt = null;
            int startIdx = startMt.Length;

            Action match = delegate()
            {
                escMt = EscRx.Match(ln, startIdx);
                endMt = EndRx.Match(ln, startIdx);
                if (endMt.Success)
                {
                    if (escMt.Success && escMt.Index < endMt.Index)
                    {
                        endMt = null;
                        startIdx = escMt.Index + escMt.Length;
                    }
                }
                else
                {
                    endMt = null;
                    startIdx = ln.Length;
                }
            };

            match();
            while (ln != null && endMt == null)
            {
                if (startIdx >= ln.Length)
                {
                    b.AppendLine(ln);
                    CallReadLine();
                    if (R.EOF) break;
                    ln = SubCurLine;
                    startIdx = 0;
                }
                match();
            }

            if (endMt == null) throw new Exception(@"No end");
            b.Append(ln.Substring(0, endMt.Index + endMt.Length));
            t.Value = b.ToString();
            Pos.Value += endMt.Index + endMt.Length;

            return t;
        }
    }

    public class ScriptTokenizer : TokenizerBase
    {
        public TokenizerBase[] Tokenizers = null;

        public ScriptTokenizer(Regex inlineRx)
            : base(null)
        {
            Regex startRx, escRx, endRx;

            startRx = new Regex(@"^""");
            escRx = new Regex(@"\\.");
            endRx = new Regex(@"""");
            BlockTokenizer stringLiteral = new BlockTokenizer(startRx, escRx, endRx, "Str");

            startRx = new Regex(@"^/\.");
            escRx = new Regex(@"$^");
            endRx = new Regex(@",/");
            BlockTokenizer commentBlock = new BlockTokenizer(startRx, escRx, endRx, "Cmt");

            startRx = inlineRx;

            Tokenizers = new TokenizerBase[]{
                stringLiteral
                , commentBlock
                , new InlineTokenizer(startRx)
            };

            foreach (TokenizerBase t in Tokenizers) t.Pos = this.Pos;
        }

        public override void Init(LineBufferedReader r)
        {
            base.SetR(r);
            foreach (TokenizerBase t in Tokenizers)
            {
                t.SetR(r);
            }
            Next();
        }

        public override Token GetToken()
        {
            foreach (TokenizerBase t in Tokenizers)
            {
                if (t.IsTokenStart())
                {
                    return t.GetToken();
                }
            }

            throw new Exception(string.Format(@"Could not tokenize the line[{0}]: '{1}'", R.ReadCount, SubCurLine));
        }
    }

    public class LineBufferedReader : IDisposable
    {
        public TextReader R = null;
        public string CurLine = null;
        public int ReadCount;
        public string Path = "";

        public LineBufferedReader(TextReader r)
        {
            R = r;
            ReadCount = -1;
        }

        public static LineBufferedReader GetInstanceWithText(string text)
        {
            return new LineBufferedReader(new StringReader(text));
        }

        public bool BOF { get { return ReadCount < 0; } }
        public bool EOF { get { return BOF == false && CurLine == null; } }

        public string ReadLine()
        {
            if (ReadCount < 0) ReadCount = 0;
            CurLine = R.ReadLine();
            if (CurLine != null) ReadCount++;
            return CurLine;
        }

        public void Dispose()
        {
            if (R != null)
            {
                R.Dispose();
                R = null;
            }
        }
    }
}
