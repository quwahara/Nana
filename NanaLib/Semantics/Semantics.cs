using System;
using System.Collections.Generic;
using System.Text;
using Nana.Tokens;
using Nana.IMRs;
using Nana.Infr;
using System.Diagnostics;
using System.Reflection;
using Nana.Delegates;
using System.IO;

namespace Nana.Semantics
{
    public class Member
    {
        public Typ Ty;
        public Nmd Value;
        public IValuable Instance;

        public Member(Typ ty, Nmd value, IValuable instance)
        {
            Ty = ty;
            Value = value;
            Instance = instance;
        }
    }

    public class Nmd
    {
        public Token Seed;

        public Nmd() { }
        [DebuggerNonUserCode]
        public Nmd(Token seed) { Seed = seed; }
        public override string ToString()
        {
            return Seed.Value + ":" + typeof(Nmd).Name;
        }

        public Nmd(string name)
        { Name = name; }

        public string Name_;
        public string Name { get { return Name_; } set { Name_ = value; } }
    }

    public interface ITyped
    {
        Typ Typ { get; }
    }

    public class Nsp : Nmd
    {
        public List<Nmd> Members_ = new List<Nmd>();

        public List<Action<Nsp>> EnsureMembersList = new List<Action<Nsp>>();

        public bool IsReferencing = false;

        public Env E;

        public Nsp(string name, bool isReferencing, Env env)
        {
            Name_ = name;
            IsReferencing = isReferencing;
            E = env;
        }

        public Nsp(string name, Env env)
            : this(name, false, env)
        {
        }

        public Nsp(Token seed, Env env)
            : this(seed.Value, env)
        {
            Seed = seed;
        }

        public void EnsureMembers()
        {
            if (EnsureMembersList.Count == 0) { return; }
            List<Action<Nsp>> tmp = EnsureMembersList;
            EnsureMembersList = new List<Action<Nsp>>();
            tmp.ForEach(delegate(Action<Nsp> a) { a(this); });
        }

        public List<Nmd> Members
        {
            get
            {
                EnsureMembers();
                return Members_;
            }
        }

        public bool ContainsKey(string name)
        { return Members.Exists(GetNamePredicate<Nmd>(name)); }

        public T BeAMember<T>(T member) where T : Nmd
        {
            if (member.Name == null) { return default(T); }
            Members.Add(member);
            return member;
        }

        public List<Nmd> FindDownAll(Predicate<Nmd> pred)
        {
            List<Nmd> founds = new List<Nmd>();
            founds.AddRange(Members.FindAll(pred));
            Members.ForEach(delegate(Nmd n)
            { if (n is Nsp) { founds.AddRange((n as Nsp).FindDownAll(pred)); } });
            return founds;
        }

        virtual public Nmd Find(string name)
        {
            return Members.Find(GetNamePredicate<Nmd>(name));
        }

        virtual public Nmd FindByNamePath(string namepath)
        {
            Debug.Assert(false == string.IsNullOrEmpty(namepath));
            return FindByNamePath(new Queue<string>(namepath.Split(new char[] { '/' })));
        }

        virtual public Nmd FindByNamePath(Queue<string> namepath)
        {
            Debug.Assert(namepath != null && namepath.Count > 0);

            Nmd n = Find(namepath.Dequeue());
            if (namepath.Count == 0) { return n; }
            Nsp nsp = n as Nsp;
            if (nsp == null) { return null; }
            return nsp.FindByNamePath(namepath);
        }

        public override string ToString()
        {
            return Bty.New().Add("{").Nv("Name", Name).Add("}").Add(":").Add(GetType().Name).ToS();
        }

        static public Predicate<T> GetNamePredicate<T>(string name) where T : Nmd
        { return delegate(T v) { return v.Name == name; }; }
    }

    public class Env : Nsp
    {
        public BuiltInTyp BTY;
        public App Ap;
        public TypeLoader TypeLdr = new TypeLoader();
        public int Sequence = 0;
        public string GetTempName() { ++Sequence; return "$" + Sequence.ToString("D6"); }
        public string OutPath = "";

        public List<Typ> RefTyps = new List<Typ>();
        public List<Typ> ArrayTyps = new List<Typ>();
        public List<Typ> GenericTypInstances = new List<Typ>();

        public Env(Token seed)
            : base(seed, null)
        {
            E = this;
            BTY = new BuiltInTyp(this);
        }

        public App NewApp(Token seed)
        {
            Ap = new App(seed, this);
            return BeAMember(Ap);
        }

        public Typ NewRefTyp(Type refType)
        {
            Typ t = new Typ(refType, this);
            RefTyps.Add(t);
            BeAMember(t);
            t.EnsureMembersList.Add(Typ.EnsureMembers);
            return t;
        }

        public Typ FindRefTyp(Type refType)
        {
            EnsureMembers();
            return RefTyps.Find(GetNamePredicate<Typ>(refType.FullName ?? refType.Name));
        }

        public Typ FindOrNewRefType(Type refType)
        {
            return FindRefTyp(refType) ?? NewRefTyp(refType);
        }

        public Typ NewArrayTyp(Typ typ, int dimension)
        {
            Typ t = new Typ(typ, this, dimension);
            ArrayTyps.Add(t);
            BeAMember(t);
            return t;
        }

        public Typ FindArrayTyp(Typ typ, int dimension)
        {
            EnsureMembers();
            return ArrayTyps.Find(delegate(Typ t) { return t.ArrayType == typ && t.Dimension == dimension; });
        }

        public Typ FindOrNewArrayTyp(Typ typ, int dimension)
        {
            return FindArrayTyp(typ, dimension) ?? NewArrayTyp(typ, dimension);
        }

        public Typ NewGenericTypInstance(Typ typ, Typ[] genericTypeParams)
        {
            Typ t = BeAMember(new Typ(typ, this, genericTypeParams));
            GenericTypInstances.Add(t);
            return t;
        }

