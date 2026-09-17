using System.Globalization;
using DKRandomizeAIImagePromptGenerator.Models;
using Microsoft.Data.Sqlite;

namespace DKRandomizeAIImagePromptGenerator.Data;

public sealed class HistoryRepository
{
    private readonly DatabaseService _database;

    public HistoryRepository(DatabaseService database)
    {
        _database = database;
    }

    public async Task SaveAsync(
        CombinationHistory history,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO CombinationHistory (
                    Id,
                    CharacterPromptId,
                    CharacterTitleSnapshot,
                    ArtistPromptId,
                    ArtistTitleSnapshot,
                    PositiveText,
                    NegativeText,
                    CreatedAtUtc)
                VALUES (
                    @id,
                    @characterPromptId,
                    @characterTitle,
                    @artistPromptId,
                    @artistTitle,
                    @positive,
                    @negative,
                    @createdAt);
                """;
            command.Parameters.AddWithValue("@id", history.Id.ToString("D"));
            command.Parameters.AddWithValue("@characterPromptId", DbGuid(history.CharacterPromptId));
            command.Parameters.AddWithValue("@characterTitle", DbValue(history.CharacterTitleSnapshot));
            command.Parameters.AddWithValue("@artistPromptId", DbGuid(history.ArtistPromptId));
            command.Parameters.AddWithValue("@artistTitle", DbValue(history.ArtistTitleSnapshot));
            command.Parameters.AddWithValue("@positive", history.PositiveText);
            command.Parameters.AddWithValue("@negative", history.NegativeText);
            command.Parameters.AddWithValue(
                "@createdAt",
                history.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        for (var index = 0; index < history.AdditionalItems.Count; index++)
        {
            var additional = history.AdditionalItems[index];

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO CombinationHistoryAdditional (
                    HistoryId,
                    PromptId,
                    SortOrder,
                    TitleSnapshot)
                VALUES (
                    @historyId,
                    @promptId,
                    @sortOrder,
                    @title);
                """;
            command.Parameters.AddWithValue("@historyId", history.Id.ToString("D"));
            command.Parameters.AddWithValue("@promptId", DbGuid(additional.PromptId));
            command.Parameters.AddWithValue("@sortOrder", index);
            command.Parameters.AddWithValue("@title", DbValue(additional.TitleSnapshot));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        transaction.Commit();
    }

    public async Task<CombinationHistory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        var history = await ReadHistoryAsync(connection, id, cancellationToken);

        if (history is null)
        {
            return null;
        }

        history.AdditionalItems.AddRange(
            await ReadAdditionalAsync(connection, history.Id, cancellationToken));

        return history;
    }

    public async Task<IReadOnlyList<CombinationHistory>> GetRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return Array.Empty<CombinationHistory>();
        }

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id,
                   CharacterPromptId,
                   CharacterTitleSnapshot,
                   ArtistPromptId,
                   ArtistTitleSnapshot,
                   PositiveText,
                   NegativeText,
                   CreatedAtUtc
            FROM CombinationHistory
            ORDER BY CreatedAtUtc DESC
            LIMIT @limit;
            """;
        command.Parameters.AddWithValue("@limit", limit);

        var histories = new List<CombinationHistory>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            histories.Add(ReadHistory(reader));
        }

        await reader.DisposeAsync();

        foreach (var history in histories)
        {
            history.AdditionalItems.AddRange(
                await ReadAdditionalAsync(connection, history.Id, cancellationToken));
        }

        return histories;
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM CombinationHistory WHERE Id = @id;";
        command.Parameters.AddWithValue("@id", id.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<CombinationHistory?> ReadHistoryAsync(
        SqliteConnection connection,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id,
                   CharacterPromptId,
                   CharacterTitleSnapshot,
                   ArtistPromptId,
                   ArtistTitleSnapshot,
                   PositiveText,
                   NegativeText,
                   CreatedAtUtc
            FROM CombinationHistory
            WHERE Id = @id;
            """;
        command.Parameters.AddWithValue("@id", id.ToString("D"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadHistory(reader);
    }

    private static CombinationHistory ReadHistory(SqliteDataReader reader)
    {
        return new CombinationHistory
        {
            Id = Guid.Parse(reader.GetString(0)),
            CharacterPromptId = ReadNullableGuid(reader, 1),
            CharacterTitleSnapshot = reader.IsDBNull(2) ? null : reader.GetString(2),
            ArtistPromptId = ReadNullableGuid(reader, 3),
            ArtistTitleSnapshot = reader.IsDBNull(4) ? null : reader.GetString(4),
            PositiveText = reader.GetString(5),
            NegativeText = reader.GetString(6),
            CreatedAt = DateTimeOffset.Parse(
                reader.GetString(7),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind)
        };
    }

    private static async Task<IReadOnlyList<CombinationHistoryAdditional>> ReadAdditionalAsync(
        SqliteConnection connection,
        Guid historyId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT PromptId, TitleSnapshot
            FROM CombinationHistoryAdditional
            WHERE HistoryId = @historyId
            ORDER BY SortOrder ASC;
            """;
        command.Parameters.AddWithValue("@historyId", historyId.ToString("D"));

        var items = new List<CombinationHistoryAdditional>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new CombinationHistoryAdditional(
                ReadNullableGuid(reader, 0),
                reader.IsDBNull(1) ? null : reader.GetString(1)));
        }

        return items;
    }

    private static Guid? ReadNullableGuid(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Guid.Parse(reader.GetString(ordinal));

    private static object DbGuid(Guid? value) =>
        value is null ? DBNull.Value : value.Value.ToString("D");

    private static object DbValue(string? value) =>
        value is null ? DBNull.Value : value;
}
