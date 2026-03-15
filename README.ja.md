<div align="center">

# MacroDotNet

夢にまで見た C# 用の**コンパイル時マクロ**

[![nuget](https://img.shields.io/nuget/vpre/MacroDotNet)](https://www.nuget.org/packages/MacroDotNet)
&nbsp;
[![🇯🇵](https://img.shields.io/badge/🇯🇵-日本語-789)](./README.ja.md)
[![🇨🇳](https://img.shields.io/badge/🇨🇳-简体中文-789)](./README.zh-CN.md)
[![🇺🇸](https://img.shields.io/badge/🇺🇸-English-789)](./README.md)
&nbsp;
[![🇯🇵](https://img.shields.io/badge/🇯🇵-詳説-green)](https://zenn.dev/sator_imaging/articles/0ac6bf76bafe2a)

*Unity 2022.3.12+ をサポートしています*

</div>


&nbsp;


- 特定のユースケースに限定されません — シンプルで汎用的なテキストテンプレートエンジンを備えています。
- 多様なトークンをサポート — `$fieldName`、`$displayName`、`$typeName`、`$inline` など。
- フィールドごとに複数の `[Macro]` テンプレートを指定可能 — 宣言順に処理されます。





&nbsp;

# 🚀 はじめに

`partial` 型の中にあるフィールドに対して `[Macro]` を使用します。

```cs
using MacroDotNet;

public partial class Example
{
    // テンプレートは複数のフィールドで再利用可能
    private const string IncrementTemplate =
        "public $static $typeName Increment$displayName() => Interlocked.Increment(ref $fieldName);";

    [Macro(IncrementTemplate)] private static int _globalCounter;
    [Macro(IncrementTemplate)] private int _retry;
    [Macro(IncrementTemplate)] private int _retry2;
    [Macro(IncrementTemplate)] private int _retry3;
}
```


生成されたメンバーは、同じ型階層内に出力されます。

```cs
// $-トークンは対応するシンボルに置き換えられます
Example.IncrementGlobalCounter();

// 再利用可能な [Macro] テンプレートによって、想像通りのメソッドが生成されます
var ex = new Example();
ex.IncrementRetry();
ex.IncrementRetry2();
ex.IncrementRetry3();
```


## 高度な使い方

`[Macro]` は引数を受け取ることができ、フィールドごとに異なるロジックをテンプレートに注入できます。

```cs
// 再利用可能な通知インターフェース (ゼロオーバーヘッド)
private const string NotifyTemplate = @"
    public $static void Set$displayName($typeName value)
    {
        // '$0' は args[0] に置き換えられます
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

// 引数なし: $0 は空文字列になります
[Macro(NotifyTemplate)] private static int _foo;  // SetFoo, SetFooWithoutNotify (static)
[Macro(NotifyTemplate)] private int _bar;         // SetBar, SetBarWithoutNotify

// 引数あり: 追加のバリデーションを注入 (引数内でも $-トークンが使用可能)
[Macro(NotifyTemplate, "if (value <= 0) throw new ArgumentOutOfRangeException(\"$fieldName\");")]
private int _valueMustBePositive;
```

```cs
Example.SetFoo(-1);
ex.SetBarWithoutNotify(-1);
ex.SetValueMustBePositive(-1);  // エラー
```





&nbsp;

# サポートされているトークン

| トークン | 詳細 |
| --- | --- |
| `$fieldName` | 生のフィールド名。 |
| `$displayName` | `^[a-zA-Z]*_` プレフィックスを削除し、必要に応じて先頭文字を大文字化します。<br/>例: `_value` -> `Value`、`foo_map` -> `Map`。 |
| `$typeName` | フィールドの完全修飾名（末尾の `?` のみ削除されます）。<br/>例: `List<string?>?` -> `global::System.Collections.Generic.List<string?>`。 |
| `$typeShortName` | フィールドのローカル名（末尾の `?` のみ削除されます）。<br/>例: `List<string?>?` -> `List<string?>`。 |
| `$typeBareName` | フィールドのローカルな素の名前（名前空間なし、ジェネリクスなし、Null許容マーカーなし）。<br/>例: `Dictionary<int, string?>?` -> `Dictionary`。 |
| `$containerType` | フィールドを保持する型の完全修飾名。構築された型 (`<int>`) ではなく、宣言された型パラメータ (`T`) を使用します。<br/>例: `global::MyNamespace.MyType<T>`。 |
| `$static` | フィールドが static の場合は `static`、それ以外の場合は空文字列。 |
| `$visibility` | フィールドのアクセシビリティ キーワード (`public`、`private` など)。 |
| `$initialValue` | フィールドの初期化テキストをそのまま。存在しない場合は `(((default!)))`。<br/>例: `new()`、`"Foo"`、`null`、`1`。 |
| `$typeArgs` | ジェネリック型引数（非ジェネリック型の場合は空）。<br/>例: `Dictionary<int, string>` -> `<int, string>`。<br/>実用例: `List<string>` に対して `IEnumerable$typeArgs` とすると `IEnumerable<string>` になります。 |
| `$typeConstraints` | フィールド型の型パラメータの制約節 (`where ...`)。<br/>例: `where T : class, IDisposable, new() where U : unmanaged`。 |
| `$0` ... `$9` | `[Macro(template, args: ...)]` から渡されるインデックス指定のマクロ引数。<br/>例: `[Macro("public string X => \"$0-$1\";", "A", "B")]` -> `"A-B"`。<br/>実用例: 1つのテンプレートを再利用し、フィールド固有の式やテキストを注入する。 |

> 注意: トークンはケースセンシティブ（大文字小文字を区別）です。


## 糖衣トークン (Sugar Tokens)

- `$inline`: 完全修飾された `[MethodImpl(AggressiveInlining)]` 属性。
- `$noinline`: 完全修飾された `[MethodImpl(NoInlining)]` 属性。

## トークンテキストのエスケープ

生成された文字列内で `$fieldName` のようなトークン文字列をそのまま出力したい場合は、`$` に対して Unicode エスケープを使用してください。

```cs
[Macro(@"public string LiteralFieldToken => ""\u0024fieldName"";")]
private int _value;
```

これにより、(`_value` ではなく) 文字列 `"$fieldName"` を返すメンバーが生成されます。なお、`"^[Rr]egular ?[Ee]xpression$"` のような認識されない $-トークンはそのまま残るため、そのようなケースでエスケープは不要です。


## using ステートメントの保持方法

生成された出力には、まず組み込みのデフォルトの `using` ステートメントが書き込まれ、次に型宣言ファイルから収集されたファイルレベルの `using` ディレクティブが追加されます。

IDE が未使用の `using` ディレクティブを削除してしまう場合は、コンパイルされないメソッド内で必要なシンボルを参照したままにしてください。

```cs
[System.Diagnostics.Conditional("NOT_COMPILED")]
private static void KeepUsings()
{
    _ = typeof(System.Threading.Interlocked);
}
```


## パフォーマンスのヒント

`MacroDotNet` ジェネレーターは、最初に見つかった `[Macro]` テンプレートのサイズに基づいてバッファを割り当て、それを残りのマクロ注釈付きフィールドで再利用します。不要なバッファ拡張を避けるために、最も大きなテンプレートを型宣言の最初に配置してください。

また、先頭のスペースを削除することでメモリフットプリントを最適化できます。

```cs
internal static class MyMacroTemplates
{
    // 可読性は高いが効率は落ちる
    public const string MyTemplate = @"
        // インデント付きのテンプレート
        ";

    // Unity プロジェクトに最適
    public const string MyTemplate =
@"// インデントなしのテンプレート
";

    // 最新の .NET 環境に最適
    public const string MyTemplate = """
        // 生文字列リテラルは最も効率的で可読性が高いです。
        """;
}
```





&nbsp;

# 診断 (Diagnostics)

- `MACRO001` (エラー): 無効なターゲットシンボル (ジェネレーターは名前付き型のみをサポートします)。
- `MACRO002` (エラー): `[Macro]` を使用する場合、保持する型は `partial` として宣言されている必要があります。
- `MACRO003` (エラー): マクロ引数が多すぎます (最大10個: `$0` から `$9`)。
- `MACRO004` (エラー): 生成されたコードに構文エラーが含まれています (行/列と生成されたソースのプレビューを含む Roslyn パーサーエラーを報告します)。
- `MACRO_DEBUG` (情報): デバッグ専用の生成コードプレビュー (Debug モードでビルドされ、生成されたコードに構文エラーがない場合に出力されます)。





&nbsp;

# 🕹️ 技術仕様

- ターゲットスキャンは型全体で行われ (`TargetAttributeName == null`)、`[Macro]` / `[MacroAttribute]` が付いたフィールドのみが処理されます。
- ジェネレーターは、ポスト初期化中にこの属性を注入します:
    - `namespace MacroDotNet { [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)] internal sealed class MacroAttribute : Attribute { ... } }`
- 1つのフィールドに複数の `[Macro]` 属性がある場合、順番に適用されます。
- マクロ引数は `params string[] args` 経由で渡されます。提供された場合、`$0`..`$9` が置き換えられます。
- 生成された出力では、改行コードは `\n` に正規化されます。





&nbsp;

# TODO

- ジェネリック型引数およびパラメータ用の位置指定 $-トークン。
    - `$typeArg0`..`9` は `T`、`int`、`string?` を返します（山括弧なし）。
        - オプションで、型パラメータを「アップグレード」できるように、`<` と `>` の囲みを含まない `$typeArg*` を許可する。
            - `Dictionary<int, string>` -> `MyClass<float, $typeArg*>`
            - 生成コード: `MyClass<float, int, string>`
    - `$typeConstraint0`..`9` は `T : ...` を返します（`where` キーワードなし）。
    - > `$typeArg` または `$typeConstraint` を探し、次の文字が `s` または `0`..`9` であるかを確認する。
- DEBUG 専用機能の構成。
    - 構文ハイライトを無効にする (`[StringSyntax(""C#-test"")]`)。
    - 生成されたコードの構文検証を無効にする。
    - 生成されたコードのプレビューを無効にする (Visual Studio で `.g.cs` に対して常に「ファイルが見つかりません」と表示される問題への回避策)。
    - > パフォーマンスのため、現在の DEBUG 検出は `DEBUG` シンボルのチェックではなく `Compilation.Options.OptimizationLevel == OptimizationLevel.Debug` をチェックしています。
- `[LoopMacro]`: 定数 `int` フィールドに注釈を付け、その値をループ回数として使用します。
    - `beforeLoop`、`loopBody`、`afterLoop` の文字列引数を取ります。
    - 0ベースのループカウンターである新しい $-トークン `$loopIndex` を導入します。定数の算術演算はコンパイル時定数になるため、オーバーヘッドなしで1ベースなどに調整可能です。
- `[GlobalMacro]`: アイデア段階。
    - グローバル名前空間にクラスや名前空間階層を宣言できるようにする...?
