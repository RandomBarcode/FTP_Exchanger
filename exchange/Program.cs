// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Exchange;
using FluentFTP;
using FluentFTP.Helpers;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

bool isApplicationJustStarted = true;

ApplicationSettingsFileObject? applicationSettingsFile = null;

try
{
    applicationSettingsFile = Application.ReadApplicationSettingsFile(Application.SettingsFileAbsolutePath);
}
catch (Exception e)
{
    Application.Journal.WriteFatal($"Ошибка при чтении файла настроек программы: \"{Application.SettingsFileAbsolutePath}\". Сообщение: \"{e.Message}\"");
    Application.Exit(1050);
}

if (applicationSettingsFile is null)
{
    Application.Journal.WriteFatal($"Возвращёны нулевые настройки при чтении файла настроек программы: \"{Application.SettingsFileAbsolutePath}\".");
    Application.Exit(1051);
}

try
{
    Application.SetApplicationSettings(applicationSettingsFile);
}
catch (Exception e)
{
    Application.Journal.WriteFatal($"Ошибка при установке настроек программы. Сообщение: \"{e.Message}\"");
    Application.Exit(1052);
}


do
{
    Application.Journal.ProcessOldJournals();

    if (isApplicationJustStarted)
    {
        Application.WriteJournalHeader();
    }

    if ( Application.RefreshRoutineList() )
    {
        foreach( Routine routine in Application.RoutineList)
        {

            Application.Journal.WriteInformation($"Запуск задания: \"{routine.RootDirectoryName}\" | \"{routine.JsonFileName}\"...");

            if (isApplicationJustStarted)
            {
                routine.WriteJournalHeader();
            }
            else
            {
                routine.Journal.WriteInformation($"Задание {(routine.RoutineEnabled ? "ВКЛЮЧЕНО": "ВЫКЛЮЧЕНО")}...");
            }

            if( routine.RoutineEnabled )
            {
                if( routine.PrepareClientWorkspace() )
                {
                    routine.Journal.ProcessOldJournals();

                    if (routine.ConnectToServer())
                    {
                        if( routine.PrepareServerWorkspace() )
                        {
                            byte[] bytes;

                            string logFileName = $"{DateTime.Now.ToString($"yyyy-MM-dd-HH-mm-ss", CultureInfo.InvariantCulture)}.log";

                            string logFilePath = Path.Combine(routine.ClientWorkAbsolutePath, logFileName);

                            FileStream logFileStream =
                                new FileStream(
                                    logFilePath,
                                    FileMode.Create,
                                    FileAccess.ReadWrite,
                                    FileShare.ReadWrite
                                );


                            bool SuccessfulTransferToClient =
                                routine.ServerToClientEnabled ?
                                    routine.DownloadFilesFromServerSourceDirectory()
                                    :
                                    true
                            ;

                            bool SuccessfulTransferToServer = 
                                routine.ClientToServerEnabled ?
                                    routine.UploadFilesFromClientSourceDirectory()
                                    : true
                            ;

                            if ( !( SuccessfulTransferToClient && SuccessfulTransferToServer ) )
                            {

                                string journalFilePath = $"{Path.Combine(routine.Journal.JournalDirectoryAbsolutePath, routine.Journal.FileName)}_{DateTime.Now.ToString($"yyyyMMdd", CultureInfo.InvariantCulture)}.log";

                                List<String> strings = new List<string>();

                                try
                                {
                                    using (FileStream stream = File.Open(journalFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                    using (StreamReader reader = new StreamReader(stream))
                                    {
                                        do
                                        {
                                            strings.Add(reader.ReadLine());
                                        } while (!reader.EndOfStream);

                                        reader.Close();
                                        stream.Close();
                                    }
                                }
                                catch (Exception e)
                                {
                                    routine.Journal.WriteError($"Ошибка при чтении файла журнала: {journalFilePath}. Сообщение: {e.Message}");
                                }

                                try
                                {
                                    using (StreamWriter writer = new StreamWriter(logFileStream))
                                    {
                                        for (int i = strings.Count < 100 ? 0 : strings.Count - 100; i < strings.Count; i++)
                                        {
                                            writer.WriteLine(strings[i]);
                                        }
                                        writer.Close();
                                    }
                                }
                                catch (Exception e)
                                {
                                    routine.Journal.WriteError($"Ошибка при записи проверочного файла журнала: {journalFilePath}. Сообщение: {e.Message}");
                                }

                            }

                            logFileStream.Close();

                            try
                            {
                                if (
                                    routine.FtpClient.UploadFile(
                                        logFilePath,
                                        Path.Combine(routine.ServerWorkRelativePath, logFileName),
                                        FtpRemoteExists.Overwrite,
                                        true
                                    ).IsFailure()
                                )
                                {
                                    routine.Journal.WriteError($"Не удалить отправить проверочный файл журнал на сервер: \"{logFileName}\".");
                                }
                            }
                            catch (Exception e)
                            {
                                routine.Journal.WriteError($"Ошибка при отправке проверочного файла журнала на сервер: \"{logFileName}\". Сообщение: {e.Message}");
                            }

                        }
                        routine.DisconnectFromServer();
                    }
                }
            }

            Application.Journal.WriteInformation($"Завершение задания: \"{routine.RootDirectoryName}\" | \"{routine.JsonFileName}\"...");
            routine.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    isApplicationJustStarted = false;

    Application.Journal.WriteInformation($"Пауза {Application.CycleInterval} мс...");


    Thread.Sleep( Application.CycleInterval );

} while (true);
