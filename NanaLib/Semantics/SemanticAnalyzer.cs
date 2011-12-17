/*
 * ● ビルド処理順序実装戦略
 * 
 * ビルドオプションを収集する
 *      -> GlobalSpaceが実装する
 * usingを収集する                          -- FormType
 *      -> SourceSpaceが実装する            
 * タイプを収集する                         -- FormType
 *      -> SourceSpaceが実装する
 *          □ 循環参照を解決する
 *          □ 継承順序正当性を検証する
 * 関数シグニチャを収集する                 -- FormFunc
 *      -> TypeSpaceが実装する
 * 
 * (-- ここまでで型の解決が終わっていること --)
 * 
 * 関数本体を中間表現のリストにする         -- FormBody
 *      -> FuncSpaceがリストを管理する
 *      -> LineSpaceが中間表現を生成する
 */
//TODO support array of defined type
//TODO 複数のファイルを処理するときは各ファイルごとにusing収集、タイプ収集...と行う
//TODO check the CSV form

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Nana.Delegates;
using Nana.Infr;
using Nana.IMRs;
using Nana.Tokens;

namespace Nana.Semantics
{
    public class ActionableDictionary<TKey, TActPrm> : Dictionary<TKey, Action<TActPrm>>
    {
        public Action<TActPrm> Default = delegate(TActPrm p) { };
        [DebuggerNonUserCode]
        public void Action(KeyValuePair<TKey, TActPrm> pair)
        { (ContainsKey(pair.Key) ? base[pair.Key] : Default)(pair.Value); }
    }

    abstract public class SemanticAnalyzer
    {
        public ActionableDictionary<string, Token> AnalyzeDic = new ActionableDictionary<string, Token>();
        public SemanticAnalyzer Above_;

        public SemanticAnalyzer(SemanticAnalyzer above)
        {
            Above_ = above;

            AnalyzeDic.Default = delegate(Token t)
            { throw new InternalError("Not supported group:" + t.Group, t); };

            foreach (MethodInfo mi in GetType().GetMethods())
            {
                if (false == mi.Name.StartsWith("Analyze")
                    || mi.Name == "Analyze"
                    || mi.Name == "AnalyzeFollows"
                    ) { continue; }
                string group = mi.Name.Substring("Analyze".Length);
                AnalyzeDic[group] = CreateInvoker(this, mi);
            }
        }

        [DebuggerNonUserCode]
        public Action<Token> CreateInvoker(SemanticAnalyzer a, MethodInfo m)
        {
            return delegate(Token t) { m.Invoke(a, new object[] { t }); };
        }

        abstract public void Analyze();

        [DebuggerNonUserCode]
        public void AnalyzeAll(Token[] tokens)
        {
            ToKeyValuePairs(tokens).ForEach(AnalyzeDic.Action);
        }

        [DebuggerNonUserCode]
        static public List<KeyValuePair<string, Token>> ToKeyValuePairs(Token[] tokens)
        {
            return new List<Token>(tokens)
                .ConvertAll<KeyValuePair<string, Token>>
                (delegate(Token t) { return new KeyValuePair<string, Token>(t.Group, t); });
        }

        virtual public void RegisterAnalyzer(SemanticAnalyzer analyzer)
        {
            if (Above_ == null) { return; }
            Above_.RegisterAnalyzer(analyzer);
        }

        public T FindUpTypeOf<T>() where T : SemanticAnalyzer
        {
            return Above_ == null ? null : Above_.GetType() == typeof(T) ? Above_ as T : Above_.FindUpTypeOf<T>();
        }

        public T FindUpTypeIs<T>() where T : SemanticAnalyzer
        {
            return Above_ == null ? null : Above_ is T ? Above_ as T : Above_.FindUpTypeIs<T>();
        }

    }

    public class EnvAnalyzer : SemanticAnalyzer
    {
        public Env Env;
        public AppAnalyzer AppAzr;
        public Token Seed;

        public List<Type> Order = new List<Type>(new Type[]
        {
            typeof(AppAnalyzer)
            , typeof(SrcAnalyzer)
            , typeof(TypAnalyzer)
            , typeof(TypBaseAnalyzer)
            , typeof(TypBodyAnalyzer)
            , typeof(ActnAnalyzer)
            , typeof(TypEnsureAnalyzer)
            , typeof(BaseInstanceConstructorCallAnalyzer)
            , typeof(ExeAnalyzer)
            , typeof(EnsureAppExeAnalyzer)
            , typeof(EnsureEntryPointAnalyzer)
            , typeof(RemoveReferencingTypeAnalyzer)
        });

        public Dictionary<Type, List<Action>> Subanalyzes = new Dictionary<Type, List<Action>>();

        public EnvAnalyzer(Token seed)
            : base(null)
        {
            Seed = seed;
            Order.ForEach(delegate(Type t) { Subanalyzes.Add(t, new List<Action>()); });
        }

        public override void RegisterAnalyzer(SemanticAnalyzer analyzer)
        {
            foreach (Type y in Order)
            {
                bool b = y == analyzer.GetType()
                    || analyzer.GetType().IsSubclassOf(y);
                if (b)
                {
                    Subanalyzes[y].Add(analyzer.Analyze);
                }
            }
        }

        public override void Analyze()
        {
            Env = new Env(Seed);
            AddSystemTyps(Env);
            AddBuiltInFunction(Env, "`p", "WriteLine", typeof(Console));
            RegisterAnalyzer(new RemoveReferencingTypeAnalyzer(this));
            AnalyzeAll(Seed.Follows);
            Order.ForEach(delegate(Type t)
            {
                Subanalyzes[t].ForEach
                    (delegate(Action a) { a(); });
            });
        }

        static public void AddSystemTyps(Env env)
        {
            Typ y;
            y = env.NewRefTyp(typeof(object));
            y = env.NewRefTyp(typeof(string));
        }

        static public void AddBuiltInFunction(Env env, string built_in_function_name, string actualname, Type holdertype)
        {
            ActnOvld ao = env.NewActnOvld(built_in_function_name);
            Typ hty = env.NewRefTyp(holdertype);
            ActnOvld actualao = hty.FindMemeber(actualname) as ActnOvld;
            Debug.Assert(actualao != null);
            ao.Members.AddRange(actualao.Members);
        }