        public Typ FindGenericTypInstance(Typ typ, Typ[] genericTypeParams)
        {
            EnsureMembers();
            return GenericTypInstances.Find(delegate(Typ t)
                {
                    bool b = t.GenericType == typ
                        && Cty.EqualForAll(t.GenericTypeParams, genericTypeParams)
                        ;
                    return b;
                });
        }

        public Typ FindOrNewGenericTypInstance(Typ typ, Typ[] genericTypeParams)
        {
            return FindGenericTypInstance(typ, genericTypeParams)
                ?? NewGenericTypInstance(typ, genericTypeParams);
        }

        public Nsp NewNsp(string ns)
        {
            return BeAMember(new Nsp(ns, true, this)); ;
        }

        public ActnOvld NewActnOvld(string name)
        {
            if (Members_.Exists(GetNamePredicate<Nmd>(name)))
            { throw new SyntaxError("The name is already defined: " + name); }
            ActnOvld ovl = new ActnOvld(new Token(name), this);
            BeAMember(ovl);
            return ovl;
        }

    }

    public class BuiltInTyp
    {
        public Typ Void;
        public Typ Object;
        public Typ String;
        public Typ Bool;
        public Typ Int;
        public Typ Array;

        public BuiltInTyp(Env e)
        {
            Void = e.FindOrNewRefType(typeof(void));
            Object = e.FindOrNewRefType(typeof(object));
            String = e.FindOrNewRefType(typeof(string));
            Bool = e.FindOrNewRefType(typeof(bool));
            Int = e.FindOrNewRefType(typeof(int));
            Array = e.FindOrNewRefType(typeof(System.Array));
        }
    }

    public class ActnOvld : Nsp
    {
        public List<Actn> Actns = new List<Actn>();

        public ActnOvld(Token seed, Env env)
            : base(seed, env)
        {
        }

        public Actn NewActn(Token seed, List<Variable> params_)
        {
            Actn a = new Actn(seed, params_, E);
            Actns.Add(a);
            return BeAMember(a);
        }

        public Actn NewActn(MethodBase mb)
        {
            Actn a = new Actn(mb, E);
            Actns.Add(a);
            return BeAMember(a);
        }

        public Fctn NewFctn(Token seed, List<Variable> params_, Typ returnTyp)
        {
            Fctn f = new Fctn(seed, params_, returnTyp, E);
            Actns.Add(f);
            return BeAMember(f);
        }

        public Fctn NewFctn(MethodBase mb)
        {
            Fctn f = new Fctn(mb, E);
            Actns.Add(f);
            return BeAMember(f);
        }

        public Actn GetActnOf(Typ ty, Typ[] argtyps, Typ ert, Actn caller)
        {
            List<Actn> cand = CreateCandidateActnList(ty, argtyps);

            //  TODO  remove candidate with accessibility
            Predicate<Actn> canAccess = delegate(Actn callee_) { return AccessControl.CanAccess(ert, caller, ty, callee_); };

            List<Actn> sel = new List<Actn>();
            Actn callee;
            foreach (Actn c in cand)
            {
                if (c.IsSameSignature(argtyps) == false) { continue; }
                sel.Add(c);
            }
            if (sel.Count > 1)
            {
                sel = sel.FindAll(canAccess);
            }
            if (sel.Count > 1)
            {
                throw new SyntaxError("More than 2 candidates methods:" + Name);
            }
            if (sel.Count == 1)
            {
                callee = sel[0];
                if (false == canAccess(callee))
                {
                    throw new SyntaxError("Can not access the function: " + Name);
                }
                return callee;
            }

            sel.Clear();
            foreach (Actn c in cand)
            {
                if (c.IsAssignableSignature(argtyps) == false) { continue; }
                sel.Add(c);
            }
            if (sel.Count == 0)
            {
                throw new SyntaxError("No candidate method for:" + Name);
            }
            if (sel.Count > 1)
            {
                sel = sel.FindAll(canAccess);
            }
            if (sel.Count > 1)
            {
                throw new SyntaxError("More than 2 candidates methods:" + Name);
            }
            callee = sel[0];
            if (false == canAccess(callee))
            {
                throw new SyntaxError("Can not access the function: " + Name);
            }
            return callee;
        }

        public List<Actn> CreateCandidateActnList(Typ ty, Typ[] argtyps)
        {
            List<Actn> candidates = new List<Actn>();

            //  inheritance hierarchy into list
            List<Typ> typs = Cty.ByNext<Typ>(delegate(Typ y) { return y.BaseTyp; }, ty);
            //  go up hierarchy and find same name ActnOvld
            List<ActnOvld> ovlds2 = typs.FindAll(delegate(Typ y) { return null != y.FindActnOvld(this.Name); })
                .ConvertAll<ActnOvld>(delegate(Typ y) { return y.FindActnOvld(this.Name); });

            //  collect same name Actn
            List<Actn> srclst = new List<Actn>(this.Actns);
            foreach (ActnOvld ovld in ovlds2)
            {
                srclst.AddRange(ovld.Actns);
            }

            //  collect Actn that has same signature or assignalbe signature but not in candidate list
            foreach (Actn a in srclst)
            {
                if (a.Params.Count != argtyps.Length) { continue; }
                if (a.IsAssignableSignature(argtyps) == false) { continue; }
                if (candidates.Exists(delegate(Actn a_) { return a_.IsSameSignature(a.Signature); })) { continue; }
                candidates.Add(a);
            }

            return candidates;
        }

        public bool Contains(Typ[] signature)
        {
            Actn a;
            for (int i = 0; i < Members.Count; ++i)
            {
                if (null == (a = (Members[i] as Actn)))
                { continue; }
                if (a.IsSameSignature(signature))
                { return true; }
            }
            return false;
        }

    }

    public interface IInstructionsHolder
    {
        List<IMR> Instructions { get; }
    }

    public class Actn : Nsp, IInstructionsHolder
    {
        static public readonly string EntryPointNameDefault = "Main";
        static public readonly string EntryPointNameImplicit = "'0'";

        public string SpecialName = "";
        public MethodAttributes MthdAttrs;
        public List<Variable> Params = new List<Variable>();
        public List<Variable> Vars = new List<Variable>();

