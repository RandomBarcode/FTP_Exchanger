using System.Text.Json;

namespace Exchange
{
    internal class ApplicationSettingsFileObject
    {
        public string Routine_Path { get; set; } = "./routine";
        public string Journal_Path { get; set; } = "./journal";
        public int Journal_Delete_Old_Files { get; set; } = 0;
        public int Cycle_Interval { get; set; } = 60000;
    }
}
