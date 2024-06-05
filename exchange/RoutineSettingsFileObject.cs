using System.Text.Json;

namespace Exchange
{
    internal class RoutineSettingsFileObject
    {
        public int Routine_Enabled { get; set; } = 0;
        public string Journal_Path { get; set; } = string.Empty;
        public int Journal_Delete_Old_Files { get; set; } = 0;
        public long Client_Free_Space_Minimum { get; set; } = 100000000;
        public int Cycles_To_Skip { get; set; } = 0;

        public string Server_Host { get; set; } = string.Empty;
        public int Server_Port { get; set; } = 21;
        public string Server_Username { get; set; } = string.Empty;
        public string Server_Password { get; set; } = string.Empty;

        public int Server_To_Client_Enabled { get; set; } = 0;

        public string Server_Source_Mask { get; set; } = string.Empty;
        public string Server_Source_Path { get; set; } = string.Empty;
        public string Client_Work_Path { get; set; } = string.Empty;
        public string Client_Target_Path { get; set; } = string.Empty;
        public int Client_Target_Overwrite { get; set; } = 0;
        public int Client_Target_Decompression { get; set; } = 0;

        public int Client_To_Server_Enabled { get; set; } = 0;

        public string Client_Source_Mask { get; set; } = string.Empty;
        public string Client_Source_Path { get; set; } = string.Empty;
        public string Server_Work_Path { get; set; } = string.Empty;
        public string Server_Target_Path { get; set; } = string.Empty;
        public int Server_Target_Overwrite { get; set; } = 0;
        public int Server_Target_Compression { get; set; } = 0;
        public int Server_Target_MD5 { get; set; } = 0;
    }
}
