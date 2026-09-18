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

        var items = BuildEffectiveItems(history);
        var character = items.FirstOrDefault(item => item.Category == PromptCategory.Character);
        var artist = items.FirstOrDefault(item => item.Category == PromptCategory.Artist);

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
            command.Parameters.AddWithValue("@characterPromptId", DbGuid(character?.PromptId));
            command.Parameters.AddWithValue("@characterTitle", DbValue(character?.TitleSnapshot));
            command.Parameters.AddWithValue("@artistPromptId", DbGuid(artist?.PromptId));
            command.Parameters.AddWithValue("@artistTitle", DbValue(artist?.TitleSnapshot));
            command.Parameters.AddWithValue("@positive", history.PositiveText);
            command.Parameters.AddWithValue("@negative", history.NegativeText);
            command.Parameters.AddWithValue(
                "@createdAt",
                history.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var additional in items.Where(item => item.Category == PromptCategory.Additional))
        {
            await using var legacyCommand = connection.CreateCommand();
            legacyCommand.Transaction = transaction;
            legacyCommand.CommandText = """
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
            legacyCommand.Parameters.AddWithValue("@historyId", history.Id.ToString("D"));
            legacyCommand.Parameters.AddWithValue("@promptId", DbGuid(additional.PromptId));
            legacyCommand.Parameters.AddWithValue("@sortOrder", additional.SortOrder);
            legacyCommand.Parameters.AddWithValue("@title", DbValue(additional.TitleSnapshot));
            await legacyCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var item in items)
        {
            await using var itemCommand = connection.CreateCommand();
            itemCommand.Transaction = transaction;
            itemCommand.CommandText = """
                INSERT INTO CombinationHistoryItems (
                    HistoryId,
                    Category,
                    PromptId,
                    SortOrder,
                    TitleSnapshot)
                VALUES (
                    @historyId,
                    @category,
                    @promptId,
                    @sortOrder,
                    @title);
                """;
            itemCommand.Parameters.AddWithValue("@historyId", history.Id.ToString("D"));
            itemCommand.Parameters.AddWithValue("@category", (int)item.Category);
            itemCommand.Parameters.AddWithValue("@promptId", DbGuid(item.PromptId));
            itemCommand.Parameters.AddWithValue("@sortOrder", item.SortOrder);
            itemCommand.Parameters.AddWithValue("@title", DbValue(item.TitleSnapshot));
            await itemCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var category in Enum.GetValues<PromptCategory>())
        {
            await using var stateCommand = connection.CreateCommand();
            stateCommand.Transaction = transaction;
            stateCommand.CommandText = """
                INSERT INTO CombinationHistoryCategoryState (
                    HistoryId,
                    Category,
                    Mode,
                    RandomCount)
                VALUES (
                    @historyId,
                    @category,
                    @mode,
                    @randomCount);
                """;
            stateCommand.Parameters.AddWithValue("@historyId", history.Id.ToString("D"));
            stateCommand.Parameters.AddWithValue("@category", (int)category);
            stateCommand.Parameters.AddWithValue("@mode", (int)history.GetMode(category));
            stateCommand.Parameters.AddWithValue("@randomCount", Math.Max(1, history.GetRandomCount(category)));
            await stateCommand.ExecuteNonQueryAsync(cancellationToken);
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

        await PopulateV2DetailsAsync(connection, history, cancellationToken);
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
            await PopulateV2DetailsAsync(connection, history, cancellationToken);
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

    private static async Task PopulateV2DetailsAsync(
        SqliteConnection connection,
        CombinationHistory history,
        CancellationToken cancellationToken)
    {
        history.Items.Clear();
        history.Items.AddRange(await ReadItemsAsync(connection, history.Id, cancellationToken));

        await ReadCategoryStateAsync(connection, history, cancellationToken);
        ApplyLegacyCompatibility(history);
    }

    private static async Task<IReadOnlyList<CombinationHistoryItem>> ReadItemsAsync(
        SqliteConnection connection,
        Guid historyId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Category, PromptId, TitleSnapshot, SortOrder
            FROM CombinationHistoryItems
            WHERE HistoryId = @historyId
            ORDER BY Category ASC, SortOrder ASC;
            """;
        command.Parameters.AddWithValue("@historyId", historyId.ToString("D"));

        var items = new List<CombinationHistoryItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new CombinationHistoryItem(
                (PromptCategory)reader.GetInt32(0),
                ReadNullableGuid(reader, 1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetInt32(3)));
        }

        return items;
    }

    private static async Task ReadCategoryStateAsync(
        SqliteConnection connection,
        CombinationHistory history,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Category, Mode, RandomCount
            FROM CombinationHistoryCategoryState
            WHERE HistoryId = @historyId;
            """;
        command.Parameters.AddWithValue("@historyId", history.Id.ToString("D"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var category = (PromptCategory)reader.GetInt32(0);
            var mode = (PromptSelectionMode)reader.GetInt32(1);
            var randomCount = Math.Max(1, reader.GetInt32(2));

            switch (category)
            {
                case PromptCategory.Character:
                    history.CharacterMode = mode;
                    history.CharacterRandomCount = randomCount;
                    break;
                case PromptCategory.Artist:
                    history.ArtistMode = mode;
                    history.ArtistRandomCount = randomCount;
                    break;
                case PromptCategory.Additional:
                    history.AdditionalMode = mode;
                    history.AdditionalRandomCount = randomCount;
                    break;
            }
        }
    }

    private static IReadOnlyList<CombinationHistoryItem> BuildEffectiveItems(CombinationHistory history)
    {
        if (history.Items.Count > 0)
        {
            return Enum.GetValues<PromptCategory>()
                .SelectMany(category => history.Items
                    .Where(item => item.Category == category)
                    .OrderBy(item => item.SortOrder)
                    .Select((item, index) => item with { SortOrder = index }))
                .ToArray();
        }

        var items = new List<CombinationHistoryItem>();

        if (history.CharacterPromptId is not null || history.CharacterTitleSnapshot is not null)
        {
            items.Add(new CombinationHistoryItem(
                PromptCategory.Character,
                history.CharacterPromptId,
                history.CharacterTitleSnapshot,
                0));
        }

        if (history.ArtistPromptId is not null || history.ArtistTitleSnapshot is not null)
        {
            items.Add(new CombinationHistoryItem(
                PromptCategory.Artist,
                history.ArtistPromptId,
                history.ArtistTitleSnapshot,
                0));
        }

        items.AddRange(history.AdditionalItems.Select((item, index) =>
            new CombinationHistoryItem(
                PromptCategory.Additional,
                item.PromptId,
                item.TitleSnapshot,
                index)));

        return items;
    }

    private static void ApplyLegacyCompatibility(CombinationHistory history)
    {
        var character = history.GetItems(PromptCategory.Character).FirstOrDefault();
        var artist = history.GetItems(PromptCategory.Artist).FirstOrDefault();

        history.CharacterPromptId = character?.PromptId;
        history.CharacterTitleSnapshot = character?.TitleSnapshot;
        history.ArtistPromptId = artist?.PromptId;
        history.ArtistTitleSnapshot = artist?.TitleSnapshot;

        history.AdditionalItems.Clear();
        history.AdditionalItems.AddRange(
            history.GetItems(PromptCategory.Additional)
                .Select(item => new CombinationHistoryAdditional(
                    item.PromptId,
                    item.TitleSnapshot)));
    }

    private static Guid? ReadNullableGuid(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Guid.Parse(reader.GetString(ordinal));

    private static object DbGuid(Guid? value) =>
        value is null ? DBNull.Value : value.Value.ToString("D");

    private static object DbValue(string? value) =>
        value is null ? DBNull.Value : value;
}
