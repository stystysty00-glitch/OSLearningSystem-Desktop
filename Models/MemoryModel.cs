namespace OSLearningSystem.Models;

public class MemoryBlock
{
    public int Id { get; set; }
    public int StartAddress { get; set; }
    public int Size { get; set; }
    public bool IsAllocated { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string Color { get; set; } = "#10b981";

    public MemoryBlock Clone()
    {
        return new MemoryBlock
        {
            Id = Id,
            StartAddress = StartAddress,
            Size = Size,
            IsAllocated = IsAllocated,
            ProcessName = ProcessName,
            Color = Color
        };
    }
}

public class MemoryProcessRequest
{
    public string ProcessName { get; set; } = string.Empty;
    public int RequiredSize { get; set; }
    public string Color { get; set; } = "#3b82f6";
}

public enum AllocationAlgorithm
{
    FirstFit,
    BestFit,
    WorstFit
}

public class PageReplacementStep
{
    public int StepIndex { get; set; }
    public int PageRequested { get; set; }
    public List<int?> Frames { get; set; } = new();
    public bool IsPageFault { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class PageReplacementResult
{
    public List<PageReplacementStep> Steps { get; set; } = new();
    public int TotalPageFaults { get; set; }
    public int TotalHits { get; set; }
    public double HitRatio => TotalHits + TotalPageFaults > 0 
        ? Math.Round((double)TotalHits / (TotalHits + TotalPageFaults) * 100, 1) 
        : 0;
}
