namespace Technopark.Models;
public class ProjectMaterial
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int TypeId { get; set; }
    public MaterialType? Type { get; set; }

    public string Name { get; set; } = "";
    public string Link { get; set; } = "";
    public DateTime UploadDate { get; set; } = DateTime.Today;
}