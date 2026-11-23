using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Windows.Navigation;

namespace Brows.Windows.Controls;

[TestFixture]
[Apartment(ApartmentState.STA)]
internal sealed class MarkdownControlTest {
    private static FlowDocumentScrollViewer ApplyTemplate(MarkdownControl control) {
        control.BeginInit();
        control.EndInit();
        control.ApplyTemplate();
        return (FlowDocumentScrollViewer)control.Template.FindName("PART_Viewer", control);
    }

    private static void WaitUntil(Func<bool> ready) {
        var elapsed = Stopwatch.StartNew();
        while (!ready() && elapsed.Elapsed < TimeSpan.FromSeconds(10)) {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }
        Assert.That(ready(), Is.True, "Timed out waiting for a document update.");
    }

    [Test]
    public void Markdown_RendersNestedBlocksAndInlineSemantics() {
        var control = new MarkdownControl { Markdown =
            "# Title\n\n> 3. **bold** and [docs][id]\n\n~~~cs\na < b\n~~~\n\n[id]: https://example.org" };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document?.Blocks.Count == 3);
        var heading = (Paragraph)viewer.Document.Blocks.FirstBlock;
        var quote = (Section)heading.NextBlock;
        var list = (List)quote.Blocks.FirstBlock;
        var paragraph = (Paragraph)list.ListItems.FirstListItem.Blocks.FirstBlock;
        var link = (Hyperlink)paragraph.Inlines.LastInline;
        var code = (Paragraph)quote.NextBlock;
        using (Assert.EnterMultipleScope()) {
            Assert.That(heading.Style, Is.SameAs(control.MarkdownHeading1Style));
            Assert.That(heading.FontSize, Is.EqualTo(28));
            Assert.That(list.StartIndex, Is.EqualTo(3));
            Assert.That(list.MarkerStyle, Is.EqualTo(TextMarkerStyle.Decimal));
            Assert.That(paragraph.Inlines.FirstInline, Is.TypeOf<Bold>());
            Assert.That(link.NavigateUri.AbsoluteUri, Is.EqualTo("https://example.org/"));
            Assert.That(((Run)code.Inlines.FirstInline).Text, Is.EqualTo("a < b"));
        }
    }

    [Test]
    public void Markdown_RendersGithubTableAndTaskList() {
        var control = new MarkdownControl { Markdown =
            "| A | B |\n| --- | ---: |\n| ~~old~~ | `new` |\n\n- [x] done\n- [ ] next" };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document?.Blocks.Count == 2);
        var rows = ((Table)viewer.Document.Blocks.FirstBlock).RowGroups[0].Rows;
        var cell = (Paragraph)rows[1].Cells[0].Blocks.FirstBlock;
        var items = ((List)viewer.Document.Blocks.LastBlock).ListItems;
        using (Assert.EnterMultipleScope()) {
            Assert.That(rows[0].FontWeight, Is.EqualTo(FontWeights.Bold));
            Assert.That(rows[1].Cells[1].TextAlignment, Is.EqualTo(TextAlignment.Right));
            Assert.That(cell.Inlines.FirstInline.Style, Is.SameAs(control.MarkdownStrikethroughStyle));
            Assert.That(new TextRange(items.FirstListItem.ContentStart, items.FirstListItem.ContentEnd).Text,
                Does.Contain("☑"));
            Assert.That(new TextRange(items.LastListItem.ContentStart, items.LastListItem.ContentEnd).Text,
                Does.Contain("☐"));
        }
    }

    [Test]
    public void Markdown_EditDuringPendingUpdatePublishesOnlyLatestDocument() {
        var source = string.Join("\n", Enumerable.Repeat("- a **list item**", 5000));
        var control = new MarkdownControl { Markdown = source };
        var viewer = ApplyTemplate(control);
        var published = 0;
        var descriptor = DependencyPropertyDescriptor.FromProperty(
            FlowDocumentScrollViewer.DocumentProperty, typeof(FlowDocumentScrollViewer));
        void OnDocumentChanged(object sender, EventArgs e) {
            published++;
        }
        descriptor.AddValueChanged(viewer, OnDocumentChanged);
        try {
            control.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => control.Markdown = "latest"));
            WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph p && p.Inlines.FirstInline is Run r &&
                r.Text == "latest");
            Assert.That(published, Is.EqualTo(1));
        }
        finally {
            descriptor.RemoveValueChanged(viewer, OnDocumentChanged);
        }
    }

    [Test]
    public void MarkdownTableCellStyle_OverridesMarkdownAlignment() {
        var control = new MarkdownControl { Markdown = "| a |\n| ---: |\n| b |" };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Table);
        var table = (Table)viewer.Document.Blocks.FirstBlock;
        var cell = table.RowGroups[0].Rows[1].Cells[0];
        Assert.That(cell.TextAlignment, Is.EqualTo(TextAlignment.Right));
        var style = new Style(typeof(TableCell));
        style.Setters.Add(new Setter(TableCell.TextAlignmentProperty, TextAlignment.Center));
        control.MarkdownTableCellStyle = style;
        Assert.That(cell.TextAlignment, Is.EqualTo(TextAlignment.Center));
    }

    [Test]
    public void Markdown_ExcessiveNestingDisplaysSourceAndRecoversOnEdit() {
        var source = new string('>', 512) + " deep";
        var control = new MarkdownControl { Markdown = source };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph);
        var paragraph = (Paragraph)viewer.Document.Blocks.FirstBlock;
        Assert.That(((Run)paragraph.Inlines.FirstInline).Text, Is.EqualTo(source));
        control.Markdown = "*recovered*";
        WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph p && p.Inlines.FirstInline is Italic);
    }

    [Test]
    public void Markdown_FailedImageUsesAlternativeText() {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".gif");
        var control = new MarkdownControl { Markdown = $"![missing image]({new Uri(path).AbsoluteUri})" };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph);
        var paragraph = (Paragraph)viewer.Document.Blocks.FirstBlock;
        Assert.That(((Run)paragraph.Inlines.FirstInline).Text, Is.EqualTo("missing image"));
    }

    [Test]
    public void Markdown_ResolvesRelativeImageAndPreservesAlternativeText() {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".gif");
        var gif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==");
        File.WriteAllBytes(path, gif);
        try {
            var control = new MarkdownControl { Markdown = $"![an *icon*]({Path.GetFileName(path)})" };
            control.SetValue(BaseUriHelper.BaseUriProperty, new Uri(path));
            var viewer = ApplyTemplate(control);
            WaitUntil(() => viewer.Document?.Blocks.Count == 1);
            var line = (Paragraph)viewer.Document.Blocks.FirstBlock;
            var image = (Image)((InlineUIContainer)line.Inlines.FirstInline).Child;
            using (Assert.EnterMultipleScope()) {
                Assert.That(image.Source, Is.Not.Null);
                Assert.That(AutomationProperties.GetName(image), Is.EqualTo("an icon"));
                Assert.That(image.Style, Is.SameAs(control.MarkdownImageStyle));
            }
        }
        finally {
            File.Delete(path);
        }
    }

    [Test]
    public void Markdown_LatestEditWinsAndStyleChangesUpdateExistingHeading() {
        var control = new MarkdownControl { Markdown = "first" };
        var viewer = ApplyTemplate(control);
        control.Markdown = "second";
        control.Markdown = "## last";
        WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph p && p.Inlines.FirstInline is Run r &&
            r.Text == "last");
        var heading = (Paragraph)viewer.Document.Blocks.FirstBlock;
        var style = new Style(typeof(Paragraph));
        style.Setters.Add(new Setter(TextElement.FontSizeProperty, 33d));
        control.MarkdownHeading2Style = style;
        WaitUntil(() => heading.FontSize == 33);
        Assert.That(viewer.Document.Blocks.FirstBlock, Is.SameAs(heading));
        control.Markdown = null;
        WaitUntil(() => viewer.Document is null);
    }
}