        public void AnalyzeCompileOptions(Token t)
        {
            Debug.Assert(t != null && t.Group == "CompileOptions");
            foreach (Token opt in t.Follows)
            {
                switch (opt.Group.ToLower())
                {
                    case "include":     /**/ Env.TypeLdr.InAssembly.Includes.Add(opt.Value); break;
                    case "reference":   /**/ Env.TypeLdr.InAssembly.LoadFrameworkClassLibrarie(opt.Value); break;
                    case "out":         /**/ Env.OutPath = opt.Value; break;
                    default:
                        if (opt.Group.ToLower().StartsWith("xxx")) { break; }
                        throw new InternalError("The compile option is not supported: " + opt.Value, t);
                }
            }
        }

        public void AnalyzeSources(Token t)
        {
            if (AppAzr != null) { throw new InternalError(@"Cannot speficy two or more ""sources"".", t); }
            AppAzr = new AppAnalyzer(t, this);
            RegisterAnalyzer(AppAzr);
        }

        public void AnalyzeSyntax(Token t)
        {
            if (AppAzr != null) { throw new InternalError(@"Cannot speficy two or more ""sources"".", t); }
            AppAzr = new AppAnalyzer(t, this);
            RegisterAnalyzer(AppAzr);
        }

        public void AnalyzeCode(Token t)
        {
            //  do nothing, place holder for next phase
        }

        public void AnalyzeIgnore(Token t)
        {
        }

    }

    abstract public class NspAnalyzer : SemanticAnalyzer
    {
        [DebuggerNonUserCode]
        public NspAnalyzer Above { get { return Above_ as NspAnalyzer; } }
        abstract public Nsp Nsp { get;}

        public Token Seed;


        public NspAnalyzer(Token seed, NspAnalyzer above)
            : base(above)
        {
            Seed = seed;
        }

        virtual public INmd Find(Token t)
        {
            return Nsp.Find(t.Value);
        }

        virtual public INmd FindUp(Token t)
        {
            return Find(t) ?? (Above != null ? Above.FindUp(t) : null);
        }

        virtual public T FindKindOf<T>(Predicate<T> p) where T : class, INmd
        {
            return Nsp.Family.FindAllTypeIs<T>().Find(p)
                ?? (Above == null ? null : Above.FindKindOf<T>(p));
        }

        static public Predicate<T> GetNamePredicate<T>(string name) where T : INmd
        { return delegate(T v) { return v.Name == name; }; }


        virtual public Typ RequireTyp(Token t)
        {
            return Above != null ? Above.RequireTyp(t) : null;
        }
    }

    public class AppAnalyzer : TypAnalyzer
    {
        public Token AppToken = null;
        public EnvAnalyzer EnvAzr;
        [DebuggerNonUserCode]
        override public Nsp Nsp { get { return App; } }

        public AppAnalyzer(Token seed, EnvAnalyzer above)
            : base(seed, null)
        {
            ActnToken = null;
            TypToken = null;
            AppToken = seed;
            Above_ = above;
            EnvAzr = above;
            Env = above.Env;
        }

        public override void Analyze()
        {
            base.Analyze();
            if (AppToken == null) { return; }
            Token t = AppToken;
            AppToken = null;
            AnalyzeAppToken(t);
            return;
        }

        public void AnalyzeAppToken(Token t)
        {
            App = Env.NewApp(t);
            Typu = App;
            Actn = App;
            AnalyzeAll(Seed.Follows);
            RegisterAnalyzer(new EnsureEntryPointAnalyzer(this));
            RegisterAnalyzer(new EnsureAppExeAnalyzer(this));
        }

        public void AnalyzeSource(Token t)
        {
            RegisterAnalyzer(new SrcAnalyzer(t, this));
        }

        public override INmd Find(Token t)
        {
            if (App.ContainsKey(t.Value)) { return App.Members_.Find(Nsp.GetNamePredicate<INmd>(t.Value)); }
            if (App.ContainsKey(t.ValueImplicit)) { return App.Members_.Find(Nsp.GetNamePredicate<INmd>(t.ValueImplicit)); }

            string value = t.Value;
            if (TypeUtil.IsBuiltIn(value))
            {
                value = TypeUtil.FromBuiltIn(value).FullName;
            }

            if (Env.ContainsKey(value)) { return Env.Members_.Find(Nsp.GetNamePredicate<INmd>(value)); }
            if (Env.ContainsKey(t.ValueImplicit)) { return Env.Members_.Find(Nsp.GetNamePredicate<INmd>(t.ValueImplicit)); }


            if (Env.TypeLdr.IsNamespace(t.ValueImplicit))
            {
                return Env.NewNsp2(t.ValueImplicit);
            }

            Type type;
            if ((type = Env.TypeLdr.GetTypeByName(t.ValueImplicit)) != null)
            {
                return Env.NewRefTyp(type);
            }

            return null;
        }

    }

    public class SrcAnalyzer : NspAnalyzer
    {
        public Env Env;
        public List<string> Usings;

        [DebuggerNonUserCode]
        override public Nsp Nsp { get { return Above.Nsp; } }

        public SrcAnalyzer(Token seed, AppAnalyzer above)
            : base(seed, above)
        {
            Env = above.FindUpTypeOf<EnvAnalyzer>().Env;
            Usings = new List<string>();
            AnalyzeDic.Default = AnalyzeDefault;
        }

        public override void Analyze()
        {
            AnalyzeAll(Seed.Follows);
        }

        public void AnalyzeUsing(Token t)
        {
            Debug.Assert(t != null);
            Debug.Assert(t.Follows != null);
            Debug.Assert(t.Follows.Length == 1);
            Debug.Assert(t.Follows[0] != null);

            Token f0 = t.Follows[0];

            Token last = SpecifiedTypAnalyzer.GoToLastIdAndBuildName(f0);
            if (Usings.Contains(last.ValueImplicit) == false) { Usings.Add(last.ValueImplicit); }
        }

        public void AnalyzeTypeDef(Token t)
        {
            RegisterAnalyzer(new TypAnalyzer(t, this));
        }

        public void AnalyzeFunc(Token t)
        {
            RegisterAnalyzer(new ActnAnalyzer(t, this));
        }

        public void AnalyzeDefault(Token t)
        {
            RegisterAnalyzer(new ExeAnalyzer(t, this));
        }

        public override INmd Find(Token t)
        {
            INmd found = Above.FindUp(t);
            int i = 0;
            while (found == null && i < Usings.Count)
            {
                found = Above.FindUp(new Token(Usings[i] + "." + t.Value));
                ++i;
            }
            return found;
        }

