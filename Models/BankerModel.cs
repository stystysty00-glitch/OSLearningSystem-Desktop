namespace OSLearningSystem.Models;

public class BankerProcess
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int[] Allocation { get; set; } = Array.Empty<int>();
    public int[] Max { get; set; } = Array.Empty<int>();
    public int[] Need { get; set; } = Array.Empty<int>();
    public bool IsFinished { get; set; }
}

public class BankerSimulationResult
{
    public bool IsSafe { get; set; }
    public List<string> SafeSequence { get; set; } = new();
    public List<string> ExecutionSteps { get; set; } = new();
    public int[] FinalAvailable { get; set; } = Array.Empty<int>();
}
