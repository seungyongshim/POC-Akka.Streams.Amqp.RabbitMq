using System;

namespace Messages
{
    public class Hello
    {
        public Hello(string name) => Name = name;

        public string Name { get;}

        public override string ToString() => Name;
    }
}
