/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */


using System;
using System.Collections.Generic;
using System.Text;

namespace Nana.Delegates
{
#if ! __MonoCS__
    public delegate void Action();
    public delegate void Action<T1, T2>(T1 p1, T2 p2);
    public delegate void Action<T1, T2, T3>(T1 p1, T2 p2, T3 p3);
    public delegate T Func<T>();
    public delegate TR Func<T1, TR>(T1 p1);
    public delegate TR Func<T1, T2, TR>(T1 p1, T2 p2);
    public delegate TR Func<T1, T2, T3, TR>(T1 p1, T2 p2, T3 p3);
    public delegate TR Func<T1, T2, T3, T4, TR>(T1 p1, T2 p2, T3 p3, T4 p4);
#endif

    public class Util
    {
        static public void NullAction() { }
        static public void NullAction<T>(T v) { }
        static public TR NullFunc<TR>() { return default(TR); }
        static public TR NullFunc<T1, TR>(T1 p1) { return default(TR); }

        static public bool NotNull(object o) { return o != null; }

        //static public Func<T2> PackF<T1, T2>(Func<T1, T2> f, T1 p) { return delegate() { return f(p); }; }

        //static public Action Chain(Action a1, Action a2) { return delegate() { a1(); a2(); }; }
    }
}