        public Typ[] Signature
        {
            get
            {
                return Params.ConvertAll<Typ>(delegate(Variable v) { return v.Typ; }).ToArray();
            }
        }

        public bool IsConstructor { get { return Nana.IMRs.IMRGenerator.IsAnyCons(Name); } }
        public bool IsEntryPoint { get { return Name == EntryPointNameDefault || Name == EntryPointNameImplicit; } }
        public bool IsInherited = false;
        public bool IsStatic { get { return (MthdAttrs & MethodAttributes.Static) == MethodAttributes.Static; } }
        public bool IsInstance { get { return (MthdAttrs & MethodAttributes.Static) != MethodAttributes.Static; } }
        public bool IsVirtual { get { return (MthdAttrs & MethodAttributes.Virtual) == MethodAttributes.Virtual; } }
        public bool Inherited = false;

        public bool IsAssignableSignature(Typ[] callertyps)
        {
            if (callertyps == null) { return false; }
            Typ[] sign =  Signature;
            if (sign.Length != callertyps.Length) { return false; }
            for (int i = 0; i < sign.Length; ++i)
            { if (sign[i].IsAssignableFrom(callertyps[i]) == false) { return false; } }
            return true;
        }

        public bool IsSameSignature(Typ[] signature)
        {
            return Cty.EqualForAll(Signature, signature);
        }

        public List<IExecutable> Exes = new List<IExecutable>();

        public List<IMR> _Intermediates = new List<IMR>();
        public List<IMR> Instructions
        {
            get { return _Intermediates; }
            set { _Intermediates = value; ; }
        }

        public Actn(Token seed, List<Variable> params_, Env env)
            : base(seed, env)
        {
            if (params_ == null) { return; }
            params_.ForEach(delegate(Variable v)
            {
                Params.Add(v);
                BeAMember(v);
            });
        }

        public MethodBase Mb;

        public Actn(MethodBase mb, Env env)
            : base(new Token(mb.Name), env)
        {
            Mb = mb;
            new List<ParameterInfo>(mb.GetParameters()).ForEach(delegate(ParameterInfo p)
                    { NewParam(p.Name, E.FindOrNewRefType(p.ParameterType)); });
            IsReferencing = true;
            MthdAttrs = mb.Attributes;
            if (MethodAttributes.SpecialName == (mb.Attributes & MethodAttributes.SpecialName))
            { SpecialName = mb.Name; }
        }

        public Variable NewThis(Typ typ)
        {
            if (typ == null) { throw new NotImplementedException("Cannot define this in here"); }
            Variable v = new Variable("this", typ, Variable.VariableKind.This);
            Vars.Add(v);
            return BeAMember(v);
        }

        public Variable NewParam(string name, Typ typ)
        {
            Variable v = new Variable(name, typ, Variable.VariableKind.Param);
            Params.Add(v);
            return BeAMember(v);
        }

        virtual public Variable NewVar(string name, Typ typ)
        {
            Variable v = new Variable(name, typ, Variable.VariableKind.Local);
            Vars.Add(v);
            return BeAMember(v);
        }

        public Variable FindVar(string name)
        {
            EnsureMembers();
            return Vars.Find(GetNamePredicate<Variable>(name));
        }

        static public string GenSignature(MethodBase mb)
        {
            StringBuilder b = new StringBuilder();
            b.Append(mb.Name);
            if (mb.IsGenericMethod)
            {
                b.Append(" [")
                    .Append(string.Join(" "
                    , new List<Type>(mb.GetGenericArguments())
                        .ConvertAll<string>(delegate(Type t)
                        { return GetNameForSignature(t); })
                        .ToArray()
                        )
                    )
                    .Append("]");
            }

            new List<ParameterInfo>(mb.GetParameters())
                .ConvertAll<StringBuilder>(delegate(ParameterInfo p)
                { return b.Append(" ").Append(GetNameForSignature(p.ParameterType)); });
            return b.ToString();
        }

        static public string GetNameForSignature(Type t)
        {
            if (t.IsGenericParameter) { return t.Name; }
            
            string name = (string.IsNullOrEmpty(t.Namespace) ? "" : t.Namespace + ".")
                    + t.Name;
            bool gtd = t.IsGenericTypeDefinition;
            if (t.IsGenericType && gtd) { return name; }
            if (t.IsGenericType && gtd == false)
            { return name + "[" + GetNameForSignature(t.GetGenericArguments()) + "]"; }
            return name;
        }

        static public string GetNameForSignature(Type[] t)
        {
            if (t == null) { return ""; }
            return string.Join(" "
                , new List<Type>(t)
                .ConvertAll<string>(delegate(Type item) { return GetNameForSignature(item); })
                .ToArray());
        }


    }

    public class Fctn : Actn, ITyped
    {
        public Typ Typ_;
        public Typ Typ { [DebuggerNonUserCode] get { return Typ_; } }

        public Fctn(Token seed, List<Variable> params_, Typ returnTyp, Env env)
            : base(seed, params_, env)
        {
            Typ_ = returnTyp;
        }

        public Fctn(MethodBase mb, Env env)
            : base(mb, env)
        {
            Type t = mb.IsConstructor && (mb.IsStatic == false)
                ? mb.DeclaringType
                : (mb as MethodInfo).ReturnType;
            Typ_ = env.FindOrNewRefType(t);
        }
    }

    public class Typ : Actn
    {
        public Typ BaseTyp;
        public TypeAttributes TypAttributes;
        public string AssemblyName;
        public string _FullName;
        public bool IsValueType;

        public bool IsVector = false;
        public bool IsArray = false;
        public Typ ArrayType;
        public int Dimension;
        public bool IsVectorOrArray;

        public bool IsGeneric = false;
        public bool IsGenericInstance = false;

        public List<ActnOvld> Ovlds = new List<ActnOvld>();

        public List<Nmd> DebuggerDisplayMembers { get { return Members_; } }

