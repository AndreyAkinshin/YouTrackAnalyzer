using IniParser;
using IniParser.Model;
using JetBrains.Annotations;

namespace YouTrackAnalyzer
{
    public class Config
    {
        public readonly string HostUrl = "https://youtrack.jetbrains.com/";

        private static readonly FileIniDataParser Parser = new FileIniDataParser();

        public const string MainConfigFileName = "config.ini";

        private const string CredentialsSectionKey = "Credentials";
        private const string LoginKey = "Login";
        private const string PasswordKey = "Password";

        private const string Unknown = "?";

        private readonly IniData data;

        [PublicAPI] public string Login => data[CredentialsSectionKey][LoginKey];
        [PublicAPI] public string Password => data[CredentialsSectionKey][PasswordKey];


        [PublicAPI]
        public void Deconstruct(out string login, out string password)
        {
            login = Login;
            password = Password;
        }

        private Config(IniData data) => this.data = data;

        public static Config CreateBlank()
        {
            var data = new IniData();
            data[CredentialsSectionKey][LoginKey] = Unknown;
            data[CredentialsSectionKey][PasswordKey] = Unknown;
            return new Config(data);
        }

        public static Config ReadFile(string filePath) => new Config(Parser.ReadFile(filePath));

        public void WriteFile(string filePath) => Parser.WriteFile(filePath, data);
    }
}