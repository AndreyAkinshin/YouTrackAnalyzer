using CommandLine;

namespace YouTrackAnalyzer
{
    public class Config
    {
        [Option('t', "token", HelpText = "Auth token. https://www.jetbrains.com/help/youtrack/standalone/Log-in-to-YouTrack.html")]
        public string Token { get; set; }
        
        [Option('c', "count", Default = 30, HelpText = "Amount of comments threshold.")]
        public int CommentThreshold { get; set; }
        
        [Option('s', "search", Default = "", HelpText = "Optional search condition.")]
        public string SearchCondition { get; set; }
        
        [Option('h', "hotCount", Default = 5, HelpText = "Amount of issues for slack summary.")]
        public int HotIssuesAmount { get; set; }

        public readonly string HostUrl = "https://youtrack.jetbrains.com/";
    }
}