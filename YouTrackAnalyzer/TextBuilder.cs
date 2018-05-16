using System.Text;

namespace YouTrackAnalyzer
{
    public class TextBuilder
    {
        private readonly StringBuilder plainTextBuilder = new StringBuilder();
        private readonly StringBuilder htmlBuilder = new StringBuilder();

        public void AppendLine(string plainText, string html)
        {
            plainTextBuilder.AppendLine(plainText);
            htmlBuilder.AppendLine(html + "<br />");
        }

        public void AppendHeader(string header)
        {
            plainTextBuilder.AppendLine($"*** {header} ***");
            htmlBuilder.AppendLine($"<h3>{header}</h3>");
        }

        public void AppendKeyValue(string key, string value)
            => AppendLine($"{key} = {value}", $"<b>{key}</b> = {value}");

        public string ToPlainText() => plainTextBuilder.ToString();
        public string ToHtml() => htmlBuilder.ToString();
    }
}