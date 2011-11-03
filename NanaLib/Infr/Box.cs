using System;
using System.Collections.Generic;
using System.Text;

namespace Nana.Infr
{
    public class Box<T> 
    { 
        public T Value;
        public Box(T value) { Value = value; }
        public Box() : this(default(T)) { }
    }
}
