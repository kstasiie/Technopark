namespace Technopark.Models;
public class ParticipationResult
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<ContestParticipation> Participations { get; set; } = [];
}