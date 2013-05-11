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
                IEnumerable<Commit> enumerable = repo.Refs.Select(r => r.ResolveToDirectReference().Target).Where(o => o is Commit).Cast<Commit>();

                // Noop header rewriter
                repo.Branches.RewriteHistory(enumerable, commitHeaderRewriter: c => CommitHeader.From(c));
                Assert.Equal(originalRefs, repo.Refs.ToList().OrderBy(r => r.CanonicalName));

                // Noop tree rewriter
                repo.Branches.RewriteHistory(enumerable, commitTreeRewriter: c => TreeDefinition.From(c.Tree));
                Assert.Equal(originalRefs, repo.Refs.ToList().OrderBy(r => r.CanonicalName));
            }
        }
    }
}
