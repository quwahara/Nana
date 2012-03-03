using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.IO;

namespace Nana.Infr
{
    public class TypeUtil
    {
        public static bool IsBuiltIn(string type)
        {
            return Regex.IsMatch(type, @"^(void|bool|byte|sbyte|char|decimal|double|float|int|uint|long|ulong|object|short|ushort|string)$");
        }

        public static Type FromBuiltIn(string type)
        {
            Type t;
            switch (type)
            {
                case "void": t = typeof(void); break;
                case "bool": t = typeof(bool); break;
                case "byte": t = typeof(byte); break;
                case "sbyte": t = typeof(sbyte); break;
                case "char": t = typeof(char); break;
                case "decimal": t = typeof(decimal); break;
                case "double": t = typeof(double); break;
                case "float": t = typeof(float); break;
                case "int": t = typeof(int); break;
                case "uint": t = typeof(uint); break;
                case "long": t = typeof(long); break;
                case "ulong": t = typeof(ulong); break;
                case "object": t = typeof(object); break;
                case "short": t = typeof(short); break;
                case "ushort": t = typeof(ushort); break;
                case "string": t = typeof(string); break;
                default: t = null; break;
            }
            return t;
        }

        public static Type FromString(string type, List<string> usings)
        {
            List<Type> rs = GetTypesFromString(type, usings);

            if (rs.Count == 0) throw new Exception("The type declaration is unkown. type:" + type);
            if (rs.Count != 1) throw new Exception("The type declaration is ambiguous. type declaration:" + type);

            return rs[0];
        }

        public static List<Type> GetTypesFromString(string type, List<string> usings)
        {
            Type t;
            List<Type> rs;

            rs = new List<Type>();
            t = FromBuiltIn(type);
            if (t != null) rs.Add(t);
            t = Type.GetType(type);
            if (t != null) rs.Add(t);

            foreach (string u in usings)
            {
                t = Type.GetType(u + "." + type);
                if (t != null) rs.Add(t);
            }

            return rs;
        }
    }

    public class TypeLoader
    {
        public TypeInAssemblyLoader InAssembly;

        public TypeLoader()
        {
            this.InAssembly = new TypeInAssemblyLoader();
        }

        public List<string> Namespaces { get { return this.InAssembly.Namespaces; } }

        public Type GetTypeByName(string name) { return GetTypeByName(name, null); }

        public Type GetTypeByName(string name, string[] usings)
        {
            Type t;

            if ((t = FromBuiltIn(name)) != null) return t;
            if ((t = this.InAssembly.GetTypeByFullName(name)) != null) return t;

            if (usings == null) return null;

            foreach (string u in usings)
            {
                if ((t = this.InAssembly.GetTypeByFullName(u + "." + name)) != null) return t;
            }

            return null;
        }

        public static bool IsBuiltIn(string type)
        {
            return Regex.IsMatch(type, @"^(bool|byte|sbyte|char|decimal|double|float|int|uint|long|ulong|object|short|ushort|string)$");
        }

        public static Type FromBuiltIn(string type)
        {
            Type t;
            switch (type)
            {
                case "void": t = typeof(void); break;
                case "bool": t = typeof(bool); break;
                case "byte": t = typeof(byte); break;
                case "sbyte": t = typeof(sbyte); break;
                case "char": t = typeof(char); break;
                case "decimal": t = typeof(decimal); break;
                case "double": t = typeof(double); break;
                case "float": t = typeof(float); break;
                case "int": t = typeof(int); break;
                case "uint": t = typeof(uint); break;
                case "long": t = typeof(long); break;
                case "ulong": t = typeof(ulong); break;
                case "object": t = typeof(object); break;
                case "short": t = typeof(short); break;
                case "ushort": t = typeof(ushort); break;
                case "string": t = typeof(string); break;
                default: t = null; break;
            }
            return t;
        }

