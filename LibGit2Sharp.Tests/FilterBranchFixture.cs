using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class FilterBranchFixture : BaseFixture
    {
        [Fact]
        public void CanRewriteHistoryWithoutChanginIt()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                var originalRefs = repo.Refs.ToList().OrderBy(r => r.CanonicalName);

                IEnumerable<Commit> enumerable = repo.Refs.Select(r => r.ResolveToDirectReference().Target).Where(o => o is Commit).Cast<Commit>();
                repo.Branches.RewriteHistory(enumerable, c => new CommitDetails { Committer = c.Committer, Author = c.Author, Message = c.Message });

                Assert.Equal(originalRefs, repo.Refs.ToList().OrderBy(r => r.CanonicalName));
            }
        }
    }
}
