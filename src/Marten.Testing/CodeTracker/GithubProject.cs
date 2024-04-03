using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JasperFx.Core;
using Newtonsoft.Json;

//using Octokit;
using FileMode = System.IO.FileMode;

namespace Marten.Testing.CodeTracker;

public class GithubProject
{
    private readonly IList<Timestamped> _events = new List<Timestamped>();

    public static GithubProject LoadFrom(string file)
    {
        var serializer = new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.All
        };

        var json = File.ReadAllText(file);

        return serializer.Deserialize<GithubProject>(new JsonTextReader(new StringReader(json)));
    }

    public GithubProject()
    {
    }

    public GithubProject(string organizationName, string projectName, DateTimeOffset createdAt)
    {
        OrganizationName = organizationName;
        ProjectName = projectName;

        _events.Add(new ProjectStarted
        {
            Name = projectName,
            Organization = organizationName,
            Timestamp = createdAt
        });
    }

    public readonly Guid Id = Guid.NewGuid();

    public string OrganizationName { get; set; }
    public string ProjectName { get; set; }

    public Timestamped[] Events
    {
        get { return _events.ToArray(); }
        set
        {
            _events.Clear();
            _events.AddRange(value);
        }
    }

    /*
    public void RecordIssue(Issue issue)
    {
        _events.Add(new IssueCreated
        {
            Description = issue.Title,
            UserName = issue.User.Login,
            Timestamp = issue.CreatedAt,
            Number = issue.Number
        });

        if (issue.State == ItemState.Closed)
        {
            _events.Add(new IssueClosed
            {
                UserName = issue.User.Login,
                Timestamp = issue.ClosedAt.Value,
                Number = issue.Number
            });

            if (_random.Next(0, 10) > 8)
            {
                _events.Add(new IssueReopened
                {
                    UserName = issue.User.Login,
                    Timestamp = issue.ClosedAt.Value.AddDays(1),
                    Number = issue.Number
                });
            }
        }
    }
    */

    public void SaveTo(string directory)
    {
        var serializer = new JsonSerializer
        {
            TypeNameHandling = TypeNameHandling.All
        };

        var file = directory.AppendPath(ProjectName + ".json");

        using (var stream = new FileStream(file, FileMode.Create))
        {
            var writer = new StreamWriter(stream);
            serializer.Serialize(new JsonTextWriter(writer), this);

            writer.Flush();
        }
    }

    /*
    public void RecordCommit(GitHubCommit commit, GitHubCommitStats stats)
    {
        var @event = new Commit
        {
            Sha = commit.Sha,
            Message = commit.Commit.Message,
            Timestamp = commit.Commit.Author.Date,
            Additions = stats.Additions,
            Deletions = stats.Deletions,
            UserName = commit.Author?.Login
        };

        _events.Add(@event);
    }
    */

    public async Task PublishEvents(IDocumentStore store, int pause, bool createEventGaps)
    {
        var events = Events.OrderBy(x => x.Timestamp).ToList();
        var started = events.OfType<ProjectStarted>().Single();
        events.Remove(started);
        events.Insert(0, started);

        var index = 0;
        var page = events.Skip(index).Take(10).ToArray();

        while (page.Length > 0)
        {
            await using (var session = store.LightweightSession())
            {
                session.Events.Append(Id, page);

                if (createEventGaps)
                {
                    for (int i = 0; i < 200; i++)
                    {
                        session.Events.Append(Id, new PlaceHolder());
                    }
                }

                await session.SaveChangesAsync();
            }

            index += 10;

            page = events.Skip(index).Take(10).ToArray();

            Thread.Sleep(pause);
        }
    }
}

public class PlaceHolder
{
}
