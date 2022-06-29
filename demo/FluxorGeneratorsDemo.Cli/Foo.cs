using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Fluxor;

namespace FluxorGeneratorsDemo.Cli;

[Dispatchable]
public record Foo();

[Dispatchable]
public record Foo1(string Text, int Number)
{
    public Foo1() : this(string.Empty, default)
    {
    }
}
