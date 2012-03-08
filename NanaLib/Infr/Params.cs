/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Nana.Infr
{
    public class Params
    {
        static public string[] Split(string input, string splitptn, string quotebgn, string quoteend, string escape)
        {
            List<string> result;
            string ebegin, eend, buf;
            bool inquotes;
            bool ignoreNext;
            Match m;

            int startidx, qidx, eqidx, sidx;

            result = new List<string>();

            input = input ?? "";
            if (quotebgn == null) throw new ArgumentNullException("quotebgn");
            quoteend = quoteend ?? quotebgn;
            escape = escape ?? "";
            ebegin = escape + quotebgn;
            eend = escape + quoteend;
            inquotes = false;
            ignoreNext = false;
            startidx = 0;
            buf = "";

            while (startidx < input.Length)
            {
                input = input.Substring(startidx);
                if (inquotes == false)
                {
                    m = Regex.Match(input, splitptn);
                    sidx = m.Success ? m.Index : int.MaxValue;
                    qidx = input.IndexOf(quotebgn);
                    eqidx = input.IndexOf(ebegin);
                    if (qidx == -1) qidx = int.MaxValue;
                    if (eqidx == -1) eqidx = int.MaxValue;

                    if (sidx == int.MaxValue && qidx == int.MaxValue && eqidx == int.MaxValue)
                    {
                        // -- not match
                        result.Add(buf + input);
                        buf = "";
                        startidx = input.Length;
                    }
                    else if (sidx < qidx && sidx < eqidx)
                    {
                        // -- matches with the spaces
                        if (ignoreNext == false || (ignoreNext && sidx > 0))
                            result.Add(buf + input.Substring(0, sidx));
                        ignoreNext = false;
                        buf = "";
                        startidx = sidx + m.Length;
                    }
                    else if (qidx < sidx && qidx < eqidx)
                    {
                        // -- matches with the quote begin
                        buf = quotebgn;
                        inquotes = true;
                        startidx = qidx + quotebgn.Length;
                    }
                    else
                    {
                        // -- matches with the escaped quote begin
                        buf += input.Substring(0, eqidx + ebegin.Length);
                        startidx = eqidx + ebegin.Length;
                    }
                }
                else
                {
                    qidx = input.IndexOf(quoteend);
                    eqidx = input.IndexOf(eend);
                    if (qidx == -1) qidx = int.MaxValue;
                    if (eqidx == -1) eqidx = int.MaxValue;

                    if (qidx == int.MaxValue && eqidx == int.MaxValue)
                    {
                        // -- not match
                        result.Add(buf + input);
                        buf = "";
                        startidx = input.Length;
                    }
                    else if (qidx < eqidx)
                    {
                        // -- matches with the quote end
                        result.Add(buf + input.Substring(0, qidx + quoteend.Length));
                        buf = "";
                        inquotes = false;
                        ignoreNext = true;
                        startidx = qidx + quoteend.Length + 1;
                    }
                    else
                    {
                        // -- matches with the escaped quote end
                        buf += input.Substring(0, eqidx + ebegin.Length);
                        startidx = eqidx + ebegin.Length;
                    }

                }
            }

            return result.ToArray();
        }

        static public string RemoveQuoteBegin(string input, string begin)
        {
            if (input == null || begin == null || begin == "") return input;
            if (input.StartsWith(begin)) input = input.Substring(begin.Length);
            return input;
        }

        static public string RemoveQuoteEnd(string input, string end)
        {
            if (input == null || end == null || end == "") return input;
            if (input.EndsWith(end)) input = input.Substring(0, input.Length - end.Length);
            return input;
        }

        static public string RemoveQuotes(string input, string begin, string end)
        {
            return RemoveQuoteEnd(RemoveQuoteBegin(input, begin), end);
        }

    }
}
