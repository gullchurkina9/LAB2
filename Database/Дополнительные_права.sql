-- Необязательный пример настройки прав для отдельного пользователя приложения.
-- Выполнять администратору SQL Server после создания QuizAppDb.
-- В LocalDB обычно используется Windows-аутентификация текущего пользователя,
-- поэтому этот скрипт нужен только при демонстрации разграничения доступа.

USE QuizAppDb;
GO
-- Пример роли с правами только на процедуры и представление:
IF DATABASE_PRINCIPAL_ID(N'QuizAppUserRole') IS NULL
    CREATE ROLE QuizAppUserRole;
GO
GRANT SELECT ON dbo.v_QuizQuestions TO QuizAppUserRole;
GRANT EXECUTE ON dbo.usp_AddQuestion TO QuizAppUserRole;
GRANT EXECUTE ON dbo.usp_UpdateQuestion TO QuizAppUserRole;
GRANT EXECUTE ON dbo.usp_DeleteQuestion TO QuizAppUserRole;
GRANT EXECUTE ON dbo.usp_GetQuestionCount TO QuizAppUserRole;
GRANT EXECUTE ON OBJECT::dbo.fn_QuestionCount TO QuizAppUserRole;
GO
