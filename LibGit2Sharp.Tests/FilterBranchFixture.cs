using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class FilterBranchFixture : BaseFixture
    {
        [Fact]
        public void CanRewriteHistoryWithoutChangingIt()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                var originalRefs = repo.Refs.ToList().OrderBy(r => r.CanonicalName);
                var commits = repo.Commits.QueryBy(new Filter{Since = repo.Refs}).ToArray();

                // Noop header rewriter
                repo.Branches.RewriteHistory(commits, commitHeaderRewriter: CommitHeader.From);
                Assert.Equal(originalRefs, repo.Refs.ToList().OrderBy(r => r.CanonicalName));
                Assert.Equal(commits, repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray());

                // Noop tree rewriter
                repo.Branches.RewriteHistory(commits, commitTreeRewriter: TreeDefinition.From);
                Assert.Equal(originalRefs, repo.Refs.ToList().OrderBy(r => r.CanonicalName));
                Assert.Equal(commits, repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray());
            }
        }

        [Fact]
        public void CanRewriteAuthor()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var commits = repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray();
                repo.Branches.RewriteHistory(commits, commitHeaderRewriter: c =>
                    {
                        var h = CommitHeader.From(c);
                        h.Author = new Signature("Ben Straub", "me@example.com", h.Author.When);
                        return h;
                    });

                IEnumerable<Commit> collection = repo.Commits.QueryBy(new Filter {Since = repo.Refs.Where(x => !x.IsTag())}).Where(c => c.Author.Name != "Ben Straub");
                Assert.Empty(collection);
            }
        }
    }
}
