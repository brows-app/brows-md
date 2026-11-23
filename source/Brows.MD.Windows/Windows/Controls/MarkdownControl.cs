using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Syntax;
using System;
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

    private readonly MarkdownImageCache Images = new();

    private FlowDocumentScrollViewer Viewer;
    private bool UpdatePending;
    private long Revision;
    private CancellationTokenSource RenderCancellation;

    private ResourceDictionary SemanticStyles => field ??= [];

    private static MarkdownDocument Parse(string source) {
        try {
            return Markdig.Markdown.Parse(source, Pipeline);
        }
        catch (ArgumentException) {
            /*
             * Markdig rejects input beyond its nesting limit. Keep the source readable during editing.
             */
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
                    Images.Update(null);
                    Viewer.Document = null;
                    continue;
                }
                var syntax = source.Length <= MarkdownRenderer.MaximumSourceLength
                    ? await Task.Run(() => Parse(source))
                    : null;
                if (revision != Revision) {
                    continue;
                }
                using (var cancellation = new CancellationTokenSource()) {
                    RenderCancellation = cancellation;
                    try {
                        var renderer = new MarkdownRenderer(
                            BaseUriHelper.GetBaseUri(this), MarkdownImageLoading, Images, cancellation.Token);
                        var document = await renderer.RenderAsync(syntax, source);
                        if (revision == Revision) {
                            Viewer.Document = document;
                            if (revision == Revision) {
                                Images.Update(renderer.ImageRequests);
                            }
                        }
                    }
                    catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
                        /*
                         * The next iteration reads the latest source and template.
                         */
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
        nameof(Markdown),
        typeof(string),
        typeof(MarkdownControl),
        new PropertyMetadata(string.Empty, (sender, _) =>
            ((MarkdownControl)sender).ScheduleUpdate()));

    /// <summary>
    /// Identifies the <see cref="MarkdownImageLoading"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownImageLoadingProperty = DependencyProperty.Register(
        nameof(MarkdownImageLoading),
        typeof(MarkdownImageLoading),
        typeof(MarkdownControl),
        new PropertyMetadata(MarkdownImageLoading.Disabled, (sender, _) => {
            var
            control = (MarkdownControl)sender;
            control.Images.Update(null);
            control.ScheduleUpdate();
        }),
        value => value is MarkdownImageLoading.Disabled or
                          MarkdownImageLoading.LocalFiles or
                          MarkdownImageLoading.LocalAndRemote);

    /// <summary>
    /// Gets or sets which image references the control can load. The default displays alternative text.
    /// </summary>
    public MarkdownImageLoading MarkdownImageLoading {
        get => (MarkdownImageLoading)GetValue(MarkdownImageLoadingProperty);
        set => SetValue(MarkdownImageLoadingProperty, value);
    }

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
        Images.Update(null);
        if (Viewer is not null) {
            Viewer.Document = null;
            Viewer.Resources.MergedDictionaries.Remove(SemanticStyles);
        }
        Viewer = GetTemplateChild(ViewerName) as FlowDocumentScrollViewer;
        Viewer?.Resources.MergedDictionaries.Add(SemanticStyles);
        ScheduleUpdate();
    }
}
