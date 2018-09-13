using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Options;
using Octokit;
using Octokit.Internal;

namespace Lykke.NuGetReferencesScanner.Domain
{
    public sealed class GitHubScanner
    {
        private readonly ConcurrentDictionary<PackageReference, HashSet<RepoInfo>> _graph = new ConcurrentDictionary<PackageReference, HashSet<RepoInfo>>();
        private string _status;
        private DateTime? _lastUpDateTime;
        private int _packagesFound;
        private readonly Timer _timer;
        private readonly string _apiKey;

        public GitHubScanner(IOptions<AppSettings> settingsAccessor)
        {
            _apiKey = settingsAccessor.Value.NuGetScannerSettings.ApiKey;
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new ArgumentNullException(nameof(_apiKey));
            }
            _timer = new Timer(Scan);
            Start();
        }

        private void Start()
        {
            _timer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }

        private async void Scan(object state)
        {
            _graph.Clear();
            var client = new GitHubClient(new ProductHeaderValue("MyAmazingApp2"), new InMemoryCredentialStore(new Credentials(_apiKey)));

            var repoClient = client.Repository;

            var scr = new SearchCodeRequest("PackageReference Lykke")
            {
                Organization = "LykkeCity",
                Extension = "csproj"
            };

            try
            {
                _status = "Scanning";
                var searchResult = await client.Search.SearchCode(scr);

                _packagesFound = searchResult.TotalCount;

                for (int i = 0; i < _packagesFound / 100; i++)
                {
                    scr = new SearchCodeRequest("PackageReference Lykke")
                    {
                        Organization = "LykkeCity",
                        Extension = "csproj",
                        Page = i,
                    };

                    searchResult = await client.Search.SearchCode(scr);
                    Console.WriteLine($"Page {i} received {searchResult.Items.Count}");


                    foreach (var item in searchResult.Items)
                    {
                        var projectContent = await repoClient.Content.GetAllContents(item.Repository.Id, item.Path);
                        var repo = RepoInfo.Parse(item.Repository.FullName, item.Repository.HtmlUrl);
                        var nugetRefs = ProjectFileParser.Parse(projectContent[0].Content);

                        Console.WriteLine($"Repo name {item.Repository.Name} file name {item.Name}");

                        foreach (var nugetRef in nugetRefs)
                        {
                            if (!_graph.TryGetValue(nugetRef, out var repoInfos))
                            {
                                repoInfos = new HashSet<RepoInfo>();
                            }
                            repoInfos.Add(repo);
                            _graph[nugetRef] = repoInfos;
                        }

                        Thread.Sleep(500);
                    }
                }
                _lastUpDateTime = DateTime.UtcNow;
                _timer.Change(TimeSpan.FromHours(1), Timeout.InfiniteTimeSpan);
                _status = "Idle";

            }
            catch (Exception ex)
            {
                _status = $"Error. Restarting in 20 min. {ex.Message}";
                _timer.Change(TimeSpan.FromMinutes(20), Timeout.InfiniteTimeSpan);

            }
        }


        public ScanResult GetScanResult()
        {
            var flatResult = _graph.SelectMany(g => g.Value.Select(v => new Tuple<PackageReference, RepoInfo>(g.Key, v))).ToArray();
            var statString = $"Last update time {_lastUpDateTime}. NuGet packages found {_packagesFound}";

            return new ScanResult(_status, statString, flatResult);
        }
    }
}
