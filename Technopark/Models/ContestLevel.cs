namespace Technopark.Models;
public class ContestLevel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<Contest> Contests { get; set; } = [];
}