        public Typ(Token seed, Env env, App app)
            : base(seed, null, env)
        {
            if (app != null) { AssemblyName = app.AssemblyName; }
            _FullName = Name;

            TypAttributes
                = TypeAttributes.Class
                | TypeAttributes.Public
                | TypeAttributes.AutoClass
                | TypeAttributes.AnsiClass
                ;

            IsValueType = false;
        }

        public void SetBaseTyp(Typ baseTyp)
        {
            BaseTyp = baseTyp;
        }

        public ActnOvld FindOrNewActnOvld(string name)
        {
            return FindActnOvld(name) ?? NewActnOvld(name);
        }

        public ActnOvld FindActnOvld(string name)
        {
            EnsureMembers();
            return Ovlds.Find(GetNamePredicate<ActnOvld>(name));
        }

        public ActnOvld NewActnOvld(string name)
        {
            if (Members_.Exists(GetNamePredicate<Nmd>(name)))
            { throw new SyntaxError("The name is already defined: " + name); }
            ActnOvld ovl = new ActnOvld(new Token(name), E);
            Ovlds.Add(ovl);
            BeAMember(ovl);
            return ovl;
        }

        public Type RefType = null;

        public Typ(Type refType, Env env)
            : base(new Token(refType.FullName ?? refType.Name), null, env)
        {
            RefType = refType;
            IsValueType = refType.IsValueType;
            IsReferencing = true;
            _FullName = refType.FullName;
            AssemblyName = refType.Assembly.GetName().Name;
        }

