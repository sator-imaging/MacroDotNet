#:package FUnit@*
#:package MacroDotNet@3.0.2
//                    ~~~~~ Push AFTER compiled generators are published

using MacroDotNet;

return FUnit.Run(args, describe =>
{
    describe("Basic Tests", it =>
    {
        it("MacroDotNet", () =>
        {
            var ex = new MacroExample();
            Must.BeEqual(1, ex.IncrementCounter());
        });
    });
});





public partial class MacroExample
{
    [Macro("public int Increment$displayName() => ++$fieldName;")]
    private int _counter;
}
