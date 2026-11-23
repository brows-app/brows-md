using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static Brows.Windows.Controls.MarkdownControl;
using MarkdownBlock = Markdig.Syntax.Block;
using MarkdownCell = Markdig.Extensions.Tables.TableCell;
using MarkdownInline = Markdig.Syntax.Inlines.Inline;
using MarkdownRow = Markdig.Extensions.Tables.TableRow;
using MarkdownTable = Markdig.Extensions.Tables.Table;
using Table = System.Windows.Documents.Table;
using TableCell = System.Windows.Documents.TableCell;
using TableRow = System.Windows.Documents.TableRow;
using WpfInline = System.Windows.Documents.Inline;

namespace Brows.Windows.Controls;

internal sealed class MarkdownRenderer {
    private static readonly DependencyProperty[] HeadingStyles = [
        MarkdownHeading1StyleProperty, MarkdownHeading2StyleProperty, MarkdownHeading3StyleProperty,
        MarkdownHeading4StyleProperty, MarkdownHeading5StyleProperty, MarkdownHeading6StyleProperty
    ];

    private readonly Uri BaseUri;
    private readonly CancellationToken Cancellation;
    private readonly Stopwatch Slice = Stopwatch.StartNew();

    private async Task YieldIfNeededAsync() {
        if (Slice.ElapsedMilliseconds >= 8) {
            await Dispatcher.Yield(DispatcherPriority.Background);
            Cancellation.ThrowIfCancellationRequested();
            Slice.Restart();
        }
    }

    private static T Styled<T>(T element, DependencyProperty style) where T : FrameworkContentElement {
        element.SetResourceReference(FrameworkContentElement.StyleProperty, style);
        return element;
    }

    private async Task AddBlocksAsync(ContainerBlock source, BlockCollection target) {
        foreach (var block in source) {
            await AddBlockAsync(block, target);
        }
    }

    private async Task<List> CreateListAsync(ListBlock source) {
        var list = Styled(new List(), source.IsOrdered ? MarkdownOrderedListStyleProperty : MarkdownUnorderedListStyleProperty);
        if (source.IsOrdered && int.TryParse(source.OrderedStart, out var start)) {
            list.StartIndex = Math.Max(1, start);
        }
        foreach (ListItemBlock item in source) {
            var entry = Styled(new ListItem(), MarkdownListItemStyleProperty);
            await YieldIfNeededAsync();
            await AddBlocksAsync(item, entry.Blocks);
            list.ListItems.Add(entry);
        }
        return list;
    }

    private async Task<Table> CreateTableAsync(MarkdownTable source) {
        var table = Styled(new Table(), MarkdownTableStyleProperty);
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        foreach (MarkdownRow row in source) {
            var target = Styled(new TableRow(), row.IsHeader ? MarkdownTableHeaderRowStyleProperty : MarkdownTableRowStyleProperty);
            group.Rows.Add(target);
            foreach (MarkdownCell cell in row) {
                var cellStyle = row.IsHeader ? MarkdownTableHeaderCellStyleProperty : MarkdownTableCellStyleProperty;
                var entry = Styled(new TableCell(), cellStyle);
                var column = target.Cells.Count;
                if (column < source.ColumnDefinitions.Count) {
                    switch (source.ColumnDefinitions[column].Alignment) {
                        case TableColumnAlign.Center:
                            entry.Tag = TextAlignment.Center;
                            break;
                        case TableColumnAlign.Right:
                            entry.Tag = TextAlignment.Right;
                            break;
                        case TableColumnAlign.Left:
                            entry.Tag = TextAlignment.Left;
                            break;
                    }
                }
                await AddBlocksAsync(cell, entry.Blocks);
                target.Cells.Add(entry);
            }
        }
        return table;
    }

    private async Task AddInlinesAsync(ContainerInline source, InlineCollection target) {
        for (var node = source?.FirstChild; node is not null; node = node.NextSibling) {
            await YieldIfNeededAsync();
            var inline = node is LinkInline { IsImage: true } image
                ? await CreateImageAsync(image)
                : CreateInline(node);
            if (node is ContainerInline children && inline is Span span) {
                await AddInlinesAsync(children, span.Inlines);
            }
            target.Add(inline);
        }
    }

    private Run Text(string value) {
        return Styled(new Run(value), MarkdownTextStyleProperty);
    }

