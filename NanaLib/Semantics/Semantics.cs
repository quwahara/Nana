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
    public class AnId
    {
        public Token Seed;
        [DebuggerNonUserCode]
        public AnId(Token seed) { Seed = seed; }
        public override string ToString()
        {
            return Seed.Value + ":" + typeof(AnId).Name;
        }
    }

    public class Member
    {
        public INmd Value;
        public IValuable Instance;

        public Member(Token seed, INmd value, IValuable instance)
        {
            Value = value;
            Instance = instance;
        }
    }

    public interface INmd
    {
        string Name { get; set; }
        Nsp Family { get; set; }
        INmd Clone();
    }

    public interface ITyped
    {
        Typ Typ { get; }
    }

    public class Nsp : INmd
    {
        public Token Seed;
        public string Name_;
        public string Name { get { return Name_; } set { Name_ = value; } }
        public Nsp Family_;
        public Nsp Family { get { return Family_; } set { Family_ = value; } }

        public List<INmd> Members_ = new List<INmd>();

        public List<Action<Nsp>> EnsureMembersList = new List<Action<Nsp>>();

        public bool IsReferencing_ = false;

        public Nsp(string name, Nsp family, bool isReferencing)
        {
            Name_ = name;
            Family = family;
            IsReferencing_ = isReferencing;
        }

        public Nsp(string name, Nsp family)
            : this(name, family, false)
        {
        }

        public Nsp(Token seed, Nsp family)
            : this(seed.Value, family)
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

        public List<INmd> Members
        {
            get
            {
                EnsureMembers();
                return Members_;
            }
        }

        public bool ContainsKey(string name)
        { return Members.Exists(GetNamePredicate<INmd>(name)); }

        public T BeAMember<T>(T member) where T : INmd
        {
            if (member.Name == null) { return default(T); }
            Members.Add(member);
            member.Family = this;
            return member;
        }

        public T FindInTypeOf<T>()
        {
            return (T)Members.Find(delegate(INmd m) { return m.GetType() == typeof(T); });
        }

        public List<T> FindAllTypeOf<T>() where T : class, INmd
        {
            return Members.FindAll(delegate(INmd v) { return v.GetType() == typeof(T); })
            .ConvertAll<T>(delegate(INmd v) { return v as T; });
        }

        public List<T> FindAllTypeIs<T>() where T : class, INmd
        {
            return Members.FindAll(delegate(INmd v) { return v is T; })
            .ConvertAll<T>(delegate(INmd v) { return v as T; });
        }

        public T FindUpTypeOf<T>() where T : Nsp
        {
            return Family_ == null ? null : Family_.GetType() == typeof(T) ? Family_ as T : Family_.FindUpTypeOf<T>();
        }

        public T FindUpTypeIs<T>() where T : Nsp
        {
            return Family_ == null ? null : Family_ is T ? Family_ as T : Family_.FindUpTypeIs<T>();
        }

        public List<T> FindDownAllTypeOf<T>() where T : Nsp
        {
            List<T> founds = new List<T>();
            founds.AddRange(FindAllTypeOf<T>());
            Members.ForEach(delegate(INmd n)
            { if (n is Nsp) { founds.AddRange((n as Nsp).FindDownAllTypeOf<T>()); } });
            return founds;
        }

        public List<T> FindDownAllTypeIs<T>() where T : Nsp
        {
            List<T> founds = new List<T>();
            founds.AddRange(FindAllTypeIs<T>());
            Members.ForEach(delegate(INmd n)
            { if (n is Nsp) { founds.AddRange((n as Nsp).FindDownAllTypeIs<T>()); } });
            return founds;
        }

        public List<INmd> FindDownAll(Predicate<INmd> pred)
        {
            List<INmd> founds = new List<INmd>();
            founds.AddRange(Members.FindAll(pred));
            Members.ForEach(delegate(INmd n)
            { if (n is Nsp) { founds.AddRange((n as Nsp).FindDownAll(pred)); } });
            return founds;
        }

        virtual public INmd Find(string name)
        {
            return Members.Find(GetNamePredicate<INmd>(name));
        }

        virtual public INmd FindByNamePath(string namepath)
        {
            Debug.Assert(false == string.IsNullOrEmpty(namepath));
            return FindByNamePath(new Queue<string>(namepath.Split(new char[] { '/' })));
        }

        virtual public INmd FindByNamePath(Queue<string> namepath)
        {
            Debug.Assert(namepath != null && namepath.Count > 0);

            INmd n = Find(namepath.Dequeue());
            if (namepath.Count == 0) { return n; }
            Nsp nsp = n as Nsp;
            if (nsp == null) { return null; }
            return nsp.FindByNamePath(namepath);
        }

        public override string ToString()
        {
            //return Name + "(" + Members_.Count + "):" + GetType().Name;
            return Bty.New().Add("{").Nv("Name", Name).Add("}").Add(":").Add(GetType().Name).ToS();
            //return Sty.Curly(Sty.Nv("Name", Name)) + ":" + GetType().Name;
        }

        public INmd Clone()
        {
            Nsp c = MemberwiseClone() as Nsp;
            foreach (INmd m in c.Members)
            { m.Family = c; }
            return c;
        }

        static public Predicate<T> GetNamePredicate<T>(string name) where T : INmd
        { return delegate(T v) { return v.Name == name; }; }
    }

    public class Env : Nsp
    {
        public TypeLoader TypeLdr = new TypeLoader();
        public int Sequence = 0;
        public string GetTempName() { ++Sequence; return "$" + Sequence.ToString("D6"); }
        public string OutPath = "";

        public Env(Token seed)
            : base(seed, null)
        {
        }

        public App NewApp(Token seed)
        {
            return BeAMember(new App(seed, this));
        }

        public Typ NewRefTyp(Type refType)
        {
            Typ t = new Typ(refType, this);
            BeAMember(t);
            t.EnsureMembersList.Add(Typ.EnsureMembers);
            return t;
        }

        public Typ FindRefTyp(Type refType)
        {
            return FindAllTypeOf<Typ>()
                .Find(GetNamePredicate<Typ>(refType.FullName ?? refType.Name));
        }

        public Typ FindOrNewRefType(Type refType)
        {
            return FindRefTyp(refType) ?? NewRefTyp(refType);
        }

        public Typ NewArrayTyp(Typ typ, int dimension)
        {
            Typ t = new Typ(typ, this, dimension);
            BeAMember(t);
            return t;
        }

        public Typ FindArrayTyp(Typ typ, int dimension)
        {
            return FindAllTypeOf<Typ>()
                .Find(delegate(Typ t) { return t.ArrayType == typ && t.Dimension == dimension; });
        }

        public Typ FindOrNewArrayTyp(Typ typ, int dimension)
        {
            return FindArrayTyp(typ, dimension) ?? NewArrayTyp(typ, dimension);
        }

        public Typ NewGenericTypInstance(Typ typ, Typ[] genericTypeParams)
        {
            Typ t = BeAMember(new Typ(typ, this, genericTypeParams)); ;
            return t;
        }

        public Typ FindGenericTypInstance(Typ typ, Typ[] genericTypeParams)
        {
            return FindAllTypeOf<Typ>()
                .Find(delegate(Typ t)
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

        public Nsp NewNsp2(string ns)
        {
            return BeAMember(new Nsp(ns, this, true)); ;
        }

        public ActnOvld NewActnOvld(string name)
        {
            if (Members_.Exists(GetNamePredicate<INmd>(name)))
            { throw new SyntaxError("The name is already defined: " + name); }
            ActnOvld ovl = new ActnOvld(new Token(name), this);
            BeAMember(ovl);
            return ovl;
        }

    }

    public class ActnOvld : Nsp
    {
        public ActnOvld(Token seed, Nsp family)
            : base(seed, family)
        {
        }

        public Actn Inherit(Actn a)
        {
            return a.GetType() == typeof(Fctn) ? InheritFctn(a as Fctn) : InheritActn(a);
        }

        public Actn InheritActn(Actn a)
        {
            Actn inh = a.Mb == null ? NewActn(a.Seed, a.Params) : NewActn(a.Mb);
            inh.IsInherited = true;
            return inh;
        }

        public Fctn InheritFctn(Fctn f)
        {
            Fctn inh = f.Mb == null ? NewFctn(f.Seed, f.Params, f.Typ) : NewFctn(f.Mb);
            inh.IsInherited = true;
            return inh;
        }

        public Actn NewActn(Token seed, List<Variable> params_)
        {
            return BeAMember(new Actn(seed, this, params_));
        }

        public Actn NewActn(MethodBase mb)
        {
            return BeAMember(new Actn(mb, this));
        }

        public Fctn NewFctn(Token seed, List<Variable> params_, Typ returnTyp)
        {
            return BeAMember(new Fctn(seed, this, params_, returnTyp));
        }

        public Fctn NewFctn(MethodBase mb)
        {
            return BeAMember(new Fctn(mb, this));
        }

        public Actn GetActnOf(Typ[] argtyps, Actn caller)
        {
            List<Actn> cand = CreateCandidateActnList(argtyps);

            //  TODO  remove candidate with accessibility
            Predicate<Actn> canAccess = delegate(Actn callee_) { return AccessControl.CanAccess(caller, callee_); };

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

        public List<Actn> CreateCandidateActnList(Typ[] argtyps)
        {
            List<Actn> candidates = new List<Actn>();

            //  inheritance hierarchy into list
            List<Typ> typs = Cty.ByNext<Typ>(delegate(Typ y) { return y.BaseTyp; }, FindUpTypeIs<Typ>());
            //  go up hierarchy and find same name ActnOvld
            List<ActnOvld> ovlds2 = typs.FindAll(delegate(Typ y) { return null != y.FindActnOvld(this.Name); })
                .ConvertAll<ActnOvld>(delegate(Typ y) { return y.FindActnOvld(this.Name); });

            //  collect same name Actn
            List<Actn> srclst = this.FindAllTypeIs<Actn>();
            foreach (ActnOvld ovld in ovlds2)
            {
                srclst.AddRange(ovld.FindAllTypeIs<Actn>());
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

        static string Signature(List<Typ> typs)
        {
            return string.Join(" "
                , typs.ConvertAll<string>(delegate(Typ y) { return y.Name; }).ToArray());
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

        public Env Env { get { return FindUpTypeOf<Env>() as Env; } }
        public App App { get { return FindUpTypeOf<App>() as App; } }

        public string SpecialName = "";
        public MethodAttributes MthdAttrs;
        //public List<Variable2> Params;
        public List<Variable> Params
        {
            get
            {
                return FindAllTypeOf<Variable>().FindAll(delegate(Variable v)
                       {
                           return v.VarKind == Variable.VariableKind.Param
                                || v.VarKind == Variable.VariableKind.ParamGeneric;
                       });
            }
        }
        public Typ[] Signature
        {
            get
            {
                return Params.ConvertAll<Typ>(delegate(Variable v) { return v.Typ; }).ToArray();
            }
        }

        public bool IsConstructor { get { return Nana.IMRs.IMRGenerator.IsAnyCons(Family.Name); } }
        public bool IsEntryPoint { get { return Family != null && (Family.Name == EntryPointNameDefault || Family.Name == EntryPointNameImplicit); } }
        public bool IsInherited = false;
        public bool IsReferencing { get { return Mb != null; } }
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
        //public List<Func<string>> _Intermediates = new List<Func<string>>();
        //public List<Func<string>> Instructions
        //{
        //    get { return _Intermediates; }
        //    set { _Intermediates = value; ; }
        //}

        public List<IMR> _Intermediates = new List<IMR>();
        public List<IMR> Instructions
        {
            get { return _Intermediates; }
            set { _Intermediates = value; ; }
        }

        //public List<object> _Intermediates = new List<object>();
        //public List<object> Instructions
        //{
        //    get { return _Intermediates; }
        //    set { _Intermediates = value; ; }
        //}

        public Actn(Token seed, Nsp family, List<Variable> params_)
            : base(seed, family)
        {
            StringBuilder b = new StringBuilder();

            if (params_ == null) { return; }
            params_.ConvertAll<StringBuilder>(delegate(Variable v)
            { return b.Append(" ").Append(v.Typ._FullName); });
            Name_ = seed.Value + b.ToString();
            params_.ForEach(delegate(Variable v)
            { BeAMember(v); });
        }

        public MethodBase Mb;

        public Actn(MethodBase mb, Nsp family)
            : base(new Token(family.Members.Count.ToString()), family)
            //: base(new Token(GenSignature(mb)), family)
        {
            Mb = mb;
            new List<ParameterInfo>(mb.GetParameters()).ForEach(delegate(ParameterInfo p)
                    { NewParam(p.Name, Env.FindOrNewRefType(p.ParameterType)); });
                    //{ NewParam(p.Name, App.FindOrNewRefType(p.ParameterType)); });
            IsReferencing_ = true;
            MthdAttrs = mb.Attributes;
            if (MethodAttributes.SpecialName == (mb.Attributes & MethodAttributes.SpecialName))
            { SpecialName = mb.Name; }
        }

        public Variable NewThis()
        {
            Typ typ = FindUpTypeOf<Typ>();
            if (typ == null) { throw new NotImplementedException("Cannot define this in here"); }
            return BeAMember(new Variable("this", this, typ, Variable.VariableKind.This));
        }

        public Variable NewParam(string name, Typ typ)
        {
            return BeAMember(new Variable(name, this, typ, Variable.VariableKind.Param));
        }

        virtual public Variable NewVar(string name, Typ typ)
        {
            return BeAMember(new Variable(name, this, typ, Variable.VariableKind.Local));
        }

        public List<Variable> FindAllLocalVariables()
        {
            return FindAllTypeOf<Variable>()
                .FindAll(delegate(Variable v) { return v.VarKind == Variable.VariableKind.Local; });
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

        public Fctn(Token seed, Nsp family, List<Variable> params_, Typ returnTyp)
            : base(seed, family, params_)
        {
            Typ_ = returnTyp;
        }

        public Fctn(MethodBase mb, Nsp family)
            : base(mb, family)
        {
            Type t = mb.IsConstructor && (mb.IsStatic == false)
                ? mb.DeclaringType
                : (mb as MethodInfo).ReturnType;
            Typ_ = family.FindUpTypeOf<Env>().FindOrNewRefType(t);
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

        public List<INmd> DebuggerDisplayMembers { get { return Members_; } }

        new public bool IsReferencing { [DebuggerNonUserCode]  get { return (RefType != null); } }

        public Typ(Token seed, Nsp family)
            : base(seed, family, null)
        {
            if (App != null) { AssemblyName = App.AssemblyName; }
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

        public ActnOvld NewActnOvld(string name)
        {
            if (Members_.Exists(GetNamePredicate<INmd>(name)))
            { throw new SyntaxError("The name is already defined: " + name); }
            ActnOvld ovl = new ActnOvld(new Token(name), this);
            BeAMember(ovl);
            return ovl;
        }

        public ActnOvld FindActnOvld(string name)
        {
            return FindAllTypeOf<ActnOvld>()
                        .Find(GetNamePredicate<ActnOvld>(name));
        }

        public Type RefType = null;

        public Typ(Type refType, Nsp family)
            : base(new Token(refType.FullName ?? refType.Name), family, null)
        {
            RefType = refType;
            IsValueType = refType.IsValueType;
            IsReferencing_ = true;
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
                ls.Add(new GenericArgument(t, this));
            }
            return ls.ToArray();
        }

        public Typ(Typ typ, Env env, int dimension)
            : base(new Token(typ._FullName + "[" + dimension + "]"), env, null)
        {
            Dimension = dimension;
            IsVector = dimension == 1;
            IsArray = dimension > 1;
            IsVectorOrArray = IsVector || IsArray;
            ArrayType = typ;
            SetBaseTyp(env.FindOrNewRefType(typeof(System.Array)));
        }

        public Typ GenericType = null;
        public Typ[] GenericTypeParams = null;
        public Dictionary<string, Typ> GenericDic = null;

        public Typ(Typ genericTyp, Env env, Typ[] genericTypeParams)
            : base(new Token(genericTyp.Name), env, null)
        {
            GenericType = genericTyp;
            GenericTypeParams = genericTypeParams;

            IsGeneric = true;
            IsGenericInstance = true;
            AssemblyName = genericTyp.AssemblyName;
            _FullName = genericTyp._FullName;
            IsValueType = genericTyp.IsValueType;
            RefType = genericTyp.RefType;

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
            //TypLoader typLdr = Root.FindRoot(above).TypLdr;
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
            return BeAMember<Prop>(new Prop(this, p));
        }

        public void InheritActnOvld(ActnOvld ov)
        { ov.FindAllTypeIs<Actn>().ForEach(InheritActn); }

        public void InheritActn(Actn a)
        { FindOrNewActnOvld(a.Name).Inherit(a); }

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
            List<ActnOvld> aos = gt.FindAllTypeIs<ActnOvld>();
            foreach (ActnOvld gao in gt.FindAllTypeIs<ActnOvld>())
            {
                ActnOvld sao = self.FindOrNewActnOvld(gao.Name);
                List<Variable> svs = new List<Variable>();
                foreach (Actn ga in gao.FindAllTypeIs<Actn>())
                {
                    Variable sv; Typ st; int genericIndex;
                    svs.Clear();
                    foreach (Variable gv in ga.FindAllTypeIs<Variable>())
                    {
                        if (gv.VarKind != Variable.VariableKind.Param)
                        {
                            continue;
                        }
                        st = TransGenericType(self, gv.Typ);
                        sv = new Variable(gv.Name, null, st, Variable.VariableKind.Param);
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

        public INmd FindMemeber(string name)
        {
            Typ typ = this;
            INmd mem = typ.Find(name);
            while (typ != null && mem == null)
            {
                if (typ.IsReferencing && typ.RefType == typeof(object))
                { break; }
                typ = typ.BaseTyp;
                mem = typ.Find(name);
            }
            if (mem != null && mem.Family != this)
            {
                mem = mem.Clone();
                mem.Family = this;
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
        public App(Token seed, Env family)
            : base(seed, family)
        {
            Name = Path.GetFileName(Env.OutPath);
            AssemblyName = Path.GetFileNameWithoutExtension(Env.OutPath);
        }

        public Nsp NewNsp(Token seed)
        {
            Nsp n = new Nsp(seed, this);
            n.Name = seed.ValueImplicit;
            BeAMember(n);
            return n;
        }

        public Typ NewTyp(Token seed)
        {
            return BeAMember(new Typ(seed, this));
        }

        override public Variable NewVar(string name, Typ typ)
        {
            return BeAMember(new Variable(name, this, typ, Variable.VariableKind.StaticField));
        }

    }

    public class Prop : ActnOvld, ITyped
    {
        public PropertyInfo P;
        public Actn Setter;
        public Fctn Getter;
        public Typ Typ_;

        public Prop(Nsp family, PropertyInfo p)
            : base(new Token(p.Name), family)
        {
            P = p;
            MethodInfo m;
            m = p.GetGetMethod(/* nonPublic:*/ true);
            Getter = null;
            if (m != null)
            {
                Getter = BeAMember<Fctn>(new Fctn(m, this));
            }
            Setter = null;
            m = p.GetSetMethod(/* nonPublic:*/ true);
            if (m != null)
            {
                Setter = BeAMember<Actn>(new Actn(m, this));
            }
            Typ_ = family.FindUpTypeOf<Env>().FindOrNewRefType(p.PropertyType);
        }

        public Typ Typ { get { return Typ_; } }
    }

    //  Return Determinacy State (It's doubtful to make sense in English.)
    //  Ç±Ç±Ç≈ïKÇ∏ÉäÉ^Å[ÉìÇÕÇ≥ÇÍÇƒÇ¢ÇÈÇ±Ç∆Ç™åàíËÇµÇƒÇ¢ÇÈÇ©ÇÃèÛë‘
    public interface IReturnDeterminacyState
    {
        bool RDS { get;}
    }

    public interface IExecutable
    {
        void Exec(IMRGenerator gen);
    }

    public class Exe /*-cution*/ : IExecutable
    {
        virtual public void Exec(IMRGenerator gen) { }
        virtual public void Give(IMRGenerator gen) { }
        virtual public void Take(IMRGenerator gen) { }
        virtual public void Addr(IMRGenerator gen) { Give(gen); }
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

    public class GenericArgument : INmd
    {
        public string _Name;
        public string Name { get { return _Name; } set { _Name = value; } }

        public Nsp _Family;
        public Nsp Family { get { return _Family; } set { _Family = value; } }

        public INmd Clone() { return MemberwiseClone() as INmd; }

        public Type RefType = null;

        public GenericArgument(Type refType, Nsp family)
        {
            RefType = refType;
            _Name = refType.Name;
            _Family = family;
        }
    }

    public class Variable : Exe,  INmd, ITyped, IVariable
    {
        public enum VariableKind
        {
            Param, This, Local, StaticField, InstanceField,
            Vector, Array, ParamGeneric
        }
        public VariableKind VarKind;
        public int GenericIndex = -1;

        public string _Name;
        public string Name { get { return _Name; } set { _Name = value; } }

        public Nsp _Family;
        public Nsp Family { get { return _Family; } set { _Family = value; } }

        public Typ Typu;
        public Typ Typ { [DebuggerNonUserCode] get { return Typu; } }

        public Variable(string name, Nsp family, Typ typu, VariableKind varKind)
            : base()
        {
            _Name = name;
            _Family = family;
            Typu = typu;
            VarKind = varKind;
        }

        public override void Give(IMRGenerator gen)
        {
            gen.LoadVariable(this);
        }

        public override void Take(IMRGenerator gen)
        {
            gen.StoreVariable(this);
        }

        public override void Addr(IMRGenerator gen)
        {
            if (Typ.IsValueType) { gen.LoadAVariable(this); }
            else { gen.LoadVariable(this); }
        }

        public INmd Clone()
        {
            return MemberwiseClone() as INmd;
        }

        public override string ToString()
        {
            //return Name + ":" + Typ.Name + ":" + typeof(Variable).Name;
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
        public Actn Callee;
        public IValuable Instance;
        public IValuable[] Args;
        public bool IsNewObj;

        public CallAction(Actn callee, IValuable instance, IValuable[] args, bool isNewObj)
        {
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
            gen.CallAction(Callee);
        }

        public override string ToString()
        {
            Func<string, object, string> nv = Sty.Nv;
            return Sty.Curly(Sty.Csv(nv("Instance", Instance), nv("Callee", Callee), nv("Args", Sty.Curly(Sty.Csv(Args))), nv("IsNewObj", IsNewObj))) + ":" + GetType().Name;



            //StringBuilder b = new StringBuilder();
            //b.Append(GetType().Name).Append("={");
            //b.Append("Instance=").Append(Instance == null ? "(null)" : Instance.ToString());
            //b.Append(", Callee=").Append(Callee.ToString());
            //b.Append(", Args={").Append(string.Join(", "
            //    , new List<IValuable>(Args).ConvertAll<string>(delegate(IValuable v) { return v.ToString(); }).ToArray()
            //    )).Append("}");
            //b.Append(", IsNewObj=" + IsNewObj.ToString());
            //b.Append("}");
            //return b.ToString();
        }

    }

    public class CallFunction : CallAction, ITyped, IValuable
    {
        public Fctn CalleeFctn { get { return Callee as Fctn; } }

        public CallFunction(Fctn callee, IValuable instance, IValuable[] args, bool isNewObj)
            : base(callee, instance, args, isNewObj)
        {  }

        public Typ Typ { get { return CalleeFctn.Typ; } }

        public void Give(IMRGenerator gen)
        {
            LoadInstance(gen);
            LoadArgs(gen);
            if (IsNewObj) { gen.NewObject(Callee); }
            else { gen.CallAction(Callee); }
            //if (IsNewObj) { gen.NewObjActionSig(Callee); }
            //else { gen.CallActionSig(Callee); }
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
        public Typ Typ { [DebuggerNonUserCode] get { return Prop.Typ; } }
        public Prop Prop;
        public IValuable Instance;

        public CallPropInfo( Prop prop, IValuable instance)
        {
            Prop = prop;
            Instance = instance;
        }

        public void Give(IMRGenerator gen)
        {
            if (Prop.Getter == null)
            { throw new SyntaxError("Cannot get value from the property"); }
            CallFunction cf = new CallFunction( Prop.Getter, Instance, new IValuable[0], /*isNewObj:*/ false);
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
        public Typ Typu;
        public Typ Typ { [DebuggerNonUserCode]get { return Typu; } }

        public ArrayInstatiation(Typ typu, IValuable[] lens, TmpVarGenerator tmpVarGen)
        {
            Typu = typu;
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

        public Variable Generate(Typ typu, IMRGenerator gen)
        {
            return DeclareVariable(GetTempName(), typu);
        }

        public Variable Substitute(Typ typu, IMRGenerator gen)
        {
            Variable v = Generate(typu, gen);
            v.Take(gen);
            return v;
        }

        public Variable Insert(Typ typu, IMRGenerator gen, IMR PlaceHolder)
        {
            Debug.Assert(PlaceHolder != null);
            Debug.Assert(GetTempName != null);
            Debug.Assert(DeclareVariable != null);

            IMRGenerator tmpgen = new IMRGenerator();
            Variable v = Substitute(typu, tmpgen);
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

        public Typ Typu;
        public Typ Typ { [DebuggerNonUserCode] get { return Typu; } }

        public ArrayAccessInfo(IValuable val, IValuable[] indices)
        {
            Typu = val.Typ.ArrayType;
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

        public Typ Typu;
        public Typ Typ { [DebuggerNonUserCode] get { return Typu; } }

        public ArraySetInfo(ArrayAccessInfo arrayAccess, IValuable giveVal)
        {
            Debug.Assert(arrayAccess != null && arrayAccess.Val != null && arrayAccess.Val.Typ != null);

            Typu = arrayAccess.Typ;
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
                t2 = Typu;
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

        static public bool CanAccess(Actn er, Actn ee)
        {
            Typ ert, eet;
            bool identAssembly;
            ert = er is Typ ? er as Typ : er.FindUpTypeIs<Typ>();
            eet = ee is Typ ? ee as Typ : ee.FindUpTypeIs<Typ>();
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
