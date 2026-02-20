namespace PureX.Core.Models;

public class FormatOption
{
    public string Extension { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}
