/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using Nana.Delegates;
using System.Collections.Specialized;

namespace Nana.Infr
{
    public class Node
    {
        static public readonly Node NullNode = new Node();

        public string Leaf = null;
        public List<Node> Branches = null;

        public static string SCAN_PATTERN = @"(?<V>([^\s\\()]|\\\\|\\\(|\\\))+)|(?<L>\()|(?<R>\))";     // rev. 3: handle escape, but not handle escape of escape
        public static string LEFT = "L";
        public static string RIGHT = "R";
        public static string VALUE = "V";

        public Node() { }
        public Node(string leaf) { this.Leaf = leaf; }
        public Node(List<Node> branches) { this.Branches = branches; }

        public static List<Node> Parse(string input)
        {
            return Parse(input, SCAN_PATTERN, LEFT, RIGHT, VALUE);
        }

        public static List<Node> Parse(string input, string scanPattern, string left, string right, string value)
        {
            Match m;
            m = Regex.Match(input, scanPattern, RegexOptions.ExplicitCapture);
            return Parse(ref m, left, right, value);
        }

        public static List<Node> Parse(ref Match m, string left, string right, string value)
        {
            List<Node> result;
            result = new List<Node>();
            while (m.Success)
            {
                // left, down
                if (m.Groups[left].Success)
                {
                    m = m.NextMatch();
                    Node tmp;
                    tmp = new Node();
                    tmp.Branches = Parse(ref m, left, right, value);
                    result.Add(tmp);
                }
                else if (m.Groups[right].Success)
                {
                    // right, up
                    m = m.NextMatch();
                    return result;
                }
                else if (m.Groups[value].Success)
                {
                    // value, pluck
                    Node tmp;
                    tmp = new Node();
                    tmp.Leaf = m.Groups[value].Value;
                    result.Add(tmp);
                    m = m.NextMatch();
                }
            }
            return result;
        }

        public override string ToString()
        {
            if (Leaf != null) return Leaf;
            if (Branches != null)
            {
                StringBuilder b = new StringBuilder();
                b.Append("(");
                if (Branches.Count >= 1) b.Append(Branches[0].ToString());
                if (Branches.Count >= 2)
                    foreach (Node c in Branches.GetRange(1, Branches.Count - 1))
                        b.Append(" " + c.ToString());
                b.Append(")");
                return b.ToString();
            }
            return "";
        }

        public static List<Node> ParseFromIndent(string input)
        {
            return ParseFromIndent(Sty.ToStringList(input));
        }

        public static List<Node> ParseFromIndent(List<string> inputs)
        {
            int idx;
            Node n;
            string ind, indpre;
            Match m;
            bool isSeparator = false;
            // object[0] -- indent, object[1] -- branches node
            Stack<object[]> stk = new Stack<object[]>();

            n = new Node(new List<Node>());

            // ignores heading empty lines.
            idx = inputs.FindIndex(delegate(string itm) { return Sty.NotNullOrEmpty(itm); });
            if (idx < 0) return n.Branches;
            if (idx > 0) inputs = inputs.GetRange(idx, inputs.Count - idx);

            Func<string, string> indf = delegate(string s)
            {
                m = Regex.Match(s, @"^\s+");
                return m.Success ? m.Value : "";
            };

            Func<string, Node, Node> push = delegate(string ind_, Node n_)
            {
                stk.Push(new object[] { ind_, n_ });
                Node nn = new Node(new List<Node>());
                n_.Branches.Add(nn);
                return nn;
            };

            indpre = "";
            ind = indf(inputs[0]);

            if (ind.Length > indpre.Length)
            {
                n = push(indpre, n);
            }

            n.Branches.Add(new Node(inputs[0].Trim()));
            indpre = ind;

            Action<string> newf = delegate(string s)
            {
                s = s.TrimEnd();
                if (s != "")
                {
                    ind = indf(s);
                    if (ind.Length > indpre.Length)
                    {
                        n = push(indpre, n);
                    }
                    else if (ind.Length < indpre.Length)
                    {
                        object[] itm;
                        n = null;
                        while (stk.Count > 0)
                        {
                            itm = stk.Pop();
                            if ((itm[0] as string).Length == ind.Length)
                            {
                                n = itm[1] as Node;
                                break;
                            }
                        }
                        if (n == null)
                            throw new Exception("Supplied text is wrong format.");
                    }

                    if (isSeparator)
                    {
                        if (stk.Count == 0) 
                            throw new Exception("Supplied text is wrong format.");

                        // creates new brances if previous line is a separator.
                        object[] itm = stk.Pop();
                        n = itm[1] as Node;
                        Node nn = new Node(new List<Node>());
                        n.Branches.Add(nn);
                        n = nn;
                        stk.Push(itm);
                    }

                    n.Branches.Add(new Node(s.Trim()));
                    indpre = ind;
                    isSeparator = false;
                }
                else
                {
                    isSeparator = true;
                }
            };

            inputs.GetRange(1, inputs.Count - 1).ForEach(newf);

            while(stk.Count>0) n = (stk.Pop() as object[])[1] as Node;

            return n.Branches;
        }

        public string ToIndent()
        {
            Action<Node> na;
            Action<List<Node>> nsa = null;
            const string ind = "    ";
            FList<string> inds = new FList<string>();
            StringBuilder b = new StringBuilder();

            na = delegate(Node n)
            {
                if (n.Leaf != null)
                {
                    b
                        .Append(inds.Deriv(Cty.ToLine))
                        .Append(n.Leaf)
                        .AppendLine();
                }
                else if (n.Branches != null)
                {
                    if (n != this) inds.Add(ind);
                    nsa(n.Branches);
                    if (n != this) inds.RemoveAt(inds.Count - 1);
                }
            };

            nsa = delegate(List<Node> ns)
            {
                if (ns.Count == 0) return;

                bool isPrevBranch = ns[0].Branches != null;
                na(ns[0]);
                for (int i = 1; i < ns.Count; i++)
                {
                    if (isPrevBranch && ns[i].Branches != null)
                    {
                        b
                            .Append(inds.Deriv(Cty.ToLine))
                            .Append(ind)
                            .AppendLine();
                    }
                    isPrevBranch = ns[i].Branches != null;
                    na(ns[i]);
                }
            };

            na(this);

            return b.ToString();
        }

        public static void Walk(Node n, Action<Node> a)
        {
            if (n.Leaf != null)
            {
                a(n);
            }
            else if (n.Branches != null)
            {
                Walk(n.Branches, a);
            }
        }

        public static void Walk(List<Node> ns, Action<Node> a)
        {
            ns.ForEach(delegate(Node nn) { Walk(nn, a); });
        }
    }
}
