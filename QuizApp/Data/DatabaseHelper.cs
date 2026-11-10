using System.Data;
using Microsoft.Data.SqlClient;
using QuizApp.Models;

namespace QuizApp.Data;

public class DatabaseHelper
{
    // Для SQL Server Express LocalDB, который обычно устанавливается вместе с Visual Studio.
    private const string MasterConnection =
        @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=True;";
    private const string ConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=QuizAppDb;Integrated Security=true;TrustServerCertificate=True;";

    public DatabaseHelper()
    {
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using (var connection = new SqlConnection(MasterConnection))
        {
            connection.Open();
            using var command = new SqlCommand(@"
IF DB_ID(N'QuizAppDb') IS NULL CREATE DATABASE QuizAppDb;", connection);
            command.ExecuteNonQuery();
        }

        using var db = new SqlConnection(ConnectionString);
        db.Open();

        string[] setupCommands =
        {
            @"IF OBJECT_ID(N'dbo.QuizQuestions', N'U') IS NULL
              CREATE TABLE dbo.QuizQuestions(
                QuestionId INT IDENTITY(1,1) PRIMARY KEY,
                QuestionText NVARCHAR(1000) NOT NULL,
                Difficulty NVARCHAR(20) NOT NULL
                  CHECK (Difficulty IN (N'Легко', N'Средне', N'Сложно'))
              );",
            @"IF OBJECT_ID(N'dbo.v_QuizQuestions', N'V') IS NULL
              EXEC(N'CREATE VIEW dbo.v_QuizQuestions AS
                SELECT QuestionId, QuestionText, Difficulty
                FROM dbo.QuizQuestions');",
            @"IF OBJECT_ID(N'dbo.usp_AddQuestion', N'P') IS NULL
              EXEC(N'CREATE PROCEDURE dbo.usp_AddQuestion
                @Text NVARCHAR(1000), @Difficulty NVARCHAR(20)
                AS BEGIN
                  SET NOCOUNT ON;
                  INSERT INTO dbo.QuizQuestions(QuestionText, Difficulty)
                  VALUES(@Text, @Difficulty);
                END');",
            @"IF OBJECT_ID(N'dbo.usp_UpdateQuestion', N'P') IS NULL
              EXEC(N'CREATE PROCEDURE dbo.usp_UpdateQuestion
                @Id INT, @Text NVARCHAR(1000), @Difficulty NVARCHAR(20)
                AS BEGIN
                  SET NOCOUNT ON;
                  UPDATE dbo.QuizQuestions
                  SET QuestionText=@Text, Difficulty=@Difficulty
                  WHERE QuestionId=@Id;
                END');",
            @"IF OBJECT_ID(N'dbo.usp_DeleteQuestion', N'P') IS NULL
              EXEC(N'CREATE PROCEDURE dbo.usp_DeleteQuestion
                @Id INT AS BEGIN
                  SET NOCOUNT ON;
                  DELETE FROM dbo.QuizQuestions WHERE QuestionId=@Id;
                END');",
            @"IF OBJECT_ID(N'dbo.usp_GetQuestionCount', N'P') IS NULL
              EXEC(N'CREATE PROCEDURE dbo.usp_GetQuestionCount
                AS BEGIN SET NOCOUNT ON;
                  SELECT COUNT(*) FROM dbo.QuizQuestions;
                END');",
            @"IF OBJECT_ID(N'dbo.fn_QuestionCount', N'FN') IS NULL
              EXEC(N'CREATE FUNCTION dbo.fn_QuestionCount()
                RETURNS INT AS BEGIN
                  DECLARE @n INT;
                  SELECT @n=COUNT(*) FROM dbo.QuizQuestions;
                  RETURN @n;
                END');"
        };

        foreach (string sql in setupCommands)
        {
            using var command = new SqlCommand(sql, db);
            command.ExecuteNonQuery();
        }
    }

    // Прямой доступ: подключение открыто во время выполнения SQL-запроса.
    public List<QuizQuestion> GetQuestionsDirect()
    {
        var result = new List<QuizQuestion>();
        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand(
            "SELECT QuestionId, QuestionText, Difficulty FROM dbo.v_QuizQuestions ORDER BY QuestionId",
            connection);
        connection.Open();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new QuizQuestion
            {
                QuestionId = reader.GetInt32(0),
                Text = reader.GetString(1),
                Difficulty = reader.GetString(2)
            });
        }
        return result;
    }

    // Отсоединённая модель: SqlDataAdapter заполняет DataTable, после чего
    // таблица данных используется без открытого подключения к БД.
    public List<QuizQuestion> GetQuestionsDisconnected()
    {
        var table = new DataTable();
        using var connection = new SqlConnection(ConnectionString);
        using var adapter = new SqlDataAdapter(
            "SELECT QuestionId, QuestionText, Difficulty FROM dbo.v_QuizQuestions ORDER BY QuestionId",
            connection);
        adapter.Fill(table);

        return table.AsEnumerable().Select(row => new QuizQuestion
        {
            QuestionId = row.Field<int>("QuestionId"),
            Text = row.Field<string>("QuestionText") ?? "",
            Difficulty = row.Field<string>("Difficulty") ?? ""
        }).ToList();
    }

    public void AddQuestion(string text, string difficulty) =>
        ExecuteProcedure("dbo.usp_AddQuestion", cmd =>
        {
            cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 1000).Value = text;
            cmd.Parameters.Add("@Difficulty", SqlDbType.NVarChar, 20).Value = difficulty;
        });

    public void UpdateQuestion(int id, string text, string difficulty) =>
        ExecuteProcedure("dbo.usp_UpdateQuestion", cmd =>
        {
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            cmd.Parameters.Add("@Text", SqlDbType.NVarChar, 1000).Value = text;
            cmd.Parameters.Add("@Difficulty", SqlDbType.NVarChar, 20).Value = difficulty;
        });

    public void DeleteQuestion(int id) =>
        ExecuteProcedure("dbo.usp_DeleteQuestion", cmd =>
            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id);

    public int GetQuestionCount()
    {
        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand("SELECT dbo.fn_QuestionCount()", connection);
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void ExecuteProcedure(string name, Action<SqlCommand> addParameters)
    {
        using var connection = new SqlConnection(ConnectionString);
        using var command = new SqlCommand(name, connection)
        {
            CommandType = CommandType.StoredProcedure
        };
        addParameters(command);
        connection.Open();
        command.ExecuteNonQuery();
    }
}