        public GenericArgument[] GetGenericArguments()
        {
            if (IsReferencing)
            {
                return GetGenericArgumentsOfReferencing();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public GenericArgument[] GetGenericArgumentsOfReferencing()
        {
            List<GenericArgument> ls = new List<GenericArgument>();
            foreach (Type t in RefType.GetGenericArguments())
            {
                ls.Add(new GenericArgument(t));
            }
            return ls.ToArray();
        }

        public Typ(Typ typ, Env env, int dimension)
            : base(new Token(typ._FullName + "[" + dimension + "]"), null, env)
        {
            Dimension = dimension;
            IsVector = dimension == 1;
            IsArray = dimension > 1;
            IsVectorOrArray = IsVector || IsArray;
            ArrayType = typ;
            SetBaseTyp(env.BTY.Array);
        }

        public Typ GenericType = null;
        public Typ[] GenericTypeParams = null;
        public Dictionary<string, Typ> GenericDic = null;

        public Typ(Typ genericTyp, Env env, Typ[] genericTypeParams)
            : base(new Token(genericTyp.Name), null, env)
        {
            GenericType = genericTyp;
            GenericTypeParams = genericTypeParams;

            IsGeneric = true;
            IsGenericInstance = true;
            AssemblyName = genericTyp.AssemblyName;
            _FullName = genericTyp._FullName;
            IsValueType = genericTyp.IsValueType;
            RefType = genericTyp.RefType;
            IsReferencing = true;

            Name = genericTyp._FullName + "<" + string.Join(","
            , new List<Typ>(genericTypeParams).ConvertAll<string>(
            delegate(Typ t_) { return t_._FullName; })
            .ToArray()
            ) + ">";

            //  map generic arguments to actual types
            GenericArgument[] gargs = genericTyp.GetGenericArguments();
            Debug.Assert(gargs.Length == genericTypeParams.Length);
            GenericDic = new Dictionary<string, Typ>();
            for (int i = 0; i < gargs.Length; ++i)
            {
                GenericDic[gargs[i].Name] = genericTypeParams[i];
            }

            EnsureMembersList.Add(EnsureGenericMembersN);
        }

        public Actn NewActn(MethodBase mb)
        {
            ActnOvld ovld = FindOrNewActnOvld(mb.Name);
            if (mb is MethodInfo)
            {
                Type returnType = (mb as MethodInfo).ReturnType;
                if (returnType
                    == typeof(void))    /**/ { return ovld.NewActn(mb); }
                else                    /**/ { return ovld.NewFctn(mb); }
            }
            if (mb is ConstructorInfo)
            {
                if (mb.IsStatic)        /**/ { return ovld.NewActn(mb); }
                else                    /**/ { return ovld.NewFctn(mb); }
            }
            return null;
        }

        public Prop NewProp(PropertyInfo p)
        {
            return BeAMember<Prop>(new Prop(p, E));
        }

        public bool IsAssignableFrom(Typ y)
        {
            Typ self = this;
            do
            {
                if (self == y) { return true; }
                if (self.IsReferencing && y.IsReferencing)
                {
                    return self.RefType.IsAssignableFrom(y.RefType);
                }
                self = self.BaseTyp;
            } while (self != null);
            return false;
        }

        static public void EnsureMembers(Nsp self)
        {
            if (false == self is Typ) { return; }
            EnsureMembers(self as Typ);
        }

        static public void EnsureMembers(Typ self)
        {
            if (self.RefType == null) { return; }
            Type refType = self.RefType;
            BindingFlags flags_ = BindingFlags.Instance
                                | BindingFlags.Static
                                | BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.FlattenHierarchy
                                ;
            List<MethodBase> ms = new List<MethodBase>();
            
            ms.AddRange(refType.GetConstructors(flags_));
            ms.AddRange(refType.GetMethods(flags_));
            ms.ConvertAll<Actn>(self.NewActn);

            new List<PropertyInfo>(refType.GetProperties(flags_))
                .ConvertAll<Prop>(self.NewProp);
        }

        static public void EnsureGenericMembersN(Nsp self)
        {
            if (false == self is Typ) { return; }
            EnsureGenericMembers(self as Typ);
        }

        static public void EnsureGenericMembers(Typ self)
        {
            Debug.Assert(self != null && self.IsGeneric && self.IsGenericInstance && self.GenericType != null);

            Typ gt = self.GenericType;
            gt.EnsureMembers();
            foreach (ActnOvld gao in gt.Ovlds)
            {
                ActnOvld sao = self.FindOrNewActnOvld(gao.Name);
                List<Variable> svs = new List<Variable>();
                foreach (Actn ga in gao.Actns)
                {
                    Variable sv; Typ st; int genericIndex;
                    svs.Clear();
                    foreach (Variable gv in ga.Params)
                    {
                        if (gv.VarKind != Variable.VariableKind.Param)
                        {
                            continue;
                        }
                        st = TransGenericType(self, gv.Typ);
                        sv = new Variable(gv.Name, st, Variable.VariableKind.Param);
                        genericIndex = self.GenericDic.ContainsKey(gv.Typ.Name)
                            ? Array.IndexOf(self.GenericTypeParams, self.GenericDic[gv.Typ.Name])
                            : -1;
                        sv.GenericIndex = genericIndex;
                        if (genericIndex >= 0)
                        {
                            sv.VarKind = Variable.VariableKind.ParamGeneric;
                        }
                        svs.Add(sv);
                    }

                    if (ga is Fctn)
                    {
                        Fctn gf = ga as Fctn;
                        Typ rettyp = TransGenericType(self, gf.Typ);
                        sao.NewFctn(new Token(ga.Name), svs, rettyp);
                    }
                    else
                    {
                        sao.NewActn(new Token(ga.Name), svs);
                    }
                }
            }
        }

        static public Typ TransGenericType(Typ instance, Typ generic)
        {
            Debug.Assert(instance != null && generic != null);

            if (instance.GenericType == generic)
            { return instance; }

            if (instance.GenericDic.ContainsKey(generic.Name))
            { return instance.GenericDic[generic.Name]; }

            return generic;
        }

        public bool IsReferencingOf(Type t)
        {
            return RefType != null && RefType == t;
        }

        public Nmd FindMemeber(string name)
        {
            Typ typ = this;
            Nmd mem = typ.Find(name);
            while (typ != null && mem == null)
            {
                if (typ.IsReferencing && typ.RefType == typeof(object))
                { break; }
                typ = typ.BaseTyp;
                mem = typ.Find(name);
            }
            return mem;
        }

        public bool IsSubclassOf(Typ t)
        {
            Typ baseType = this;
            do
            {
                if (baseType == t) { return true; }
                baseType = baseType.BaseTyp;
            } while (baseType != null);

            return false;
        }
    }

    public class App : Typ
    {
        public App(Token seed, Env env)
            : base(seed, env, null)
        {
            Name = Path.GetFileName(E.OutPath);
            AssemblyName = Path.GetFileNameWithoutExtension(E.OutPath);
        }

        public Nsp NewNsp(Token seed)
        {
            Nsp n = new Nsp(seed, E);
            n.Name = seed.ValueImplicit;
            BeAMember(n);
            return n;
        }

        public Typ NewTyp(Token seed)
        {
            return BeAMember(new Typ(seed, E, this));
        }

        override public Variable NewVar(string name, Typ typ)
        {
            Variable v = new Variable(name, typ, Variable.VariableKind.StaticField);
            Vars.Add(v);
            return BeAMember(v);
        }

    }

    public class Prop : ActnOvld, ITyped
    {
        public PropertyInfo P;
        public Actn Setter;
        public Fctn Getter;
        public Typ Typ_;

        public Prop(PropertyInfo p, Env env)
            : base(new Token(p.Name), env)
        {
            P = p;
            MethodInfo m;
            m = p.GetGetMethod(/* nonPublic:*/ true);
            Getter = null;
            if (m != null)
            {
                Getter = BeAMember<Fctn>(new Fctn(m, env));
            }
            Setter = null;
            m = p.GetSetMethod(/* nonPublic:*/ true);
            if (m != null)
            {
                Setter = BeAMember<Actn>(new Actn(m, env));
            }
            Typ_ = env.FindOrNewRefType(p.PropertyType);
        }

        public Typ Typ { get { return Typ_; } }
    }

    //  Return Determinacy State (It's doubtful to make sense in English.)
    //  ここで必ずリターンはされていることが決定しているかの状態
    public interface IReturnDeterminacyState
    {
        bool RDS { get;}
    }

    public interface IExecutable
    {
        void Exec(IMRGenerator gen);
    }

    public class DoNothing : IExecutable
    {
        public void Exec(IMRGenerator gen)
        {
            //  do nothing
        }
    }

    public interface IValuable : IExecutable, ITyped
    {
        void Give(IMRGenerator gen);
        void Addr(IMRGenerator gen);
    }

    public class Literal :IValuable
    {
        public object Value;
        public Typ Typ_;
        public TmpVarGenerator TmpVarGen;

        public Literal(object value, Typ typ)
            : this(value, typ, null)
        {
        }

        public Literal(object value, Typ typ, TmpVarGenerator tmpVarGen)
        {
            Value = value;
            Typ_ = typ;
            TmpVarGen = tmpVarGen;
        }

        public void Give(IMRGenerator gen)
        {
            gen.LoadLiteral(this);
        }

        public void Addr(IMRGenerator gen)
        {
            Give(gen);
        }

        public void Exec(IMRGenerator gen)
        {
            Give(gen);
            gen.Pop();
        }

        public Typ Typ { get { return Typ_; } }
    }

    public interface IAssignable
    {
        void Take(IMRGenerator gen);
    }

    public interface IVariable : IAssignable, IValuable
    {
    }

    public class Ret : IExecutable, IReturnDeterminacyState
    {
        public void Exec(IMRGenerator gen)
        {
            gen.Ret();
        }

        public bool RDS { get { return true; } }
    }

    public class GenericArgument : Nmd
    {
        public Type RefType = null;

        public GenericArgument(Type refType)
        {
            RefType = refType;
            Name_ = refType.Name;
        }
    }

    public class Variable : Nmd, ITyped, IVariable
    {
        public enum VariableKind
        {
            Param, This, Local, StaticField, InstanceField,
            Vector, Array, ParamGeneric
        }
        public VariableKind VarKind;
        public int GenericIndex = -1;

        public Typ Typ_;
        public Typ Typ { [DebuggerNonUserCode] get { return Typ_; } }

        public Variable(string name, Typ typ, VariableKind varKind)
            : base()
        {
            Name_ = name;
            Typ_ = typ;
            VarKind = varKind;
        }

        public void Exec(IMRGenerator gen) { }

        public void Give(IMRGenerator gen)
        {
            gen.LoadVariable(this);
        }

        public void Take(IMRGenerator gen)
        {
            gen.StoreVariable(this);
        }

        public void Addr(IMRGenerator gen)
        {
            if (Typ.IsValueType) { gen.LoadAVariable(this); }
            else { gen.LoadVariable(this); }
        }

        public override string ToString()
        {
            Func<string, object, string> nv = Sty.Nv;
            return Sty.Curly(Sty.Csv(nv("Name", Name), nv("Typ", Typ), nv("VarKind", VarKind))) + ":" + GetType().Name;
        }
    }

    public class Assign : IVariable
    {
        public Typ Typ { [DebuggerNonUserCode]get { return TakeVar.Typ; } }
        public IValuable GiveVal;
        public IVariable TakeVar;

        public Assign(IValuable give, IVariable take)
        {
            GiveVal = give;
            TakeVar = take;
        }

        public void Take(IMRGenerator gen)
        {
            Exec(gen);
            TakeVar.Take(gen);
        }

        public void Give(IMRGenerator gen)
        {
            Exec(gen);
            TakeVar.Give(gen);
        }

        public void Addr(IMRGenerator gen)
        {
            Give(gen);
        }

        public void Exec(IMRGenerator gen)
        {
            GiveVal.Give(gen);
            TakeVar.Take(gen);
        }

    }

    public class ReturnValue : IExecutable, IReturnDeterminacyState
    {
        public IValuable GiveVal = null;

        public void Exec(IMRGenerator gen)
        {
            GiveVal.Give(gen);
            gen.Ret();
        }

        public bool RDS { get { return true; } }
    }

    public class CallAction : IExecutable
    {
        public Typ CalleeTy;
        public Actn Callee;
        public IValuable Instance;
        public IValuable[] Args;
        public bool IsNewObj;

        public CallAction(Typ calleety, Actn callee, IValuable instance, IValuable[] args, bool isNewObj)
        {
            CalleeTy = calleety;
            Callee = callee;
            Instance = instance;
            Args = args;
            IsNewObj = isNewObj;
        }

        public void LoadInstance(IMRGenerator gen)
        {
            if (Instance != null)
            {
                Instance.Addr(gen);
                if (Instance is Literal && Instance.Typ.IsValueType)
                {
                    Variable v = (Instance as Literal).TmpVarGen.Substitute(Instance.Typ, gen);
                    v.Addr(gen);
                }
            }
        }

        public void LoadArgs(IMRGenerator gen)
        {
            foreach (IValuable v in Args) { v.Give(gen); }
        }

        virtual public void Exec(IMRGenerator gen)
        {
            LoadInstance(gen);
            LoadArgs(gen);
            gen.CallAction(CalleeTy, Callee);
        }

        public override string ToString()
        {
            Func<string, object, string> nv = Sty.Nv;
            return Sty.Curly(Sty.Csv(nv("Instance", Instance), nv("Callee", Callee), nv("Args", Sty.Curly(Sty.Csv(Args))), nv("IsNewObj", IsNewObj))) + ":" + GetType().Name;
        }

    }

    public class CallFunction : CallAction, ITyped, IValuable
    {
        public Fctn CalleeFctn { get { return Callee as Fctn; } }

        public CallFunction(Typ calleety, Fctn callee, IValuable instance, IValuable[] args, bool isNewObj)
            : base(calleety, callee, instance, args, isNewObj)
        {  }

        public Typ Typ { get { return CalleeFctn.Typ; } }

        public void Give(IMRGenerator gen)
        {
            LoadInstance(gen);
            LoadArgs(gen);
            if (IsNewObj) { gen.NewObject(CalleeTy, Callee); }
            else { gen.CallAction(CalleeTy, Callee); }
        }

        public void Addr(IMRGenerator gen)
        {
            Give(gen);
        }

        override public void Exec(IMRGenerator gen)
        {
            Give(gen);
            gen.Pop();
        }

    }

    public class CallPropInfo : IValuable
    {
        public Typ CalleeTy;
        public Typ Typ { [DebuggerNonUserCode] get { return Prop.Typ; } }
        public Prop Prop;
        public IValuable Instance;

        public CallPropInfo(Typ calleety, Prop prop, IValuable instance)
        {
            CalleeTy = calleety;
            Prop = prop;
            Instance = instance;
        }

        public void Give(IMRGenerator gen)
        {
            if (Prop.Getter == null)
            { throw new SyntaxError("Cannot get value from the property"); }
            CallFunction cf = new CallFunction(CalleeTy, Prop.Getter, Instance, new IValuable[0], /*isNewObj:*/ false);
            cf.Give(gen);
        }

        public void Addr(IMRGenerator gen)
        {
            Give(gen);
        }

        public void Exec(IMRGenerator gen)
        {
            Give(gen);
            gen.Pop();
        }

    }

    public class CalcInfo : IValuable
    {
        public string Sign;
        public IValuable Lv;
        public IValuable Rv;
        public Typ Typ_;

        public CalcInfo(string sign, IValuable lv, IValuable rv, Typ typ)
        {
            Sign = sign;
            Lv = lv;
            Rv = rv;
            Typ_ = typ;
        }

        public void Give(IMRGenerator gen)
        {
            Lv.Give(gen);
            Rv.Give(gen);
            gen.Ope(Sign, Typ_);
        }

        public void Addr(IMRGenerator gen)
        {
            Give(gen);
        }

        public void Exec(IMRGenerator gen)
        {
            Give(gen);
            gen.Pop();
        }

        public Typ Typ { get { return Typ_; } }
    }

    public class IfInfo : IExecutable, IReturnDeterminacyState
    {
        public class Component
        {
            public IValuable Condition;
            public IExecutable[] Lines;
        }

        public string Fix;
        public Component IfThen;
        public Component[] ElifThen;
        public IExecutable[] Else;
        public bool RDS_;
        public bool RDS { get { return RDS_; } }

        public IfInfo(string uniqFix, Component ifThen, Component[] elifThen, IExecutable[] else_, bool rds)
        {
            Fix = uniqFix;
            IfThen = ifThen;
            ElifThen = elifThen;
            Else = else_;
            RDS_ = rds;
        }

        public void Exec(IMRGenerator gen)
        {
            string endlbl;
            Stack<string> elselbls;
            Action gotoend, brelse, putelse;

            endlbl = "endif" + Fix;
            elselbls = new Stack<string>();
            elselbls.Push(endlbl);
            if (Else != null) { elselbls.Push("else" + Fix); };
            for (int i = ElifThen.Length; i > 0; i--)
            { elselbls.Push("elif" + Fix + "_" + i.ToString()); }

            gotoend = delegate() { gen.Br("endif" + Fix); };
            putelse = delegate() { gen.PutLabel(elselbls.Pop()); };
            brelse = delegate() { gen.BrFalse(elselbls.Peek()); };

            IfThen.Condition.Give(gen);
            brelse();
            foreach (IExecutable s in IfThen.Lines) { s.Exec(gen); }
            gotoend();

            foreach (Component elt_ in ElifThen)
            {
                putelse();
                elt_.Condition.Give(gen);
                brelse();
                foreach (IExecutable l_ in elt_.Lines) { l_.Exec(gen); }
                gotoend();
            }

            if (Else != null)
            {
                putelse();
                foreach (IExecutable l_ in Else) { l_.Exec(gen); }
            }

            gen.PutLabel(endlbl);
        }

    }

    public class WhileInfo : IExecutable
    {
        public Literal Dolbl;
        public Literal Endlbl;
        public IValuable Condition;
        public IExecutable[] Lines;

        public WhileInfo(Literal dolbl, Literal endlbl, IValuable condition, IExecutable[] lines)
        {
            Dolbl = dolbl;
            Endlbl = endlbl;
            Condition = condition;
            Lines = lines;
        }

        public void Exec(IMRGenerator gen)
        {
            gen.PutLabel(Dolbl.Value.ToString());
            Condition.Give(gen);
            gen.BrFalse(Endlbl.Value.ToString());
            foreach (IExecutable u_ in Lines) { u_.Exec(gen); }
            gen.Br(Dolbl.Value.ToString());
            gen.PutLabel(Endlbl.Value.ToString());
        }

    }

    public class BranchInfo : IExecutable
    {
        public Literal Label;
        public BranchInfo(Literal label)
        {
            Label = label;
        }

        public void Exec(IMRGenerator gen)
        {
            gen.Br(Label.Value.ToString());
        }
    }

    public class ArrayInstatiation : IValuable
    {
        public IValuable[] Lens;
        public TmpVarGenerator TmpVarGen;
        // prepare for over twice referenced instatication
        public IMR PlaceHolder;
        public Variable TmpVar;
        public Typ Typ_;
        public Typ Typ { [DebuggerNonUserCode]get { return Typ_; } }

        public ArrayInstatiation(Typ typ, IValuable[] lens, TmpVarGenerator tmpVarGen)
        {
            Typ_ = typ;
            Lens = lens;
            TmpVarGen = tmpVarGen;
        }

        public void Exec(IMRGenerator gen)
        {
            Give(gen);
            gen.Pop();
        }
        public void Give(IMRGenerator imrs)
        {
            if (PlaceHolder == null) { Give1st(imrs); }
            else { GiveSubsequent(imrs); }
        }

        public void Addr(IMRGenerator gen)
        {
            Give(gen);
        }

        public void Give1st(IMRGenerator gen)
        {
            foreach (IValuable v in Lens)
            { v.Give(gen); }

            PlaceHolder = gen.NewArray(Typ);
        }

        public void GiveSubsequent(IMRGenerator gen)
        {
            if (TmpVar == null)
            { TmpVar = TmpVarGen.Insert(Typ, gen, PlaceHolder); }
            TmpVar.Give(gen);
        }

    }

    public class TmpVarGenerator
    {
        public Func<string> GetTempName;
        public Func<string, Typ, Variable> DeclareVariable;

        public TmpVarGenerator(Func<string> getTempName, Func<string, Typ, Variable> declareVariable)
        {
            GetTempName = getTempName;
            DeclareVariable = declareVariable;
        }

        public Variable Generate(Typ typ, IMRGenerator gen)
        {
            return DeclareVariable(GetTempName(), typ);
        }

        public Variable Substitute(Typ typ, IMRGenerator gen)
        {
            Variable v = Generate(typ, gen);
            v.Take(gen);
            return v;
        }

        public Variable Insert(Typ typ, IMRGenerator gen, IMR PlaceHolder)
        {
            Debug.Assert(PlaceHolder != null);
            Debug.Assert(GetTempName != null);
            Debug.Assert(DeclareVariable != null);

            IMRGenerator tmpgen = new IMRGenerator();
            Variable v = Substitute(typ, tmpgen);
            v.Give(tmpgen);

            int idx = gen.IndexOf(PlaceHolder);
            Debug.Assert(idx >= 0);
            idx += 1;
            if (idx < gen.Count) { gen.InsertRange(idx, tmpgen); }
            else { gen.AddRange(tmpgen); }

            return v;
        }
    }

    public class ArrayAccessInfo : IValuable
    {
        public IValuable Val;
        public IValuable[] Indices;

        public Typ Typ_;
        public Typ Typ { [DebuggerNonUserCode] get { return Typ_; } }

        public ArrayAccessInfo(IValuable val, IValuable[] indices)
        {
            Typ_ = val.Typ.ArrayType;
            Val = val;
            Indices = indices;
        }

        public void Give(IMRGenerator gen)
        {
            Val.Give(gen);
            Array.ForEach<IValuable>(Indices,
                delegate(IValuable v_) { v_.Give(gen); });

            gen.LdArrayElement(Val.Typ);
        }
        public void Addr(IMRGenerator gen)
        {
            Give(gen);
        }
        public void Exec(IMRGenerator gen)
        {
            Give(gen);
            gen.Pop();
        }
        public override string ToString()
        {
            return Val.ToString() + ":" + GetType();
        }
    }

    public class ArraySetInfo : IValuable
    {
        public ArrayAccessInfo ArrayAccess;
        public IValuable GiveVal;

        public Typ Typ_;
        public Typ Typ { [DebuggerNonUserCode] get { return Typ_; } }

        public ArraySetInfo(ArrayAccessInfo arrayAccess, IValuable giveVal)
        {
            Debug.Assert(arrayAccess != null && arrayAccess.Val != null && arrayAccess.Val.Typ != null);

            Typ_ = arrayAccess.Typ;
            ArrayAccess = arrayAccess;
            GiveVal = giveVal;
        }

        public void Give(IMRGenerator gen)
        {
            Exec(gen);
            ArrayAccess.Give(gen);
        }

        public void Addr(IMRGenerator gen)
        {
            Give(gen);
        }

        public void Exec(IMRGenerator gen)
        {
            ArrayAccess.Val.Give(gen);
            Array.ForEach<IValuable>(ArrayAccess.Indices,
                delegate(IValuable v_) { v_.Give(gen); });
            GiveVal.Give(gen);

            Typ t = ArrayAccess.Val.Typ;
            Typ t2;
         
            if (t.IsVector)
            {
                t2 = Typ_;
            }
            else if (t.IsArray)
            {
                t2 = ArrayAccess.Val.Typ;
            }
            else
            {
                throw new NotSupportedException();
            }
            gen.StArrayElement(t, t2);
        }
    }

    public class AccessControl
    {
        [Flags]
        public enum Modifier
        {
            None            /**/ = 0x0000,
            AssemblyAnd     /**/ = 0x0001,
            AssemblyOr      /**/ = 0x0002,
            AssemblyAny     /**/ = 0x0004,

            Private         /**/ = 0x0008 | AssemblyAny,
            Family          /**/ = 0x0010 | AssemblyAny,
            Public          /**/ = 0x0020 | AssemblyAny,

            FamAndAssem     /**/ = Family | AssemblyAnd,
            Assembly        /**/ = AssemblyAnd,
            FamOrAssem      /**/ = Family | AssemblyOr
        }

        [Flags]
        public enum Relation
        {
            None            /**/ = 0x0000,
            Nested          /**/ = 0x0001,
            InheritedType   /**/ = 0x0002,
            IdentType       /**/ = 0x0004
        }

        static public bool Contains(MethodAttributes flag, MethodAttributes isIn)
        {
            return (isIn & flag) == flag;
        }

        static public bool Contains(Modifier flag, Modifier isIn)
        {
            return (isIn & flag) == flag;
        }

        static public bool Contains(Relation flag, Relation isIn)
        {
            return (isIn & flag) == flag;
        }

        static public bool CanAccessByRelation(Relation r, Modifier m)
        {
            if (Contains(Modifier.Private, m))
            {
                return Contains(Relation.Nested, r) || Contains(Relation.IdentType, r);
            }
            else if (Contains(Modifier.Family, m))
            {
                return Contains(Relation.Nested, r) || Contains(Relation.IdentType, r) || Contains(Relation.InheritedType, r);
            }
            else
            {
                // Modifier.Public
                return true;
            }
        }

        static public bool CanAccessByAssembly(bool identAssembly, Modifier m)
        {
            if (Contains(Modifier.AssemblyAnd, m))
            {
                return identAssembly == true;
            }
            else if (Contains(Modifier.AssemblyOr, m))
            {
                return identAssembly == true;
            }
            else
            {
                // Modifier.AssemblyAny
                return true;
            }
        }

        static public Modifier FromMethodAttributes(MethodAttributes a)
        {
            if (Contains(MethodAttributes.Public        /*0x06*/, a)) return Modifier.Public;
            if (Contains(MethodAttributes.FamORAssem    /*0x05*/, a)) return Modifier.FamOrAssem;
            if (Contains(MethodAttributes.Family        /*0x04*/, a)) return Modifier.Family;
            if (Contains(MethodAttributes.Assembly      /*0x03*/, a)) return Modifier.Assembly;
            if (Contains(MethodAttributes.FamANDAssem   /*0x02*/, a)) return Modifier.FamAndAssem;
            if (Contains(MethodAttributes.Private       /*0x01*/, a)) return Modifier.Private;
            return Modifier.None;
        }

        static public bool CanAccess(bool identAssembly, Relation r, Modifier m)
        {
            if (Contains(Modifier.AssemblyAnd, m))
            {
                return CanAccessByAssembly(identAssembly, m) && CanAccessByRelation(r, m);
            }
            else if (Contains(Modifier.AssemblyOr, m))
            {
                return CanAccessByAssembly(identAssembly, m) || CanAccessByRelation(r, m);
            }
            else
            {
                // Modifier.AssemblyAny
                return CanAccessByRelation(r, m);
            }
        }

        static public bool CanAccess(Typ ert, Actn er, Typ eet, Actn ee)
        {
            bool identAssembly;
            identAssembly = ert.AssemblyName == eet.AssemblyName;

            Relation r = Relation.None;
            if (ert == eet)
            {
                r = Relation.IdentType;
            }
            else if (ert != null && eet != null && ert.IsSubclassOf(eet))
            {
                r = Relation.InheritedType;
            }
            //TODO implemet condition for Relation.Nested

            Modifier m;
            m = FromMethodAttributes(ee.MthdAttrs);

            return CanAccess(identAssembly, r, m);
        }

        public enum Req { Any, Instance, Type }

        static public readonly Req[,] IdentTypeSITbl = new Req[,] {
            //  <callee>
            //  satatic     instance            <caller>
            {   Req.Any,    Req.Instance },  // static
            {   Req.Any,    Req.Any      }   // instance
        };

        static public readonly Req[,] AnotherTypeSITbl = new Req[,] {
            //  <callee>
            //  satatic     instance            <caller>
            {   Req.Type,   Req.Instance },  // static
            {   Req.Type,   Req.Instance }   // instance
        };

    }

}