        public override Typ RequireTyp(Token t)
        {
            Token last = SpecifiedTypAnalyzer.GoToLastIdAndBuildName(t);
            Typ typ = Above.FindUp(last) as Typ;
            int i = 0;
            while (typ == null && i < Usings.Count)
            {
                typ = Above.FindUp(new Token(Usings[i] + "." + last.ValueImplicit)) as Typ;
                ++i;
            }
            if (typ == null) { return null; }

            Token array = last;
            while (array != null && SpecifiedTypAnalyzer.IsArray(array.Follows))
            {
                int dim = SpecifiedTypAnalyzer.GetDimension(array.Follows);
                typ = Env.NewArrayTyp(typ, dim);
                array = array.Follows != null && array.Follows.Length > 0
                    ? array.Follows[array.Follows.Length - 1]
                    : null;
            }

            return typ;
        }
        
    }

    public class SpecifiedTypAnalyzer
    {

        static public Token GoToLastIdAndBuildName(Token t)
        {
            Debug.Assert(t != null);
            Debug.Assert(t.Group == "Id" || t.Group == "TypeSpec2");

            Token pre = t;
            Token next;

            if (pre.ValueImplicit == "") { pre.ValueImplicit = pre.Value; }
            while (pre.Follows != null && pre.Follows.Length > 0)
            {
                if (pre.Follows[0].Value == "[") { break; }
                
                Debug.Assert(pre.Follows.Length == 1);
                Debug.Assert(pre.Follows[0].Value == ".");
                Debug.Assert(pre.Follows[0].Follows != null);
                Debug.Assert(pre.Follows[0].Follows.Length == 1);
                Debug.Assert(pre.Follows[0].Follows[0] != null);
                Debug.Assert(pre.Follows[0].Follows[0].Group == "Id" || pre.Follows[0].Follows[0].Group == "TypeSpec2");

                next = pre.Follows[0].Follows[0];
                next.ValueImplicit = pre.ValueImplicit + "." + next.Value;
                pre = next;
            }

            return pre;
        }

        static public bool IsArray(Token[] follows)
        {
            return follows != null && follows.Length > 0
                && follows[follows.Length - 1].Value == "[";
        }

        public static int GetDimension(Token[] follows)
        {
            Debug.Assert(follows != null && follows.Length > 0);

            Token a_ = follows[follows.Length - 1];
            Token[] fs;
            List<Token> fs2;
            int dim;
            dim = 1;
            fs = a_.Follows != null ? a_.Follows : new Token[0];
            fs2 = new List<Token>();
            foreach (Token f in fs)
            {
                if (Regex.IsMatch(f.Value, @",+"))
                { dim += f.Value.Length; }
            }
            return dim;
        }

    }

    public class TypAnalyzer : ActnAnalyzer
    {
        public Token TypToken = null;

        public Typ Typu;
        [DebuggerNonUserCode]
        override public Nsp Nsp { get { return Typu; } }

        public TypAnalyzer(Token seed, NspAnalyzer above)
            : base(seed, above)
        {
            ActnToken = null;
            TypToken = seed;
        }

        public override void Analyze()
        {
            base.Analyze();
            if (TypToken == null) { return; }
            Token t = TypToken;
            TypToken = null;
            AnalyzeTypToken(t);
            return;
        }

        public void AnalyzeTypToken(Token t)
        {
            Token name = t.FindGroupOf("Name");
            if (name == null || string.IsNullOrEmpty(name.Value))
            { throw new SyntaxError("Specify name to the class", t); }

            AppAnalyzer appazr = FindUpTypeOf<AppAnalyzer>();
            Debug.Assert(appazr != null);
            App app = appazr.App;

            Debug.Assert(app != null);

            Typu = app.NewTyp(name);

            Token baseTypeDef = t.FindGroupOf("BaseTypeDef");
            if (baseTypeDef == null)
            {
                baseTypeDef = new Token();
                baseTypeDef.FlwsAdd("System.Object", "Id");
            }
            RegisterAnalyzer(new TypBaseAnalyzer(baseTypeDef, this));

            Token body = t.FindGroupOf("TypeBody");
            if (body == null || body.Follows == null)
            { throw new SyntaxError("Specify the class body", t); }
            RegisterAnalyzer(new TypBodyAnalyzer(body, this));

            RegisterAnalyzer(new TypEnsureAnalyzer(null, this));
        }

    }

    public class TypBaseAnalyzer : SemanticAnalyzer
    {
        [DebuggerNonUserCode]
        public TypAnalyzer Above { get { return Above_ as TypAnalyzer; } }
        public Typ BaseTyp;
        public Token Seed;

        public TypBaseAnalyzer(Token seed, TypAnalyzer above)
            : base(above)
        {
            Seed = seed;
        }

        override public void Analyze()
        {
            Above.Typu.BaseTyp = Above.RequireTyp(Seed.Follows[0]);
        }

    }

    public class TypBodyAnalyzer : SemanticAnalyzer
    {
        public Token Seed;
        [DebuggerNonUserCode]
        public TypAnalyzer Above { get { return Above_ as TypAnalyzer; } }

        public TypBodyAnalyzer(Token seed, TypAnalyzer above)
            : base(above)
        {
            Seed = seed;
        }

        override public void Analyze()
        {
            AnalyzeAll(Seed.Follows);
        }

        public void AnalyzeFunc(Token t)
        {
            RegisterAnalyzer(new ActnAnalyzer(t, Above));
        }

    }

    public class TypEnsureAnalyzer : SemanticAnalyzer
    {
        [DebuggerNonUserCode]
        public TypAnalyzer Above { get { return Above_ as TypAnalyzer; } }

        public TypEnsureAnalyzer(Token seed, TypAnalyzer above)
            : base(above)
        { }

        override public void Analyze()
        {
            if (Above.Typu.FindAllTypeIs<ActnOvld>()
                .Exists(delegate(ActnOvld ao_) { 
                    return ao_.FindAllTypeIs<Actn>()
                    .Exists(delegate(Actn a){ return a.IsInherited == false && a.Name == ".ctor"; });}))
            { return; }

            Token t = GenFuncToken("cons", /* name */ null, /* returnType */ null);
            (new ActnAnalyzer(t, Above)).Analyze();
        }

