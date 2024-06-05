using FluentFTP;
using FluentFTP.Helpers;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Exchange
{
    internal class Routine : IDisposable
    {
        private bool disposed = false;

        public FtpClient FtpClient { get; set; } = new FtpClient();

        public string RootDirectoryAbsolutePath { get; set; } = String.Empty;
        public string RootDirectoryName { get; set; } = String.Empty;

        public string JsonFileAbsolutePath { get; set; } = String.Empty;
        public string JsonFileName { get; set; } = String.Empty;

        public bool RoutineEnabled { get; set; } = false;
        
        public Journal Journal;
        public string JournalDirectoryAbsolutePath { get; set; } = String.Empty;
        public bool JournalDeleteOldFiles { get; set; } = false;
        public long ClientFreeSpaceMinimum { get; set; } = 100000000;
        public int CyclesToSkip { get; set; } = 0;
        public int CyclesSkipped { get; set; } = 0;


        public bool ServerToClientEnabled { get; set; } = false;
        public string ServerSourceMask { get; set; } = string.Empty;
        public string ServerSourceRelativePath { get; set; } = string.Empty;
        public string ServerWorkRelativePath { get; set; } = string.Empty;
        public string ServerWorkSourceRelativePath { get; set; } = string.Empty;
        public string ServerWorkTargetRelativePath { get; set; } = string.Empty;
        public string ServerTargetRelativePath { get; set; } = string.Empty;
        public string ServerSourceAbsolutePath { get; set; } = string.Empty;
        public string ServerWorkAbsolutePath { get; set; } = string.Empty;
        public string ServerTargetAbsolutePath { get; set; } = string.Empty;
        public bool ServerTargetOverwrite { get; set; } = false;
        public bool ServerTargetCompression { get; set; } = false;
        public bool ServerTargetHashMD5 { get; set; } = false;

        public bool ClientToServerEnabled { get; set; } = false;
        public string ClientSourceMask { get; set; } = string.Empty;
        public string ClientSourceAbsolutePath { get; set; } = string.Empty;
        public string ClientWorkAbsolutePath { get; set; } = string.Empty;
        public string ClientWorkSourceAbsolutePath { get; set; } = string.Empty;
        public string ClientWorkServerAbsolutePath { get; set; } = string.Empty;
        public string ClientWorkTargetAbsolutePath { get; set; } = string.Empty;
        public string ClientTargetAbsolutePath { get; set; } = string.Empty;
        public bool ClientTargetOverwrite { get; set; } = false;
        public bool ClientTargetDecompression { get; set; } = false;

        public string ConnectionHost { get; set; } = string.Empty;
        public int ConnectionPort { get; set; } = 21;
        public string ConnectionUserName { get; set; } = string.Empty;
        public string ConnectionPassWord { get; set; } = string.Empty;

        public Routine( RoutineSettingsFileObject routineSettingsFile , string jsonFileAbsolutePath )
        {
            JsonFileAbsolutePath = jsonFileAbsolutePath;
            JsonFileName = Path.GetFileName(jsonFileAbsolutePath);

            RootDirectoryAbsolutePath = Path.GetDirectoryName(jsonFileAbsolutePath);
            RootDirectoryName = Path.GetFileName(RootDirectoryAbsolutePath);

            RoutineEnabled = routineSettingsFile.Routine_Enabled == 0 ? false : true;

            ClientFreeSpaceMinimum = routineSettingsFile.Client_Free_Space_Minimum;

            JournalDirectoryAbsolutePath = routineSettingsFile.Journal_Path.First() == '.' ?
                Path.GetFullPath(Path.Combine(RootDirectoryAbsolutePath, routineSettingsFile.Journal_Path))
                :
                Path.GetFullPath(routineSettingsFile.Journal_Path);
            ;

            JournalDeleteOldFiles = routineSettingsFile.Journal_Delete_Old_Files == 0 ? false : true;

            Journal = new Journal(JournalDirectoryAbsolutePath, Path.GetFileNameWithoutExtension(JsonFileName), JournalDeleteOldFiles);

            ConnectionHost = routineSettingsFile.Server_Host;
            ConnectionPort = routineSettingsFile.Server_Port;
            ConnectionUserName = routineSettingsFile.Server_Username;
            ConnectionPassWord = routineSettingsFile.Server_Password;

            ServerToClientEnabled = routineSettingsFile.Server_To_Client_Enabled == 0 ? false : true;

            ClientSourceMask = routineSettingsFile.Client_Source_Mask;

            ClientSourceAbsolutePath = routineSettingsFile.Client_Source_Path.First() == '.' ?
                Path.GetFullPath(Path.Combine(RootDirectoryAbsolutePath,routineSettingsFile.Client_Source_Path))
                :
                Path.GetFullPath(routineSettingsFile.Client_Source_Path);
                ;

            ClientWorkAbsolutePath = routineSettingsFile.Client_Work_Path.First() == '.' ?
                Path.GetFullPath(Path.Combine(RootDirectoryAbsolutePath,routineSettingsFile.Client_Work_Path))
                :
                Path.GetFullPath(routineSettingsFile.Client_Work_Path);
                ;

            ClientWorkSourceAbsolutePath = Path.Combine(ClientWorkAbsolutePath, "source");
            ClientWorkServerAbsolutePath = Path.Combine(ClientWorkAbsolutePath, "server");
            ClientWorkTargetAbsolutePath = Path.Combine(ClientWorkAbsolutePath, "target");

            ClientTargetAbsolutePath = routineSettingsFile.Client_Target_Path.First() == '.' ?
                Path.GetFullPath(Path.Combine(RootDirectoryAbsolutePath, routineSettingsFile.Client_Target_Path))
                :
                Path.GetFullPath(routineSettingsFile.Client_Target_Path);
                ;

            ClientTargetOverwrite = routineSettingsFile.Client_Target_Overwrite == 0 ? false : true;
            ClientTargetDecompression = routineSettingsFile.Client_Target_Decompression == 0 ? false : true;



            ClientToServerEnabled = routineSettingsFile.Client_To_Server_Enabled == 0 ? false : true;

            ServerSourceMask = routineSettingsFile.Server_Source_Mask;
            ServerSourceRelativePath = routineSettingsFile.Server_Source_Path;
            ServerSourceAbsolutePath = new Uri(new Uri($"ftp://{ConnectionHost}:{ConnectionPort}"), ServerSourceRelativePath).ToString();

            ServerWorkRelativePath = routineSettingsFile.Server_Work_Path;

            ServerWorkSourceRelativePath = $"{ServerWorkRelativePath}/source";
            ServerWorkTargetRelativePath = $"{ServerWorkRelativePath}/target";

            ServerWorkAbsolutePath = new Uri(new Uri($"ftp://{ConnectionHost}:{ConnectionPort}"), routineSettingsFile.Server_Work_Path).ToString();
            ServerTargetRelativePath = routineSettingsFile.Server_Target_Path;
            ServerTargetAbsolutePath = new Uri(new Uri($"ftp://{ConnectionHost}:{ConnectionPort}"), ServerTargetRelativePath).ToString();
            ServerTargetOverwrite = routineSettingsFile.Server_Target_Overwrite == 0 ? false : true;
            ServerTargetCompression = routineSettingsFile.Server_Target_Compression == 0 ? false : true;
            ServerTargetHashMD5 = routineSettingsFile.Server_Target_MD5 == 0 ? false : true;

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
                    Journal.Dispose();
                    FtpClient.Dispose();
                }
                disposed = true;
            }
        }

        public void WriteJournalHeader()
        {
            Journal.WriteInformation($"--------------------------------------------------------------------");
            Journal.WriteInformation($" Exchange v{Assembly.GetExecutingAssembly().GetName().Version.ToString()} | {RootDirectoryName} | {JsonFileName}");
            Journal.WriteInformation($"---------------------------- НАСТРОЙКИ -----------------------------");
            Journal.WriteInformation($"                       Задание : {(RoutineEnabled ? "Включено" : "Выключено")}");
            Journal.WriteInformation($"              Каталог журналов : {JournalDirectoryAbsolutePath}");
            Journal.WriteInformation($"      Минимум свободного места : {ClientFreeSpaceMinimum.ToString("#,0")} Байт");
            Journal.WriteInformation($"----------------------------- СЕРВЕР -------------------------------");
            Journal.WriteInformation($"                         Адрес : {ConnectionHost}");
            Journal.WriteInformation($"                          Порт : {ConnectionPort}");
            Journal.WriteInformation($"                  Пользователь : {ConnectionUserName}");
            Journal.WriteInformation($"                        Пароль : **********");
            Journal.WriteInformation($"---------------------------- ОТПРАВКА ------------------------------");
            Journal.WriteInformation($"  Отправка с клиента на сервер : {(ClientToServerEnabled ? "Включена" : "Выключена")}");
            Journal.WriteInformation($"       Маска файлов на клиенте : {ClientSourceMask}");
            Journal.WriteInformation($"     Каталог клиента исходящий : {ClientSourceAbsolutePath}");
            Journal.WriteInformation($"     Каталог сервера временный : {ServerWorkAbsolutePath}");
            Journal.WriteInformation($"       Каталог сервера целевой : {ServerTargetAbsolutePath}");
            Journal.WriteInformation($"       Сжатие исходящих файлов : {(ServerTargetCompression ? "Да" : "Нет")}");
            Journal.WriteInformation($"          MD5 исходящих файлов : {(ServerTargetHashMD5 ? "Да" : "Нет")}");
            Journal.WriteInformation($"   Перезапись имеющихся файлов : {(ServerTargetOverwrite ? "Да" : "Нет")}");
            Journal.WriteInformation($"---------------------------- ПОЛУЧЕНИЕ -----------------------------");
            Journal.WriteInformation($" Получение с сервера на клиент : {(ServerToClientEnabled ? "Включено" : "Выключено")}");
            Journal.WriteInformation($"       Маска файлов на сервере : {ServerSourceMask}");
            Journal.WriteInformation($"     Каталог сервера исходящий : {ServerSourceAbsolutePath}");
            Journal.WriteInformation($"     Каталог клиента временный : {ClientWorkAbsolutePath}");
            Journal.WriteInformation($"       Каталог клиента целевой : {ClientTargetAbsolutePath}");
            Journal.WriteInformation($"   Перезапись имеющихся файлов : {(ClientTargetOverwrite ? "Да" : "Нет")}");
            Journal.WriteInformation($"    Декомпрессия сжатых файлов : {(ClientTargetDecompression ? "Да" : "Нет")}");
            Journal.WriteInformation($"--------------------------------------------------------------------");
        }

        public bool PrepareServerWorkspace()
        {
            if (CreateServerDirectory(ServerSourceRelativePath))
                if (CreateServerDirectory(ServerTargetRelativePath))
                    if (CreateServerDirectory(ServerWorkRelativePath))
                       if (CreateServerDirectory(ServerWorkTargetRelativePath))
                          if (CreateServerDirectory(ServerWorkSourceRelativePath))
                            {
                                FtpListItem[] ftpListItems;

                                try
                                {
                                    ftpListItems = FtpClient.GetListing(ServerWorkSourceRelativePath);
                                }
                                catch (Exception e)
                                {
                                    Journal.WriteError($"Ошибка при получении списка файлов на сервере из временной папки исходящих файлов. Сообщение: {e.Message}.");
                                    return false;
                                }

                                foreach (FtpListItem ftpListItem in ftpListItems)
                                {
                                    if (ftpListItem.Type == FtpObjectType.File)
                                    {
                                        try
                                        {
                                            if (!FtpClient.MoveFile(ftpListItem.FullName, Path.Combine(ServerSourceRelativePath, ftpListItem.Name), FtpRemoteExists.Overwrite))
                                            {
                                                Journal.WriteFatal($"Файл \"{ftpListItem.Name}\" не может быть перемещён из временой папки исходящих файлов в исходную.");
                                                return false;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Journal.WriteFatal($"Ошибка при перемещении файлов из временой папки исходящих файлов в исходную. Сообщение:{e.Message}");
                                            return false;   
                                        }
                                    }
                                }

                                try
                                {
                                    ftpListItems = FtpClient.GetListing(ServerWorkRelativePath);
                                }
                                catch (Exception e)
                                {
                                    Journal.WriteError($"Ошибка поиска файлов-маркеров на сервере во временной папке. Сообщение: {e.Message}.");
                                    return false;
                                }

                                foreach (FtpListItem ftpListItem in ftpListItems)
                                {
                                    if (ftpListItem.Type == FtpObjectType.File)
                                    {
                                        if (IsFileNameMatchesPattern(ftpListItem.Name, "*.log"))
                                        try
                                        {
                                            FtpClient.DeleteFile(ftpListItem.FullName);
                                        }
                                        catch (Exception e)
                                        {
                                            Journal.WriteError($"Ошибка при удалении файла-маркера на сервере: \"ftpListItem.FullName\". Сообщение:{e.Message}");
                                            return false;
                                        }
                                    }
                                }

                                return true;
                            }
            return false;
        }

        public bool CreateServerDirectory(string directoryPath)
        {

            bool directoryExists = false;

            try
            {
                directoryExists = FtpClient.DirectoryExists(directoryPath);
            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка при проверке на наличие каталога на сервере: \"{directoryPath}\". Сообщение: {e.Message}");
                return false;
            }

            if ( directoryExists )
            {
                return true;
            }
            else
            {
                Journal.WriteWarning($"На сервере не существует каталог: \"{directoryPath}\". Создаём!");
                try
                {
                    return FtpClient.CreateDirectory(directoryPath);
                }
                catch (Exception e)
                {
                    Journal.WriteError($"Ошибка при создании каталога на сервере: \"{directoryPath}\". Сообщение: {e.Message}");
                    return false;
                }
            }
        }

        public bool PrepareClientWorkspace()
        {
            if( CreateClientDirectory(JournalDirectoryAbsolutePath) )
                if( CreateClientDirectory(ClientSourceAbsolutePath) )
                    if( CreateClientDirectory(ClientWorkSourceAbsolutePath) )
                        if (CreateClientDirectory(ClientWorkServerAbsolutePath))
                            if (CreateClientDirectory(ClientWorkTargetAbsolutePath))
                                if (CreateClientDirectory(ClientTargetAbsolutePath))
                                    if(DeleteDirectoryContents(ClientWorkServerAbsolutePath))
                                        if (DeleteDirectoryContents(ClientWorkTargetAbsolutePath))
                                        {
                                            string[] files = Array.Empty<string>();

                                            try
                                            {
                                                files = Directory.GetFiles(ClientWorkSourceAbsolutePath);
                                            }
                                            catch (Exception e)
                                            {
                                                Journal.WriteFatal($"Ошибка проверки наличия исходящих файлов во временном каталоге клиента: {ClientSourceAbsolutePath}. Сообщение: {e.Message}");
                                                return false;
                                            }

                                            foreach (string file in files)
                                            {
                                                try
                                                {
                                                    File.Move(file, Path.Combine(ClientSourceAbsolutePath,Path.GetFileName(file)));
                                                }
                                                catch (Exception e)
                                                {
                                                    Journal.WriteFatal($"Ошибка перемещения исходящего файла из временного каталога в исходный: {ClientSourceAbsolutePath}. Сообщение: {e.Message}");
                                                    return false;
                                                }
                                            }


                                            try
                                            {
                                                files = Directory.GetFiles(ClientWorkAbsolutePath,"*.log");
                                            }
                                            catch (Exception e)
                                            {
                                                Journal.WriteError($"Ошибка поиска файлов-маркеров на клиенте во временной папке. Сообщение: {e.Message}.");
                                                return false;
                                            }

                                            foreach (string file in files)
                                            {
                                                try
                                                {
                                                    File.Delete(file);
                                                }
                                                catch (Exception e)
                                                {
                                                    Journal.WriteError($"Ошибка удаления файла-маркера во временной папке: {file}. Сообщение: {e.Message}");
                                                    return false;
                                                }
                                            }


                                            if (Path.GetPathRoot(ClientWorkAbsolutePath) != Path.GetPathRoot(ClientSourceAbsolutePath))
                                            {
                                                Journal.WriteWarning($"Каталоги клиента \"исходящий\" и \"временный\" находятся на разных дисках!");
                                            }

                                            if (Path.GetPathRoot(ClientWorkAbsolutePath) != Path.GetPathRoot(ClientTargetAbsolutePath))
                                            {
                                                Journal.WriteWarning($"Каталоги клиента \"целевой\" и \"временный\" находятся на разных дисках!");
                                            }

                                            return true;
                                        }
            return false;
        }

        public bool CreateClientDirectory(string directoryAbsolutePath)
        {
            bool directoryExists = false;

            try
            {
                directoryExists = Directory.Exists(directoryAbsolutePath);
            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка при проверке на наличие каталога на клиенте: \"{directoryAbsolutePath}\". Сообщение: {e.Message}");
                return false;
            }

            if ( directoryExists )
            {
                return true;
            }
            else
            {
                Journal.WriteWarning($"На клиенте не существует каталог: \"{directoryAbsolutePath}\". Создаём!");
                try
                {
                    Directory.CreateDirectory(directoryAbsolutePath);
                }
                catch (Exception e)
                {
                    Journal.WriteError($"Ошибка при создании каталога на клиенте: \"{directoryAbsolutePath}\". Сообщение: {e.Message}");
                    return false;
                }
                return Directory.Exists(directoryAbsolutePath);
            }
        }

        public bool ConnectToServer()
        {
            if (FtpClient.IsConnected)
                FtpClient.Disconnect();

            FtpClient.Host = ConnectionHost;
            FtpClient.Port = ConnectionPort;
            FtpClient.Credentials.UserName = ConnectionUserName;
            FtpClient.Credentials.Password = ConnectionPassWord;

            try
            {
                Journal.WriteInformation($"Соединяемся с сервером: \"ftp://{ConnectionHost}:{ConnectionPort}...");
                FtpClient.AutoConnect();
            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка соединения с сервером: \"ftp://{ConnectionHost}:{ConnectionPort}. Сообщение: \"{e.Message}\"");
                return false;
            }

            if (FtpClient.IsAuthenticated)
            {
                Journal.WriteInformation($"Соединение с сервером установлено.");
                return true;
            }
            else
            {
                if (FtpClient.IsConnected)
                    FtpClient.Disconnect();
                Journal.WriteError($"Невозможно соединиться с сервером: \"ftp://{ConnectionHost}:{ConnectionPort}.");
                return false;
            }

        }

        public bool DisconnectFromServer()
        {
            Journal.WriteInformation($"Закрываем соединение с сервером: \"ftp://{ConnectionHost}:{ConnectionPort}\"...");
            if (FtpClient.IsConnected)
                FtpClient.Disconnect();
            return FtpClient.IsConnected;
        }

        public string CreateFileMD5(string originalFileAbsolutePath)
        {
            try
            {

                string originalFileName = Path.GetFileName(originalFileAbsolutePath);
                string calculatedFileName = $"{originalFileName}.md5";
                string calculatedFileAbsolutePath = Path.Combine(ClientWorkServerAbsolutePath, calculatedFileName);


                Journal.WriteDebug($"Создаём MD5 файла: \"{originalFileName}\"... ");

                FileStream calculatedFileStream = new FileStream(calculatedFileAbsolutePath, FileMode.Create);
                string hashMD5 = Convert.ToHexString(
                    System.Security.Cryptography.MD5.Create().ComputeHash(File.ReadAllBytes(originalFileAbsolutePath))
                );

                //string addFileName = $" *{originalFileName}";
                //calculatedFileStream.Write(Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(1251), Encoding.UTF8.GetBytes($"{hashMD5}{addFileName}")), 0, hashMD5.Length+addFileName.Length);

                calculatedFileStream.Write(ASCIIEncoding.ASCII.GetBytes(hashMD5), 0, hashMD5.Length);
                calculatedFileStream.Dispose();
                return calculatedFileAbsolutePath;
            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка при создании MD5 файла:\n{originalFileAbsolutePath}\nСообщение:\n{e.Message}");
                return "";
            }
        }

        public string CompressFile(string originalFileAbsolutePath)
        {
            try
            {
                if (new DriveInfo(ClientWorkAbsolutePath).TotalFreeSpace - new FileInfo(originalFileAbsolutePath).Length < ClientFreeSpaceMinimum)
                {
                    Journal.WriteError($"Недостаточно свободного места для создания сжатого файла: \"{Path.GetFileName(originalFileAbsolutePath)}\".");
                    return "";
                }
            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка при определении необходимого количества места для сжатия файла: \"{Path.GetFileName(originalFileAbsolutePath)}\". Сообщение: {e.Message}.");
                return "";
            }

            try
            {

                string originalFileName = Path.GetFileName(originalFileAbsolutePath);

                string compressedFileName = $"{originalFileName}.zip";
                string compressedFileAbsolutePath = Path.Combine(ClientWorkServerAbsolutePath, compressedFileName);

                Journal.WriteDebug($"Сжимаем файл: \"{originalFileName}\"... ");

                FileStream compressedFileStream = new FileStream(compressedFileAbsolutePath, FileMode.Create);
                ZipArchive compressedZipArchive = new ZipArchive(compressedFileStream, ZipArchiveMode.Update);
                compressedZipArchive.CreateEntryFromFile(originalFileAbsolutePath, originalFileName);
                compressedZipArchive.Dispose();

                return compressedFileAbsolutePath;
            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка при сжатии файла:\n{originalFileAbsolutePath}\nСообщение:\n{e.Message}");
                return "";
            }
        }

        public bool DeleteFile(string fileAbsolutePath)
        {
            if( File.Exists(fileAbsolutePath) )
            {
                try
                {
                    File.Delete( fileAbsolutePath );
                }
                catch (Exception e)
                {
                    Journal.WriteError($"Ошибка при удалении файла:\n{fileAbsolutePath}\nСообщение обшибке:\n{e.Message}");
                }
            }
            else
            {
                return true;
            }
            return !File.Exists(fileAbsolutePath);
        }

        public bool DeleteDirectoryContents(string directoryAbsolutePath)
        {
            try
            {
                foreach (string entryAbsolutePath in Directory.GetFileSystemEntries(directoryAbsolutePath))
                {
                    FileAttributes fileAttributes = File.GetAttributes(entryAbsolutePath);

                    if (fileAttributes.HasFlag(FileAttributes.Directory))
                    {
                        Directory.Delete(entryAbsolutePath, true);
                    }
                    else
                    {
                        File.Delete(entryAbsolutePath);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка при удалении содержимого каталога: \"{directoryAbsolutePath}\". Сообщение: {e.Message}");
                return false;
            }
        }

        public bool UploadFile(string fileSourceAbsolutePath, string fileTargetPath)
        {

            string fileSourceName = Path.GetFileName(fileSourceAbsolutePath);

            bool isFailure = false;

            try
            {
                Journal.WriteInformation($"Отправляем файл: \"{Path.GetFileName(fileTargetPath)}\"... ");

                isFailure = FtpClient.UploadFile(
                   fileSourceAbsolutePath,
                   fileTargetPath,
                   FtpRemoteExists.Overwrite,
                   true,
                   FtpVerify.Retry
                ).IsFailure();

            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка при отправке файла: \"{fileSourceName}\". Сообщение: {e.Message}");
                isFailure = true;
            }

            if (!isFailure)
            {
                Journal.WriteInformation("Готово.");
            }
            else
            {
                Journal.WriteError($"Ошибка при отправке файла: \"{fileSourceName}\".");
            }

            return !isFailure;
        }

        public bool UploadTransaction(string fileAbsolutePath)
        {
            bool isFailure = false;

            try
            {
                string fileToUploadSourceAbsolutePath = fileAbsolutePath;

                if (ServerTargetCompression)
                {
                    string compressedFileAbsolutePath = CompressFile(fileAbsolutePath);
                    if (!String.IsNullOrWhiteSpace(compressedFileAbsolutePath))
                    {
                        fileToUploadSourceAbsolutePath = compressedFileAbsolutePath;
                    }
                    else
                    {
                        isFailure = true;
                    }
                }

                if (!isFailure)
                {
                    string fileToUploadTargetName = Path.GetFileNameWithoutExtension(fileToUploadSourceAbsolutePath);
                    string fileToUploadTargetExtension = Path.GetExtension(fileToUploadSourceAbsolutePath).Substring(1);

                    string fileToUploadWorkUploadPath = $"{ServerWorkTargetRelativePath}/{fileToUploadTargetName}.{fileToUploadTargetExtension}";
                    string fileToUploadTargetPath = $"{ServerTargetRelativePath}/{fileToUploadTargetName}.{fileToUploadTargetExtension}";

                    bool isFileExistsOnServer = false;

                    try
                    {
                        isFileExistsOnServer = FtpClient.FileExists(fileToUploadTargetPath);
                        if (isFileExistsOnServer)
                        {
                            Journal.WriteWarning($"Файл уже существует на сервере: \"{fileToUploadTargetName}.{fileToUploadTargetExtension}\".");
                        }
                    }
                    catch (Exception e)
                    {
                        Journal.WriteError($"Ошибка при проверке на наличие файла: \"{fileToUploadTargetName}.{fileToUploadTargetExtension}\". Сообщение: \"{e.Message}\"");
                        isFailure = true;
                    }

                    if (!isFailure)
                    {
                        if (isFileExistsOnServer && ServerTargetOverwrite == false)
                        {
                            fileToUploadTargetName = $"{fileToUploadTargetName}_{GetTimeStamp("_")}";
                        }

                        fileToUploadWorkUploadPath = $"{ServerWorkTargetRelativePath}/{fileToUploadTargetName}.{fileToUploadTargetExtension}";
                        fileToUploadTargetPath = $"{ServerTargetRelativePath}/{fileToUploadTargetName}.{fileToUploadTargetExtension}";

                        if (UploadFile(fileToUploadSourceAbsolutePath, fileToUploadWorkUploadPath))
                        {
                            if (ServerTargetHashMD5)
                            {
                                string fileToUploadMD5AbsolutePath = CreateFileMD5(fileToUploadSourceAbsolutePath);

                                if (!String.IsNullOrWhiteSpace(fileToUploadMD5AbsolutePath))
                                {
                                    if (UploadFile(fileToUploadMD5AbsolutePath, $"{fileToUploadWorkUploadPath}.md5"))
                                    {
                                        try
                                        {
                                            FtpClient.MoveFile($"{fileToUploadWorkUploadPath}.md5", $"{fileToUploadTargetPath}.md5", FtpRemoteExists.Overwrite);
                                        }
                                        catch (Exception e)
                                        {
                                            Journal.WriteError($"Ошибка при перемещении отправленного файла на сервере: \"{fileToUploadWorkUploadPath}.md5\". Сообщение: {e.Message}");
                                            isFailure = true;
                                        }

                                        if (isFailure)
                                        {
                                            try
                                            {
                                                FtpClient.DeleteFile($"{fileToUploadWorkUploadPath}.md5");
                                            }
                                            catch (Exception e)
                                            {
                                                Journal.WriteError($"Ошибка при удалении отправленного файла на сервере: \"{fileToUploadWorkUploadPath}.md5\". Сообщение: {e.Message}");
                                                isFailure = true;
                                            }
                                            isFailure = true;
                                        }
                                    }
                                    else isFailure = true;
                                    DeleteFile(fileToUploadMD5AbsolutePath);
                                }
                                else isFailure = true;
                            }

                            if (isFailure)
                            {
                                try
                                {
                                    FtpClient.DeleteFile($"{fileToUploadWorkUploadPath}");
                                }
                                catch (Exception e)
                                {
                                    Journal.WriteError($"Ошибка при удалении отправленного файла на сервере: \"{fileToUploadWorkUploadPath}\". Сообщение: {e.Message}");
                                }
                            }
                            else
                            {
                                try
                                {
                                    FtpClient.MoveFile($"{fileToUploadWorkUploadPath}", $"{fileToUploadTargetPath}", FtpRemoteExists.Overwrite);
                                }
                                catch (Exception e)
                                {
                                    Journal.WriteError($"Ошибка при перемещении отправленного файла на сервере: \"{fileToUploadWorkUploadPath}\". Сообщение: {e.Message}");
                                    isFailure = true;
                                }

                                if (isFailure)
                                {
                                    try
                                    {
                                        FtpClient.DeleteFile($"{fileToUploadWorkUploadPath}");
                                    }
                                    catch (Exception e)
                                    {
                                        Journal.WriteError($"Ошибка при удалении отправленного файла на сервере: \"{fileToUploadWorkUploadPath}\". Сообщение: {e.Message}");
                                    }
                                }
                            }
                            DeleteFile(fileToUploadSourceAbsolutePath);
                        }
                        else isFailure = true;
                    }
                }
            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка при отправке файла: \"{fileAbsolutePath}\". Сообщение: {e.Message}");
                isFailure = true;
            }

            return !isFailure;

        }

        public bool UploadFilesFromClientSourceDirectory()
        {
            bool isFailure = false;

            string[] files = Array.Empty<string>();

            try
            {
                Journal.WriteInformation("Проверка наличия файлов на клиенте...");
                files = Directory.GetFiles(ClientSourceAbsolutePath);
            }
            catch (Exception e)
            {
                Journal.WriteFatal($"Ошибка при опросе каталога исходящих файлов на клиенте: {ClientSourceAbsolutePath}. Сообщение: {e.Message}");
                isFailure = true;
            }

            if( !isFailure  )
            {
                foreach (var fileSourceAbsolutePath in files)
                {
                    bool isFileNameMatchesClientSourceMask = false;

                    foreach (string fileMask in ClientSourceMask.Split(";"))
                    {
                        if (IsFileNameMatchesPattern(Path.GetFileName(fileSourceAbsolutePath), fileMask))
                            isFileNameMatchesClientSourceMask = true;
                    }

                    if (isFileNameMatchesClientSourceMask)
                    {
                        try
                        {

                            string fileName = Path.GetFileName(fileSourceAbsolutePath);

                            string fileWorkSourceAbsolutePath = Path.Combine(ClientWorkSourceAbsolutePath, fileName);

                            Journal.WriteInformation($"На клиенте новый файл для отправки: \"{fileName}\".");

                            try
                            {
                                File.Move(fileSourceAbsolutePath, fileWorkSourceAbsolutePath);
                            }
                            catch (Exception e)
                            {
                                Journal.WriteWarning($"Исходящий файл не может быть перемещён из исходного каталога во временный: \"{fileName}\". Сообщение: {e.Message}");
                                isFailure = true;
                            }

                            if( !isFailure )
                            {
                                try
                                {
                                    File.Move(fileWorkSourceAbsolutePath, fileSourceAbsolutePath);
                                }
                                catch (Exception e)
                                {
                                    Journal.WriteWarning($"Исходящий файл не может быть перемещён из временного каталога в исходный: \"{fileName}\". Сообщение: {e.Message}");
                                    isFailure = true;
                                }
                            }

                            if (!isFailure)
                            {
                                if (UploadTransaction(fileSourceAbsolutePath))
                                {
                                    if (!DeleteFile(fileSourceAbsolutePath))
                                    {
                                        Journal.WriteFatal($"Отправленный файл не может быть удалён: \"{fileSourceAbsolutePath}\".");
                                    };
                                }
                                else
                                {
                                    isFailure = true;
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            Journal.WriteError($"Ошибка при отправке файла: \"{fileSourceAbsolutePath}\". Сообщение: {e.Message}");
                        }
                    }
                }
            }

            return !isFailure;
        }

        public bool DownloadFilesFromServerSourceDirectory()
        {
            bool isFailure = false;

            FtpListItem[] ftpListItems = null;

            try
            {
                Journal.WriteInformation("Проверка наличия файлов на сервере...");
                ftpListItems = FtpClient.GetListing(ServerSourceRelativePath);
            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка при получении списка файлов на сервере. Сообщение: {e.Message}.");
                isFailure = true;
            }

            if (!isFailure)
            {
                foreach (FtpListItem ftpListItem in ftpListItems)
                {
                    isFailure = false;
                    if (ftpListItem.Type == FtpObjectType.File)
                    {
                        bool isFileNameMatchesServerSourceMask = false;

                        foreach (string fileMask in ServerSourceMask.Split(";"))
                        {
                            if (IsFileNameMatchesPattern(ftpListItem.Name, fileMask))
                                isFileNameMatchesServerSourceMask = true;
                        }

                        if (isFileNameMatchesServerSourceMask)
                        {
                            string fileToDownloadName = ftpListItem.Name;
                            string fileToDownloadSourcePath = $"{ftpListItem.FullName}";
                            string fileToDownloadWorkSourcePath = $"{ServerWorkSourceRelativePath}/{fileToDownloadName}";

                            Journal.WriteInformation($"На сервере новый файл для получения: \"{fileToDownloadName}\"");

                            if (new DriveInfo(ClientWorkTargetAbsolutePath).TotalFreeSpace
                                 - (Path.GetExtension(ftpListItem.Name).ToUpper() == ".ZIP" && ClientTargetDecompression ? ftpListItem.Size * 2 : ftpListItem.Size)
                                 < ClientFreeSpaceMinimum
                            )
                            {
                                Journal.WriteError($"Недостаточно свободного места для принятия файла: \"{ftpListItem.Name}\".");
                                isFailure = true;
                            }

                            if (!isFailure)
                            {
                                try
                                {
                                    FtpClient.MoveFile(fileToDownloadSourcePath, fileToDownloadWorkSourcePath, FtpRemoteExists.Overwrite);
                                }
                                catch (Exception e)
                                {
                                    Journal.WriteWarning($"Файла на сервере не может быть перемещён из исходящего каталог во временный: \"{fileToDownloadName}\". Сообщение: {e.Message}");
                                    isFailure = true;
                                }

                                if( !isFailure )
                                {
                                    try
                                    {
                                        FtpClient.MoveFile(fileToDownloadWorkSourcePath, fileToDownloadSourcePath, FtpRemoteExists.Overwrite);
                                    }
                                    catch (Exception e)
                                    {
                                        Journal.WriteWarning($"Файла на сервере не может быть перемещён из временного каталога в рабочий: \"{fileToDownloadName}\". Сообщение: {e.Message}");
                                        isFailure = true;
                                    }
                                    if (!isFailure)
                                    {
                                        if (DownloadTransaction(fileToDownloadSourcePath))
                                        {
                                            try
                                            {
                                                FtpClient.DeleteFile(fileToDownloadSourcePath);
                                            }
                                            catch (Exception e)
                                            {
                                                Journal.WriteError($"Файл на сервере не может быть удалён: \"{fileToDownloadName}\". Сообщение: {e.Message}");
                                                isFailure = true;
                                            }
                                        }
                                        else
                                        {
                                            Journal.WriteError($"Ошибка при получении файла с сервера: \"{fileToDownloadName}\".");
                                            isFailure = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return !isFailure;
        }

        public bool DownloadTransaction(string fileToDownloadPath)
        {
            bool isTransactionFailed = false;
            bool isTransactionFinished = false;

            string fileToDownloadName = Path.GetFileName(fileToDownloadPath);

            string downloadedFileAbsolutePath = DownloadFile(fileToDownloadPath, ClientWorkTargetAbsolutePath, fileToDownloadName);

            if (!String.IsNullOrEmpty(downloadedFileAbsolutePath))
            {

                string downloadedFileTargetName = Path.GetFileNameWithoutExtension(downloadedFileAbsolutePath);
                string downloadedFileTargetExtension = Path.GetExtension(downloadedFileAbsolutePath).Substring(1);

                string downloadedFileTargetPath = Path.Combine(ClientTargetAbsolutePath, $"{downloadedFileTargetName}.{downloadedFileTargetExtension}");

                string timeStamp = GetTimeStamp("_");

                if (downloadedFileTargetExtension == "zip")
                {
                    if (ClientTargetDecompression)
                    {
                        if (DecompressFile(downloadedFileAbsolutePath, ClientTargetAbsolutePath, ClientTargetOverwrite, timeStamp))
                        {
                            try
                            {
                                File.Delete(downloadedFileAbsolutePath);
                                isTransactionFinished = true;
                            }
                            catch (Exception e)
                            {
                                Journal.WriteError($"Ошибка при удалении скачанного архива: \"{downloadedFileAbsolutePath}\". Сообщение: {e.Message}");
                                isTransactionFailed = true;
                            }
                        }
                        else
                        {
                            isTransactionFailed = true;
                        }
                    }
                }

                if (!isTransactionFailed && !isTransactionFinished)
                {
                    bool isFileExistsOnClient = false;

                    try
                    {
                        isFileExistsOnClient = File.Exists(downloadedFileTargetPath);
                        if (isFileExistsOnClient)
                        {
                            Journal.WriteWarning($"Файл уже существует на клиенте: \"{downloadedFileTargetName}.{downloadedFileTargetExtension}\".");
                        }

                    }
                    catch (Exception e)
                    {
                        Journal.WriteError($"Ошибка при проверке наличия файла на клиенте: \"{downloadedFileTargetName}.{downloadedFileTargetExtension}\". Сообщение: {e.Message}");
                        isTransactionFailed = true;
                    }

                    if (!isTransactionFailed)
                    {
                        if (isFileExistsOnClient && ClientTargetOverwrite == false)
                        {
                            downloadedFileTargetName = $"{downloadedFileTargetName}_{GetTimeStamp("_")}";
                            Journal.WriteInformation($"Переименовываем файл: \"{downloadedFileTargetName}.{downloadedFileTargetExtension}\".");
                        }

                        string fileToDownloadTargetFullName = $"{downloadedFileTargetName}.{downloadedFileTargetExtension}";

                        try
                        {
                            if (ClientTargetOverwrite)
                                if (File.Exists(Path.Combine(ClientTargetAbsolutePath, fileToDownloadTargetFullName)))
                                {
                                    Journal.WriteInformation($"Перезаписываем файл: \"{fileToDownloadTargetFullName}\".");
                                    File.Delete(Path.Combine(ClientTargetAbsolutePath, fileToDownloadTargetFullName));
                                }

                            File.Move(downloadedFileAbsolutePath, Path.Combine(ClientTargetAbsolutePath, fileToDownloadTargetFullName));

                            Journal.WriteInformation("Готово.");
                        }
                        catch (Exception e)
                        {
                            Journal.WriteError($"Ошибка при перемещении файла из временного каталога в целевой: \"{fileToDownloadTargetFullName}\". Сообщение: {e.Message}");
                            isTransactionFailed = true;
                        }
                    }
                }
            }
            else isTransactionFailed = true;

            return !isTransactionFailed;
        }

        public string DownloadFile(string fileToDownloadPath, string directoryAbsolutePath, string fileName)
        {
            bool isDownloadFailed = false;

            string downloadedFileAbsolutePath = Path.Combine(directoryAbsolutePath, fileName);

            try
            {
                Journal.WriteInformation($"Получаем файл: \"{fileName}\"... ");

                var ftpStatus = FtpClient.DownloadFile(
                    downloadedFileAbsolutePath,
                    fileToDownloadPath,
                    FtpLocalExists.Overwrite,
                    FtpVerify.Retry
                );

                if (ftpStatus.IsFailure())
                {
                    Journal.WriteError($"Ошибка при получении файла: \"{fileName}\"");
                    isDownloadFailed = true;
                }
                else
                {
                    Journal.WriteInformation("Готово.");
                }
            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка при получении файла: \"{fileName}\". Сообщение: {e.Message}.");
                isDownloadFailed = true;
            }
            return isDownloadFailed ? "" : downloadedFileAbsolutePath;
        }

        public bool DecompressFile(string sourceFileAbsolutePath, string targetDirectoryAbsolutePath, bool overwriteExistingObjects, string timestampToAppend)
        {
            bool isDecompressionFailed = false;
            try
            {
                if (File.Exists(sourceFileAbsolutePath) == false)
                    isDecompressionFailed = true;
            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка при проверке на наличие сжатого файла: \"{sourceFileAbsolutePath}\". Сообщение: {e.Message}");
                isDecompressionFailed = true;
            }

            if (!isDecompressionFailed)
            {
                if (!targetDirectoryAbsolutePath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    targetDirectoryAbsolutePath += Path.DirectorySeparatorChar;

                ZipArchive zipArchive;

                try
                {
                    FileStream zipFileStream = new FileStream(sourceFileAbsolutePath, FileMode.Open);
                    zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read, false, Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage));

                    foreach (ZipArchiveEntry zipArchiveEntry in zipArchive.Entries)
                    {
                        string sourceEntryName = zipArchiveEntry.Name;
                        string targetEntryName = zipArchiveEntry.Name;
                        string targetEntryAbsolutePath = Path.Combine(targetDirectoryAbsolutePath, targetEntryName);
                        try
                        {
                            if (Directory.Exists(targetEntryAbsolutePath) || File.Exists(targetEntryAbsolutePath))
                            {
                                Journal.WriteWarning($"Файл уже существует: \"{targetEntryAbsolutePath}\"");

                                if (overwriteExistingObjects == false)
                                {
                                    targetEntryName = $"{Path.GetFileNameWithoutExtension(sourceEntryName)}_{timestampToAppend}{Path.GetExtension(sourceEntryName)}";
                                    targetEntryAbsolutePath = Path.Combine(targetDirectoryAbsolutePath, targetEntryName);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Journal.WriteError($"Ошибка при проверке на наличие сжатого файла в целевом каталоге клиента: \"{sourceFileAbsolutePath}\". Сообщение: {e.Message}");
                            isDecompressionFailed = true;
                        }
                        if (!isDecompressionFailed)
                        {
                            try
                            {
                                Journal.WriteInformation($"Извлекаем объект из архива: \"{targetEntryName}\".");
                                if (File.Exists(targetEntryAbsolutePath))
                                {
                                    Journal.WriteInformation($"Перезаписываем файл: \"{targetEntryName}\".");
                                }
                                zipArchiveEntry.ExtractToFile(targetEntryAbsolutePath, true);
                            }
                            catch (Exception e)
                            {
                                Journal.WriteError($"Ошибка при извлечении объект из архива: \"{targetEntryAbsolutePath}\". Сообщение: {e.Message}");
                                isDecompressionFailed = true;
                            }
                        }
                    }

                    zipArchive.Dispose();
                    zipFileStream.Dispose();
                }
                catch (Exception e)
                {
                    Journal.WriteError($"Ошибка при чтении сжатого файла: \"{sourceFileAbsolutePath}\". Сообщение: {e.Message}");
                    isDecompressionFailed = true;
                }
            }

            if (!isDecompressionFailed)
            {
                Journal.WriteInformation("Готово.");
            }
            return !isDecompressionFailed;
        }

        public bool IsFileNameMatchesPattern(string str, string pattern)
        {
            return new Regex(
                "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            ).IsMatch(str);
        }

        public string GetTimeStamp(string separator)
        {
            return DateTime.Now.ToString($"yyyy-MM-dd{separator}HH-mm-ss", CultureInfo.InvariantCulture);
        }

        public bool CreateWorkDoneMarkerOnServer(string directoryPath)
        {
            return true;
        }

        public bool CreateWorkDoneMarkerOnClient(string directoryPath)
        {
            return true;
        }

    }
}
