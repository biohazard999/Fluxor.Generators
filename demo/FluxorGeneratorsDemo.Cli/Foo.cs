using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Fluxor;

namespace FluxorGeneratorsDemo.Cli;

//[Dispatchable]
//public record Foo();

//[Dispatchable]
//public record Foo1(string Text, int Number)
//{
//    public Foo1() : this(string.Empty, default)
//    {
//    }
//}

//[Dispatchable]
//public record Foo2(string Text, int Number = 1)
//{
//}

//[Dispatchable]
//public record Foo3(string Text = "", int Number = 1)
//{
//}

//[Dispatchable]
//public record Foo4(string? Text = null, int Number = 1)
//{
//}


[Dispatchable]
public record Foo5(
    //Foo Text = Foo.Val1,
    //Foo? X = null,
    //string? Y = "",
    //string? Z = null,
    Foo? A = Foo.Val2,
    int? X = 6,
    int? Y = null
)
{
}

public enum Foo
{
    Val1, Val2
}
