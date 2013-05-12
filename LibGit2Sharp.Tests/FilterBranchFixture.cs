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
                repo.RewriteHistory(commits, commitHeaderRewriter: CommitRewriteInfo.SameAs);
                Assert.Equal(originalRefs, repo.Refs.ToList().OrderBy(r => r.CanonicalName));
                Assert.Equal(commits, repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray());

                // Noop tree rewriter
                repo.RewriteHistory(commits, commitTreeRewriter: TreeDefinition.From);
                Assert.Equal(originalRefs, repo.Refs.ToList().OrderBy(r => r.CanonicalName));
                Assert.Equal(commits, repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray());
            }
        }

        [Fact]
        public void CanRewriteAuthorOfCommitsNotBeingPointedAtByTags()
        {
            string path = CloneStandardTestRepo();
            using (var repo = new Repository(path))
            {
                var commits = repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray();
                repo.RewriteHistory(commits, commitHeaderRewriter: c =>
                        CommitRewriteInfo.From(c, author: new Signature("Ben Straub", "me@example.com", c.Author.When)));

                var nonTagRefs = repo.Refs.Where(x => !x.IsTag());
                Assert.Empty(repo.Commits.QueryBy(new Filter {Since = nonTagRefs})
                                 .Where(c => c.Author.Name != "Ben Straub"));
            }
        }

        [Fact]
        public void CanRewriteTrees()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                repo.RewriteHistory(repo.Head.Commits, commitTreeRewriter: c =>
                    {
                        var td = TreeDefinition.From(c);
                        td.Remove("README");
                        return td;
                    });

                Assert.Empty(repo.Head.Commits.Where(c => c["README"] != null));
            }
        }
    }
}
