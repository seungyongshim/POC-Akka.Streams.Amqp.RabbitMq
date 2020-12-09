using System;
using System.Collections.Generic;
using System.Text;

namespace Messages
{
    public class World
    {
        public string Name { get; set; }

        public override string ToString() => "World " + Name;
    }
}
