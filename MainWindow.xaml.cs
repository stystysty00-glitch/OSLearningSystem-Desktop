using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using OSLearningSystem.Models;
using OSLearningSystem.Services;

namespace OSLearningSystem;

public partial class MainWindow : Window
{
    private readonly CpuSchedulingService _cpuService = new();
    private readonly MemoryManagementService _memoryService = new();
    private readonly DeadlockService _deadlockService = new();
    private readonly QuizService _quizService = new();

    private ObservableCollection<ProcessItem> _processes = new();
    private List<MemoryBlock> _memoryBlocks = new();
    private List<BankerProcessItemViewModel> _bankerProcesses = new();
    private List<QuizQuestion> _quizQuestions = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        InitCpuModule();
        InitMemoryModule();
        InitBankerModule();
        InitQuizModule();
        LoadTheoryTopic(0);
    }

    // =========================================================================
    // 1. CPU SCHEDULING MODULE
    // =========================================================================
    private void InitCpuModule()
    {
        OnLoadTypicalProcesses(this, new RoutedEventArgs());
    }

    private void OnLoadTypicalProcesses(object sender, RoutedEventArgs e)
    {
        _processes = new ObservableCollection<ProcessItem>
        {
            new() { Id = 1, Name = "P1", ArrivalTime = 0, BurstTime = 5, Priority = 2, Color = "#2563EB" },
            new() { Id = 2, Name = "P2", ArrivalTime = 1, BurstTime = 3, Priority = 1, Color = "#059669" },
            new() { Id = 3, Name = "P3", ArrivalTime = 2, BurstTime = 8, Priority = 4, Color = "#D97706" },
            new() { Id = 4, Name = "P4", ArrivalTime = 3, BurstTime = 4, Priority = 3, Color = "#7C3AED" }
        };
        GridProcesses.ItemsSource = _processes;
        OnRunCpuSimulation(this, new RoutedEventArgs());
    }

    private void OnAddProcess(object sender, RoutedEventArgs e)
    {
        int newId = _processes.Count + 1;
        string[] colors = { "#2563EB", "#059669", "#D97706", "#7C3AED", "#EC4899", "#06B6D4" };
        string color = colors[(newId - 1) % colors.Length];

        _processes.Add(new ProcessItem
        {
            Id = newId,
            Name = $"P{newId}",
            ArrivalTime = newId > 1 ? _processes[newId - 2].ArrivalTime + 1 : 0,
            BurstTime = 4,
            Priority = 2,
            Color = color
        });
    }

    private void OnCpuAlgorithmChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PanelQuantum == null || TxtAlgoDesc == null) return;

        int idx = CbCpuAlgorithm.SelectedIndex;
        PanelQuantum.Visibility = idx == 3 ? Visibility.Visible : Visibility.Collapsed;

        TxtAlgoDesc.Text = idx switch
        {
            0 => "FCFS (First-Come, First-Served): процеси обслуговуються строго за порядком надходження. Невитісняючий алгоритм.",
            1 => "SJF (Shortest Job First): процес із найменшим Burst Time виконується першим. Мінімізує середній час очікування.",
            2 => "SRTF (Shortest Remaining Time First): витісняючий варіант SJF. Якщо прибуває процес із меншим залишковим часом, поточний витісняється.",
            3 => "Round Robin (RR): процеси виконуються почергово з фіксованим квантом часу q. Забезпечує справедливий розподіл CPU без голодування.",
            4 => "Priority: процеси виконуються згідно з їхніми пріоритетами (менше число = вищий пріоритет).",
            _ => string.Empty
        };
    }

    private void OnQuantumChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // auto-update if initialized
    }

    private void OnRunCpuSimulation(object sender, RoutedEventArgs e)
    {
        if (_processes.Count == 0) return;

        CpuAlgorithm algo = CbCpuAlgorithm.SelectedIndex switch
        {
            0 => CpuAlgorithm.FCFS,
            1 => CpuAlgorithm.SJF_NonPreemptive,
            2 => CpuAlgorithm.SJF_Preemptive,
            3 => CpuAlgorithm.RoundRobin,
            4 => CpuAlgorithm.Priority_NonPreemptive,
            _ => CpuAlgorithm.RoundRobin
        };

        int quantum = (int)SliderQuantum.Value;
        var cloneList = _processes.Select(p => p.Clone()).ToList();
        var result = _cpuService.Simulate(cloneList, algo, quantum);

        GridResults.ItemsSource = result.Processes;
        TxtAvgWT.Text = $"{result.AverageWaitingTime:F2} такти";
        TxtAvgTAT.Text = $"{result.AverageTurnaroundTime:F2} тактів";
        TxtTotalTime.Text = $"{result.TotalExecutionTime} тактів";
        TxtCpuUtil.Text = $"{result.CpuUtilization:F1} %";

        DrawGanttChart(result.GanttChart, result.TotalExecutionTime);
    }

    private void DrawGanttChart(List<GanttBlock> blocks, int totalTime)
    {
        GanttCanvas.Children.Clear();
        GanttAxisCanvas.Children.Clear();

        if (blocks.Count == 0 || totalTime <= 0) return;

        double availableWidth = 620; // default visible width
        double pixelsPerUnit = Math.Max(26, availableWidth / totalTime);
        double currentX = 10;

        // Draw Start 0 Marker
        AddAxisMarker(currentX, 0);

        foreach (var block in blocks)
        {
            double blockWidth = block.Duration * pixelsPerUnit;

            // Block Rectangle
            var rect = new Border
            {
                Width = blockWidth,
                Height = 46,
                Background = (SolidColorBrush)new BrushConverter().ConvertFromString(block.Color)!,
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4)
            };

            var label = new TextBlock
            {
                Text = $"{block.ProcessName}\n[{block.StartTime}-{block.EndTime}]",
                FontWeight = FontWeights.Bold,
                FontSize = 10,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            rect.Child = label;

            Canvas.SetLeft(rect, currentX);
            Canvas.SetTop(rect, 2);
            GanttCanvas.Children.Add(rect);

            currentX += blockWidth;
            AddAxisMarker(currentX, block.EndTime);
        }

        GanttCanvas.Width = currentX + 30;
        GanttAxisCanvas.Width = currentX + 30;
    }

    private void AddAxisMarker(double x, int time)
    {
        var line = new Line
        {
            X1 = x,
            Y1 = 0,
            X2 = x,
            Y2 = 8,
            Stroke = (SolidColorBrush)new BrushConverter().ConvertFromString("#64748B")!,
            StrokeThickness = 1.5
        };
        GanttAxisCanvas.Children.Add(line);

        var txt = new TextBlock
        {
            Text = time.ToString(),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#475569")!,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetLeft(txt, x - 10);
        Canvas.SetTop(txt, 9);
        GanttAxisCanvas.Children.Add(txt);
    }

    // =========================================================================
    // 2. MEMORY MANAGEMENT MODULE
    // =========================================================================
    private void InitMemoryModule()
    {
        OnResetMemory(this, new RoutedEventArgs());
        OnRunPagingSimulation(this, new RoutedEventArgs());
    }

    private void OnResetMemory(object sender, RoutedEventArgs e)
    {
        _memoryBlocks = new List<MemoryBlock>
        {
            new() { Id = 1, StartAddress = 0, Size = 150, IsAllocated = true, ProcessName = "ОС (Ядро)", Color = "#0F172A" },
            new() { Id = 2, StartAddress = 150, Size = 200, IsAllocated = false, ProcessName = "Вільний блок", Color = "#10B981" },
            new() { Id = 3, StartAddress = 350, Size = 100, IsAllocated = true, ProcessName = "Драйвери", Color = "#2563EB" },
            new() { Id = 4, StartAddress = 450, Size = 300, IsAllocated = false, ProcessName = "Вільний блок", Color = "#10B981" },
            new() { Id = 5, StartAddress = 750, Size = 274, IsAllocated = false, ProcessName = "Вільний блок", Color = "#10B981" }
        };
        RefreshMemoryUi();
        TxtMemStatus.Text = "Оперативну пам'ять (RAM 1024 КБ) скинуто до початкового стану.";
    }

    private void RefreshMemoryUi()
    {
        GridMemoryBlocks.ItemsSource = null;
        GridMemoryBlocks.ItemsSource = _memoryBlocks;
        DrawRamMap();
    }

    private void OnAllocateMemory(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtMemSize.Text, out int reqSize) || reqSize <= 0)
        {
            TxtMemStatus.Text = "Помилка: введіть коректний числовий розмір блоку!";
            return;
        }

        string pName = string.IsNullOrWhiteSpace(TxtMemProcName.Text) ? "P_New" : TxtMemProcName.Text.Trim();
        var algo = (AllocationAlgorithm)CbMemAlgo.SelectedIndex;

        var available = _memoryBlocks.Where(b => !b.IsAllocated && b.Size >= reqSize).ToList();
        if (!available.Any())
        {
            TxtMemStatus.Text = $"❌ Помилка: недостатньо суміжного простору для {reqSize} КБ!";
            return;
        }

        MemoryBlock? target = algo switch
        {
            AllocationAlgorithm.FirstFit => available.First(),
            AllocationAlgorithm.BestFit => available.OrderBy(b => b.Size).First(),
            AllocationAlgorithm.WorstFit => available.OrderByDescending(b => b.Size).First(),
            _ => available.First()
        };

        int idx = _memoryBlocks.IndexOf(target);
        int remaining = target.Size - reqSize;

        string[] pColors = { "#D97706", "#7C3AED", "#EC4899", "#0891B2", "#CA8A04" };
        string color = pColors[Math.Abs(pName.GetHashCode()) % pColors.Length];

        _memoryBlocks[idx] = new MemoryBlock
        {
            Id = target.Id,
            StartAddress = target.StartAddress,
            Size = reqSize,
            IsAllocated = true,
            ProcessName = pName,
            Color = color
        };

        if (remaining > 0)
        {
            _memoryBlocks.Insert(idx + 1, new MemoryBlock
            {
                Id = _memoryBlocks.Max(b => b.Id) + 1,
                StartAddress = target.StartAddress + reqSize,
                Size = remaining,
                IsAllocated = false,
                ProcessName = "Вільний блок",
                Color = "#10B981"
            });
        }

        TxtMemStatus.Text = $"✅ Успішно виділено {reqSize} КБ для «{pName}» за алгоритмом {algo}.";
        RefreshMemoryUi();
    }

    private void OnFreeMemoryBlock(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is MemoryBlock block && block.IsAllocated)
        {
            if (block.ProcessName.Contains("Ядро"))
            {
                TxtMemStatus.Text = "⚠️ Захищено: ядро ОС не може бути вивантажене з оперативної пам'яті!";
                return;
            }

            block.IsAllocated = false;
            block.ProcessName = "Вільний блок";
            block.Color = "#10B981";

            // Coalesce adjacent free blocks
            for (int i = 0; i < _memoryBlocks.Count - 1; i++)
            {
                if (!_memoryBlocks[i].IsAllocated && !_memoryBlocks[i + 1].IsAllocated)
                {
                    _memoryBlocks[i].Size += _memoryBlocks[i + 1].Size;
                    _memoryBlocks.RemoveAt(i + 1);
                    i--;
                }
            }

            TxtMemStatus.Text = $"Блок за адресою 0x{block.StartAddress:X4} успішно звільнено та об'єднано.";
            RefreshMemoryUi();
        }
    }

    private void DrawRamMap()
    {
        CanvasRamMap.Children.Clear();
        double canvasWidth = CanvasRamMap.ActualWidth > 100 ? CanvasRamMap.ActualWidth : 1150;
        int totalRam = 1024;
        double currentX = 0;

        foreach (var b in _memoryBlocks)
        {
            double w = ((double)b.Size / totalRam) * canvasWidth;
            var border = new Border
            {
                Width = Math.Max(2, w - 1),
                Height = 60,
                Background = (SolidColorBrush)new BrushConverter().ConvertFromString(b.Color)!,
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 1, 0)
            };

            var text = new TextBlock
            {
                Text = $"{b.ProcessName}\n{b.Size} KB",
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            border.Child = text;

            Canvas.SetLeft(border, currentX);
            Canvas.SetTop(border, 2);
            CanvasRamMap.Children.Add(border);

            currentX += w;
        }
    }

    // SUBTAB: PAGE REPLACEMENT
    private void OnRunPagingSimulation(object sender, RoutedEventArgs e)
    {
        string raw = TxtRefString.Text;
        int framesCount = CbFrameCount.SelectedIndex == 0 ? 3 : 4;
        bool isLru = CbPagingAlgo.SelectedIndex == 1;

        var pages = raw.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(s => int.TryParse(s, out int v) ? v : 0)
                       .ToList();

        if (pages.Count == 0) return;

        var table = new DataTable();
        table.Columns.Add("Крок", typeof(string));
        table.Columns.Add("Запит", typeof(string));
        for (int f = 1; f <= framesCount; f++) table.Columns.Add($"Кадр {f}", typeof(string));
        table.Columns.Add("Статус", typeof(string));

        var frames = new List<int>();
        var lruHistory = new List<int>();
        int faults = 0;
        int hits = 0;

        for (int i = 0; i < pages.Count; i++)
        {
            int p = pages[i];
            bool hit = frames.Contains(p);

            if (hit)
            {
                hits++;
                if (isLru)
                {
                    lruHistory.Remove(p);
                    lruHistory.Add(p);
                }
            }
            else
            {
                faults++;
                if (frames.Count < framesCount)
                {
                    frames.Add(p);
                }
                else
                {
                    if (isLru)
                    {
                        int victim = lruHistory.First();
                        lruHistory.RemoveAt(0);
                        int replaceIdx = frames.IndexOf(victim);
                        frames[replaceIdx] = p;
                    }
                    else
                    {
                        frames.RemoveAt(0);
                        frames.Add(p);
                    }
                }
                if (isLru) lruHistory.Add(p);
            }

            var row = table.NewRow();
            row["Крок"] = $"#{i + 1}";
            row["Запит"] = p.ToString();
            for (int f = 0; f < framesCount; f++)
            {
                row[$"Кадр {f + 1}"] = f < frames.Count ? frames[f].ToString() : "-";
            }
            row["Статус"] = hit ? "HIT (Влучання)" : "MISS (Помилка сторінки)";
            table.Rows.Add(row);
        }

        GridPagingSteps.ItemsSource = table.DefaultView;
        TxtPageFaults.Text = faults.ToString();
        TxtHits.Text = hits.ToString();
        double ratio = (hits + faults > 0) ? Math.Round((double)hits / (hits + faults) * 100, 1) : 0;
        TxtHitRatio.Text = $"{ratio:F1} %";
    }

    // =========================================================================
    // 3. DEADLOCKS & BANKER MODULE
    // =========================================================================
    public class BankerProcessItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string AllocationText { get; set; } = string.Empty;
        public string MaxText { get; set; } = string.Empty;
        public string NeedText { get; set; } = string.Empty;
    }

    private void InitBankerModule()
    {
        OnLoadBankerExample(this, new RoutedEventArgs());
    }

    private void OnLoadBankerExample(object sender, RoutedEventArgs e)
    {
        _bankerProcesses = new List<BankerProcessItemViewModel>
        {
            new() { Name = "P0", AllocationText = "[0, 1, 0]", MaxText = "[7, 5, 3]", NeedText = "[7, 4, 3]" },
            new() { Name = "P1", AllocationText = "[2, 0, 0]", MaxText = "[3, 2, 2]", NeedText = "[1, 2, 2]" },
            new() { Name = "P2", AllocationText = "[3, 0, 2]", MaxText = "[9, 0, 2]", NeedText = "[6, 0, 0]" },
            new() { Name = "P3", AllocationText = "[2, 1, 1]", MaxText = "[2, 2, 2]", NeedText = "[0, 1, 1]" },
            new() { Name = "P4", AllocationText = "[0, 0, 2]", MaxText = "[4, 3, 3]", NeedText = "[4, 3, 1]" },
        };
        GridBankerProcesses.ItemsSource = _bankerProcesses;
        TxtAvailableVector.Text = "Доступні ресурси (Available): A=3, B=3, C=2";
        OnRunBankerAlgorithm(this, new RoutedEventArgs());
    }

    private void OnRunBankerAlgorithm(object sender, RoutedEventArgs e)
    {
        int[,] alloc = { { 0, 1, 0 }, { 2, 0, 0 }, { 3, 0, 2 }, { 2, 1, 1 }, { 0, 0, 2 } };
        int[,] max = { { 7, 5, 3 }, { 3, 2, 2 }, { 9, 0, 2 }, { 2, 2, 2 }, { 4, 3, 3 } };
        int[] avail = { 3, 3, 2 };

        int n = 5;
        int m = 3;
        int[,] need = new int[n, m];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < m; j++)
                need[i, j] = max[i, j] - alloc[i, j];

        int[] work = (int[])avail.Clone();
        bool[] finish = new bool[n];
        var seq = new List<string>();
        var log = new List<string>
        {
            "Ініціалізація алгоритму Банкіра Дейкстри:",
            $"Вектор Work = [{work[0]}, {work[1]}, {work[2]}]",
            "Пошук процесу з Need[i] <= Work..."
        };

        int count = 0;
        while (count < n)
        {
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                if (!finish[i])
                {
                    bool canAlloc = true;
                    for (int j = 0; j < m; j++)
                    {
                        if (need[i, j] > work[j])
                        {
                            canAlloc = false;
                            break;
                        }
                    }

                    if (canAlloc)
                    {
                        log.Add($"Процес P{i}: Need=[{need[i, 0]},{need[i, 1]},{need[i, 2]}] <= Work=[{work[0]},{work[1]},{work[2]}]. Задовольняється!");
                        for (int j = 0; j < m; j++) work[j] += alloc[i, j];
                        log.Add($"  -> Виділені ресурси звільнено. Новий Work=[{work[0]},{work[1]},{work[2]}]");

                        seq.Add($"P{i}");
                        finish[i] = true;
                        found = true;
                        count++;
                    }
                }
            }

            if (!found) break;
        }

        bool isSafe = (count == n);
        TxtBankerLog.Text = string.Join(Environment.NewLine, log);

        if (isSafe)
        {
            BorderBankerStatus.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#DCFCE7")!;
            BorderBankerStatus.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#86EFAC")!;
            TxtBankerStatusTitle.Text = "✅ СИСТЕМА ПЕРЕБУВАЄ В БЕЗПЕЧНОМУ СТАНІ (SAFE STATE)!";
            TxtBankerStatusTitle.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#15803D")!;
            TxtSafeSequence.Text = $"Безпечна послідовність (Safe Sequence): < {string.Join(" → ", seq)} >";
        }
        else
        {
            BorderBankerStatus.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#FEE2E2")!;
            BorderBankerStatus.BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#FCA5A5")!;
            TxtBankerStatusTitle.Text = "❌ УВАГА: ЗАГРОЗА ТУПИКА (DEADLOCK DETECTED)!";
            TxtBankerStatusTitle.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#B91C1C")!;
            TxtSafeSequence.Text = "Безпечну послідовність процесів побудувати неможливо.";
        }
    }

    // =========================================================================
    // 4. QUIZ MODULE
    // =========================================================================
    private void InitQuizModule()
    {
        _quizQuestions = _quizService.GetQuestions();
        RenderQuizQuestions();
    }

    private void OnRestartQuiz(object sender, RoutedEventArgs e)
    {
        foreach (var q in _quizQuestions) q.SelectedOptionIndex = null;
        BadgeEcts.Visibility = Visibility.Collapsed;
        TxtQuizScore.Text = "Результат: тестування розпочато заново";
        RenderQuizQuestions();
    }

    private void RenderQuizQuestions()
    {
        PanelQuestionsContainer.Children.Clear();

        for (int i = 0; i < _quizQuestions.Count; i++)
        {
            var q = _quizQuestions[i];
            int qIndex = i;

            var card = new Border
            {
                Background = Brushes.White,
                BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#E2E8F0")!,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 10)
            };

            var stack = new StackPanel();

            var header = new TextBlock
            {
                Text = $"Питання {i + 1} ({q.Category}): {q.QuestionText}",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#0F172A")!,
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            stack.Children.Add(header);

            for (int opt = 0; opt < q.Options.Count; opt++)
            {
                int optIndex = opt;
                var rb = new RadioButton
                {
                    Content = q.Options[opt],
                    GroupName = $"Question_{qIndex}",
                    FontSize = 12,
                    Margin = new Thickness(6, 4, 0, 4),
                    IsChecked = (q.SelectedOptionIndex == optIndex)
                };
                rb.Checked += (s, ev) => { q.SelectedOptionIndex = optIndex; };
                stack.Children.Add(rb);
            }

            card.Child = stack;
            PanelQuestionsContainer.Children.Add(card);
        }
    }

    public void CompleteQuizForDemo()
    {
        foreach (var q in _quizQuestions)
        {
            q.SelectedOptionIndex = q.CorrectOptionIndex;
        }
        OnSubmitQuiz(this, new RoutedEventArgs());
    }

    private void OnSubmitQuiz(object sender, RoutedEventArgs e)
    {
        var result = _quizService.EvaluateQuiz(_quizQuestions);
        TxtQuizScore.Text = $"Правильних відповідей: {result.CorrectAnswers} з {result.TotalQuestions} ({result.Percentage:F1}%)";
        TxtEctsGrade.Text = $"Оцінка ECTS: {result.Grade}";
        BadgeEcts.Visibility = Visibility.Visible;

        // Visual highlight answers
        for (int i = 0; i < PanelQuestionsContainer.Children.Count; i++)
        {
            if (PanelQuestionsContainer.Children[i] is Border card && card.Child is StackPanel stack)
            {
                var q = _quizQuestions[i];
                bool isCorrect = (q.SelectedOptionIndex == q.CorrectOptionIndex);

                card.BorderBrush = isCorrect
                    ? (SolidColorBrush)new BrushConverter().ConvertFromString("#86EFAC")!
                    : (SolidColorBrush)new BrushConverter().ConvertFromString("#FCA5A5")!;

                var explText = new TextBlock
                {
                    Text = $"💡 Пояснення: {q.Explanation}",
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#0369A1")!,
                    Margin = new Thickness(6, 8, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                };
                stack.Children.Add(explText);
            }
        }
    }

    // =========================================================================
    // 5. THEORY HANDBOOK MODULE
    // =========================================================================
    private void OnTheoryTopicChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListTheoryTopics == null || TxtTheoryTitle == null) return;
        LoadTheoryTopic(ListTheoryTopics.SelectedIndex);
    }

    private void LoadTheoryTopic(int idx)
    {
        switch (idx)
        {
            case 0:
                TxtTheoryTitle.Text = "1. Поняття процесу, життєвий цикл та диспетчеризація";
                TxtTheoryBody.Text = "Процес (Process) — це екземпляр виконуваної програми разом із виділеними їй системними ресурсами: адресним простором, регістрами процесора, чергою відкритих файлів та стеком викликів.\n\nЖиттєвий цикл процесу включає 5 фундаментальних станів:\n1. New (Новий) — процес ініціалізовано, створюється блок керування PCB (Process Control Block).\n2. Ready (Готовий) — процес очікує виділення кванта процесорного часу в черзі Ready Queue.\n3. Running (Виконується) — інструкції процесу безпосередньо виконуються процесором (CPU).\n4. Waiting / Blocked (Очікування) — процес заблокований в очікуванні події введення-виведення (I/O) або звільнення ресурсу.\n5. Terminated (Завершений) — виконання процесу завершено, ядро ОС повертає виділені ресурси в загальний пул пам'яті.";
                break;
            case 1:
                TxtTheoryTitle.Text = "2. Алгоритми диспетчеризації короткострокового планувальника";
                TxtTheoryBody.Text = "Диспетчеризація процесора (CPU Scheduling) — це серцевина багатозадачної операційної системи. Короткостроковий планувальник обирає процес із черги готових процесів.\n\nКлючові метрики ефективності:\n• Completion Time (CT) — абсолютний момент завершення процесу.\n• Turnaround Time (TAT) — повний час перебування в системі: TAT = CT - AT.\n• Waiting Time (WT) — сумарний час очікування в черзі: WT = TAT - BT.\n\nРозглянуті алгоритми:\n1. FCFS: найпростіший, відсутність витіснення, страждає на ефект конвою (Convoy Effect).\n2. SJF: оптимальний за середнім WT, але вимагає попереднього знання часу виконання.\n3. Round Robin: ідеальний для систем із розподілом часу (Time-Sharing). Кожному процесу виділяється квант q. При q -> inf алгоритм переходить у FCFS; при малому q зростають накладні витрати на контекстне перемикання (Context Switch).";
                break;
            case 2:
                TxtTheoryTitle.Text = "3. Динамічний розподіл оперативної пам'яті та фрагментація";
                TxtTheoryBody.Text = "Керування оперативною пам'яттю (Memory Management) відповідає за безпечний та ефективний розподіл RAM між операційною системою та прикладними програмами.\n\nЕвристики безперервного виділення блоків:\n• First Fit (Перший придатний): виділяє перший вільний блок достатнього розміру. Найшвидший пошук.\n• Best Fit (Найкращий придатний): виділяє блок із найменшою різницею розміру. Зменшує розміри втрат, але породжує дрібні марні залишки.\n• Worst Fit (Найгірший придатний): виділяє найбільший доступний блок, залишаючи великі фрагменти.\n\nФрагментація пам'яті:\n- Зовнішня: загальної вільної пам'яті достатньо, але вона розпорошена на незв'язні фрагменти.\n- Внутрішня: втрата простору всередині виділеного фіксованого блоку.\nДля усунення зовнішньої фрагментації застосовується ущільнення пам'яті (Compaction / Coalescing).";
                break;
            case 3:
                TxtTheoryTitle.Text = "4. Віртуальна пам'ять та заміщення сторінок (Paging)";
                TxtTheoryBody.Text = "Сторінкова організація (Paging) дозволяє усунути вимогу неперервного розміщення процесів у фізичній пам'яті. Адресний простір розбивається на логічні сторінки (Pages), а фізична пам'ять — на кадри сторінок (Page Frames).\n\nПомилка сторінки (Page Fault) виникає, коли запитана сторінка відсутня в оперативній пам'яті і потребує завантаження зі вторинного сховища (Swap / Pagefile).\n\nАлгоритми заміщення сторінок:\n• FIFO (First-In, First-Out): вивантажує сторінку, яка перебуває в пам'яті найдовше. Може виникати аномалія Беладі (збільшення кадрів призводить до збільшення кількості промахів).\n• LRU (Least Recently Used): вивантажує сторінку, до якої не було звернень найдовший період часу. Практично наближається до оптимального алгоритму Беладі.";
                break;
            case 4:
                TxtTheoryTitle.Text = "5. Взаємні блокування (Deadlocks) та алгоритм Банкіра";
                TxtTheoryBody.Text = "Взаємне блокування (Deadlock) — ситуація, коли набір процесів заблокований, оскільки кожен із них утримує ресурс і очікує на ресурс, захоплений іншим процесом групи.\n\n4 необхідні умови Коффмана:\n1. Взаємне виключення (ресурс неподільний).\n2. Утримання та очікування (процес утримує ресурси під час очікування нових).\n3. Відсутність витіснення (ресурс не можна відібрати примусово).\n4. Циклічне очікування (існує замкнений ланцюг очікувань).\n\nАлгоритм Банкіра Е. Дейкстри — це алгоритм уникнення взаємних блокувань для багаторесурсних систем. Перед кожним виділенням система перевіряє, чи залишиться вона в безпечному стані (Safe State), тобто чи існує принаймні одна безпечна послідовність завершення всіх процесів (Safe Sequence).";
                break;
            case 5:
                TxtTheoryTitle.Text = "6. Архітектурна модель десктопного застосунку Visual Studio";
                TxtTheoryBody.Text = "Програмний комплекс розроблено як повноцінний автономний Windows-застосунок у середовищі Microsoft Visual Studio 2022 на мові C# 12 із графічною підсистемою WPF (Windows Presentation Foundation) та платформою .NET 10.\n\nАрхітектура застосунку реалізує принципи розділення обов'язків (Separation of Concerns):\n• Data Layer (Models): типізовані класи сутностей ProcessItem, MemoryBlock, BankerProcess, QuizQuestion.\n• Business Logic Layer (Services): незалежні сервіси CpuSchedulingService, MemoryManagementService, DeadlockService, QuizService.\n• Presentation Layer (WPF XAML): декларативна векторна розмітка інтерфейсу з апаратно-прискореною візуалізацією часових діаграм Ганта, карти RAM та матриць взаємних блокувань.\n\nЗастосунок не потребує веб-браузера чи сторонніх веб-серверів і працює автономно на будь-якому комп'ютері під керуванням Windows 10/11.";
                break;
        }
    }
}
