namespace Technopark.Models;
public class Contest
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Organizer { get; set; } = "";

    public int LevelId { get; set; }
    public ContestLevel? Level { get; set; }

    public DateTime Date { get; set; }

    public ICollection<ContestParticipation> Participations { get; set; } = [];
    public string DateDisplay => Date.ToString("dd.MM.yyyy");
}