        static public Token GenFuncToken(string func, string name, string returnType)
        {
            Debug.Assert(false == string.IsNullOrEmpty(func));

            Token f;

            f = new Token(func);

            if (string.IsNullOrEmpty(name) == false)
            { f.FlwsAdd(name); }

            f.FlwsAdd("(", "PrmDef").FlwsAdd(")");

            if (string.IsNullOrEmpty(returnType) == false)
            {
                f.FlwsAdd(":", "TypeSpec");
                f.FlwsTail.FlwsAdd(returnType, "Id");
            }

            f.FlwsAdd("..", "Block");
            f.FlwsTail.Follows = new Token[0];

            return f;
        }


    }

    public class ActnAnalyzer : NspAnalyzer
    {
        public Env Env;
        public App App;
        public Actn Actn;
        [DebuggerNonUserCode]
        override public Nsp Nsp { get { return Actn; } }
        public Token ActnToken = null;

        public ActnAnalyzer(Token seed, NspAnalyzer above)
            : base(seed, above)
        {
            ActnToken = seed;
            NspAnalyzer a = above;
            while (a != null)
            {
                if (a.Nsp != null && a.Nsp.GetType() == typeof(App))
                {
                    App = a.Nsp as App;
                    Env = App.Env;
                    break;
                }
                a = a.Above;
            }
        }

        override public void Analyze()
        {
            if (ActnToken == null) { return; }
            Token t = ActnToken;
            ActnToken = null;
            AnalyzeActnToken(t);
            return;
        }

        public void AnalyzeActnToken(Token t)
        {
            bool isStatic, isCtor;
            bool isInTypDecl = false;
            string ftyp = ResolveFuncType(t.Value, isInTypDecl);

            MethodAttributes attrs = AnalyzeAttrs(ftyp);
            isStatic = (attrs & MethodAttributes.Static) == MethodAttributes.Static;
            string nameasm = AnalyzeName(ftyp, t);
            isCtor = nameasm == Nana.IMRs.IMRGenerator.InstCons;

            TypAnalyzer typazr2 = FindUpTypOfAppAnalyzer();
            Debug.Assert(typazr2 != null);
            ActnOvld ovld = typazr2.Typu.FindOrNewActnOvld(nameasm);

            List<Token> prms = new List<Token>();
            Token prmpre = t.FindGroupOf("PrmDef");
            Token prm;
            while (prmpre != null && (prm = prmpre.FindGroupOf("Prm")) != null)
            {
                prms.Add(prm);
                prmpre = prm.FindGroupOf("Separator");
            }

            List<Variable> prmls = new List<Variable>();
            Token ty;
            foreach (Token p in prms)
            {
                ty = p.FindGroupOf("TypeSpec2");
                Typ typ = Above.RequireTyp(ty);
                Debug.Assert(typ != null);
                Debug.Assert(string.IsNullOrEmpty(p.Value) == false);
                prmls.Add(new Variable(p.Value, null, typ, Variable.VariableKind.Param));
            }

            Typ returnType = null;
            if (isCtor)
            {
                returnType = Above.Nsp is Typ
                    ? Above.Nsp as Typ : Above.Nsp.FindUpTypeIs<Typ>();
            }

            Actn = returnType == null
                ? ovld.NewActn(new Token(nameasm), prmls)
                : ovld.NewFctn(new Token(nameasm), prmls, returnType);

            Actn.MthdAttrs = attrs;

            Token body = t.FindGroupOf("Block");
            Debug.Assert(body != null && body.Follows != null);
            foreach (Token exe in body.Follows)
            { RegisterAnalyzer(new ExeAnalyzer(exe, this)); }

            //  generate instance variable
            if (Actn.IsInstance)
            {
                TypAnalyzer typazr = FindUpTypeOf<TypAnalyzer>();
                if (typazr == null)
                { throw new SyntaxError("Cannot define instance constructor in this sapce", t); }
                Actn.NewThis();
            }

            if (Nana.IMRs.IMRGenerator.IsInstCons(nameasm))
            {
                Token baseInstanceConstructorCall = new Token(".ctor");
                RegisterAnalyzer(new BaseInstanceConstructorCallAnalyzer(baseInstanceConstructorCall, this));
            }

        }

        public TypAnalyzer FindUpTypOfAppAnalyzer()
        {
            SemanticAnalyzer a = Above;
            while (a != null)
            {
                if (a.GetType() == typeof(TypAnalyzer)) { return a as TypAnalyzer; }
                if (a.GetType() == typeof(AppAnalyzer)) { return a as AppAnalyzer; }
                a = a.Above_;
            }
            return null;
        }

        public string ResolveFuncType(string func, bool isInTypDecl)
        {
            if (func != "fun") return func;
            if (isInTypDecl) { return "vfun"; }
            return "sfun";
        }

        static public MethodAttributes AnalyzeAttrs(string ftyp)
        {
            MethodAttributes attrs;
            attrs = MethodAttributes.Public;
            if (ftyp == "sfun" || ftyp == "scons") { attrs |= MethodAttributes.Static; }
            if (ftyp == "vfun") { attrs |= MethodAttributes.Virtual; }
            return attrs;
        }

        static public string AnalyzeName(string ftyp, Token seed)
        {
            string nameasm = null;
            if (ftyp == "cons") { nameasm = Nana.IMRs.IMRGenerator.InstCons; }
            if (ftyp == "scons") { nameasm = Nana.IMRs.IMRGenerator.StatCons; }
            if (ftyp == "sfun" || ftyp == "nfun" || ftyp == "vfun") { nameasm = seed.Follows[0].Value; }
            return nameasm;
        }

        static public Token GenFuncToken(string func, string name, string returnType)
        {
            Debug.Assert(false == string.IsNullOrEmpty(func));

            Token f;

            f = new Token(func);

            if (string.IsNullOrEmpty(name) == false)
            { f.FlwsAdd(name); }

            f.FlwsAdd("(", "PrmDef").FlwsAdd(")");

            if (string.IsNullOrEmpty(returnType) == false)
            {
                f.FlwsAdd(":", "TypeSpec");
                f.FlwsTail.FlwsAdd(returnType, "Id");
            }

            f.FlwsAdd("..", "Block");
            f.FlwsTail.Follows = new Token[0];

            return f;
        }

    }

    public class BaseInstanceConstructorCallAnalyzer : SemanticAnalyzer
    {
        [DebuggerNonUserCode]
        public ActnAnalyzer Above { get { return Above_ as ActnAnalyzer; } }

