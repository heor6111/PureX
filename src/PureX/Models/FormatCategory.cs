using PureX.Core.Models;

namespace PureX.Models;

public class FormatCategory
{
    public string CategoryName { get; set; } = string.Empty;
    public List<FormatOption> Formats { get; set; } = new();
}
