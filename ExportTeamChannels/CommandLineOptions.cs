using CommandLine;

namespace ExportTeamChannels
{
    public class CommandLineOptions
    {
        [Option('t', "teamid", Required = true, HelpText = "The Team ID that you want to export.")]
        public string TeamID { get; set; }

        [Option('b', "bearertoken", Required = true, HelpText = "The bearer token for the graph api (get by logging in using the Microsoft Graph Explorer)")]
        public string BearerToken { get; set; }
    }
}