        public void LoadFrameworkClassLibraries(string[] assemblyFileNames)
        {
            this.InAssembly.LoadFrameworkClassLibraries(assemblyFileNames);
        }

        public bool IsNamespace(string name)
        {
            return this.InAssembly.Namespaces.Contains(name);
        }
    }

    public class TypeInAssemblyLoader
    {
        string MscorlibPath;
        public List<string> Includes;
        public List<Assembly> Assemblies;
        public List<string> Namespaces;

        public TypeInAssemblyLoader()
        {
            this.Includes = new List<string>();
            this.Assemblies = new List<Assembly>();
            this.Namespaces = new List<string>();

            Assembly mscorlib = Assembly.Load("mscorlib.dll");
            string yen = Path.DirectorySeparatorChar.ToString();
            this.MscorlibPath = Path.GetDirectoryName(mscorlib.Location) + yen;
            this.Includes.Add(this.MscorlibPath);

            this.Assemblies.Add(mscorlib);
            SetUpNamespaces(mscorlib);
        }

        public Assembly[] LoadFrameworkClassLibraries(string[] assemblyFileNames)
        {
            List<Assembly> ls = new List<Assembly>();
            foreach (string fn in assemblyFileNames)
            {
                ls.Add(LoadFrameworkClassLibrarie(fn));
            }
            return ls.ToArray();
        }

        public Assembly LoadFrameworkClassLibrarie(string assemblyFileName)
        {
            string path = FindLocation(assemblyFileName);
            return Register(path);
        }

        public string FindLocation(string assemblyFileName)
        {
            string afn = assemblyFileName;
            string path = null;
            if (afn.ToLower().EndsWith(".dll") == false) afn += ".dll";
            if (File.Exists(Path.GetFullPath(afn)))
            {
                path = Path.GetFullPath(afn);
            }
            else
            {
                string ds = Path.DirectorySeparatorChar.ToString();
                string p;
                for (int i = Includes.Count - 1; i >= 0; i--)
                {
                    p = Includes[i];
                    p += p.EndsWith(ds) ? "" : ds;
                    path = p + afn;
                    if (File.Exists(path)) break;
                    path = null;
                }
            }

            if (path == null)
            { throw new FileNotFoundException("Could not find the assembly file:", assemblyFileName); }
            return path;
        }

        public Assembly Register(string path)
        {
            Assembly asm = Assembly.LoadFile(path);
            if (this.Assemblies.Contains(asm) == false)
            {
                this.Assemblies.Add(asm);
            }

            SetUpNamespaces(asm);

            this.Namespaces.Sort();

            return asm;
        }

        private void SetUpNamespaces(Assembly asm)
        {
            Type[]             ts = asm.GetExportedTypes();
            StringBuilder b;
            char[] sep = new char[] { Type.Delimiter };
            string[] spl;
            Action<string> addToNSs = delegate(string ns)
            {
                if (this.Namespaces.Contains(ns) == false)
                    this.Namespaces.Add(ns);
            };
            foreach (Type t in ts)
            {
                if (string.IsNullOrEmpty(t.Namespace)) continue;
                b = new StringBuilder();
                spl = t.Namespace.Split(sep);
                if (spl.Length >= 1)
                {
                    addToNSs(spl[0]);
                    b.Append(spl[0]);
                    for (int i = 1; i < spl.Length; i++)
                    {
                        b.Append(Type.Delimiter).Append(spl[i]);
                        addToNSs(b.ToString());
                    }
                }
            }
        }

        public Type GetTypeByFullName(string fullName)
        {
            Type t;
            foreach (Assembly a in this.Assemblies)
            {
                if ((t = a.GetType(fullName)) != null) return t;
            }
            return null;
        }

        public List<Type> GetTypesFromString(string fullName)
        {
            Type t;
            List<Type> ts;

            ts = new List<Type>();
            foreach (Assembly a in this.Assemblies)
            {
                t = a.GetType(fullName);
                if (t != null) ts.Add(t);
            }

            return ts;
        }
    }

}
