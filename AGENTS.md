# Repository guidance

This file applies to the entire repository. Read the relevant project README and
nearby implementation and tests before making changes.

## Where to start

[README.md](README.md) has a working XAML example and the public style reference.
The WPF library lives in [source/Brows.MD.Windows](source/Brows.MD.Windows).
The [sample application](samples/Brows.MD.Windows.Sample) pairs a live editor with a styled preview.
Its [notebook theme](samples/Brows.MD.Windows.Sample/Resources/NotebookTheme.xaml) demonstrates the public style properties.

| When changing | Start here |
| --- | --- |
| Markdown dialect or live updates | [MarkdownControl.cs](source/Brows.MD.Windows/Windows/Controls/MarkdownControl.cs) configures Markdig, coalesces edits, and publishes the latest document. |
| How syntax appears in WPF | [MarkdownRenderer.cs](source/Brows.MD.Windows/Windows/Controls/MarkdownRenderer.cs) maps syntax nodes to native document elements. Parsing runs on a worker. Document construction runs on the WPF dispatcher and yields between batches. |
| Styling or the control template | [MarkdownControl.Styles.cs](source/Brows.MD.Windows/Windows/Controls/MarkdownControl.Styles.cs) defines the semantic style properties. [Generic.xaml](source/Brows.MD.Windows/Themes/Generic.xaml) supplies their defaults and the `PART_Viewer` template part. Style changes update the displayed elements in place. |
| Control behavior tests | [MarkdownControlTest.cs](tests/Brows.MD.Windows.Tests/Windows/Controls/MarkdownControlTest.cs) exercises the public control on an STA thread. Its helpers apply the template and pump the dispatcher until a document update completes. |

## Build and test

From the repository root on Windows, use the .NET SDK selected by [global.json](global.json).
For a quick development run, `dotnet test -c Release -f net10.0-windows` builds and tests one target.
To validate all four target frameworks, run the solution with the same restore, build, and test sequence as CI.

```powershell
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-build --no-restore
```

Target frameworks and the `out/` artifacts directory are configured in
[Directory.Build.props](Directory.Build.props). Package versions live in
[Directory.Packages.props](Directory.Packages.props).

## Skills

The general conventions for this repository live in agent skills under
`.agents/skills/` and `.claude/skills/`. Use the skill that matches the task, and
follow it where it differs from older code or other guidance.

| Task | Skill |
| --- | --- |
| Writing or editing C#, `.csproj`, `.props`, `.targets`, or `Directory.Packages.props` | `brows-csharp` |
| Restoring, building, and choosing the validation scope | `brows-code-building` |
| Writing and running NUnit tests | `brows-code-testing` |
| Git status, staging, commits, branches, and worktrees | `brows-git` |
| Planning substantial changes under `agents/plans/` | `brows-code-plan` |
| Reviewing code and writing reports under `agents/reviews/` | `brows-code-review` |
| Fixing numbered review issues, including per-issue worktrees | `brows-code-fix` |
| Performance and SIMD analysis | `analyzing-dotnet-performance`, `vectorization` |
