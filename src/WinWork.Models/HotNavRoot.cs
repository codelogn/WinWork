namespace WinWork.Models;

public class HotNavRoot
{
    public int Id { get; set; }
    public int HotNavId { get; set; }
    public string Path { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public virtual HotNav? HotNav { get; set; }
}
