namespace TransactionSyncService.Data;

internal static class SqlIdentifier
{
    public static string QuoteIdentifier(string identifier)
    {
        var cleaned = identifier.Trim();
        if (string.IsNullOrWhiteSpace(cleaned) || cleaned.Contains(';'))
        {
            throw new InvalidOperationException($"Invalid SQL identifier: {identifier}");
        }

        return $"[{cleaned.Replace("]", "]]")}]";
    }

    public static string QuoteTableName(string tableName)
    {
        var parts = tableName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2)
        {
            throw new InvalidOperationException($"Invalid SQL table name: {tableName}");
        }

        return string.Join('.', parts.Select(QuoteIdentifier));
    }
}
