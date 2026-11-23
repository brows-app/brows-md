using Markdig;
using Markdig.Extensions.EmphasisExtras;
using System;
using Markdig.Syntax;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Brows.Windows.Controls;

/// <summary>
/// Displays Markdown as a selectable WPF flow document.
/// </summary>
[TemplatePart(Name = ViewerName, Type = typeof(FlowDocumentScrollViewer))]
public sealed partial class MarkdownControl : Control {
    static MarkdownControl() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MarkdownControl),
            new FrameworkPropertyMetadata(typeof(MarkdownControl)));
    }

    private const string ViewerName = "PART_Viewer";
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseTaskLists()
        .UseAutoLinks()
        .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
        .DisableHtml()
        .Build();

    private FlowDocumentScrollViewer Viewer;
    private bool UpdatePending;
    private long Revision;
    private CancellationTokenSource RenderCancellation;

    private ResourceDictionary SemanticStyles => field ??= new ResourceDictionary();

    private static MarkdownDocument Parse(string source) {
        try {
            return Markdig.Markdown.Parse(source, Pipeline);
        }
        catch (ArgumentException) {
            // Markdig rejects input beyond its nesting limit. Keep the source readable during editing.
            return null;
        }
    }

    private void ScheduleUpdate() {
        Revision++;
        RenderCancellation?.Cancel();
        if (Viewer is null || UpdatePending) {
            return;
        }
        UpdatePending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(UpdateDocument));
    }

    private async void UpdateDocument() {
        try {
            if (Viewer is null) {
                return;
            }
            long revision;
            do {
                revision = Revision;
                var source = Markdown;
                if (string.IsNullOrEmpty(source)) {
                    Viewer.Document = null;
                    continue;
                }
                var syntax = await Task.Run(() => Parse(source));
                if (revision != Revision) {
                    continue;
                }
                using (var cancellation = new CancellationTokenSource()) {
                    RenderCancellation = cancellation;
                    try {
                        var renderer = new MarkdownRenderer(BaseUriHelper.GetBaseUri(this), cancellation.Token);
                        var document = await renderer.RenderAsync(syntax, source);
                        if (revision == Revision) {
                            Viewer.Document = document;
                        }
                    }
                    catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
                        // The next iteration reads the latest source and template.
                    }
                    finally {
                        RenderCancellation = null;
                    }
                }
            } while (Viewer is not null && revision != Revision);
        }
        finally {
            UpdatePending = false;
        }
    }

    /// <summary>
    /// Identifies the <see cref="Markdown"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(
        nameof(Markdown), typeof(string), typeof(MarkdownControl),
        new PropertyMetadata(string.Empty, (sender, _) => ((MarkdownControl)sender).ScheduleUpdate()));

    /// <summary>
    /// Gets or sets the Markdown source. Null or empty input clears the viewer.
    /// </summary>
    public string Markdown {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    /// <inheritdoc/>
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        if (Viewer is not null) {
            Viewer.Document = null;
            Viewer.Resources.MergedDictionaries.Remove(SemanticStyles);
        }
        Viewer = GetTemplateChild(ViewerName) as FlowDocumentScrollViewer;
        Viewer?.Resources.MergedDictionaries.Add(SemanticStyles);
        ScheduleUpdate();
    }
}
