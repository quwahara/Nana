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
    }

    public class Tuple3<T1, T2, T3>
    {
        public Tuple3() { }
        public Tuple3(T1 f1, T2 f2, T3 f3) { F1 = f1; F2 = f2; F3 = f3; }
        public T1 F1; public T2 F2; public T3 F3;
    }
}
