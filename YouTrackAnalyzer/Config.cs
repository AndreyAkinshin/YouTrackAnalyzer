using CommandLine;

namespace YouTrackAnalyzer
{
    public class Config
    {
        [Option('u', "user", HelpText = "Login.")]
        public string Login { get; set; }

        [Option('p', "password", HelpText = "Password.")]
        public string Password { get; set; }
        
        [Option('t', "token", HelpText = "Auth token. https://www.jetbrains.com/help/youtrack/standalone/Log-in-to-YouTrack.html")]
        public string Token { get; set; }
        
        public readonly string HostUrl = "https://youtrack.jetbrains.com/";

        public const string MainConfigFileName = "config.ini";

        private const string CredentialsSectionKey = "Credentials";

        private const string Unknown = "?";
    }
}