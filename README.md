# SystemTextJsonMergePatch

RFC 7396 JSON Merge Patch implementation for ASP.NET Core using pure System.Text.Json.

Drop-in replacement for `Morcatko.AspNetCore.JsonMergePatch`.

## Features

- Pure `System.Text.Json` — zero Newtonsoft.Json dependency
- Full RFC 7396 compliance
- ASP.NET Core input formatter for `application/merge-patch+json`
- Nested object and array support
- `PatchBuilder<T>` with 4 Build overloads
- `DiffBuilder` for object diff comparison
- `ApplyTo` and `ApplyToT` for cross-type patching

## Quick Start

```csharp
// Register in Program.cs
builder.Services.AddMvcCore().AddJsonMergePatch();

// Build a patch from JSON
var patch = PatchBuilder<MyDto>.Build(jsonElement);

// Apply to target
var dto = new MyDto();
patch.ApplyTo(dto);

// Check which fields were provided
var fields = patch.Operations.Select(op => op.path.TrimStart('/'));
```

## Migration from Morcatko

1. Replace NuGet: `Morcatko.AspNetCore.JsonMergePatch.SystemText` -> `SystemTextJsonMergePatch`
2. Replace usings: `using Morcatko.AspNetCore.JsonMergePatch;` -> `using SystemTextJsonMergePatch;`
3. Replace registration: `.AddSystemTextJsonMergePatch()` -> `.AddJsonMergePatch()`
4. Remove FQN prefixes: `Morcatko.AspNetCore.JsonMergePatch.SystemText.Builders.PatchBuilder<T>` -> `PatchBuilder<T>`

## About

This project was created entirely by [Claude Code](https://claude.ai/code) (Anthropic Claude Opus 4.6), authored by [neardreams](https://github.com/neardreams).

## License

MIT
