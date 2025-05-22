namespace MapHive.Services;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MapHive.Singletons;
using Microsoft.Extensions.Hosting;

public class DatabaseResetService(
    IFileLoggerSingleton fileLogger,
    IHostEnvironment hostEnvironment,
    IHostApplicationLifetime applicationLifetime) : IHostedService, IDisposable
{
    private readonly IFileLoggerSingleton _fileLogger = fileLogger;
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment;
    private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime;
    private Timer? _timer;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(30); // Run every 30 minutes
    private bool _disposed = false; // To detect redundant calls

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _fileLogger.LogToFile("Database Reset Service is starting.");

        _timer = new Timer(DoWork, null, dueTime: _period, period: _period);

        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        _fileLogger.LogToFile($"Database Reset Service is working. Triggered at: {DateTime.Now}");
        try
        {
            // Determine database file path (development and runtime fallback)
            string dbFileName = "maphive.db";

            string devPath = Path.Combine(path1: _hostEnvironment.ContentRootPath, path2: "MapHive", path3: dbFileName);
            string dbPath = File.Exists(devPath)
                ? devPath
                : Path.Combine(path1: AppContext.BaseDirectory, path2: dbFileName);

            // Schedule database file deletion after delay to allow process shutdown and release the lock
            try
            {
                string deleteCommand = $"/C timeout /T 5 /NOBREAK & del /F /Q \"{dbPath}\" > \"{_fileLogger.LogDirectory}\\{nameof(DatabaseResetService)}.log\" 2>&1";
                ProcessStartInfo procStart = new()
                {
                    FileName = "cmd.exe",
                    Arguments = deleteCommand,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                _ = Process.Start(procStart);
                _fileLogger.LogToFile($"Scheduled deletion of database file: {dbPath}");
            }
            catch (Exception ex)
            {
                _fileLogger.LogToFile($"Failed to schedule deletion of database file: {dbPath}\n" + ex.ToString());
            }

            // Trigger application shutdown to complete deletion and restart
            _applicationLifetime.StopApplication(); // Request application shutdown
        }
        catch (Exception ex)
        {
            _fileLogger.LogToFile("An error occurred during the database reset process.\n" + ex.ToString());
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _fileLogger.LogToFile("Database Reset Service is stopping.");
        _ = (_timer?.Change(dueTime: Timeout.Infinite, period: 0)); // Assign to discard
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _timer?.Dispose();
        }
        _disposed = true;
    }
}
