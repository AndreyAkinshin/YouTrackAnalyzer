using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Humanizer;
using JetBrains.TeamCity.ServiceMessages.Write;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using YouTrackSharp;
using YouTrackSharp.Issues;

namespace YouTrackAnalyzer
{
    public static class Program
    {
        private const string SearchFiler = "#Unresolved Assignee: Unassigned order by: updated";
        private const int CommentThreshold = 50;
        private static readonly TimeSpan TimeThreshold = TimeSpan.FromDays(7);
        private static Config ourConfig;

        public static async Task Main(string[] args)
        {
            try
            {
                await Task.Run(() => Parser.Default.ParseArguments<Config>(args)
                    .WithParsed(c => { ourConfig = c; })
                    .WithNotParsed(HandleParseError));

                var textBuilder = new TextBuilder();
                if (string.IsNullOrEmpty(ourConfig.Login) && string.IsNullOrEmpty(ourConfig.Token))
                {
                    Console.WriteLine("Either login+password are required or authorisation token.");
                    return;
                }

                Connection connection = null;
                if (!string.IsNullOrEmpty(ourConfig.Token))
                    connection = new BearerTokenConnection(ourConfig.HostUrl, ourConfig.Token);
                if (!string.IsNullOrEmpty(ourConfig.Login))
                    connection = new UsernamePasswordConnection(ourConfig.HostUrl, ourConfig.Login, ourConfig.Password);

                var sw = Stopwatch.StartNew();

                var issuesService = connection.CreateIssuesService();
                var dexpIssues = await issuesService.GetIssuesInProject(
                    "DEXP", SearchFiler, take: 2000, updatedAfter: DateTime.Now - TimeThreshold);
                var dexpHotIssues = dexpIssues
                    .OrderByDescending(it => it.Comments.Count)
                    .Where(it => it.Comments.Count > CommentThreshold)
                    .ToList();

                var topHotTextBuilder = new TextBuilder();
                var dexpTopHotIssues = dexpHotIssues.Take(5);

                var dexpHotAgregated = Agregate(dexpHotIssues);
                var dexpTopAgregated = AgregateTop(dexpTopHotIssues);
                sw.Stop();
                textBuilder.AppendHeader("DEXP HOT (" + dexpHotIssues.Count + ")");
                topHotTextBuilder.AppendHeader("Top 5 of " + dexpHotIssues.Count + " hot issues");

                textBuilder.AppendLine(dexpHotAgregated.ToPlainText(), dexpHotAgregated.ToHtml());
                textBuilder.AppendHeader("Statistics");
                textBuilder.AppendKeyValue("Time", $"{sw.Elapsed.TotalSeconds:0.00} sec");
                textBuilder.AppendKeyValue("dexpIssues.Count", dexpIssues.Count.ToString());
                textBuilder.AppendKeyValue("despHotIssues.Count", dexpHotIssues.Count.ToString());
                topHotTextBuilder.AppendLine(dexpTopAgregated, dexpTopAgregated);

                File.WriteAllText("report.html", textBuilder.ToHtml());
                File.WriteAllText("report.txt", textBuilder.ToPlainText());
                //File.WriteAllText("top-report.txt", topHotTextBuilder.ToPlainText());
                using (var writer = new TeamCityServiceMessages().CreateWriter(Console.WriteLine))
                {
                    writer.WriteBuildParameter("env.short_report", topHotTextBuilder.ToPlainText());
                }
            }
            catch (UnauthorizedConnectionException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Can't establish a connection to YouTrack");
                Console.WriteLine(e.Demystify());
                Console.ResetColor();
            }
        }

        private static TextBuilder Agregate(IEnumerable<Issue> dexpHotIssues)
        {
            var sb = new TextBuilder();
            foreach (var issue in dexpHotIssues)
            {
                var id = issue.Id;
                var url = ourConfig.HostUrl + "issue/" + id;

                var title = issue.Summary.Truncate(80, "...").Replace("<", "&lt;").Replace(">", "&gt;");
                var comments = "comment".ToQuantity(issue.Comments.Count);
                sb.AppendLine(
                    $"{id} {title} / {comments}",
                    $"<a target=\"_blank\" href=\"{url}\">{id}</a> {title} / <b>{comments}</b>");
            }

            return sb;
        }
        
        private static string AgregateTop(IEnumerable<Issue> dexpHotIssues)
        {
            var sb = new StringBuilder();
            foreach (var issue in dexpHotIssues)
            {
                var id = issue.Id;
                var url = ourConfig.HostUrl + "issue/" + id;

                var title = issue.Summary.Truncate(80, "...").Replace("<", "&lt;").Replace(">", "&gt;").Replace("“", "'");
                var comments = "comment".ToQuantity(issue.Comments.Count);
                sb.AppendLine($"<{url}|{id}> {title} / {comments}");
            }

            return sb.ToString();
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            foreach (var error in errs)
            {
                Console.WriteLine(error);
            }
        }
    }
}