    private WpfInline CreateInline(MarkdownInline node) {
        switch (node) {
            case LiteralInline literal:
                return Text(literal.Content.ToString());
            case HtmlEntityInline entity:
                return Text(entity.Transcoded.ToString());
            case CodeInline code:
                return Styled(new Run(code.Content), MarkdownInlineCodeStyleProperty);
            case LineBreakInline line:
                return line.IsHard ? Styled(new LineBreak(), MarkdownHardBreakStyleProperty) : Text(" ");
            case TaskList task:
                return Styled(new Run(task.Checked ? "☑" : "☐"), MarkdownTaskMarkerStyleProperty);
            case EmphasisInline emphasis:
                return emphasis switch {
                    { DelimiterChar: '~' } => Styled(new Span(), MarkdownStrikethroughStyleProperty),
                    { DelimiterCount: 2 } => Styled(new Bold(), MarkdownStrongStyleProperty),
                    _ => Styled(new Italic(), MarkdownEmphasisStyleProperty)
                };
            case LinkInline link:
                return CreateLink(link.GetDynamicUrl?.Invoke() ?? link.Url, link.Title);
            case AutolinkInline auto:
                var address = auto.IsEmail ? "mailto:" + auto.Url : auto.Url;
                var hyperlink = CreateLink(address, null);
                hyperlink.Inlines.Add(Text(auto.Url));
                return hyperlink;
            case ContainerInline:
                return new Span();
            default:
                return Text(node.ToString());
        }
    }

    private static Hyperlink CreateLink(string address, string title) {
        var link = Styled(new Hyperlink(), MarkdownLinkStyleProperty);
        link.ToolTip = title;
        if (Uri.TryCreate(address, UriKind.RelativeOrAbsolute, out var uri)) {
            link.NavigateUri = uri;
        }
        return link;
    }

    private static BitmapImage LoadFileImage(Uri uri) {
        using (var stream = File.OpenRead(uri.LocalPath)) {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
    }

    private async Task<WpfInline> CreateImageAsync(LinkInline link) {
        var url = link.GetDynamicUrl?.Invoke() ?? link.Url;
        var description = new StringBuilder();
        foreach (var child in link.Descendants()) {
            switch (child) {
                case LiteralInline literal:
                    description.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    description.Append(code.Content);
                    break;
                case HtmlEntityInline entity:
                    description.Append(entity.Transcoded.ToString());
                    break;
                case LineBreakInline:
                    description.Append(' ');
                    break;
                case AutolinkInline auto:
                    description.Append(auto.Url);
                    break;
            }
        }
        var alt = description.ToString();
        if (!Uri.TryCreate(BaseUri, url, out var uri)) {
            return Text(alt);
        }
        var image = new Image { ToolTip = link.Title ?? alt };
        image.SetResourceReference(FrameworkElement.StyleProperty, MarkdownImageStyleProperty);
        AutomationProperties.SetName(image, alt);
        var host = new InlineUIContainer(image);
        image.ImageFailed += (_, _) => host.Child = new TextBlock { Text = alt };
        try {
            if (uri.IsFile) {
                image.Source = await Task.Run(() => LoadFileImage(uri), Cancellation);
                Cancellation.ThrowIfCancellationRequested();
            }
            else {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.EndInit();
                image.Source = bitmap;
            }
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or FormatException or
            UnauthorizedAccessException or WebException or ArgumentException or COMException or SecurityException) {
            return Text(alt);
        }
        return host;
    }

    private async Task AddBlockAsync(MarkdownBlock source, BlockCollection target) {
        await YieldIfNeededAsync();
        switch (source) {
            case HeadingBlock heading:
                var title = Styled(new Paragraph(), HeadingStyles[heading.Level - 1]);
                await AddInlinesAsync(heading.Inline, title.Inlines);
                target.Add(title);
                break;
            case ParagraphBlock paragraph:
                var text = Styled(new Paragraph(), MarkdownParagraphStyleProperty);
                await AddInlinesAsync(paragraph.Inline, text.Inlines);
                target.Add(text);
                break;
            case CodeBlock code:
                var preformatted = Styled(new Paragraph(Text(code.Lines.ToString())), MarkdownCodeBlockStyleProperty);
                if (code is FencedCodeBlock fenced && !string.IsNullOrEmpty(fenced.Info)) {
                    preformatted.Tag = fenced.Info;
                }
                target.Add(preformatted);
                break;
            case ThematicBreakBlock:
                target.Add(Styled(new Paragraph(), MarkdownThematicBreakStyleProperty));
                break;
            case QuoteBlock quote:
                var section = Styled(new Section(), MarkdownQuoteStyleProperty);
                await AddBlocksAsync(quote, section.Blocks);
                target.Add(section);
                break;
            case ListBlock list:
                target.Add(await CreateListAsync(list));
                break;
            case MarkdownTable table:
                target.Add(await CreateTableAsync(table));
                break;
        }
    }

    internal MarkdownRenderer(Uri baseUri, CancellationToken cancellationToken) {
        BaseUri = baseUri;
        Cancellation = cancellationToken;
    }

    internal async Task<FlowDocument> RenderAsync(MarkdownDocument syntax, string source) {
        var document = Styled(new FlowDocument(), MarkdownDocumentStyleProperty);
        if (syntax is null) {
            document.Blocks.Add(Styled(new Paragraph(Text(source)), MarkdownParagraphStyleProperty));
        }
        else {
            await AddBlocksAsync(syntax, document.Blocks);
        }
        return document;
    }
}
