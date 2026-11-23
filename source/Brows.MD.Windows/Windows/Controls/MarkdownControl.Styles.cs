using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Brows.Windows.Controls;

public sealed partial class MarkdownControl {
    private static void OnStyleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) {
        var control = (MarkdownControl)sender;
        if (args.NewValue is null) {
            control.SemanticStyles.Remove(args.Property);
        }
        else {
            control.SemanticStyles[args.Property] = args.NewValue;
        }
    }

    private static DependencyProperty RegisterStyle(string name, Type target) {
        return DependencyProperty.Register(name, typeof(Style), typeof(MarkdownControl), new PropertyMetadata(null, OnStyleChanged),
            value => value is null || ((Style)value).TargetType.IsAssignableFrom(target));
    }

    /// <summary>
    /// Identifies the <see cref="MarkdownDocumentStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownDocumentStyleProperty =
        RegisterStyle(nameof(MarkdownDocumentStyle), typeof(FlowDocument));

    /// <summary>
    /// Identifies the <see cref="MarkdownParagraphStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownParagraphStyleProperty =
        RegisterStyle(nameof(MarkdownParagraphStyle), typeof(Paragraph));

    /// <summary>
    /// Identifies the <see cref="MarkdownHeading1Style"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownHeading1StyleProperty =
        RegisterStyle(nameof(MarkdownHeading1Style), typeof(Paragraph));

    /// <summary>
    /// Identifies the <see cref="MarkdownHeading2Style"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownHeading2StyleProperty =
        RegisterStyle(nameof(MarkdownHeading2Style), typeof(Paragraph));

    /// <summary>
    /// Identifies the <see cref="MarkdownHeading3Style"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownHeading3StyleProperty =
        RegisterStyle(nameof(MarkdownHeading3Style), typeof(Paragraph));

    /// <summary>
    /// Identifies the <see cref="MarkdownHeading4Style"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownHeading4StyleProperty =
        RegisterStyle(nameof(MarkdownHeading4Style), typeof(Paragraph));

    /// <summary>
    /// Identifies the <see cref="MarkdownHeading5Style"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownHeading5StyleProperty =
        RegisterStyle(nameof(MarkdownHeading5Style), typeof(Paragraph));

    /// <summary>
    /// Identifies the <see cref="MarkdownHeading6Style"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownHeading6StyleProperty =
        RegisterStyle(nameof(MarkdownHeading6Style), typeof(Paragraph));

    /// <summary>
    /// Identifies the <see cref="MarkdownQuoteStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownQuoteStyleProperty =
        RegisterStyle(nameof(MarkdownQuoteStyle), typeof(Section));

    /// <summary>
    /// Identifies the <see cref="MarkdownCodeBlockStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownCodeBlockStyleProperty =
        RegisterStyle(nameof(MarkdownCodeBlockStyle), typeof(Paragraph));

    /// <summary>
    /// Identifies the <see cref="MarkdownThematicBreakStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownThematicBreakStyleProperty =
        RegisterStyle(nameof(MarkdownThematicBreakStyle), typeof(Paragraph));

    /// <summary>
    /// Identifies the <see cref="MarkdownOrderedListStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownOrderedListStyleProperty =
        RegisterStyle(nameof(MarkdownOrderedListStyle), typeof(List));

    /// <summary>
    /// Identifies the <see cref="MarkdownUnorderedListStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownUnorderedListStyleProperty =
        RegisterStyle(nameof(MarkdownUnorderedListStyle), typeof(List));

    /// <summary>
    /// Identifies the <see cref="MarkdownListItemStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownListItemStyleProperty =
        RegisterStyle(nameof(MarkdownListItemStyle), typeof(ListItem));

    /// <summary>
    /// Identifies the <see cref="MarkdownTableStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownTableStyleProperty =
        RegisterStyle(nameof(MarkdownTableStyle), typeof(Table));

    /// <summary>
    /// Identifies the <see cref="MarkdownTableHeaderRowStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownTableHeaderRowStyleProperty =
        RegisterStyle(nameof(MarkdownTableHeaderRowStyle), typeof(TableRow));

    /// <summary>
    /// Identifies the <see cref="MarkdownTableRowStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownTableRowStyleProperty =
        RegisterStyle(nameof(MarkdownTableRowStyle), typeof(TableRow));

    /// <summary>
    /// Identifies the <see cref="MarkdownTableHeaderCellStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownTableHeaderCellStyleProperty =
        RegisterStyle(nameof(MarkdownTableHeaderCellStyle), typeof(TableCell));

    /// <summary>
    /// Identifies the <see cref="MarkdownTableCellStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownTableCellStyleProperty =
        RegisterStyle(nameof(MarkdownTableCellStyle), typeof(TableCell));

    /// <summary>
    /// Identifies the <see cref="MarkdownInlineCodeStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownInlineCodeStyleProperty =
        RegisterStyle(nameof(MarkdownInlineCodeStyle), typeof(Run));

    /// <summary>
    /// Identifies the <see cref="MarkdownEmphasisStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownEmphasisStyleProperty =
        RegisterStyle(nameof(MarkdownEmphasisStyle), typeof(Italic));

    /// <summary>
    /// Identifies the <see cref="MarkdownStrongStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownStrongStyleProperty =
        RegisterStyle(nameof(MarkdownStrongStyle), typeof(Bold));

    /// <summary>
    /// Identifies the <see cref="MarkdownStrikethroughStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownStrikethroughStyleProperty =
        RegisterStyle(nameof(MarkdownStrikethroughStyle), typeof(Span));

    /// <summary>
    /// Identifies the <see cref="MarkdownLinkStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownLinkStyleProperty =
        RegisterStyle(nameof(MarkdownLinkStyle), typeof(Hyperlink));

    /// <summary>
    /// Identifies the <see cref="MarkdownImageStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownImageStyleProperty =
        RegisterStyle(nameof(MarkdownImageStyle), typeof(Image));

    /// <summary>
    /// Identifies the <see cref="MarkdownTaskMarkerStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownTaskMarkerStyleProperty =
        RegisterStyle(nameof(MarkdownTaskMarkerStyle), typeof(Run));

    /// <summary>
    /// Identifies the <see cref="MarkdownTextStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownTextStyleProperty =
        RegisterStyle(nameof(MarkdownTextStyle), typeof(Run));

    /// <summary>
    /// Identifies the <see cref="MarkdownHardBreakStyle"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty MarkdownHardBreakStyleProperty =
        RegisterStyle(nameof(MarkdownHardBreakStyle), typeof(LineBreak));

    /// <summary>
    /// Gets or sets the style for the FlowDocument representing document content.
    /// </summary>
    public Style MarkdownDocumentStyle {
        get => (Style)GetValue(MarkdownDocumentStyleProperty);
        set => SetValue(MarkdownDocumentStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Paragraph representing paragraph content.
    /// </summary>
    public Style MarkdownParagraphStyle {
        get => (Style)GetValue(MarkdownParagraphStyleProperty);
        set => SetValue(MarkdownParagraphStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Paragraph representing heading 1 content.
    /// </summary>
    public Style MarkdownHeading1Style {
        get => (Style)GetValue(MarkdownHeading1StyleProperty);
        set => SetValue(MarkdownHeading1StyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Paragraph representing heading 2 content.
    /// </summary>
    public Style MarkdownHeading2Style {
        get => (Style)GetValue(MarkdownHeading2StyleProperty);
        set => SetValue(MarkdownHeading2StyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Paragraph representing heading 3 content.
    /// </summary>
    public Style MarkdownHeading3Style {
        get => (Style)GetValue(MarkdownHeading3StyleProperty);
        set => SetValue(MarkdownHeading3StyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Paragraph representing heading 4 content.
    /// </summary>
    public Style MarkdownHeading4Style {
        get => (Style)GetValue(MarkdownHeading4StyleProperty);
        set => SetValue(MarkdownHeading4StyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Paragraph representing heading 5 content.
    /// </summary>
    public Style MarkdownHeading5Style {
        get => (Style)GetValue(MarkdownHeading5StyleProperty);
        set => SetValue(MarkdownHeading5StyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Paragraph representing heading 6 content.
    /// </summary>
    public Style MarkdownHeading6Style {
        get => (Style)GetValue(MarkdownHeading6StyleProperty);
        set => SetValue(MarkdownHeading6StyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Section representing quote content.
    /// </summary>
    public Style MarkdownQuoteStyle {
        get => (Style)GetValue(MarkdownQuoteStyleProperty);
        set => SetValue(MarkdownQuoteStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Paragraph representing code block content.
    /// </summary>
    public Style MarkdownCodeBlockStyle {
        get => (Style)GetValue(MarkdownCodeBlockStyleProperty);
        set => SetValue(MarkdownCodeBlockStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Paragraph representing thematic break content.
    /// </summary>
    public Style MarkdownThematicBreakStyle {
        get => (Style)GetValue(MarkdownThematicBreakStyleProperty);
        set => SetValue(MarkdownThematicBreakStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the List representing ordered list content.
    /// </summary>
    public Style MarkdownOrderedListStyle {
        get => (Style)GetValue(MarkdownOrderedListStyleProperty);
        set => SetValue(MarkdownOrderedListStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the List representing unordered list content.
    /// </summary>
    public Style MarkdownUnorderedListStyle {
        get => (Style)GetValue(MarkdownUnorderedListStyleProperty);
        set => SetValue(MarkdownUnorderedListStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the ListItem representing list item content.
    /// </summary>
    public Style MarkdownListItemStyle {
        get => (Style)GetValue(MarkdownListItemStyleProperty);
        set => SetValue(MarkdownListItemStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Table representing table content.
    /// </summary>
    public Style MarkdownTableStyle {
        get => (Style)GetValue(MarkdownTableStyleProperty);
        set => SetValue(MarkdownTableStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the TableRow representing table header row content.
    /// </summary>
    public Style MarkdownTableHeaderRowStyle {
        get => (Style)GetValue(MarkdownTableHeaderRowStyleProperty);
        set => SetValue(MarkdownTableHeaderRowStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the TableRow representing table row content.
    /// </summary>
    public Style MarkdownTableRowStyle {
        get => (Style)GetValue(MarkdownTableRowStyleProperty);
        set => SetValue(MarkdownTableRowStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the TableCell representing table header cell content.
    /// </summary>
    public Style MarkdownTableHeaderCellStyle {
        get => (Style)GetValue(MarkdownTableHeaderCellStyleProperty);
        set => SetValue(MarkdownTableHeaderCellStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the TableCell representing table cell content.
    /// </summary>
    public Style MarkdownTableCellStyle {
        get => (Style)GetValue(MarkdownTableCellStyleProperty);
        set => SetValue(MarkdownTableCellStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Run representing inline code content.
    /// </summary>
    public Style MarkdownInlineCodeStyle {
        get => (Style)GetValue(MarkdownInlineCodeStyleProperty);
        set => SetValue(MarkdownInlineCodeStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Italic representing emphasis content.
    /// </summary>
    public Style MarkdownEmphasisStyle {
        get => (Style)GetValue(MarkdownEmphasisStyleProperty);
        set => SetValue(MarkdownEmphasisStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Bold representing strong content.
    /// </summary>
    public Style MarkdownStrongStyle {
        get => (Style)GetValue(MarkdownStrongStyleProperty);
        set => SetValue(MarkdownStrongStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Span representing strikethrough content.
    /// </summary>
    public Style MarkdownStrikethroughStyle {
        get => (Style)GetValue(MarkdownStrikethroughStyleProperty);
        set => SetValue(MarkdownStrikethroughStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Hyperlink representing link content.
    /// </summary>
    public Style MarkdownLinkStyle {
        get => (Style)GetValue(MarkdownLinkStyleProperty);
        set => SetValue(MarkdownLinkStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Image representing image content.
    /// </summary>
    public Style MarkdownImageStyle {
        get => (Style)GetValue(MarkdownImageStyleProperty);
        set => SetValue(MarkdownImageStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Run representing task marker content.
    /// </summary>
    public Style MarkdownTaskMarkerStyle {
        get => (Style)GetValue(MarkdownTaskMarkerStyleProperty);
        set => SetValue(MarkdownTaskMarkerStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the Run representing text content.
    /// </summary>
    public Style MarkdownTextStyle {
        get => (Style)GetValue(MarkdownTextStyleProperty);
        set => SetValue(MarkdownTextStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the style for the LineBreak representing hard break content.
    /// </summary>
    public Style MarkdownHardBreakStyle {
        get => (Style)GetValue(MarkdownHardBreakStyleProperty);
        set => SetValue(MarkdownHardBreakStyleProperty, value);
    }
}
