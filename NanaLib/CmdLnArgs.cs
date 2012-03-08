/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Nana.Delegates;
using Nana.Syntaxes;
using Nana.Tokens;

namespace Nana
{
    public class CmdLnArgs
    {
        static public readonly string OptHead = @"^(/|-)";

        static public readonly string[] Options = {
            "out"
            , "help"
            , "reference"
            , "verbose"
            , "xxxsyntax"
            , "xxxil"
            , "xxxtrace"
            };

        static public Token PickOpt(string arg)
        {
            string opt = "", val = "";

            Func<bool> split = delegate()
            {
                Match m;
                m = Regex.Match(arg, string.Format(@"{0}{1}(:|=)?", OptHead, opt));
                if (m.Success == false) return false;
                if (m.Length > opt.Length)
                {
                    val = arg.Substring(m.Length);
                }
                return true;
            };

            foreach (string o in Options)
            {
                opt = o;
                if (split())
                {
                    return NewOpt(o, val);
                }
            }

            return null;
        }

        static public Token NewOpt(string opt, string val)
        {
            return new Token(val, opt);
        }

        static public Token PickSrc(string arg)
        {
            return new Token(arg, "SourcePath");
        }

        static public Token GetCmdLnArgs(string[] args)
        {
            if (args == null)
                throw new ArgumentNullException("args");

            List<Token> opts = new List<Token>();
            List<Token> srcs = new List<Token>();

            Action<string> Pick = delegate(string arg)
            {
                if (arg == null) return;
                arg = arg.Trim();
                if (arg == "") return;

                Token t;
                if (Regex.IsMatch(arg, OptHead))
                {
                    if ((t = PickOpt(arg)) == null)
                    {
                        throw new Exception(string.Format("Not supported option: {0}", arg));
                    }
                    opts.Add(t);
                }
                else
                {
                    srcs.Add(PickSrc(arg));
                }
            };

            foreach (string arg in args)
            {
                Pick(arg);
            }

            Token srct = new Token("", "Sources");
            srct.Follows = srcs.ToArray();

            Token optt = new Token("", "CompileOptions");
            optt.Follows = opts.ToArray();

            if (0 == optt.Select("@out").Length && srcs.Count > 0)
            {
                string v = srcs[0].Value;
                string dir = System.IO.Path.GetDirectoryName(v);
                string fn = System.IO.Path.GetFileNameWithoutExtension(v) + ".exe";
                optt.FlwsAdd(NewOpt("out", System.IO.Path.Combine(dir, fn)));
            }

            Token ret = new Token("", "Arguments");
            ret.Follows = new Token[] { optt, srct };

            return ret;
        }
    }
}
