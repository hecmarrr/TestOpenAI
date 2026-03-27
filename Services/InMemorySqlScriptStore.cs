using System.Collections.Concurrent;
using System.Text;

namespace SqlProcedureDeployer.Services;

public sealed class InMemorySqlScriptStore : ISqlScriptStore
{
    private readonly ConcurrentDictionary<Guid, StoredSqlScript> _scripts = new();

    public async Task<StoredSqlScript> SaveAsync(string fileName, Stream sqlFileStream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(sqlFileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var sqlContent = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(sqlContent))
        {
            throw new InvalidOperationException("El archivo SQL está vacío.");
        }

        var script = new StoredSqlScript
        {
            ScriptId = Guid.NewGuid(),
            FileName = fileName,
            SqlContent = sqlContent,
            UploadedAtUtc = DateTimeOffset.UtcNow
        };

        _scripts[script.ScriptId] = script;
        return script;
    }

    public bool TryGet(Guid scriptId, out StoredSqlScript? script)
    {
        var found = _scripts.TryGetValue(scriptId, out var internalScript);
        script = internalScript;
        return found;
    }
}
