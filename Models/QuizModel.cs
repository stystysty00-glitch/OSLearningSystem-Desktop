namespace OSLearningSystem.Models;

public class QuizQuestion
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectOptionIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public int? SelectedOptionIndex { get; set; }
}

public class QuizResult
{
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public double Percentage => TotalQuestions > 0 ? Math.Round((double)CorrectAnswers / TotalQuestions * 100, 1) : 0;
    public string Grade => Percentage switch
    {
        >= 90 => "Відмінно (A)",
        >= 82 => "Дуже добре (B)",
        >= 74 => "Добре (C)",
        >= 64 => "Задовільно (D)",
        >= 60 => "Достатньо (E)",
        _ => "Незадовільно (F)"
    };
}
