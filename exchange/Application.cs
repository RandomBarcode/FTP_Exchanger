using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Exchange
{
    internal static class Application
    {

        public static int CycleInterval { get; set; } = 60000;

        public static string SettingsFileAbsolutePath { get; set; } = Path.Combine( AppContext.BaseDirectory, "exchange.json" );

        public static string JournalDirectoryAbsolutePath { get; set; } = Path.Combine( AppContext.BaseDirectory, "journal" );

        public static bool JournalDeleteOldFiles { get; set; } = false;

        public static string RoutineDirectoryAbsolutePath { get; set; } = Path.Combine( AppContext.BaseDirectory, "routine" );

        public static Journal Journal = new Journal(JournalDirectoryAbsolutePath, "exchange", false);

        public static List<string> RoutineSettingsFilePathList = new List<string>();

        public static List<Routine> RoutineList = new List<Routine>();

        public static void SetApplicationSettings(ApplicationSettingsFileObject applicationSettingsFile)
        {
            JournalDirectoryAbsolutePath = Path.GetFullPath(applicationSettingsFile.Journal_Path);
            JournalDeleteOldFiles = applicationSettingsFile.Journal_Delete_Old_Files == 0 ? false : true;
            RoutineDirectoryAbsolutePath = Path.GetFullPath(applicationSettingsFile.Routine_Path);
            CycleInterval = applicationSettingsFile.Cycle_Interval;
            Journal.Dispose();
            Journal = new Journal(JournalDirectoryAbsolutePath, "exchange", JournalDeleteOldFiles);
        }

        public static ApplicationSettingsFileObject? ReadApplicationSettingsFile(string jsonFileAbsolutePath)
        {
            return JsonSerializer.Deserialize<ApplicationSettingsFileObject>(
                File.ReadAllText(jsonFileAbsolutePath),
                new JsonSerializerOptions() { 
                    PropertyNameCaseInsensitive = true 
                }
            );
        }

        public static void Exit(int exitCode)
        {
            Journal.WriteFatal($"Выходим из программы с кодом возврата: {exitCode}");
            System.Environment.Exit(exitCode);
        }

        public static void WriteJournalHeader()
        {
            Journal.WriteInformation($"--------------------------------------------------------------------");
            Journal.WriteInformation($"                      Exchange v{Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
            Journal.WriteInformation($"---------------------------- НАСТРОЙКИ -----------------------------");
            Journal.WriteInformation($"   Каталог журналов : {Application.JournalDirectoryAbsolutePath}");
            Journal.WriteInformation($"   Каталог процедур : {Application.RoutineDirectoryAbsolutePath}");
            Journal.WriteInformation($"  Интервал проверки : {Application.CycleInterval.ToString("#,0")} мс");
            Journal.WriteInformation($"--------------------------------------------------------------------");
        }

        public static bool RefreshRoutineList()
        {
            if (RoutineList.Count > 0)
                RoutineList.Clear();

            string[] directories = Array.Empty<string>();

            try
            {
                directories = Directory.GetDirectories(RoutineDirectoryAbsolutePath);
            }
            catch (Exception e)
            {
                Journal.WriteError($"Ошибка при получении списка папок с процедурами. Сообщение: \"{e.Message}\"");
                return false;
            }

            Journal.WriteInformation($"Ищем процедуры в папке: \"{RoutineDirectoryAbsolutePath}\"...");
            foreach (string directory in directories)
            {
                string? jsonFileFound = null;
                try
                {
                    jsonFileFound = Directory.GetFiles(directory, "*.json").FirstOrDefault();
                }
                catch (Exception e)
                {
                    Journal.WriteError($"Ошибка при поиске процедур в папке: \"{directory}\". Сообщение: \"{e.Message}\"");
                }

                if( !String.IsNullOrWhiteSpace(jsonFileFound) )
                {
                    RoutineSettingsFileObject? routineSettingsFileObject = null;
                    try
                    {
                        routineSettingsFileObject = JsonSerializer.Deserialize<RoutineSettingsFileObject>(
                            File.ReadAllText(jsonFileFound),
                            new JsonSerializerOptions()
                            {
                                PropertyNameCaseInsensitive = true
                            }
                        );
                    }
                    catch (Exception e)
                    {
                        Journal.WriteError($"Ошибка при чтении файла с настройками процедуры: {jsonFileFound}. Сообщение: \"{e.Message}\"");
                    }
                    if (routineSettingsFileObject is not null)
                    {
                        Journal.WriteInformation($"Берём в работу процедуру из файла: \"{jsonFileFound}\".");
                        RoutineList.Add( new Routine( routineSettingsFileObject , jsonFileFound ));
                    }
                }
            }
            Journal.WriteInformation($"Поиск процедур закончен.");

            return true;
        }

    }
}
