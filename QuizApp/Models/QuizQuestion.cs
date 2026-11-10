namespace QuizApp.Models;

public class QuizQuestion
{
    public int QuestionId { get; set; }
    public string Text { get; set; } = "";
    public string Difficulty { get; set; } = "";

    public override string ToString() => $"{QuestionId}. {Text} — {Difficulty}";
}
