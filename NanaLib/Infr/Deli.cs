/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Nana.Infr
{
    /// <summary>
    /// LIst with DEpth
    /// </summary>
    public class Deli
    {
        public string Name = "";
        public int Depth { get { return Parent == null ? 0 : 1 + Parent.Depth; } }
        public Deli Parent = null;
        public List<string> List = new List<string>();
        public List<Deli> Subs = new List<Deli>();

        public static readonly Deli Zero = new Deli();

        public Deli Sub(string path)
        {
            Deli d;
            int idx;
            string name;

            d = this;
            if (path.StartsWith("#"))
            {
                while (d.Parent != null) d = d.Parent;
                path = path.Substring(1);
            }

            idx = path.IndexOf("#");
            name = idx >= 0 ? path.Substring(0, idx) : path;
            foreach (Deli s in d.Subs)
            {
                if (s.Name == name)
                {
                    return idx < 0 ? s : s.Sub(path.Substring(idx + 1));
                }
            }

            return Deli.Zero;
        }

        public static List<string> SubNames(Deli d)
        {
            List<string> result = new List<string>();
            foreach (Deli m in d.Subs) result.Add(m.Name);
            //d.Subs.ForEach(m => result.Add(m.Name));
            return result;
        }

        public static Deli Load(string text)
        {
            using (StringReader r = new StringReader(text)) return Load(r);
        }

        public static Deli Load(TextReader r)
        {
            Deli result = new Deli();
            string line;
            while ((line = r.ReadLine()) != null) Parse(ref result, line);
            while (result.Parent != null) result = result.Parent;
            return result;
        }

        public static void Parse(ref Deli deli, string line)
        {
            string ln;
            int d;
            ln = line.TrimStart();
            if ((d = ln.IndexOf("#")) == 0)
            {
                d++;
                while (d < ln.Length && ln[d] == '#') d++;
                if (d > (deli.Depth + 1)) throw new Exception("Structure Error. Too much '#' in line:" + line + ".");
                while (deli.Depth >= d) { deli = deli.Parent; }
                Deli tmp = new Deli();
                tmp.Name = ln.Substring(d).Trim();
                tmp.Parent = deli;
                deli = tmp;
                deli.Parent.Subs.Add(deli);
            }
            else
            {
                deli.List.Add(line);
            }
        }

        #region ToString()
        public override string  ToString()
        {
            return ToString(0);
        }
        public string ToString(int offset)
        {
            if (offset > Depth) throw new Exception("The offset must be less or equal its Depth. Depth:" + Depth + " offset:" + offset);

            StringBuilder b = new StringBuilder();
            if ((Depth - offset) > 0)
            {
                for (int i = 0; i < (Depth - offset); i++) b.Append("#");
                b.Append(Name);
                b.AppendLine();
            }
            foreach (string itm in List)
            {
                b.Append(itm);
                b.AppendLine();
            }
            foreach (Deli s in Subs)
            {
                b.Append(s.ToString(offset));
            }
            return b.ToString();
        }
        public string ToStringAsDepthN(int n)
        {
            return ToString(Depth - n);
        }

        #endregion
    }
}
