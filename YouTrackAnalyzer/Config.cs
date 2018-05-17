using CommandLine;

namespace YouTrackAnalyzer
{
    public class Config
    {
        [Option('u', "user", Required = true, HelpText = "Login.")]
        public string Login { get; set; }

        [Option('p', "password", Required = true, HelpText = "Password.")]
        public string Password { get; set; }
        
        public readonly string HostUrl = "https://youtrack.jetbrains.com/";

        public const string MainConfigFileName = "config.ini";

        private const string CredentialsSectionKey = "Credentials";

        private const string Unknown = "?";
    }
}