        public BaseInstanceConstructorCallAnalyzer(Token seed, ActnAnalyzer above)
            : base(above)
        { }

        override public void Analyze()
        {
            Typ mytyp = Above.FindUpTypeOf<TypAnalyzer>().Typu;
            Actn callee = mytyp.BaseTyp.FindActnOvld(".ctor").GetActnOf(new Typ[] { }, Above.Actn);
            IValuable instance = Above.Actn.FindAllTypeIs<Variable>().Find(Nsp.GetNamePredicate<Variable>("this"));
            Above.Actn.Exes.Add(new CallAction(callee, instance, new IValuable[] { }, false /*:isNewObj*/));
        }

    }

    public class EnsureEntryPointAnalyzer : SemanticAnalyzer
    {
        [DebuggerNonUserCode]
        public AppAnalyzer Above { get { return Above_ as AppAnalyzer; } }

        public EnsureEntryPointAnalyzer(AppAnalyzer above)
            : base(above)
        { }

        override public void Analyze()
        {
            App app = Above.App;
            List<Actn> actns = app.FindDownAllTypeIs<Actn>();
            List<Actn> founds = actns.FindAll(delegate(Actn a) { return a.IsEntryPoint; });
            if (founds.Count > 1)
            { throw new SyntaxError("Specify one entry point. There were two entry points or more."); }
            if (founds.Count == 1)
            { return; }

            Token t = ActnAnalyzer.GenFuncToken("sfun", Actn.EntryPointNameImplicit, "void");
            (new ActnAnalyzer(t, Above)).Analyze();
        }

    }

    public class EnsureAppExeAnalyzer : SemanticAnalyzer
    {
        [DebuggerNonUserCode]
        public AppAnalyzer Above { get { return Above_ as AppAnalyzer; } }

        public EnsureAppExeAnalyzer(AppAnalyzer above)
            : base(above)
        { }

        override public void Analyze()
        {
            App app = Above.App;
            if (app.Exes.Count == 0) { return; }

            Token t = ActnAnalyzer.GenFuncToken("scons", Actn.EntryPointNameImplicit, "void");
            (new ActnAnalyzer(t, Above)).Analyze();
            Actn cctor = app.FindActnOvld(".cctor").GetActnOf(new Typ[] { }, app);
            cctor.Exes.AddRange(app.Exes);
            app.Exes.Clear();
        }
    }

    public class RemoveReferencingTypeAnalyzer : SemanticAnalyzer
    {
        [DebuggerNonUserCode]
        public EnvAnalyzer Above { get { return Above_ as EnvAnalyzer; } }

        public RemoveReferencingTypeAnalyzer(EnvAnalyzer above)
            : base(above)
        {
        }

        override public void Analyze()
        {
            Env env = Above.Env;
            int i = env.Members.Count;
            env.Members.RemoveAll(delegate(INmd n)
            {
                return n is Typ && (n as Typ).IsReferencing == true;
            });

            i = env.Members.Count;
        }
    }

    public class ExeAnalyzer : SemanticAnalyzer
    {
        public Stack<Literal> Breaks;
        public Stack<Literal> Continues;
        public Env Env;
        public Actn Actn;
        public Token Seed;

        [DebuggerNonUserCode]
        public NspAnalyzer Above { get { return Above_ as NspAnalyzer; } }

        public ExeAnalyzer(Token seed, NspAnalyzer above)
            : base(above)
        {
            Breaks = new Stack<Literal>();
            Continues = new Stack<Literal>();
            Seed = seed;
        }

        override public void Analyze()
        {
            Breaks.Clear();
            Continues.Clear();

            Actn = FindUpTypeIs<ActnAnalyzer>().Actn;
            Env = Actn.Env;
            Actn.Exes.Add(Require<IExecutable>(Seed));
        }

        public TR Require<TR>(Token t)
        {
            object s; if ((s = Gate(t)) is TR) { return (TR)s; }
            throw new SyntaxError("Require :" + typeof(TR).Name, t);
        }

        public object Gate(Token t)
        {
            object u = null;
            switch (t.Group)
            {
                case "Prior":    /**/ u = Gate(t.Follows[0]); break;
                //case "Factor":   /**/ u = Factor(t); break;
                case "Num":   /**/ u = Num(t); break;
                case "Str":   /**/ u = Str(t); break;
                case "Bol":   /**/ u = Bol(t); break;
                case "Id":   /**/ u = Id(t); break;
                case "Expr":     /**/ u = Expression(t); break;
                case "If":       /**/ u = If(t); break;
                case "While":    /**/ u = While(t); break;
                case "TypeSpec2":    /**/ u = TypeSpec(t); break;
                case "Typ":   /**/ u = DefineVariable(t); break;
                default:
                    throw new SyntaxError(@"Could not process the sentence: " + t.Group, t);
            }
            return u;
        }

        public object Expression(Token t)
        {
            object u = null;
            switch (t.Value)
            {
                case "=":
                case "<-":  /**/ u = AssignAyzr(t, t.Second, t.First); break;
                case "->":  /**/ u = AssignAyzr(t, t.First, t.Second); break;
                case ":":   /**/ u = DefineVariable(t); break;
                case "(":   /**/ u = CallFunc(t); break;
                case "[":   /**/ u = Bracket(t); break;
                case "{":   /**/ u = Curly(t); break;
                case ".":   /**/ u = Dot(t); break;
                case ";":   /**/ u = new DoNothing(); break;
                case "+":
                case "-":
                case "*":
                case "/":
                case "%":

                case "and":
                case "or":
                case "xor":

                case "==":
                case "!=":
                case "<":
                case ">":
                case "<=":
                case ">=":
                    u = Calc(t); break;

                default:
                    throw new InternalError(@"The operator is not supported: " + t.Value, t);
            }
            return u;
        }

        public object Num(Token t)
        {
            return new Literal(int.Parse(t.Value), Env.FindOrNewRefType(typeof(int)));
        }
                        
        public object Str(Token t)
        {
            return new Literal(t.Value.Substring(1, t.Value.Length - 2), Env.FindOrNewRefType(typeof(string)));
        }

        public object Bol(Token t)
        {
            return new Literal(t.Value == "true", Env.FindOrNewRefType(typeof(bool)));
        }

