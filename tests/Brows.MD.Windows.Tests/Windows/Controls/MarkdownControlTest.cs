using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
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
        var style = new Style(typeof(Run));
        style.Setters.Add(new Setter(TextElement.FontSizeProperty, 21d));
        var control = new MarkdownControl {
            MarkdownTextStyle = style,
            Markdown = $"![missing image]({new Uri(path).AbsoluteUri})",
            MarkdownImageLoading = MarkdownImageLoading.LocalFiles
        };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph);
        var paragraph = (Paragraph)viewer.Document.Blocks.FirstBlock;
        Assert.That(new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text, Is.EqualTo("missing image"));
        Assert.That(paragraph.Inlines.FirstInline.FontSize, Is.EqualTo(21));
    }

    [Test]
    public void Markdown_ResolvesRelativeImageAndPreservesAlternativeText() {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".gif");
        var gif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==");
        File.WriteAllBytes(path, gif);
        try {
            var imageName = Path.GetFileName(path);
            var control = new MarkdownControl {
                Markdown = $"![an *icon*]({imageName}) ![]({imageName})",
                MarkdownImageLoading = MarkdownImageLoading.LocalFiles
            };
            control.SetValue(BaseUriHelper.BaseUriProperty, new Uri(path));
            var viewer = ApplyTemplate(control);
            var publishedWithPlaceholder = false;
            var descriptor = DependencyPropertyDescriptor.FromProperty(
                FlowDocumentScrollViewer.DocumentProperty, typeof(FlowDocumentScrollViewer));

            void OnDocumentChanged(object sender, EventArgs e) {
                publishedWithPlaceholder = viewer.Document?.Blocks.FirstBlock is Paragraph line &&
                    line.Inlines.FirstInline is Run { Text: "an icon" };
            }

            descriptor.AddValueChanged(viewer, OnDocumentChanged);
            try {
                WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph line &&
                    line.Inlines.LastInline is InlineUIContainer { Child: Image { Source: not null } });
            }
            finally {
                descriptor.RemoveValueChanged(viewer, OnDocumentChanged);
            }

            var paragraph = (Paragraph)viewer.Document.Blocks.FirstBlock;
            var image = (Image)((InlineUIContainer)paragraph.Inlines.FirstInline).Child;
            var untitledImage = (Image)((InlineUIContainer)paragraph.Inlines.LastInline).Child;
            control.Measure(new Size(800, 600));
            control.Arrange(new Rect(0, 0, 800, 600));
            control.UpdateLayout();
            using (Assert.EnterMultipleScope()) {
                Assert.That(publishedWithPlaceholder, Is.True);
                Assert.That(AutomationProperties.GetName(image), Is.EqualTo("an icon"));
                Assert.That(image.Style, Is.SameAs(control.MarkdownImageStyle));
                Assert.That(image.ActualWidth, Is.EqualTo(1).Within(0.01));
                Assert.That(image.ActualHeight, Is.EqualTo(1).Within(0.01));
                Assert.That(untitledImage.ToolTip, Is.Null);
            }
            ImageSource publishedSource = null;
            void CaptureEditedImage(object sender, EventArgs e) {
                var edited = (Paragraph)viewer.Document.Blocks.FirstBlock;
                publishedSource = ((edited.Inlines.FirstInline as InlineUIContainer)?.Child as Image)?.Source;
            }
            descriptor.AddValueChanged(viewer, CaptureEditedImage);
            try {
                control.Markdown += " edited";
                WaitUntil(() => !ReferenceEquals(paragraph, viewer.Document.Blocks.FirstBlock));
                Assert.That(publishedSource, Is.SameAs(image.Source));
            }
            finally {
                descriptor.RemoveValueChanged(viewer, CaptureEditedImage);
            }
            control.MarkdownImageLoading = MarkdownImageLoading.Disabled;
            WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph line && line.Inlines.FirstInline is Run);
            Assert.That(((Run)((Paragraph)viewer.Document.Blocks.FirstBlock).Inlines.FirstInline).Text,
                Is.EqualTo("an icon"));
            control.MarkdownImageLoading = MarkdownImageLoading.LocalFiles;
            WaitUntil(() => viewer.Document.Blocks.FirstBlock is Paragraph restored &&
                restored.Inlines.FirstInline is InlineUIContainer { Child: Image });
            File.Delete(path);
            control.Markdown += " again";
            WaitUntil(() => viewer.Document.Blocks.FirstBlock is Paragraph edited &&
                edited.Inlines.FirstInline is Run { Text: "an icon" });
        }
        finally {
            File.Delete(path);
        }
    }

    [Test]
    public void Markdown_DefaultImagePolicyDisplaysAlternativeTextForExistingFile() {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".gif");
        File.WriteAllBytes(path, Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw=="));
        try {
            var control = new MarkdownControl { Markdown = $"![icon]({new Uri(path).AbsoluteUri})" };
            var viewer = ApplyTemplate(control);
            WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph);
            var paragraph = (Paragraph)viewer.Document.Blocks.FirstBlock;
            Assert.That(paragraph.Inlines.FirstInline, Is.TypeOf<Run>().With.Property(nameof(Run.Text)).EqualTo("icon"));
        }
        finally {
            File.Delete(path);
        }
    }

    [TestCase("https://192.0.2.1/image.gif")]
    [TestCase("file://192.0.2.1/share/image.gif")]
    public void Markdown_LocalImagePolicyDisplaysAlternativeTextForRemoteUris(string address) {
        var control = new MarkdownControl {
            Markdown = $"![outside]({address})",
            MarkdownImageLoading = MarkdownImageLoading.LocalFiles
        };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph);
        var paragraph = (Paragraph)viewer.Document.Blocks.FirstBlock;
        Assert.That(paragraph.Inlines.FirstInline, Is.TypeOf<Run>().With.Property(nameof(Run.Text)).EqualTo("outside"));
    }

    [Test]
    public void Markdown_InlineRichChangelogRendersInFull() {
        var source = string.Concat(Enumerable.Repeat("Updated **component** with a [fix](docs).\n\n", 420));
        var control = new MarkdownControl { Markdown = source };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document is not null);
        Assert.That(viewer.Document.Blocks.Count, Is.EqualTo(420));
    }

    [TestCase(MarkdownImageLoading.LocalFiles)]
    [TestCase(MarkdownImageLoading.LocalAndRemote)]
    public void Markdown_LoadsRelativeApplicationResource(MarkdownImageLoading policy) {
        var control = new MarkdownControl { MarkdownImageLoading = policy };
        var baseUri = new Uri("pack://application:,,,/Brows.MD.Windows.Tests;component/Resources/Page.xaml");
        control.SetValue(BaseUriHelper.BaseUriProperty, baseUri);
        control.Markdown = "![packaged icon](Icon.gif)";
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph paragraph &&
            paragraph.Inlines.FirstInline is InlineUIContainer { Child: Image { Source.Width: 1 } });
    }

    [TestCase(true, TestName = "Markdown_RejectsOversizedDeclaredLengthBeforeReadingBody")]
    [TestCase(false, TestName = "Markdown_RejectsOversizedBodyWithoutDeclaredLength")]
    public void Markdown_RejectsOversizedDownloadAndKeepsAlternativeText(bool declareLength) {
        const int oversizedResponseLength = 16 * 1024 * 1024 + 1;
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var control = new MarkdownControl {
            MarkdownImageLoading = MarkdownImageLoading.LocalAndRemote,
            Markdown = $"![oversized image](http://127.0.0.1:{port}/icon.gif)"
        };
        try {
            var viewer = ApplyTemplate(control);
            WaitUntil(() => listener.Pending() && viewer.Document is not null);
            using var client = listener.AcceptTcpClient();
            using var stream = client.GetStream();
            stream.ReadTimeout = 5000;
            using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
            while (!string.IsNullOrEmpty(reader.ReadLine())) {
                // Consume the request headers before replying.
            }

            var contentLength = declareLength ? $"Content-Length: {oversizedResponseLength}\r\n" : "";
            var headers = Encoding.ASCII.GetBytes(
                $"HTTP/1.1 200 OK\r\nContent-Type: image/gif\r\n{contentLength}Connection: close\r\n\r\n");
            stream.Write(headers, 0, headers.Length);
            if (!declareLength) {
                stream.WriteTimeout = 5000;
                var chunk = new byte[64 * 1024];
                for (var remaining = oversizedResponseLength; remaining > 0; remaining -= chunk.Length) {
                    stream.Write(chunk, 0, Math.Min(remaining, chunk.Length));
                }
            }

            // An edit can retry the URI once the client rejects the still-open response.
            FlowDocument lastEdit = null;
            WaitUntil(() => {
                if (listener.Pending()) {
                    return true;
                }
                if (!ReferenceEquals(lastEdit, viewer.Document)) {
                    lastEdit = viewer.Document;
                    control.Markdown += ".";
                }
                return false;
            });
            var document = viewer.Document;
            Assert.That(new TextRange(document.ContentStart, document.ContentEnd).Text,
                Does.StartWith("oversized image"));
        }
        finally {
            control.MarkdownImageLoading = MarkdownImageLoading.Disabled;
            listener.Stop();
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void Markdown_PendingDownloadKeepsTextAndAllowsLaterImage(bool success) {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".gif");
        var gif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///ywAAAAAAQABAAACAUwAOw==");
        try {
            File.WriteAllBytes(path, gif);
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var control = new MarkdownControl {
                MarkdownImageLoading = MarkdownImageLoading.LocalAndRemote,
                Markdown = $"![pending](http://127.0.0.1:{port}/icon.gif) ![local]({new Uri(path).AbsoluteUri})"
            };
            var viewer = ApplyTemplate(control);
            WaitUntil(() => listener.Pending() && viewer.Document?.Blocks.FirstBlock is Paragraph line &&
                line.Inlines.LastInline is InlineUIContainer { Child: Image });
            using var client = listener.AcceptTcpClient();
            var first = viewer.Document;
            control.Markdown += " edited";
            WaitUntil(() => !ReferenceEquals(first, viewer.Document));
            var paragraph = (Paragraph)viewer.Document.Blocks.FirstBlock;
            Assert.That(listener.Pending(), Is.False);
            Assert.That(new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text, Does.StartWith("pending"));

            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
            stream.ReadTimeout = 5000;
            while (!string.IsNullOrEmpty(reader.ReadLine())) {
                // Consume the HTTP request headers before replying.
            }
            var status = success ? "200 OK" : "404 Not Found";
            var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Length: {gif.Length}\r\nConnection: close\r\n\r\n");
            stream.Write(headers, 0, headers.Length);
            stream.Write(gif, 0, gif.Length);
            stream.Flush();
            if (success) {
                WaitUntil(() => paragraph.Inlines.FirstInline is InlineUIContainer { Child: Image { Source.Width: 1 } });
            }
            else {
                // A retry after the failed response must keep the alternative text and issue a new request.
                FlowDocument lastEdit = null;
                WaitUntil(() => {
                    if (listener.Pending()) {
                        return true;
                    }
                    if (!ReferenceEquals(lastEdit, viewer.Document)) {
                        lastEdit = viewer.Document;
                        control.Markdown += ".";
                    }
                    return false;
                });
                Assert.That(new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text, Does.StartWith("pending"));
            }
            control.MarkdownImageLoading = MarkdownImageLoading.Disabled;
        }
        finally {
            listener.Stop();
            File.Delete(path);
        }
    }

    [Test]
    public void Markdown_DenseInlineParagraphShowsSizeNotice() {
        var control = new MarkdownControl { Markdown = string.Concat(Enumerable.Repeat("*a* ", 4000)) };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document is not null);
        Assert.That(new TextRange(viewer.Document.ContentStart, viewer.Document.ContentEnd).Text,
            Does.Contain("exceeds the preview size limit"));
    }

    [TestCase(251_000, false)]
    [TestCase(2100, true)]
    public void Markdown_LargeInputShowsSizeNoticeAndRecoversOnEdit(int length, bool manyBlocks) {
        var source = manyBlocks ? string.Concat(Enumerable.Repeat("A paragraph.\n\n", length)) : new string('a', length);
        var control = new MarkdownControl { Markdown = source };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document is not null);
        Assert.That(new TextRange(viewer.Document.ContentStart, viewer.Document.ContentEnd).Text,
            Does.Contain("exceeds the preview size limit"));
        control.Markdown = "# Short";
        WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph p &&
            p.Inlines.FirstInline is Run r && r.Text == "Short");
    }

    [Test]
    public void MarkdownTextStyle_StylesProseWhileCodeKeepsBlockFont() {
        var style = new Style(typeof(Run));
        style.Setters.Add(new Setter(TextElement.FontFamilyProperty, new FontFamily("Georgia")));
        var control = new MarkdownControl { Markdown = "Prose\n\n~~~\ncode\n~~~", MarkdownTextStyle = style };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document?.Blocks.Count == 2);
        var prose = (Paragraph)viewer.Document.Blocks.FirstBlock;
        var code = (Paragraph)prose.NextBlock;
        using (Assert.EnterMultipleScope()) {
            Assert.That(prose.Inlines.FirstInline.FontFamily.Source, Is.EqualTo("Georgia"));
            Assert.That(code.Inlines.FirstInline.FontFamily.Source, Is.EqualTo("Consolas"));
        }
    }

    [Test]
    public void MarkdownDocumentStyle_InheritsControlFontsAndCanOverrideThem() {
        var control = new MarkdownControl {
            Markdown = "Body",
            FontFamily = new FontFamily("Arial"),
            FontSize = 20,
            FontStyle = FontStyles.Italic,
            FontWeight = FontWeights.Bold,
            FontStretch = FontStretches.Expanded
        };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document is not null);
        var document = viewer.Document;
        using (Assert.EnterMultipleScope()) {
            Assert.That(document.FontFamily.Source, Is.EqualTo("Arial"));
            Assert.That(document.FontSize, Is.EqualTo(20));
            Assert.That(document.FontStyle, Is.EqualTo(FontStyles.Italic));
            Assert.That(document.FontWeight, Is.EqualTo(FontWeights.Bold));
            Assert.That(document.FontStretch, Is.EqualTo(FontStretches.Expanded));
        }

        var style = new Style(typeof(FlowDocument));
        style.Setters.Add(new Setter(FlowDocument.FontSizeProperty, 28d));
        control.MarkdownDocumentStyle = style;
        control.FontFamily = new FontFamily("Consolas");
        control.FontSize = 24;
        using (Assert.EnterMultipleScope()) {
            Assert.That(viewer.Document, Is.SameAs(document));
            Assert.That(document.FontFamily.Source, Is.EqualTo("Consolas"));
            Assert.That(document.FontSize, Is.EqualTo(28));
        }
    }

    [Test]
    public void Markdown_UntitledReferenceLinkHasNoTooltip() {
        var control = new MarkdownControl { Markdown = "[plain][r] [titled](https://example.org \"title\")\n\n[r]: https://example.org" };
        var viewer = ApplyTemplate(control);
        WaitUntil(() => viewer.Document?.Blocks.FirstBlock is Paragraph);
        var paragraph = (Paragraph)viewer.Document.Blocks.FirstBlock;
        using (Assert.EnterMultipleScope()) {
            Assert.That(((Hyperlink)paragraph.Inlines.FirstInline).ToolTip, Is.Null);
            Assert.That(((Hyperlink)paragraph.Inlines.LastInline).ToolTip, Is.EqualTo("title"));
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
