namespace Technopark.Models;
public class ContestParticipation
{
    public int Id { get; set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int ContestId { get; set; }
    public Contest? Contest { get; set; }

    public DateTime ApplicationDate { get; set; } = DateTime.Today;

    public int? ResultId { get; set; }
    public ParticipationResult? Result { get; set; }

    public int? Place { get; set; }
}