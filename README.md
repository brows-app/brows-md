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
Changing a semantic style updates the existing elements in place.

## Markdown support

[Markdig](https://github.com/xoofx/markdig) handles CommonMark syntax and the enabled GitHub-style extensions
for pipe tables, task lists, autolinks, and strikethrough. The renderer creates native WPF elements directly.
HTML tags appear as text. Task markers display their checked state. Fenced code preserves its text and exposes
the language identifier through the paragraph's `Tag`. If the parser rejects excessive nesting, the preview
displays the original source.

Links expose their destination through `Hyperlink.NavigateUri`. Handle the bubbling `Hyperlink.RequestNavigate`
event on the control or its parent to choose how your application opens links. Images load through WPF's bitmap
codecs and expose their alternative text to automation. Relative image paths resolve against the control's WPF
base URI. When previewing a file, set `BaseUriHelper.BaseUriProperty` on the control to the source file's absolute
URI before assigning `Markdown`. Image references can access local files and network locations.

## Semantic styles

Each property accepts a WPF `Style` for the corresponding element. Set properties on an individual control
or in a shared style targeting `MarkdownControl`.

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
