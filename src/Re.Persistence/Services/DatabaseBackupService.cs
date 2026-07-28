namespace Re.Persistence.Services;

public interface IDatabaseBackupService
{
    Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default);
}

public sealed class DatabaseBackupService : IDatabaseBackupService
{
    private readonly Context.ReDbContext _db;

    public DatabaseBackupService(Context.ReDbContext db)
    {
        _db = db;
    }

    public async Task<string> CreateBackupAsync(string destinationDirectory, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(destinationDirectory);
        var fileName = $"ReErpDb_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        var backupPath = Path.Combine(destinationDirectory, fileName);

        var sql = $"BACKUP DATABASE [ReErpDb] TO DISK = '{backupPath}' WITH FORMAT, MEDIANAME = 'ReErpBackup', NAME = 'Full Backup of ReErpDb';";
        await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ExecuteSqlRawAsync(_db.Database, sql, cancellationToken);

        return backupPath;
    }
}