        public object Id(Token t)
        {
            if (t.Value == "break")
            {
                if (Breaks.Count == 0)
                { throw new SyntaxError("Can not place break", t); }
                return new BranchInfo(Breaks.Peek());
            }

            if (t.Value == "continue")
            {
                if (Continues.Count == 0)
                { throw new SyntaxError("Can not place continue", t); }
                return new BranchInfo(Continues.Peek());
            }

            if (string.IsNullOrEmpty(t.ValueImplicit)) { t.ValueImplicit = t.Value; }

            return Above.FindUp(t) as object ?? new AnId(t);
        }

        public object AssignAyzr(Token assign, Token give, Token take)
        {
            IValuable gv2 = Require<IValuable>(give);
            object tu = Gate(take);

            if ((tu.GetType() == typeof(AnId)) == false
                && (tu is IAssignable) == false
                && (tu is ArrayAccessInfo) == false
                )
            {
                throw new SyntaxError("Can not assign to: " + take.Value, take);
            }

            if (tu is ArrayAccessInfo)
            {
                return new ArraySetInfo(tu as ArrayAccessInfo, gv2);
            }
            if (tu.GetType() == typeof(AnId))
            {
                tu = Actn.NewVar((tu as AnId).Seed.Value, gv2.Typ);
            }

            return new Assign(gv2, tu as IVariable);
        }

        public object TypeSpec(Token t)
        {
            return Id(t);
        }

        public object DefineVariable(Token t)
        {
            Debug.Assert(t.First != null);
            Debug.Assert(t.First.Group == "Id");
            Debug.Assert(t.Follows != null);
            Debug.Assert(t.Follows.Length == 1);

            AnId id = Require<AnId>(t.First);
            Typ ty = Above.RequireTyp(t.Follows[0]);

            return Actn.NewVar(id.Seed.Value, ty);
        }

        public object Calc(Token t)
        {
            IValuable lv, rv;

            lv = Require<IValuable>(t.First);
            rv = Require<IValuable>(t.Second);
            Typ boolty = Env.FindOrNewRefType(typeof(bool));
            Typ intty = Env.FindOrNewRefType(typeof(int));
            Typ stringty = Env.FindOrNewRefType(typeof(string));

            string ope = t.Value;
            Typ tp = lv.Typ;
            if (tp.IsReferencingOf(typeof(int)))
            {
                CalcInfo c;
                switch (ope)
                {
                    case "+": c = new CalcInfo(new C[] { C.Add }, lv, rv, intty); break;
                    case "-": c = new CalcInfo(new C[] { C.Sub }, lv, rv, intty); break;
                    case "*": c = new CalcInfo(new C[] { C.Mul }, lv, rv, intty); break;
                    case "/": c = new CalcInfo(new C[] { C.Div }, lv, rv, intty); break;
                    case "%": c = new CalcInfo(new C[] { C.Rem }, lv, rv, intty); break;
                    case "==": c = new CalcInfo(new C[] { C.Ceq }, lv, rv, boolty); break;
                    case "!=": c = new CalcInfo(new C[] { C.Ceq, C.Neg }, lv, rv, boolty); break;
                    case "<": c = new CalcInfo(new C[] { C.Clt }, lv, rv, boolty); break;
                    case ">": c = new CalcInfo(new C[] { C.Cgt }, lv, rv, boolty); break;
                    case "<=": c = new CalcInfo(new C[] { C.Cgt, C.Neg }, lv, rv, boolty); break;
                    case ">=": c = new CalcInfo(new C[] { C.Clt, C.Neg }, lv, rv, boolty); break;
                    case "and": c = new CalcInfo(new C[] { C.And }, lv, rv, boolty); break;
                    case "or": c = new CalcInfo(new C[] { C.Or }, lv, rv, boolty); break;
                    case "xor": c = new CalcInfo(new C[] { C.Xor }, lv, rv, boolty); break;
                    default:
                        throw new SyntaxError("Can not use '" + ope + "'", t);
                }
                return c;
            }
            else if (tp.IsReferencingOf(typeof(bool)))
            {
                CalcInfo c;
                switch (ope)
                {
                    case "==": c = new CalcInfo(new C[] { C.Ceq }, lv, rv, boolty); break;
                    case "!=": c = new CalcInfo(new C[] { C.Ceq, C.Neg }, lv, rv, boolty); break;
                    case "and": c = new CalcInfo(new C[] { C.And }, lv, rv, boolty); break;
                    case "or": c = new CalcInfo(new C[] { C.Or }, lv, rv, boolty); break;
                    case "xor": c = new CalcInfo(new C[] { C.Xor }, lv, rv, boolty); break;
                    default:
                        throw new SyntaxError("Can not use '" + ope + "'", t);
                }
                return c;
            }
            else if (tp.IsReferencingOf(typeof(string)))
            {
                if (ope == "+")
                {
                    Fctn concat = stringty.FindActnOvld("Concat").GetActnOf(new Typ[] { stringty, stringty }, Actn) as Fctn;
                    return new CallFunction(concat, /* instance */ null, new IValuable[] { lv, rv }, /* isNewObj */ false);
                }
                else
                {
                    throw new SyntaxError("Can not use '" + ope + "'", t);
                }
            }
            else
            {
                throw new SyntaxError("Can not use '" + ope + "'", t);
            }
        }

        public object While(Token while_)
        {
            if (while_.Follows.Length < 2)
            {
                Token e = while_;
                throw new SyntaxError("No condition token or then token for while", e);
            }

            Token cond, do_;
            cond = while_.Follows[0];
            do_ = while_.Follows[1];

            string fix = Env.GetTempName();
            Literal dolbl, endlbl;

            dolbl = new Literal("do" + fix, null);
            endlbl = new Literal("endwhile" + fix, null);

            Breaks.Push(endlbl);
            Continues.Push(dolbl);

            IValuable condv = Require<IValuable>(cond);
            if (false == condv.Typ.IsReferencingOf(typeof(bool)))
            {
                throw new TypeError("condition expression was not bool type", cond);
            }
            List<IExecutable> lines = new List<IExecutable>();
            Array.ForEach<Token>(do_.Follows, delegate(Token f_) { lines.Add(Require<IExecutable>(f_)); });

            Continues.Pop();
            Breaks.Pop();

            return new WhileInfo(dolbl, endlbl, condv, lines.ToArray());
        }

        public object If(Token if_)
        {
            Token cond, then, else_;
            List<Token> elifs;
            IfInfo.Component ifthen;
            List<IfInfo.Component> elifthen = new List<IfInfo.Component>();
            IExecutable[] elsels = null;

