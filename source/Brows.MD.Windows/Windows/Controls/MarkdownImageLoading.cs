namespace Brows.Windows.Controls;

/// <summary>
/// Specifies which Markdown image references a control may load.
/// </summary>
public enum MarkdownImageLoading {
    /// <summary>
    /// Displays alternative text for all images without accessing the referenced locations.
    /// </summary>
    Disabled,

    /// <summary>
    /// Allows compiled application resources and non-UNC file URIs. Mapped drives and redirected paths can still access remote storage.
    /// </summary>
    LocalFiles,

    /// <summary>
    /// Allows compiled application resources, file URIs (including UNC paths), and HTTP and HTTPS URIs.
    /// </summary>
    LocalAndRemote
}
