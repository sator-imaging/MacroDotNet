<div align="center">

# MacroDotNet

梦寐以求的 C# **编译时宏**

[![nuget](https://img.shields.io/nuget/vpre/MacroDotNet)](https://www.nuget.org/packages/MacroDotNet)
&nbsp;
[![🇯🇵](https://img.shields.io/badge/🇯🇵-日本語-789)](./README.ja.md)
[![🇨🇳](https://img.shields.io/badge/🇨🇳-简体中文-789)](./README.zh-CN.md)
[![🇺🇸](https://img.shields.io/badge/🇺🇸-English-789)](./README.md)
&nbsp;
[![🇯🇵](https://img.shields.io/badge/🇯🇵-詳説-green)](https://zenn.dev/sator_imaging/articles/0ac6bf76bafe2a)

*支持 Unity 2022.3.12+*

</div>


&nbsp;


- 不限于特定用例 — 拥有一个简单、通用的文本模板引擎。
- 支持各种标记 — `$fieldName`, `$displayName`, `$typeName`, `$inline` 等。
- 每个字段支持多个 `[Macro]` 模板 — 按声明顺序处理。





&nbsp;

# 🚀 快速入门

在 `partial` 类型内的字段上使用 `[Macro]`。

```cs
using MacroDotNet;

public partial class Example
{
    // 模板可用于多个字段
    private const string IncrementTemplate =
        "public $static $typeName Increment$displayName() => Interlocked.Increment(ref $fieldName);";

    [Macro(IncrementTemplate)] private static int _globalCounter;
    [Macro(IncrementTemplate)] private int _retry;
    [Macro(IncrementTemplate)] private int _retry2;
    [Macro(IncrementTemplate)] private int _retry3;
}
```


生成的成员将发射到相同的包含类型层次结构中。

```cs
// $-标记将被替换为对应的符号
Example.IncrementGlobalCounter();

// 可重用的 [Macro] 模板如你所想地生成方法
var ex = new Example();
ex.IncrementRetry();
ex.IncrementRetry2();
ex.IncrementRetry3();
```


## 高级用法

`[Macro]` 可以接受参数，以便为模板注入针对每个字段的逻辑。

```cs
// 可重用的通知接口 (零开销)
private const string NotifyTemplate = @"
    public $static void Set$displayName($typeName value)
    {
        // '$0' 将被 args[0] 替换
        $0

        if ($fieldName == value) return;
        $fieldName = value;
        NotifyChanged(""$fieldName"");
    }
    public $static void Set$displayNameWithoutNotify($typeName value)
    {
        $0

        if ($fieldName == value) return;
        $fieldName = value;
    }";

// 无参数: $0 将为空字符串
[Macro(NotifyTemplate)] private static int _foo;  // SetFoo, SetFooWithoutNotify (static)
[Macro(NotifyTemplate)] private int _bar;         // SetBar, SetBarWithoutNotify

// 带参数: 注入额外的验证 ($-标记也可用于参数中)
[Macro(NotifyTemplate, "if (value <= 0) throw new ArgumentOutOfRangeException(\"$fieldName\");")]
private int _valueMustBePositive;
```

```cs
Example.SetFoo(-1);
ex.SetBarWithoutNotify(-1);
ex.SetValueMustBePositive(-1);  // 错误
```





&nbsp;

# 支持的标记

| 标记 | 详情 |
| --- | --- |
| `$fieldName` | 原始字段名称。 |
| `$displayName` | 移除 `^[a-zA-Z]*_` 前缀，然后在需要时将首字母大写。<br/>例如: `_value` -> `Value`, `foo_map` -> `Map`。 |
| `$typeName` | 带有全名的字段类型（仅剥离结尾的 `?`）。<br/>例如: `List<string?>?` -> `global::System.Collections.Generic.List<string?>`。 |
| `$typeShortName` | 字段类型的本地名称（仅剥离结尾的 `?`）。<br/>例如: `List<string?>?` -> `List<string?>`。 |
| `$typeBareName` | 字段类型的本地裸名称（无命名空间、无泛型、无可空标记）。<br/>例如: `Dictionary<int, string?>?` -> `Dictionary`。 |
| `$containerType` | 包含类型的全名。使用声明的泛型参数 (`T`)，而非构造类型 (`<int>`)。<br/>例如: `global::MyNamespace.MyType<T>`。 |
| `$static` | 当字段为静态时为 `static`，否则为空。 |
| `$visibility` | 字段访问修饰符关键字（`public`, `private` 等）。 |
| `$initialValue` | 原始字段初始化文本，缺失时为 `(((default!)))`。<br/>例如: `new()`, `"Foo"`, `null`, `1`。 |
| `$typeArgs` | 泛型类型参数（非泛型类型为空）。<br/>例如: `Dictionary<int, string>` -> `<int, string>`。<br/>实际用途: `List<string>` 的 `IEnumerable$typeArgs` 变为 `IEnumerable<string>`。 |
| `$typeConstraints` | 来自字段类型泛型参数的约束子句 (`where ...`)。<br/>例如: `where T : class, IDisposable, new() where U : unmanaged`。 |
| `$0` ... `$9` | 来自 `[Macro(template, args: ...)]` 的索引宏参数。<br/>例如: `[Macro("public string X => \"$0-$1\";", "A", "B")]` -> `"A-B"`。<br/>实际用途: 重用一个模板并注入特定于字段的表达式或文本。 |

> 注意: 标记区分大小写。


## 糖衣标记

- `$inline`: 完全限定的 `[MethodImpl(AggressiveInlining)]` 特性。
- `$noinline`: 完全限定的 `[MethodImpl(NoInlining)]` 特性。

## 转义标记文本

当你希望在生成的字符串内容中包含字面标记文本（如 `$fieldName`）时，请对 `$` 使用 Unicode 转义。

```cs
[Macro(@"public string LiteralFieldToken => ""\u0024fieldName"";")]
private int _value;
```

这将生成一个返回 `"$fieldName"`（而非实际字段名）的成员。注意，`"^[Rr]egular ?[Ee]xpression$"` 或其他无法识别的 $-标记将保持原样（即在这种情况下不需要转义）。


## 如何保留 Using 语句

生成的输出首先写入内置的默认 `using` 语句，然后附加从类型声明文件中收集的文件级 `using` 指令。

如果你的 IDE 移除了未使用的 `using` 指令，请在一个未编译的方法中保持对所需符号的引用。

```cs
[System.Diagnostics.Conditional("NOT_COMPILED")]
private static void KeepUsings()
{
    _ = typeof(System.Threading.Interlocked);
}
```


## 性能提示

`MacroDotNet` 生成器根据第一个找到的 `[Macro]` 模板大小分配缓冲区，并将其重用于剩余带有宏标注的字段。为了避免不必要的缓冲区扩展，请将最大的模板放在类型声明的开头。

此外，移除前导空格可以优化内存占用。

```cs
internal static class MyMacroTemplates
{
    // 可读性更好但效率较低
    public const string MyTemplate = @"
        // 带有缩进的模板
        ";

    // 最适合 Unity 项目
    public const string MyTemplate =
@"// 无缩进的模板
";

    // 最适合最新的 .NET 环境
    public const string MyTemplate = """
        // 原始字符串字面量最高效且易读。
        """;
}
```





&nbsp;

# 诊断

- `MACRO001` (错误): 无效的目标符号（生成器仅支持命名类型）。
- `MACRO002` (错误): 使用 `[Macro]` 时，包含类型必须声明为 `partial`。
- `MACRO003` (错误): 宏参数过多（最多 10 个：`$0` 到 `$9`）。
- `MACRO004` (错误): 生成的代码包含语法错误（报告带有行/列和生成的源预览的 Roslyn 解析器错误）。
- `MACRO_DEBUG` (信息): 仅限调试的生成代码预览（在调试模式下构建且生成的代码无语法错误时发射）。





&nbsp;

# 🕹️ 技术规范

- 扫描目标是全类型的 (`TargetAttributeName == null`)，然后仅处理带有 `[Macro]` / `[MacroAttribute]` 的字段。
- 生成器在后期初始化期间注入此特性：
    - `namespace MacroDotNet { [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)] internal sealed class MacroAttribute : Attribute { ... } }`
- 单个字段上的多个 `[Macro]` 特性按顺序应用。
- 宏参数通过 `params string[] args` 传递；提供时将替换 `$0`..`$9`。
- 生成输出中的换行符被标准化为 `\n`。





&nbsp;

# TODO

- 针对泛型类型参数和参数的位置 $-标记。
    - `$typeArg0`..`9` 返回 `T`, `int`, `string?`（无尖括号）。
        - 可选地允许不包含 `<` 和 `>` 围栏的 `$typeArg*`，以便能够“升级”类型参数：
            - `Dictionary<int, string>` -> `MyClass<float, $typeArg*>`
            - 生成的代码: `MyClass<float, int, string>`
    - `$typeConstraint0`..`9` 返回 `T : ...`（无 `where` 关键字）。
    - > 查找 `$typeArg` 或 `$typeConstraint` 然后检查下一个字符: `s` 或 `0`..`9`。
- 针对仅限调试（DEBUG-only）功能的配置。
    - 禁用语法高亮 (`[StringSyntax(""C#-test"")]`)。
    - 禁用对生成代码的语法验证。
    - 禁用生成代码预览（Visual Studio 的解决方法，它总是对 `.g.cs` 显示“文件未找到”）。
    - > 为了性能，目前的 DEBUG 检测检查 `Compilation.Options.OptimizationLevel == OptimizationLevel.Debug` 而非检查 `DEBUG` 符号。
- `[LoopMacro]`: 标注在常量 `int` 字段上，将值用作循环计数。
    - 接受字符串参数 `beforeLoop`, `loopBody` 和 `afterLoop`。
    - 引入新的 $-标记 `$loopIndex`，它是基于 0 的循环计数器。任何常量数字的算术运算都将成为编译时常量，因此可以无开销地调整为基于 1 或其他基数。
- `[GlobalMacro]`: 只是一个想法。
    - 能够在全局命名空间中声明类或命名空间层次结构...？