            if (if_.Follows.Length < 2)
            {
                Token e = if_;
                throw new SyntaxError("No condition token or then token for if", e);
            }

            cond = if_.Follows[0];
            then = if_.Follows[1];

            Token f;
            elifs = new List<Token>();
            else_ = null;
            for (int i = 2; i < (if_.Follows.Length - 1); i++)
            {
                f = if_.Follows[i];
                if (f.Group == "Elif") { elifs.Add(f); continue; }
                if (f.Group == "Else") { else_ = f; continue; }
            }

            string fix = Env.GetTempName();
            List<IExecutable> lines = new List<IExecutable>();


            IValuable v = Require<IValuable>(cond);
            if (false == v.Typ.IsReferencingOf(typeof(bool)))
            {
                throw new TypeError("condition expression was not bool type", cond);
            }
            ifthen = new IfInfo.Component();
            ifthen.Condition = v;
            Array.ForEach<Token>(then.Follows, delegate(Token t_) { lines.Add(Require<IExecutable>(t_)); });
            ifthen.Lines = lines.ToArray();

            IValuable velif;
            IfInfo.Component elifc;
            elifs.ForEach(delegate(Token elif)
            {
                velif = Require<IValuable>(elif.Follows[0]);

                if (false == v.Typ.IsReferencingOf(typeof(bool)))
                {
                    throw new TypeError("condition expression was not bool type", elif);
                }
                elifc = new IfInfo.Component();
                elifc.Condition = velif;
                lines.Clear();
                Array.ForEach<Token>(elif.Follows[1].Follows,
                    delegate(Token l_) { lines.Add(Require<IExecutable>(l_)); });
                elifc.Lines = lines.ToArray();
                elifthen.Add(elifc);
            });

            if (else_ != null)
            {
                lines.Clear();
                Array.ForEach<Token>(else_.Follows, delegate(Token l_) { lines.Add(Require<IExecutable>(l_)); });
                elsels = lines.ToArray();
            }

            return new IfInfo(fix, ifthen, elifthen.ToArray(), elsels);
        }

        public object CallFunc(Token t)
        {
            List<Typ> argtyps = new List<Typ>();
            List<IValuable> argvals = new List<IValuable>();
            object first;
            Type firstty;

            // arguments
            if (t.Second.Follows.Length > 0)
            {
                Token[] args = t.Second.Follows;
                if ((args.Length % 2) != 1)
                {
                    throw new SyntaxError("The argument count must be odd", t);
                }
                for (int i = 1; i < args.Length; i += 2)
                {
                    if (args[i].Value != ",")
                    {
                        throw new SyntaxError("No comma is between arguments", t);
                    }
                }
                for (int i = 0; i < args.Length; i += 2)
                {
                    Token argt = args[i];
                    IValuable v;
                    v = Require<IValuable>(argt);
                    argvals.Add(v);
                    argtyps.Add(v.Typ);
                }
            }

            first = Gate(t.First);
            if (first == null)
            { throw new SyntaxError("It is not a function or constructor", t.First); }
            firstty = first.GetType();

            if (firstty != typeof(Member) && firstty != typeof(ActnOvld) && firstty != typeof(Typ))
            { throw new SyntaxError("It is not a function or constructor", t.First); }

            ActnOvld actovl = null;
            Member mbr = null;
            if (firstty == typeof(Member))
            {
                mbr = first as Member;
                if (mbr.Value.GetType() != typeof(ActnOvld))
                { throw new NotImplementedException(); }

                actovl = mbr.Value as ActnOvld;
            }
            if (firstty == typeof(ActnOvld))
            {
                actovl = first as ActnOvld;
            }
            bool isNewObj = false;
            if (firstty == typeof(Typ))
            {
                isNewObj = true;
                actovl = (first as Typ).FindActnOvld(Nana.IMRs.IMRGenerator.InstCons);
            }
            Debug.Assert(actovl != null);

            Actn sig = null;

            sig = actovl.GetActnOf(argtyps.ToArray(), Actn);
            if (sig == null) { throw new SyntaxError("It is not a member", t.First); }

            IValuable instance = mbr == null ? null : mbr.Instance;

            if (sig.GetType() == typeof(Actn))
            { return new CallAction(sig, instance, argvals.ToArray(), false /*:isNewObj*/); }

            if (sig.GetType() == typeof(Fctn))
            { return new CallFunction(sig as Fctn, instance, argvals.ToArray(), isNewObj); }

            throw new NotImplementedException();
        }

        public object Dot(Token t)
        {
            Debug.Assert(t.First != null);
            Debug.Assert(t.Second != null);

            object holder = Gate(t.First);
            object comb = null;

            if (holder.GetType() == typeof(Nsp))
            {
                Nsp nsp = holder as Nsp;
                Token sec = t.Second;
                if (sec.Group != "Id")
                { throw new SyntaxError("Specify type, func or variable name", sec); }
                sec.ValueImplicit = nsp.Name + "." + sec.Value;
                comb = Gate(sec);
                return comb;
            }

            Typ y = null;
            IValuable v = null;
            if (holder is IValuable)
            {
                v = holder as IValuable;
                y = v.Typ;
            }
            if (holder.GetType() == typeof(Typ))
            {
                y = holder as Typ;
            }
            if (y == null) { throw new SyntaxError("It has no member", t.First); }

            //TODO load funcs automaticaly
            //y.GetActions();

            INmd mbr = y.FindMemeber(t.Second.Value);

            if (mbr == null) { throw new SyntaxError("It is not a member", t.Second); }

            if (mbr is Prop) { return new CallPropInfo(mbr as Prop, v); };

            return new Member(t, mbr, v);
        }

        static public readonly Token Empty = new Token("(Empty)", "Empty");
        static public readonly Token Comma = new Token(",", "Factor");

