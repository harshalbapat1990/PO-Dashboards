namespace DesignAutomation.AppCatalog.Model;

/// <summary>Raw section block text</summary>
public record ReadmeBlock
{
    public int Level { get; set; } = 1;

    public string Header { get; set; } = String.Empty;

    public List<string> Text { get; set; } = [];
}
