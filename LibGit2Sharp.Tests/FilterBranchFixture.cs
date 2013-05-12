using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;
using Xunit.Sdk;

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
                var commits = repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray();

                // Noop header rewriter
                repo.RewriteHistory(commits, commitHeaderRewriter: CommitRewriteInfo.SameAs);
                Assert.Equal(originalRefs,
                             repo.Refs.Where(x => !x.CanonicalName.StartsWith("refs/original"))
                                 .OrderBy(r => r.CanonicalName)
                                 .ToList());
                Assert.Equal(commits, repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray());

                // Noop tree rewriter
                repo.RewriteHistory(commits, commitTreeRewriter: TreeDefinition.From);
                Assert.Equal(originalRefs,
                             repo.Refs.Where(x => !x.CanonicalName.StartsWith("refs/original"))
                                 .OrderBy(r => r.CanonicalName)
                                 .ToList());
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

                var nonTagRefs = repo.Refs.Where(x => !x.IsTag()).Where(x => !x.CanonicalName.StartsWith("refs/original"));
                Assert.Empty(repo.Commits.QueryBy(new Filter { Since = nonTagRefs })
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

        [Fact]
        public void CanCustomizeBackupRefNames()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                repo.RewriteHistory(repo.Head.Commits, c => CommitRewriteInfo.From(c, message: ""));
                Assert.NotEmpty(repo.Refs.Where(x => x.CanonicalName.StartsWith("refs/original")));

                Assert.Empty(repo.Refs.Where(x => x.CanonicalName.StartsWith("refs/rewritten")));
                repo.RewriteHistory(repo.Head.Commits,
                    commitHeaderRewriter: c => CommitRewriteInfo.From(c, message: "abc"),
                    referenceNameRewriter: x => "refs/rewritten/" + x);
                Assert.NotEmpty(repo.Refs.Where(x => x.CanonicalName.StartsWith("refs/rewritten")));
            }
        }

        [Fact]
        public void DoesNotRewriteRefsThatDontChange()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                repo.RewriteHistory(new[] { repo.Lookup<Commit>("c47800c") },
                                    c => CommitRewriteInfo.From(c, message: "abc"));
                Assert.Null(repo.Refs["refs/original/heads/packed-test"]);
                Assert.NotNull(repo.Refs["refs/original/heads/br2"]);
            }
        }


        // This test should rewrite br2, but not packed-test:
        // *   a4a7dce (br2) Merge branch 'master' into br2
        // |\
        // | * 9fd738e a fourth commit
        // | * 4a202b3 (packed-test) a third commit
        // * | c47800c branch commit one                <----- rewrite this one
        // |/
        // * 5b5b025 another commit
        // * 8496071 testing
        [Fact]
        public void HandlesExistingBackedUpRefs()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                var commits = repo.Commits.QueryBy(new Filter { Since = repo.Refs }).ToArray();
                repo.RewriteHistory(commits, commitHeaderRewriter: c => CommitRewriteInfo.From(c, message: "abc"));

                throw new AssertException("What should we do here?");
                repo.RewriteHistory(commits, commitHeaderRewriter: c => CommitRewriteInfo.From(c, message: "abc"));
                Assert.Empty(repo.Refs.Where(x => x.CanonicalName.StartsWith("refs/original/original/")));
            }
        }

        [Fact]
        public void CanRewriteParents()
        {
            string path = CloneBareTestRepo();
            using (var repo = new Repository(path))
            {
                // Graft the orphan "test" branch to the tip of "packed"
                // Before:
                // * e90810b  (test, lw, e90810b, test)
                // |
                // * 6dcf9bf
                //
                // * 41bc8c6  (packed)
                // |
                // * 5001298
                // ... and after:
                // * f558880  (test, lw, e90810b, test)
                // |
                // * 0c25efa
                // |   <---- add this link
                // * 41bc8c6  (packed)
                // |
                // * 5001298
                var commitToRewrite = repo.Lookup<Commit>("6dcf9bf");
                var newParent = repo.Lookup<Commit>("41bc8c6");
                bool hasBeenCalled = false;

                repo.RewriteHistory(new[] { commitToRewrite }, parentRewriter: originalParents =>
                {
                    Assert.Empty(originalParents);
                    Assert.False(hasBeenCalled);
                    hasBeenCalled = true;
                    return new[] { newParent };
                });

                Assert.Contains(newParent, repo.Lookup<Commit>("refs/heads/test~").Parents);
            }
        }
    }
}
