namespace OSLearningSystem.Services;

using OSLearningSystem.Models;

public class MemoryManagementService
{
    // Симуляція розподілу суміжної пам'яті
    public (bool Success, string Message, List<MemoryBlock> UpdatedBlocks) Allocate(
        List<MemoryBlock> blocks, 
        MemoryProcessRequest request, 
        AllocationAlgorithm algorithm)
    {
        var updated = blocks.Select(b => b.Clone()).ToList();
        int targetIdx = -1;

        var freeBlocksWithIndex = updated
            .Select((block, index) => new { block, index })
            .Where(x => !x.block.IsAllocated && x.block.Size >= request.RequiredSize)
            .ToList();

        if (freeBlocksWithIndex.Count == 0)
        {
            return (false, $"Недостатньо суміжної пам'яті для процесу {request.ProcessName} ({request.RequiredSize} KB)!", updated);
        }

        switch (algorithm)
        {
            case AllocationAlgorithm.FirstFit:
                targetIdx = freeBlocksWithIndex.First().index;
                break;

            case AllocationAlgorithm.BestFit:
                targetIdx = freeBlocksWithIndex.OrderBy(x => x.block.Size).First().index;
                break;

            case AllocationAlgorithm.WorstFit:
                targetIdx = freeBlocksWithIndex.OrderByDescending(x => x.block.Size).First().index;
                break;
        }

        var chosen = updated[targetIdx];
        int originalSize = chosen.Size;
        int remainingSize = originalSize - request.RequiredSize;

        chosen.Size = request.RequiredSize;
        chosen.IsAllocated = true;
        chosen.ProcessName = request.ProcessName;
        chosen.Color = request.Color;

        if (remainingSize > 0)
        {
            var freeChunk = new MemoryBlock
            {
                Id = updated.Max(b => b.Id) + 1,
                StartAddress = chosen.StartAddress + request.RequiredSize,
                Size = remainingSize,
                IsAllocated = false,
                ProcessName = "Вільний блок",
                Color = "#10b981"
            };
            updated.Insert(targetIdx + 1, freeChunk);
        }

        return (true, $"Процес {request.ProcessName} успішно розміщено в блоці (Адреса: {chosen.StartAddress}, Розмір: {request.RequiredSize} KB). Залишок: {remainingSize} KB.", updated);
    }

    public List<MemoryBlock> Deallocate(List<MemoryBlock> blocks, int blockId)
    {
        var updated = blocks.Select(b => b.Clone()).ToList();
        var target = updated.FirstOrDefault(b => b.Id == blockId);
        if (target != null && target.IsAllocated)
        {
            target.IsAllocated = false;
            target.ProcessName = "Вільний блок";
            target.Color = "#10b981";
        }

        // Об'єднання сусідніх вільних блоків (Coalescing)
        var merged = new List<MemoryBlock>();
        foreach (var b in updated)
        {
            if (merged.Count > 0 && !merged.Last().IsAllocated && !b.IsAllocated)
            {
                merged.Last().Size += b.Size;
            }
            else
            {
                merged.Add(b);
            }
        }

        return merged;
    }

    // Симуляція заміщення сторінок пам'яті (FIFO / LRU)
    public PageReplacementResult SimulatePageReplacement(List<int> referenceString, int frameCount, string algorithm)
    {
        var result = new PageReplacementResult();
        var frames = new List<int?>(new int?[frameCount]);
        var queueFIFO = new Queue<int>();
        var lruHistory = new Dictionary<int, int>(); // page -> last used time

        for (int i = 0; i < referenceString.Count; i++)
        {
            int page = referenceString[i];
            var step = new PageReplacementStep
            {
                StepIndex = i + 1,
                PageRequested = page
            };

            if (frames.Contains(page))
            {
                // Hit
                result.TotalHits++;
                step.IsPageFault = false;
                step.Description = $"Попадання (Hit): Сторінка {page} вже в пам'яті";
                if (algorithm == "LRU")
                {
                    lruHistory[page] = i;
                }
            }
            else
            {
                // Page Fault
                result.TotalPageFaults++;
                step.IsPageFault = true;

                if (frames.Any(f => f == null))
                {
                    // Є вільний слот
                    int emptyIdx = frames.IndexOf(null);
                    frames[emptyIdx] = page;
                    if (algorithm == "FIFO") queueFIFO.Enqueue(page);
                    if (algorithm == "LRU") lruHistory[page] = i;
                    step.Description = $"Помилка сторінки (Page Fault): Розміщено у вільний фрейм #{emptyIdx + 1}";
                }
                else
                {
                    // Потрібно замінити
                    int victimPage = -1;
                    if (algorithm == "FIFO")
                    {
                        victimPage = queueFIFO.Dequeue();
                        queueFIFO.Enqueue(page);
                    }
                    else // LRU
                    {
                        victimPage = frames.Where(f => f.HasValue)
                                           .Select(f => f!.Value)
                                           .OrderBy(p => lruHistory.ContainsKey(p) ? lruHistory[p] : -1)
                                           .First();
                        lruHistory[page] = i;
                    }

                    int replaceIdx = frames.IndexOf(victimPage);
                    frames[replaceIdx] = page;
                    step.Description = $"Помилка сторінки: Замінено сторінку {victimPage} на {page} у фреймі #{replaceIdx + 1}";
                }
            }

            step.Frames = new List<int?>(frames);
            result.Steps.Add(step);
        }

        return result;
    }
}
