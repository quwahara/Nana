using System;
using System.Collections.Generic;
using System.Text;
using Nana.Syntaxes;
using Nana.Tokens;

namespace Nana.Infr
{
    public class Error : Exception
    {
        public string Path;
        public int Row;
        public int Col;

        public Error(string message, string path, int row, int col)
            : base(message)
        {
            Path = path;
            Row = row;
            Col = col;
        }

        public Error(string message, string path)
            : this(message, path, -1, -1)
        {
        }

        public Error(string message)
            : this(message, "", -1, -1)
        {
        }

        public Error(string message, Token t)
            : this(message, t.Path, t.Row, t.Col)
        {
        }
    }

    public class InternalError : Error
    {
        public InternalError(string message)
            : base(message)
        {
        }
        public InternalError(string message, Token t)
            : base(message, t.Path, t.Row, t.Col)
        {
        }
    }

    public class SyntaxError : Error
    {
        public SyntaxError(string message)
            : base(message)
        {
        }
        public SyntaxError(string message, Token t)
            : base(message, t.Path, t.Row, t.Col)
        {
        }
    }

    public class TypeError : Error
    {
        public TypeError(string message, Token t)
            : base(message, t.Path, t.Row, t.Col)
        {
        }
    }

    public class AccessError : Error
    {
        public AccessError(string message, Token t)
            : base(message, t.Path, t.Row, t.Col)
        {
        }
    }

    public class IMRTranslation : Error
    {
        public IMRTranslation(string message)
            : base(message)
        {
        }
        public IMRTranslation(string message, Token t)
            : base(message, t.Path, t.Row, t.Col)
        {
        }
    }
}
