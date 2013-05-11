using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   The collection of Branches in a <see cref = "Repository" />
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class BranchCollection : IEnumerable<Branch>
    {
        internal readonly Repository repo;

        /// <summary>
        ///   Needed for mocking purposes.
        /// </summary>
        protected BranchCollection()
        { }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "BranchCollection" /> class.
        /// </summary>
        /// <param name = "repo">The repo.</param>
        internal BranchCollection(Repository repo)
        {
            this.repo = repo;
        }

        /// <summary>
        ///   Gets the <see cref = "LibGit2Sharp.Branch" /> with the specified name.
        /// </summary>
        public virtual Branch this[string name]
        {
            get
            {
                Ensure.ArgumentNotNullOrEmptyString(name, "name");

                if (LooksLikeABranchName(name))
                {
                    return BuildFromReferenceName(name);
                }

                Branch branch = BuildFromReferenceName(ShortToLocalName(name));
                if (branch != null)
                {
                    return branch;
                }

                branch = BuildFromReferenceName(ShortToRemoteName(name));
                if (branch != null)
                {
                    return branch;
                }

                return BuildFromReferenceName(ShortToRefName(name));
            }
        }

        private static string ShortToLocalName(string name)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", Reference.LocalBranchPrefix, name);
        }

        private static string ShortToRemoteName(string name)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", Reference.RemoteTrackingBranchPrefix, name);
        }

        private static string ShortToRefName(string name)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}", "refs/", name);
        }

        private Branch BuildFromReferenceName(string canonicalName)
        {
            var reference = repo.Refs.Resolve<Reference>(canonicalName);
            return reference == null ? null : new Branch(repo, reference, canonicalName);
        }

        #region IEnumerable<Branch> Members

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        public virtual IEnumerator<Branch> GetEnumerator()
        {
            return Proxy.git_branch_foreach(repo.Handle, GitBranchType.GIT_BRANCH_LOCAL | GitBranchType.GIT_BRANCH_REMOTE, branchToCanoncialName)
                .Select(n => this[n])
                .GetEnumerator();
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref = "IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        ///   Create a new local branch with the specified name
        /// </summary>
        /// <param name = "name">The name of the branch.</param>
        /// <param name = "commit">The target commit.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Add(string name, Commit commit, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNullOrEmptyString(name, "name");
            Ensure.ArgumentNotNull(commit, "commit");

            using (Proxy.git_branch_create(repo.Handle, name, commit.Id, allowOverwrite)) {}

            return this[ShortToLocalName(name)];
        }

        /// <summary>
        ///   Deletes the specified branch.
        /// </summary>
        /// <param name = "branch">The branch to delete.</param>
        public virtual void Remove(Branch branch)
        {
            Ensure.ArgumentNotNull(branch, "branch");

            using (ReferenceSafeHandle referencePtr = repo.Refs.RetrieveReferencePtr(branch.CanonicalName))
            {
                Proxy.git_branch_delete(referencePtr);
            }
        }

        /// <summary>
        ///   Renames an existing local branch with a new name.
        /// </summary>
        /// <param name = "branch">The current local branch.</param>
        /// <param name = "newName">The new name the existing branch should bear.</param>
        /// <param name = "allowOverwrite">True to allow silent overwriting a potentially existing branch, false otherwise.</param>
        /// <returns>A new <see cref="Branch"/>.</returns>
        public virtual Branch Move(Branch branch, string newName, bool allowOverwrite = false)
        {
            Ensure.ArgumentNotNull(branch, "branch");
            Ensure.ArgumentNotNullOrEmptyString(newName, "newName");

            if (branch.IsRemote)
            {
                throw new LibGit2SharpException(
                    string.Format(CultureInfo.InvariantCulture,
                        "Cannot rename branch '{0}'. It's a remote tracking branch.", branch.Name));
            }

            using (ReferenceSafeHandle referencePtr = repo.Refs.RetrieveReferencePtr(Reference.LocalBranchPrefix + branch.Name))
            {
                using (ReferenceSafeHandle ref_out = Proxy.git_branch_move(referencePtr, newName, allowOverwrite))
                {
                }
            }

            return this[newName];
        }

        /// <summary>
        ///   Update properties of a branch.
        /// </summary>
        /// <param name="branch">The branch to update.</param>
        /// <param name="actions">Delegate to perform updates on the branch.</param>
        /// <returns>The updated branch.</returns>
        public virtual Branch Update(Branch branch, params Action<BranchUpdater>[] actions)
        {
            var updater = new BranchUpdater(repo, branch);

            foreach (Action<BranchUpdater> action in actions)
            {
                action(updater);
            }

            return this[branch.Name];
        }

        private static bool LooksLikeABranchName(string referenceName)
        {
            return referenceName == "HEAD" ||
                referenceName.LooksLikeLocalBranch() ||
                referenceName.LooksLikeRemoteTrackingBranch();
        }

        private static string branchToCanoncialName(IntPtr namePtr, GitBranchType branchType)
        {
            string shortName = Utf8Marshaler.FromNative(namePtr);

            switch (branchType)
            {
                case GitBranchType.GIT_BRANCH_LOCAL:
                    return ShortToLocalName(shortName);
                case GitBranchType.GIT_BRANCH_REMOTE:
                    return ShortToRemoteName(shortName);
                default:
                    return shortName;
            }
        }

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "Count = {0}", this.Count());
            }
        }

        public virtual void RewriteHistory(
            IEnumerable<Commit> commits,
            Func<Commit, CommitHeader> commitHeaderRewriter = null,
            Func<Commit, TreeDefinition> commitTreeRewriter = null)
        {
            IList<Reference> originalRefs = repo.Refs.ToList();
            if (originalRefs.Count == 0)
            {
                // No ref to rewrite. What should we do here? Silently return? Throw InvalidOperationException?
                return;
            }

            commitHeaderRewriter = commitHeaderRewriter ?? CommitHeader.From;
            commitTreeRewriter = commitTreeRewriter ?? TreeDefinition.From;

            // Find out which refs lead to at which one the commits
            var refsToRewrite = repo.Refs.SubsetOfTheseReferencesThatCanReachAnyOfTheseCommits(repo.Refs, commits);

            // TODO Back up the refs to refs/original

            var shaMap = new Dictionary<Commit, Commit>();
            foreach (var commit in repo.Commits.QueryBy(new Filter { Since = refsToRewrite, SortBy = GitSortOptions.Reverse | GitSortOptions.Topological}))
            {
                // Get the new commit header
                var newHeader = commitHeaderRewriter(commit);

                // Get the new commit tree
                var newTreeDefinition = commitTreeRewriter(commit);
                var newTree = repo.ObjectDatabase.CreateTree(newTreeDefinition);

                // Find the new parents
                var newParents = commit.Parents.Select(oldParent => shaMap.ContainsKey(oldParent) ? shaMap[oldParent] : oldParent).ToList();

                // Create the new commit
                var newCommit = repo.ObjectDatabase.CreateCommit(newHeader.Message, newHeader.Author,
                                                                 newHeader.Committer, newTree,
                                                                 newParents);

                // Record the rewrite
                shaMap[commit] = newCommit;
            }

            // Rewrite the refs
            foreach (var reference in refsToRewrite)
            {
                // Symbolic ref? Leave it alone
                if (reference is SymbolicReference)
                    continue;

                // Avoid tags; chaining can get hairy
                if (reference.IsTag())
                    continue;

                // Direct ref? Overwrite it, point to the new commit
                var directRef = reference as DirectReference;
                var oldCommit = directRef.Target as Commit;
                if (oldCommit == null) continue;
                if (shaMap.ContainsKey(oldCommit))
                {
                    repo.Refs.UpdateTarget(directRef, shaMap[oldCommit].Id, "filter branch");
                }
            }
        }
    }
}
