using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Globalization;
using System.IO.Compression;
using System.IO;

namespace Exchange
{
    internal class Journal : IDisposable
    {

        private bool disposed = false;

        public Logger Logger { get; set; }

        public string ConsoleOutputTemplate { get; set; } = "{Timestamp: HH:mm:ss} |{CustomConsoleLevel} {Message:lj}{NewLine}{Exception}";

        public string FileOutputTemplate { get; set; } = "{Timestamp:yy-MM-dd HH:mm:ss} |{CustomFileLevel} {Message:lj}{NewLine}{Exception}";

        public string FileName = String.Empty;
        public string JournalDirectoryAbsolutePath = String.Empty;

        public bool JournalDeleteOldFiles { get; set; } = false;

        public class CustomConsoleLogEventEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                var CustomLevel = string.Empty;

                switch (logEvent.Level)
                {
                    case LogEventLevel.Debug:
                        CustomLevel = " \u001b[1m\u001b[32m[MESSAGE]";
                        break;

                    case LogEventLevel.Error:
                        CustomLevel = " \u001b[1m\u001b[31m[ERROR]";
                        break;

                    case LogEventLevel.Fatal:
                        CustomLevel = " \u001b[41m\u001b[1m\u001b[37m[FATAL]";
                        break;

                    case LogEventLevel.Information:
                        CustomLevel = "";
                        break;

                    case LogEventLevel.Verbose:
                        CustomLevel = " [VERBOSE]";
                        break;

                    case LogEventLevel.Warning:
                        CustomLevel = " \u001b[1m\u001b[33m[WARNING]";
                        break;
                }

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CustomConsoleLevel", CustomLevel));
            }
        }

        public class CustomFileLogEventEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                var CustomLevel = string.Empty;

                switch (logEvent.Level)
                {
                    case LogEventLevel.Debug:
                        CustomLevel = " [MESSAGE]";
                        break;

                    case LogEventLevel.Error:
                        CustomLevel = " [ERROR]";
                        break;

                    case LogEventLevel.Fatal:
                        CustomLevel = " [FATAL]";
                        break;

                    case LogEventLevel.Information:
                        CustomLevel = "";
                        break;

                    case LogEventLevel.Verbose:
                        CustomLevel = " [VERBOSE]";
                        break;

                    case LogEventLevel.Warning:
                        CustomLevel = " [WARNING]";
                        break;
                }

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CustomFileLevel", CustomLevel));
            }
        }

        public Journal( string journalDirectoryAbsolutePath , string fileName , bool deleteOldFiles = false ) {

            disposed = false;

            FileName = fileName;

            JournalDirectoryAbsolutePath = journalDirectoryAbsolutePath;
            JournalDeleteOldFiles = deleteOldFiles;

            Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.With<CustomConsoleLogEventEnricher>()
                .Enrich.With<CustomFileLogEventEnricher>()
                .WriteTo.Console(
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose,
                    outputTemplate: ConsoleOutputTemplate
                )
                .WriteTo.File(
                    System.IO.Path.Combine(JournalDirectoryAbsolutePath, $"{FileName}_.log"),
                    shared: true,
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                    outputTemplate: FileOutputTemplate
                )
                .CreateLogger();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    Logger.Dispose();
                }
                disposed = true;
            }
        }

        public void WriteVerbose( string message )
        {
            if( disposed ) return;
            try
            {
                Logger.Verbose(message);
            }
            catch ( Exception e )
            {
                Logger.Fatal( $"Ошибка при записи сообщения [VERBOSE] в журнал. Сообщение: {e.Message}");
            }
        }

        public void WriteDebug( string message )
        {
            if (disposed) return;
            try
            {
                Logger.Debug(message);
            }
            catch ( Exception e )
            {
                Logger.Fatal($"Ошибка при записи сообщения [DEBUG] в журнал. Сообщение: {e.Message}");
            }
        }

        public void WriteInformation(string message)
        {
            if (disposed) return;
            try
            {
                Logger.Information(message);
            }
            catch (Exception e)
            {
                Logger.Fatal($"Ошибка при записи сообщения [INFORMATION] в журнал. Сообщение: {e.Message}");
            }
        }

        public void WriteWarning(string message)
        {
            if (disposed) return;
            try
            {
                Logger.Warning(message);
            }
            catch (Exception e)
            {
                Logger.Fatal($"Ошибка при записи сообщения [WARNING] в журнал. Сообщение: {e.Message}");
            }
        }

        public void WriteError(string message)
        {
            if (disposed) return;
            try
            {
                Logger.Error(message);
            }
            catch (Exception e)
            {
                Logger.Fatal($"Ошибка при записи сообщения [ERROR] в журнал. Сообщение: {e.Message}");
            }
        }

        public void WriteFatal(string message)
        {
            if (disposed) return;
            try
            {
                Logger.Fatal(message);
            }
            catch (Exception e)
            {
                Logger.Fatal($"Ошибка при записи сообщения [FATAL] в журнал. Сообщение: {e.Message}");
            }
        }

        public bool ProcessOldJournals()
        {
            if (disposed) return false;

            string[] files = Array.Empty<string>();

            string fileName = Path.GetFileNameWithoutExtension(FileName);

            try
            {
                files = Directory.GetFiles(JournalDirectoryAbsolutePath, $"{fileName}_*.log");
            }
            catch (Exception e)
            {
                WriteFatal($"Ошибка при опросе каталога журналов: {JournalDirectoryAbsolutePath}. Сообщение: {e.Message}");
                return false;
            }

            foreach (string file in files)
            {
                if (Path.GetFileName(file) != $"{fileName}_{DateTime.Now.ToString($"yyyyMMdd", CultureInfo.InvariantCulture)}.log")
                {
                    if (JournalDeleteOldFiles)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception e )
                        {
                            WriteError($"Ошибка при удаления файла журнала: \"{file}\". Сообщение: {e.Message}");
                        }
                    }
                    else
                    {
                        bool archiveDirectoryExists = false;

                        string archiveDirectoryAbsolutePath = Path.Combine(JournalDirectoryAbsolutePath, "archive");

                        try
                        {
                            archiveDirectoryExists = Directory.Exists(archiveDirectoryAbsolutePath);
                        }
                        catch (Exception e)
                        {
                            WriteError($"Ошибка при проверке наличия каталога для компрессии файлов журнала: \"{archiveDirectoryAbsolutePath}\". Cообщение: {e.Message}");
                            return false;
                        }

                        if (!archiveDirectoryExists)
                        {
                            try
                            {
                                Directory.CreateDirectory(archiveDirectoryAbsolutePath);
                            }
                            catch (Exception e)
                            {
                                WriteError($"Ошибка при создании каталога для компрессии файлов журнала: \"{archiveDirectoryAbsolutePath}\". Cообщение: {e.Message}");
                                return false;
                            }
                        }

                        if (archiveDirectoryExists)
                        {
                            string originalFileName = Path.GetFileName(file);

                            string compressedFileName = $"{originalFileName}.zip";
                            string compressedFileAbsolutePath = Path.Combine(archiveDirectoryAbsolutePath, compressedFileName);

                            WriteDebug($"Сжимаем файл журнала: \"{originalFileName}\"... ");

                            try
                            {
                                FileStream compressedFileStream = new FileStream(compressedFileAbsolutePath, FileMode.Create);
                                ZipArchive compressedZipArchive = new ZipArchive(compressedFileStream, ZipArchiveMode.Update);
                                compressedZipArchive.CreateEntryFromFile(file, originalFileName);
                                compressedZipArchive.Dispose();
                            }
                            catch (Exception e)
                            {
                                WriteError($"Ошибка при компрессии файла журнала: \"{file}\". Cообщение: {e.Message}");
                                return false;
                            }

                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception e)
                            {
                                WriteError($"Ошибка при удалении файла старого журнала: \"{file}\". Cообщение: {e.Message}");
                                return false;
                            }

                        }

                    }
                }
            }
            return true;
        }
    }
}
