using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using CommandLine;
using Humanizer;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using YouTrackSharp;
using YouTrackSharp.Issues;

namespace YouTrackAnalyzer
{
    public static class Program
    {
        private const string SearchFiler = "#Unresolved Assignee: Unassigned order by: updated";
        private static readonly TimeSpan TimeThreshold = TimeSpan.FromDays(7);
        private static Config ourConfig;

        public static async Task Main(string[] args)
        {
            try
            {
                await Task.Run(() => Parser.Default.ParseArguments<Config>(args)
                    .WithParsed(c => { ourConfig = c; })
                    .WithNotParsed(HandleParseError));
                
                if (ourConfig == null)
                    return;

                var textBuilder = new TextBuilder();
                var connection = new BearerTokenConnection(ourConfig.HostUrl, ourConfig.Token);
                var commentThreshold = ourConfig.CommentThreshold;

                var sw = Stopwatch.StartNew();
                
                var issuesService = connection.CreateIssuesService(); 
                var list = new List<Issue>();
                for (int i = 0; i < 20; i++)
                {
                    try
                    {
                        var dexpIssues = await issuesService.GetIssuesInProject(
                            "DEXP", $"{SearchFiler} {ourConfig.SearchCondition}", skip: i*100,take: 100, updatedAfter: DateTime.Now - TimeThreshold);
                        list.AddRange(dexpIssues);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e);
                    }
                }

                var dexpHotIssues = list
                    .OrderByDescending(it => it.Comments.Count)
                    .Where(it => it.Comments.Count > commentThreshold)
                    .ToList();

                var topHotTextBuilder = new TextBuilder();
                var dexpTopHotIssues = dexpHotIssues.Take(ourConfig.HotIssuesAmount);

                var dexpHotAggregated = Aggregate(dexpHotIssues);
                var dexpTopAggregated = AggregateTop(dexpTopHotIssues);
                sw.Stop();
                textBuilder.AppendHeader("DEXP HOT (" + dexpHotIssues.Count + ")");
                var maxCount = dexpHotIssues.Count >= 5 ? 5 : dexpHotIssues.Count;
                topHotTextBuilder.AppendHeader($"Top {maxCount} of {dexpHotIssues.Count} hot issues");

                textBuilder.AppendLine(dexpHotAggregated.ToPlainText(), dexpHotAggregated.ToHtml());
                textBuilder.AppendHeader("Statistics");
                textBuilder.AppendKeyValue("Time", $"{sw.Elapsed.TotalSeconds:0.00} sec");
                textBuilder.AppendKeyValue("dexpIssues.Count", list.Count.ToString());
                textBuilder.AppendKeyValue("dexpHotIssues.Count", dexpHotIssues.Count.ToString());
                topHotTextBuilder.AppendLine(dexpTopAggregated, dexpTopAggregated);

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

        private static TextBuilder Aggregate(IEnumerable<Issue> dexpHotIssues)
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
        
        private static string AggregateTop(IEnumerable<Issue> dexpHotIssues)
        {
            var sb = new StringBuilder();
            foreach (var issue in dexpHotIssues)
            {
                var id = issue.Id;
                var url = ourConfig.HostUrl + "issue/" + id;

                var title = issue.Summary.Truncate(80, "...").Replace("<", "&lt;").Replace(">", "&gt;")
                  .Replace("“", "'").Replace("”", "'").Replace("\"", "'").Replace("\"", "'")
                  .Replace("\'", String.Empty)
                  .Replace(@"\", "/");
                title = HttpUtility.JavaScriptStringEncode(title);
                title = Regex.Replace(title, @"[^\u0000-\u007F]+", string.Empty);
                var comments = "comment".ToQuantity(issue.Comments.Count);
                sb.AppendLine($"<{url}|{id}> {title} / {comments}");
            }

            return sb.ToString();
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            foreach (var error in errs)
            {
                Console.Error.WriteLine(error);
            }
        }
    }
}