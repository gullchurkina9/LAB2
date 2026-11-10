using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.SqlClient;
using QuizApp.Data;
using QuizApp.Models;

namespace QuizApp;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<QuizQuestion> _questions = new();
    private DatabaseHelper? _database;

    public MainWindow()
    {
        InitializeComponent();
        QuestionsListBox.ItemsSource = _questions;
        try
        {
            _database = new DatabaseHelper();
            LoadQuestions();
            StatusLabel.Text = "Подключение к базе данных выполнено.";
            StatusLabel.Foreground = Brushes.ForestGreen;
        }
        catch (Exception ex)
        {
            ShowDatabaseError(ex);
        }
        UpdateOptionsInfo();
    }

    // Для обычной загрузки используется прямое чтение через SqlDataReader.
    private void LoadQuestions()
    {
        if (_database == null) return;
        _questions.Clear();
        foreach (var question in _database.GetQuestionsDirect())
            _questions.Add(question);
        QuestionCountLabel.Text = $"Вопросов: {_database.GetQuestionCount()}";
    }

    // Отсоединённая модель вызывается отдельно для демонстрации SqlDataAdapter + DataTable.
    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_database == null) return;
            _questions.Clear();
            foreach (var question in _database.GetQuestionsDisconnected())
                _questions.Add(question);
            QuestionCountLabel.Text = $"Вопросов: {_database.GetQuestionCount()}";
            StatusLabel.Text = "Список обновлён через отсоединённую модель (DataTable).";
            StatusLabel.Foreground = Brushes.ForestGreen;
        }
        catch (Exception ex) { ShowDatabaseError(ex); }
    }

    private void AddQuestion_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInput(out string text, out string difficulty)) return;
        try
        {
            _database!.AddQuestion(text, difficulty);
            LoadQuestions();
            QuestionTextBox.Clear();
            StatusLabel.Text = "Вопрос добавлен и сохранён в базе данных.";
            StatusLabel.Foreground = Brushes.ForestGreen;
        }
        catch (Exception ex) { ShowDatabaseError(ex); }
    }

    private void UpdateQuestion_Click(object sender, RoutedEventArgs e)
    {
        if (QuestionsListBox.SelectedItem is not QuizQuestion selected)
        {
            ShowInputMessage("Выберите вопрос, который нужно изменить.");
            return;
        }
        if (!ValidateInput(out string text, out string difficulty)) return;
        try
        {
            _database!.UpdateQuestion(selected.QuestionId, text, difficulty);
            LoadQuestions();
            StatusLabel.Text = "Изменения сохранены в базе данных.";
            StatusLabel.Foreground = Brushes.ForestGreen;
        }
        catch (Exception ex) { ShowDatabaseError(ex); }
    }

    private void DeleteQuestion_Click(object sender, RoutedEventArgs e)
    {
        if (QuestionsListBox.SelectedItem is not QuizQuestion selected)
        {
            ShowInputMessage("Сначала выберите вопрос для удаления.");
            return;
        }
        if (MessageBox.Show("Удалить выбранный вопрос?", "Подтверждение",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        try
        {
            _database!.DeleteQuestion(selected.QuestionId);
            LoadQuestions();
            QuestionTextBox.Clear();
            StatusLabel.Text = "Вопрос удалён из базы данных.";
            StatusLabel.Foreground = Brushes.ForestGreen;
        }
        catch (Exception ex) { ShowDatabaseError(ex); }
    }

    private bool ValidateInput(out string text, out string difficulty)
    {
        text = QuestionTextBox.Text.Trim();
        difficulty = (EditDifficultyComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Легко";
        if (string.IsNullOrWhiteSpace(QuizTitleTextBox.Text))
        {
            ShowInputMessage("Введите название квиза.");
            QuizTitleTextBox.Focus();
            return false;
        }
        if (string.IsNullOrWhiteSpace(text))
        {
            ShowInputMessage("Введите текст вопроса.");
            QuestionTextBox.Focus();
            return false;
        }
        return true;
    }

    private void QuestionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (QuestionsListBox.SelectedItem is QuizQuestion question)
        {
            QuestionTextBox.Text = question.Text;
            EditDifficultyComboBox.SelectedIndex = question.Difficulty switch
            {
                "Средне" => 1, "Сложно" => 2, _ => 0
            };
            StatusLabel.Text = $"Выбран вопрос №{question.QuestionId}.";
            StatusLabel.Foreground = Brushes.SteelBlue;
        }
    }

    private void ClearFields_Click(object sender, RoutedEventArgs e)
    {
        QuestionsListBox.SelectedItem = null;
        QuestionTextBox.Clear();
        EditDifficultyComboBox.SelectedIndex = 0;
        StatusLabel.Text = "Поля ввода очищены.";
        StatusLabel.Foreground = Brushes.SlateGray;
    }

    private void Option_Changed(object sender, RoutedEventArgs e) => UpdateOptionsInfo();

    private void UpdateOptionsInfo()
    {
        string mode = ReturnRadioButton?.IsChecked == true ? "с возвратом" : "однопроходное";
        string timer = TimerCheckBox?.IsChecked == true ? "таймер включён" : "без таймера";
        string hints = HintsCheckBox?.IsChecked == true ? "подсказки разрешены" : "подсказки выключены";
        if (OptionsInfoLabel != null) OptionsInfoLabel.Text = $"Режим: {mode}; {timer}; {hints}.";
    }

    private void ShowInputMessage(string message)
    {
        StatusLabel.Text = message;
        StatusLabel.Foreground = Brushes.IndianRed;
        MessageBox.Show(message, "Проверка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void ShowDatabaseError(Exception ex)
    {
        StatusLabel.Text = "Ошибка работы с базой данных.";
        StatusLabel.Foreground = Brushes.IndianRed;
        MessageBox.Show("Не удалось выполнить операцию с базой данных. Проверьте, установлен ли SQL Server LocalDB.\n\n" +
                        ex.Message, "Ошибка базы данных", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
