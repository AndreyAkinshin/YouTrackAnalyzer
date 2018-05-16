using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using YouTrackSharp;

namespace YouTrackAnalyzer
{
    public static class Program
    {
        private const string SearchFiler = "#Unresolved Assignee: Unassigned order by: updated";
        private const int CommentThreshold = 50;
        private static readonly TimeSpan TimeThreshold = TimeSpan.FromDays(7);

        public static async Task Main()
        {
            if (!File.Exists(Config.MainConfigFileName))
            {
                Config.CreateBlank().WriteFile(Config.MainConfigFileName);
                Console.WriteLine(Config.MainConfigFileName + " is created. Please, fill it.");
                return;
            }

            var config = Config.ReadFile(Config.MainConfigFileName);

            try
            {
                var textBuilder = new TextBuilder();
                var connection = new UsernamePasswordConnection(config.HostUrl, config.Login, config.Password);

                var sw = Stopwatch.StartNew();

                var issuesService = connection.CreateIssuesService();
                var dexpIssues = await issuesService.GetIssuesInProject(
                    "DEXP", SearchFiler, take: 2000, updatedAfter: DateTime.Now - TimeThreshold);
                var despHotIssues = dexpIssues
                    .OrderByDescending(it => it.Comments.Count)
                    .Where(it => it.Comments.Count > CommentThreshold)
                    .ToList();

                textBuilder.AppendHeader("DEXP HOT (" + despHotIssues.Count + ")");
                foreach (var issue in despHotIssues)
                {
                    var id = issue.Id;
                    var url = config.HostUrl + "/issue/" + id;
                    var title = issue.Summary.Truncate(80, "...").Replace("<", "&lt;").Replace(">", "&gt;");
                    var comments = "comment".ToQuantity(issue.Comments.Count);
                    textBuilder.AppendLine(
                        $"{id} {title} / {comments}",
                        $"<a href=\"{url}\">{id}</a> {title} / <b>{comments}</b>");
                }

                sw.Stop();

                textBuilder.AppendHeader("Statistics");
                textBuilder.AppendKeyValue("Time", $"{sw.Elapsed.TotalSeconds:0.00} sec");
                textBuilder.AppendKeyValue("dexpIssues.Count", dexpIssues.Count.ToString());
                textBuilder.AppendKeyValue("despHotIssues.Count", despHotIssues.Count.ToString());

                File.WriteAllText("report.html", textBuilder.ToHtml());
                File.WriteAllText("report.txt", textBuilder.ToPlainText());
                Console.WriteLine(textBuilder.ToPlainText());
            }
            catch (UnauthorizedConnectionException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Can't establish a connection to YouTrack");
                Console.WriteLine(e.Demystify());
                Console.ResetColor();
            }
        }
    }
}