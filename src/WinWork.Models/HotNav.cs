using System.Collections.Generic;

namespace WinWork.Models;

public class HotNav
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IncludeFiles { get; set; } = true;
    public int? MaxDepth { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<HotNavRoot> Roots { get; set; } = new List<HotNavRoot>();
}
