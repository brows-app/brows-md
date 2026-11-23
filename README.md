# Brows.MD.Windows

`MarkdownControl` displays Markdown in a selectable, scrollable WPF `FlowDocument`.
Reference `Brows.MD.Windows` from a WPF project targeting .NET Framework 4.6.2 or 4.8, or .NET 8 or 10 for Windows.

## Run the sample

From the repository root on Windows with the .NET 10 SDK, launch the Markdown notebook.

```powershell
dotnet run --project samples/Brows.MD.Windows.Sample -f net10.0-windows
```

Edit the field note in the left panel to see the preview update on the right. Drag the divider to resize the panels.
The sample's `Resources/NotebookTheme.xaml` shows how to style headings, quotations, tables, task markers, and inline code.

## Binding Markdown

Bind `Markdown` to a string property. An editor can also drive the preview directly.

```xml
<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:brows="http://schemas.brows.app/winfx/2026/xaml/presentation">
  <Grid.ColumnDefinitions>
    <ColumnDefinition />
    <ColumnDefinition />
  </Grid.ColumnDefinitions>
  <Grid.Resources>
    <Style x:Key="Title" TargetType="Paragraph">
      <Setter Property="FontSize" Value="32" />
      <Setter Property="FontWeight" Value="Bold" />
      <Setter Property="Margin" Value="0,12,0,6" />
    </Style>
  </Grid.Resources>
  <TextBox x:Name="Editor" AcceptsReturn="True" Text="# Hello" />
  <brows:MarkdownControl Grid.Column="1"
                         Markdown="{Binding Text, ElementName=Editor}"
                         MarkdownHeading1Style="{StaticResource Title}" />
</Grid>
```

The control coalesces edits, parses on a worker, and yields to the dispatcher during document construction.
The previous document stays visible until the latest revision is ready. A source change rebuilds the document.
Changing a semantic style updates the existing elements in place. The preview displays a size notice above 250,000 UTF-16 code
units, 2,000 Markdown blocks, or 8,000 total syntax nodes (including inlines).

## Markdown support

[Markdig](https://github.com/xoofx/markdig) handles CommonMark syntax and the enabled GitHub-style extensions
for pipe tables, task lists, autolinks, and strikethrough. The renderer creates native WPF elements directly.
HTML tags appear as text. Task markers display their checked state. Fenced code preserves its text and exposes
the language identifier through the paragraph's `Tag`. If the parser rejects excessive nesting, the preview
displays the original source.

Links expose their destination through `Hyperlink.NavigateUri`. Handle the bubbling `Hyperlink.RequestNavigate`
event on the control or its parent to choose how your application opens links.

The default `MarkdownImageLoading="Disabled"` displays alternative text and skips image I/O. Set the property
to `LocalFiles` to allow non-UNC file URIs or to `LocalAndRemote` to also allow UNC, HTTP, and HTTPS URIs.
Both enabled modes also allow compiled WPF resources under `pack://application:,,,/`. Loose pack content and
site-of-origin resources fall back to alternative text. Mapped drives and redirected paths can still reach
remote storage in `LocalFiles` mode.
Relative image paths resolve against the control's WPF base URI. For a file preview, set
`BaseUriHelper.BaseUriProperty` to the source file's absolute URI before assigning `Markdown`.
Enabled images display selectable alternative text while loading and keep it if the load fails.
HTTP and HTTPS image responses are limited to 16 MiB. Edits reuse loaded images while their resolved URI
remains in the preview. File changes in length or last-write time refresh the image in the background on the
next edit.
The default image style scales down to fit the available width with a maximum of 640 DIPs.

## Semantic styles

Each property accepts a WPF `Style` for the corresponding element. Set properties on an individual control
or in a shared style targeting `MarkdownControl`. The document inherits the control's font properties.
Set any of those properties in `MarkdownDocumentStyle` to override the inherited values.

A setter in `MarkdownTextStyle` applies directly to literal runs and takes precedence over values those runs
inherit from headings and other blocks. Code-block text inherits its formatting from `MarkdownCodeBlockStyle`.
Use `MarkdownParagraphStyle` to set prose formatting at the paragraph level.

| Dependency properties | Style target |
| --- | --- |
| `MarkdownDocumentStyle` | `FlowDocument` |
| `MarkdownHeading1Style` through `MarkdownHeading6Style`, `MarkdownParagraphStyle`, `MarkdownCodeBlockStyle`, `MarkdownThematicBreakStyle` | `Paragraph` |
| `MarkdownQuoteStyle` | `Section` |
| `MarkdownOrderedListStyle`, `MarkdownUnorderedListStyle` | `List` |
| `MarkdownListItemStyle` | `ListItem` |
| `MarkdownTableStyle` | `Table` |
| `MarkdownTableHeaderRowStyle`, `MarkdownTableRowStyle` | `TableRow` |
| `MarkdownTableHeaderCellStyle`, `MarkdownTableCellStyle` | `TableCell` |
| `MarkdownTextStyle`, `MarkdownInlineCodeStyle`, `MarkdownTaskMarkerStyle` | `Run` |
| `MarkdownStrongStyle`, `MarkdownEmphasisStyle`, `MarkdownStrikethroughStyle` | `Bold`, `Italic`, `Span`, respectively |
| `MarkdownLinkStyle`, `MarkdownImageStyle`, `MarkdownHardBreakStyle` | `Hyperlink`, `Image`, `LineBreak`, respectively |

Cells with explicit Markdown column alignment expose a WPF `TextAlignment` value through `TableCell.Tag`.
The default cell styles use it to align the column. Ordered lists starting at zero display from one,
the minimum starting index supported by the WPF `List` element.

A replacement control template can host the document in a `FlowDocumentScrollViewer` named `PART_Viewer`.

## Licenses

Brows.MD.Windows is distributed under the [MIT license](LICENSE). Its Markdown parser is Markdig,
copyright © 2016–2026 Alexandre Mutel. The repository and the Brows NuGet package include
[Markdig's BSD-2-Clause notice](third-party/Markdig/LICENSE.txt). Applications that redistribute
the Markdig binary should include that notice in their documentation or accompanying materials.
