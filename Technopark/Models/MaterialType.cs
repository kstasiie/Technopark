namespace Technopark.Models;
public class MaterialType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<ProjectMaterial> Materials { get; set; } = [];
}