        /// <summary>
        /// collect separated tokens in circumfixes by commas
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        static public Token[] Circumfixed(Token t)
        {
            Token[] fs;
            List<Token> fs2;
            List<Token> ls;
            string prev;

            fs = t != null && t.Follows != null
                    ? t.Follows
                    : new Token[0]
                    ;

            // separate ',,'(consecutive commas) into one token
            fs2 = new List<Token>();
            foreach (Token f in fs)
            {
                if (Regex.IsMatch(f.Value, @",,+") == false)
                { fs2.Add(f); continue; }

                foreach (char c in f.Value)
                { fs2.Add(Comma); }
            }

            // for first token collection
            prev = ",";
            // for last token collection. a sentinel
            fs2.Add(Comma);
            ls = new List<Token>();
            foreach (Token f in fs2)
            {
                if (f.Value == ",")
                {
                    // no token between commas
                    if (prev == ",") { ls.Add(Empty); }
                }
                else if (prev == ",")
                {
                    // there is a token after comma
                    ls.Add(f);
                }
                else
                {
                    throw new SyntaxError("bad separated form by comma", f);
                }
                prev = f.Value;
            }

            return ls.ToArray();
        }

        public object Bracket(Token t)
        {
            Token r;                                        // root of "[]" syntax
            List<Token> rs = new List<Token>();                                        // root of "[]" syntax
            Token[] cirfxd;                                 // contents in "[]"
            List<Token[]> cirfxdlst = new List<Token[]>();  // list of cirfxd

            // collect circumfixed
            r = t;
            while (r.Group == "Expr" && r.Value == "[")
            {
                if (r.First == null) { throw new SyntaxError("not specified identifier in front of '[]'", r); }
                rs.Add(r);
                cirfxd = Circumfixed(r.Second);
                cirfxdlst.Add(cirfxd);
                r = r.First;
            }

            // <contents are:>                          --> <meaning can be:>
            // all empty                                --> type
            // 1st has length, follows are all empty    --> instantiation
            // all has indices                          --> accessing array
            // (not above)                              --> error

            Predicate<Token>    /**/ tprd = delegate(Token t_) { return t_ == Empty; };
            Predicate<Token[]>  /**/ tsprd = delegate(Token[] ts_) { return Array.TrueForAll<Token>(ts_, tprd); };

            bool isFirstEmpty = tsprd(cirfxdlst[cirfxdlst.Count - 1]);
            bool isFollowEmpty = cirfxdlst.GetRange(0, cirfxdlst.Count - 1).TrueForAll(tsprd);

            // resolve the spec(ifier) that specifies meaning of "[]" syntax is type or array access
            object spec;
            Typ typ = null;
            IValuable val = null;
            spec = Gate(r);

            if (spec.GetType() == typeof(Typ))
            {
                typ = spec as Typ;
                for (int i = 0; i < rs.Count; ++i)
                { typ = Env.FindOrNewArrayTyp(typ, cirfxdlst[i].Length); }
            }
            else if (spec is IValuable)
            {
                val = spec as IValuable;
                if (val.Typ.IsVectorOrArray == false)
                { throw new SyntaxError("require an array value in front of '[]'", r); }
            }
            else
            {
                throw new SyntaxError("require an arrya type or value in front of '[]'", r);
            }

            // meaning: type
            if (typ != null && isFirstEmpty && isFollowEmpty)
            { return typ; }

            // meaning: instantiation
            if (typ != null)
            {
                if (isFirstEmpty)
                { throw new SyntaxError("not specified array lenght", r); }

                if (isFollowEmpty == false)
                { throw new SyntaxError("cannot specify lenght", r); }

                // get array length
                IValuable len;
                List<IValuable> lens = new List<IValuable>();
                foreach (Token c in cirfxdlst[cirfxdlst.Count - 1])
                {
                    len = Require<IValuable>(c);
                    Typ y = len.Typ;
                    if (y.IsReferencing == false || y.RefType != typeof(int))
                    { throw new TypeError("specified not int value to array lenght", c); }
                    lens.Add(len);
                }

                ArrayInstatiation ins;
                ins = new ArrayInstatiation(typ, lens.ToArray(), Env.GetTempName, Actn.NewVar);
                return ins;
            }

            // meaning: accessing array
            {
                ArrayAccessInfo acc = null;
                List<Token[]> rev = new List<Token[]>(cirfxdlst);
                rev.Reverse();
                foreach (Token[] cf_ in rev)
                {
                    IValuable idx;
                    List<IValuable> idcs = new List<IValuable>();
                    foreach (Token c in cf_)
                    {
                        idx = Require<IValuable>(c);
                        Typ y = idx.Typ;
                        if (y.IsReferencing == false || y.RefType != typeof(int))
                        { throw new TypeError("specified not int value to array lenght", c); }
                        idcs.Add(idx);
                    }
                    acc = new ArrayAccessInfo(val, idcs.ToArray());
                    val = acc;
                }
                return acc;
            }
        }

        public object Curly(Token t)
        {
            //System.Collections.Generic.List`1

            // get the contents
            Token spc = t.First;
            Token[] cts = t.Second.Follows;
            if (cts == null || cts.Length == 0)
            {
                throw new SyntaxError("No type parameter(s) for generic type", t);
            }
            if (cts.Length % 2 != 1)
            {
                throw new SyntaxError("Type parameter(s) is bad format", t);
            }

            List<Token> contents = new List<Token>();
            contents.Add(cts[0]);
            for (int i = 1; i < cts.Length; i += 2)
            {
                if (cts[i].Value != "," || cts[i].Group != "Id")
                {
                    throw new SyntaxError("Type parameter(s) is bad format", t);
                }
                contents.Add(cts[i + 1]);
            }

            Typ tp = null;
            {
                // get type or valuable
                object specsem = Gate(spc);
                if (specsem.GetType() != typeof(AnId))
                { throw new SyntaxError("Not a generic type", spc); }
                //  Generic type name consists of name, "`" and count of type parameter.
                Token ttt = (specsem as AnId).Seed;
                //ttt.Value += "`" + contents.Count.ToString();
                ttt.ValueImplicit += "`" + contents.Count.ToString();
                tp = Above.RequireTyp(ttt);
                //TODO  require style get
                //tp = Gate(ttt) as Typ2;
                if (tp == null)
                { throw new SyntaxError("Unkown generic type:" + ttt.ValueImplicit, ttt); }
            }

            // get type parameters
            List<Typ> tprms = new List<Typ>();
            {
                // get array length
                foreach (Token c in contents)
                {
                    Typ s = Above.RequireTyp(c);
                    if (s == null)
                    { throw new SyntaxError("specified not type in type parameter", c); }
                    tprms.Add(s);
                }
            }

            //  find instance type
            Typ typinst = Env.FindOrNewGenericTypInstance(tp, tprms.ToArray());
            return typinst;
        }

    }

}
