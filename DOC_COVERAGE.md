# Documentation Coverage Report

## Overall Check Result

The English `README.md` provides **excellent coverage** of the implemented features in `MacroDotNet`. All currently supported tokens, diagnostic codes, and core technical specifications are accurately documented. The `TODO` section correctly identifies features that are currently in the planning phase and not yet implemented in the source code.

## Detailed Result Follows

### Supported Tokens
All 14 tokens found in `src/MacroDotNet.cs` are documented in `README.md`:
- **Core Tokens**: `$fieldName`, `$displayName`, `$typeName`, `$typeShortName`, `$typeBareName`, `$containerType`, `$static`, `$visibility`, `$initialValue`, `$typeArgs`, `$typeConstraints`, `$0`...`$9`.
- **Sugar Tokens**: `$inline`, `$noinline`.

### Diagnostics
All 5 diagnostic codes and their purposes are documented:
- `MACRO001`: Invalid target symbol.
- `MACRO002`: Missing `partial` declaration.
- `MACRO003`: Too many macro arguments.
- `MACRO004`: Generated code syntax errors.
- `MACRO_DEBUG`: Debug-only code preview.

### Technical Specifications
- **Attribute Injection**: Correctness of the injected `MacroAttribute` (internal, conditional on DEBUG) is confirmed.
- **Multiple Macros**: Processing order (declaration order) is documented and implemented.
- **Using Statements**: Logic for collecting and appending `using` directives is accurately described.
- **Performance**: Buffer allocation and indentation-related optimization tips align with the generator's implementation.

### Identified Discrepancies
- **`$displayName` Regex**: The `README.md` describes the prefix removal regex as `^[a-zA-Z]*_`. However, the actual implementation in `src/MacroDotNet.cs` uses `^[^_]*_`. This means the implementation is more permissive, removing any characters up to the first underscore, rather than just alphabetic characters.

### TODO Items
The following items in the `README.md` TODO section are confirmed to be **unimplemented** in the current codebase, making the documentation accurate regarding the project's roadmap:
- Positional `$typeArg` and `$typeConstraint` tokens.
- Configuration for DEBUG-only features.
- `[LoopMacro]` attribute.
- `[GlobalMacro]` concept.
