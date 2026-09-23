using System.Globalization;
using DKRandomizeAIImagePromptGenerator.Models;
using Microsoft.Data.Sqlite;

namespace DKRandomizeAIImagePromptGenerator.Data;

public sealed class PromptRepository
{
    private readonly DatabaseService _database;

    public PromptRepository(DatabaseService database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<PromptItem>> SearchAsync(
        PromptCategory? category = null,
        string? searchText = null,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        var conditions = new List<string>();

        if (category is not null)
        {
            conditions.Add("p.Category = @category");
            command.Parameters.AddWithValue("@category", (int)category.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            conditions.Add("(p.Title LIKE @search OR p.PositivePrompt LIKE @search OR p.NegativePrompt LIKE @search)");
            command.Parameters.AddWithValue("@search", $"%{searchText.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            conditions.Add("EXISTS (SELECT 1 FROM PromptTags pt JOIN Tags t ON t.Id = pt.TagId WHERE pt.PromptId = p.Id AND t.NormalizedName LIKE @tag)");
            command.Parameters.AddWithValue("@tag", $"%{NormalizeTag(tag)}%");
        }

        command.CommandText = $"""
            SELECT p.Id, p.Category, p.Title, p.PositivePrompt, p.NegativePrompt,
                   p.Memo, p.ImagePath, p.CreatedAtUtc, p.UpdatedAtUtc
            FROM PromptItems p
            {(conditions.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", conditions))}
            ORDER BY p.UpdatedAtUtc DESC, p.Title COLLATE NOCASE ASC;
            """;

        var items = new List<PromptItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadPrompt(reader));
        }

        await reader.DisposeAsync();

        foreach (var item in items)
        {
            item.Tags.AddRange(await GetTagsAsync(connection, item.Id, cancellationToken));
        }

        return items;
    }

    public async Task<PromptItem?> GetByTitleAsync(
        PromptCategory category,
        string title,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Category, Title, PositivePrompt, NegativePrompt,
                   Memo, ImagePath, CreatedAtUtc, UpdatedAtUtc
            FROM PromptItems
            WHERE Category = @category
              AND Title = @title COLLATE NOCASE
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@category", (int)category);
        command.Parameters.AddWithValue("@title", title.Trim());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var item = ReadPrompt(reader);
        await reader.DisposeAsync();
        item.Tags.AddRange(await GetTagsAsync(connection, item.Id, cancellationToken));
        return item;
    }

    public async Task<bool> ExistsByTitleAsync(
        PromptCategory category,
        string title,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM PromptItems
                WHERE Category = @category
                  AND Title = @title COLLATE NOCASE
            );
            """;
        command.Parameters.AddWithValue("@category", (int)category);
        command.Parameters.AddWithValue("@title", title.Trim());

        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken)) != 0;
    }

    public async Task<PromptItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Category, Title, PositivePrompt, NegativePrompt,
                   Memo, ImagePath, CreatedAtUtc, UpdatedAtUtc
            FROM PromptItems
            WHERE Id = @id;
            """;
        command.Parameters.AddWithValue("@id", id.ToString("D"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var item = ReadPrompt(reader);
        await reader.DisposeAsync();
        item.Tags.AddRange(await GetTagsAsync(connection, item.Id, cancellationToken));
        return item;
    }

    public async Task CreateAsync(
        PromptItem item,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO PromptItems (
                    Id, Category, Title, PositivePrompt, NegativePrompt,
                    Memo, ImagePath, CreatedAtUtc, UpdatedAtUtc)
                VALUES (
                    @id, @category, @title, @positive, @negative,
                    @memo, @imagePath, @createdAt, @updatedAt);
                """;
            AddPromptParameters(command, item);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await ReplaceTagsAsync(connection, transaction, item.Id, item.Tags, cancellationToken);
        transaction.Commit();
    }

    public async Task UpdateAsync(
        PromptItem item,
        CancellationToken cancellationToken = default)
    {
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                UPDATE PromptItems
                SET Category = @category,
                    Title = @title,
                    PositivePrompt = @positive,
                    NegativePrompt = @negative,
                    Memo = @memo,
                    ImagePath = @imagePath,
                    UpdatedAtUtc = @updatedAt
                WHERE Id = @id;
                """;
            AddPromptParameters(command, item);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await ReplaceTagsAsync(connection, transaction, item.Id, item.Tags, cancellationToken);
        await RemoveOrphanTagsAsync(connection, transaction, cancellationToken);
        transaction.Commit();
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM PromptItems WHERE Id = @id;";
            command.Parameters.AddWithValue("@id", id.ToString("D"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await RemoveOrphanTagsAsync(connection, transaction, cancellationToken);
        transaction.Commit();
    }

    private static PromptItem ReadPrompt(SqliteDataReader reader)
    {
        return new PromptItem
        {
            Id = Guid.Parse(reader.GetString(0)),
            Category = (PromptCategory)reader.GetInt32(1),
            Title = reader.GetString(2),
            PositivePrompt = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            NegativePrompt = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            Memo = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            ImagePath = reader.IsDBNull(6) ? null : reader.GetString(6),
            CreatedAt = DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            UpdatedAt = DateTimeOffset.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }

    private static void AddPromptParameters(SqliteCommand command, PromptItem item)
    {
        command.Parameters.AddWithValue("@id", item.Id.ToString("D"));
        command.Parameters.AddWithValue("@category", (int)item.Category);
        command.Parameters.AddWithValue("@title", item.Title);
        command.Parameters.AddWithValue("@positive", DbValue(item.PositivePrompt));
        command.Parameters.AddWithValue("@negative", DbValue(item.NegativePrompt));
        command.Parameters.AddWithValue("@memo", DbValue(item.Memo));
        command.Parameters.AddWithValue("@imagePath", DbValue(item.ImagePath));
        command.Parameters.AddWithValue("@createdAt", item.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@updatedAt", item.UpdatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
    }

    private static async Task<IReadOnlyList<string>> GetTagsAsync(
        SqliteConnection connection,
        Guid promptId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.Name
            FROM Tags t
            JOIN PromptTags pt ON pt.TagId = t.Id
            WHERE pt.PromptId = @promptId
            ORDER BY t.Name COLLATE NOCASE ASC;
            """;
        command.Parameters.AddWithValue("@promptId", promptId.ToString("D"));

        var tags = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tags.Add(reader.GetString(0));
        }

        return tags;
    }

    private static async Task ReplaceTagsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid promptId,
        IEnumerable<string> tags,
        CancellationToken cancellationToken)
    {
        await using (var deleteLinks = connection.CreateCommand())
        {
            deleteLinks.Transaction = transaction;
            deleteLinks.CommandText = "DELETE FROM PromptTags WHERE PromptId = @promptId;";
            deleteLinks.Parameters.AddWithValue("@promptId", promptId.ToString("D"));
            await deleteLinks.ExecuteNonQueryAsync(cancellationToken);
        }

        var distinctTags = tags
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var tag in distinctTags)
        {
            var normalized = NormalizeTag(tag);
            string tagId;

            await using (var findTag = connection.CreateCommand())
            {
                findTag.Transaction = transaction;
                findTag.CommandText = "SELECT Id FROM Tags WHERE NormalizedName = @normalized;";
                findTag.Parameters.AddWithValue("@normalized", normalized);
                tagId = (string?)await findTag.ExecuteScalarAsync(cancellationToken) ?? string.Empty;
            }

            if (tagId.Length == 0)
            {
                tagId = Guid.NewGuid().ToString("D");
                await using var insertTag = connection.CreateCommand();
                insertTag.Transaction = transaction;
                insertTag.CommandText = "INSERT INTO Tags (Id, Name, NormalizedName) VALUES (@id, @name, @normalized);";
                insertTag.Parameters.AddWithValue("@id", tagId);
                insertTag.Parameters.AddWithValue("@name", tag);
                insertTag.Parameters.AddWithValue("@normalized", normalized);
                await insertTag.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var insertLink = connection.CreateCommand();
            insertLink.Transaction = transaction;
            insertLink.CommandText = "INSERT OR IGNORE INTO PromptTags (PromptId, TagId) VALUES (@promptId, @tagId);";
            insertLink.Parameters.AddWithValue("@promptId", promptId.ToString("D"));
            insertLink.Parameters.AddWithValue("@tagId", tagId);
            await insertLink.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task RemoveOrphanTagsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM Tags WHERE NOT EXISTS (SELECT 1 FROM PromptTags pt WHERE pt.TagId = Tags.Id);";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string NormalizeTag(string tag) => tag.Trim().ToUpperInvariant();

    private static object DbValue(string? value) =>
        string.IsNullOrEmpty(value) ? DBNull.Value : value;
}
