using Microsoft.Data.Sqlite;

namespace DKRandomizeAIImagePromptGenerator.Data;

public sealed class DatabaseService
{
    public const int CurrentSchemaVersion = 2;

    private readonly AppDataPaths _paths;
    private readonly string _connectionString;

    public DatabaseService(AppDataPaths paths)
    {
        _paths = paths;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = paths.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _paths.EnsureDirectories();

        await using var connection = await OpenConnectionAsync(cancellationToken);
        var version = await GetSchemaVersionAsync(connection, cancellationToken);

        if (version > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Database schema version {version} is newer than supported version {CurrentSchemaVersion}.");
        }

        if (version == 0)
        {
            await MigrateToVersion1Async(connection, cancellationToken);
            version = 1;
        }

        if (version < 2)
        {
            await MigrateToVersion2Async(connection, cancellationToken);
        }
    }

    public async Task<SqliteConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        await command.ExecuteNonQueryAsync(cancellationToken);

        return connection;
    }

    private static async Task<int> GetSchemaVersionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value);
    }

    private static async Task MigrateToVersion1Async(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE PromptItems (
                Id TEXT PRIMARY KEY NOT NULL,
                Category INTEGER NOT NULL,
                Title TEXT NOT NULL,
                PositivePrompt TEXT NULL,
                NegativePrompt TEXT NULL,
                Memo TEXT NULL,
                ImagePath TEXT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE Tags (
                Id TEXT PRIMARY KEY NOT NULL,
                Name TEXT NOT NULL,
                NormalizedName TEXT NOT NULL UNIQUE
            );

            CREATE TABLE PromptTags (
                PromptId TEXT NOT NULL,
                TagId TEXT NOT NULL,
                PRIMARY KEY (PromptId, TagId),
                FOREIGN KEY (PromptId) REFERENCES PromptItems(Id) ON DELETE CASCADE,
                FOREIGN KEY (TagId) REFERENCES Tags(Id) ON DELETE CASCADE
            );

            CREATE INDEX IX_PromptItems_Category ON PromptItems(Category);
            CREATE INDEX IX_PromptTags_TagId ON PromptTags(TagId);

            CREATE TABLE CombinationHistory (
                Id TEXT PRIMARY KEY NOT NULL,
                CharacterPromptId TEXT NULL,
                CharacterTitleSnapshot TEXT NULL,
                ArtistPromptId TEXT NULL,
                ArtistTitleSnapshot TEXT NULL,
                PositiveText TEXT NOT NULL,
                NegativeText TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                FOREIGN KEY (CharacterPromptId) REFERENCES PromptItems(Id) ON DELETE SET NULL,
                FOREIGN KEY (ArtistPromptId) REFERENCES PromptItems(Id) ON DELETE SET NULL
            );

            CREATE TABLE CombinationHistoryAdditional (
                HistoryId TEXT NOT NULL,
                PromptId TEXT NULL,
                SortOrder INTEGER NOT NULL,
                TitleSnapshot TEXT NULL,
                PRIMARY KEY (HistoryId, SortOrder),
                FOREIGN KEY (HistoryId) REFERENCES CombinationHistory(Id) ON DELETE CASCADE,
                FOREIGN KEY (PromptId) REFERENCES PromptItems(Id) ON DELETE SET NULL
            );

            PRAGMA user_version = 1;
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
        transaction.Commit();
    }

    private static async Task MigrateToVersion2Async(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction();
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE CombinationHistoryItems (
                HistoryId TEXT NOT NULL,
                Category INTEGER NOT NULL,
                PromptId TEXT NULL,
                SortOrder INTEGER NOT NULL,
                TitleSnapshot TEXT NULL,
                PRIMARY KEY (HistoryId, Category, SortOrder),
                FOREIGN KEY (HistoryId) REFERENCES CombinationHistory(Id) ON DELETE CASCADE,
                FOREIGN KEY (PromptId) REFERENCES PromptItems(Id) ON DELETE SET NULL
            );

            CREATE INDEX IX_CombinationHistoryItems_PromptId
                ON CombinationHistoryItems(PromptId);

            CREATE TABLE CombinationHistoryCategoryState (
                HistoryId TEXT NOT NULL,
                Category INTEGER NOT NULL,
                Mode INTEGER NOT NULL,
                RandomCount INTEGER NOT NULL DEFAULT 1,
                PRIMARY KEY (HistoryId, Category),
                FOREIGN KEY (HistoryId) REFERENCES CombinationHistory(Id) ON DELETE CASCADE
            );

            INSERT INTO CombinationHistoryItems (
                HistoryId, Category, PromptId, SortOrder, TitleSnapshot)
            SELECT Id, 0, CharacterPromptId, 0, CharacterTitleSnapshot
            FROM CombinationHistory
            WHERE CharacterPromptId IS NOT NULL
               OR CharacterTitleSnapshot IS NOT NULL;

            INSERT INTO CombinationHistoryItems (
                HistoryId, Category, PromptId, SortOrder, TitleSnapshot)
            SELECT Id, 1, ArtistPromptId, 0, ArtistTitleSnapshot
            FROM CombinationHistory
            WHERE ArtistPromptId IS NOT NULL
               OR ArtistTitleSnapshot IS NOT NULL;

            INSERT INTO CombinationHistoryItems (
                HistoryId, Category, PromptId, SortOrder, TitleSnapshot)
            SELECT HistoryId, 2, PromptId, SortOrder, TitleSnapshot
            FROM CombinationHistoryAdditional;

            INSERT INTO CombinationHistoryCategoryState (
                HistoryId, Category, Mode, RandomCount)
            SELECT Id, 0,
                   CASE
                       WHEN CharacterPromptId IS NOT NULL
                         OR CharacterTitleSnapshot IS NOT NULL THEN 1
                       ELSE 2
                   END,
                   1
            FROM CombinationHistory;

            INSERT INTO CombinationHistoryCategoryState (
                HistoryId, Category, Mode, RandomCount)
            SELECT Id, 1,
                   CASE
                       WHEN ArtistPromptId IS NOT NULL
                         OR ArtistTitleSnapshot IS NOT NULL THEN 1
                       ELSE 2
                   END,
                   1
            FROM CombinationHistory;

            INSERT INTO CombinationHistoryCategoryState (
                HistoryId, Category, Mode, RandomCount)
            SELECT h.Id, 2,
                   CASE
                       WHEN EXISTS (
                           SELECT 1
                           FROM CombinationHistoryAdditional a
                           WHERE a.HistoryId = h.Id
                       ) THEN 1
                       ELSE 2
                   END,
                   1
            FROM CombinationHistory h;

            PRAGMA user_version = 2;
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
        transaction.Commit();
    }
}
