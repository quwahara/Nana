/*
 * Copyright (C) 2011 Mitsuaki Kuwahara
 * Released under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Nana.Infr
{
    public class Tuple2<T1, T2>
    {
        public T1 F1; public T2 F2;
        public Tuple2() { }
        public Tuple2(T1 f1, T2 f2) { F1 = f1; F2 = f2; }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (null == obj) { return false; }
            if (obj.GetType() != typeof(Tuple2<T1, T2>)) { return false; }
            Tuple2<T1, T2> t = (Tuple2<T1, T2>)obj;
            if (null != F1) /**/ { if (false == F1.Equals(t.F1)) { return false; } }
            else            /**/ { if (null != t.F1) { return false; } }
            if (null != F2) /**/ { if (false == F2.Equals(t.F2)) { return false; } }
            else            /**/ { if (null != t.F2) { return false; } }
            return true;
        }

        public static bool operator ==(Tuple2<T1, T2> a, Tuple2<T1, T2> b)
        {
            if (null == (object)a && null == (object)b) { return true; }
            if (null == (object)a) { return false; }
            return a.Equals(b);
        }

        public static bool operator !=(Tuple2<T1, T2> a, Tuple2<T1, T2> b)
        {
            if (null == (object)a && null == (object)b) { return false; }
            if (null == (object)a) { return true; }
            return false == a.Equals(b);
        }

    }

    public class Tuple3<T1, T2, T3>
    {
        public Tuple3() { }
        public Tuple3(T1 f1, T2 f2, T3 f3) { F1 = f1; F2 = f2; F3 = f3; }
        public T1 F1; public T2 F2; public T3 F3;
    }

    public class Tuple4<T1, T2, T3, T4>
    {
        public Tuple4() { }
        public Tuple4(T1 f1, T2 f2, T3 f3, T4 f4) { F1 = f1; F2 = f2; F3 = f3; F4 = f4; }
        public T1 F1; public T2 F2; public T3 F3; public T4 F4;
    }
}
