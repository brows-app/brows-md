using System;
using System.IO;
using System.Windows;

namespace Brows;

internal sealed partial class MarkdownSampleWindow : Window {
    public MarkdownSampleWindow() {
        InitializeComponent();
        var note = new Uri("pack://application:,,,/Brows.MD.Windows.Sample;component/FieldNote.md");
        using (var reader = new StreamReader(Application.GetResourceStream(note).Stream)) {
            Editor.Text = reader.ReadToEnd();
        }
    }
}
