namespace LibGit2Sharp
{
    /// <summary>
    /// Class for containing commit metadata when rewriting history
    /// </summary>
    public class CommitRewriteInfo
    {
        /// <summary>
        /// The author to be used for the new commit
        /// </summary>
        public Signature Author { get; set; }

        /// <summary>
        /// The committer to be used for the new commit
        /// </summary>
        public Signature Committer { get; set; }

        /// <summary>
        /// The message to be used for the new commit
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Match the <see cref="Commit"/> passed in
        /// </summary>
        /// <param name="commit">The <see cref="Commit"/> whose information is to be copied</param>
        /// <returns>A new <see cref="CommitRewriteInfo"/> object that matches the info for the <paramref name="commit"/>.</returns>
        public static CommitRewriteInfo SameAs(Commit commit)
        {
            return new CommitRewriteInfo
                {
                    Author = commit.Author,
                    Committer = commit.Committer,
                    Message = commit.Message,
                };
        }

        /// <summary>
        /// Match but optionally override the <see cref="Commit"/> passed in
        /// </summary>
        /// <param name="commit">The <see cref="Commit"/> whose information is to be copied</param>
        /// <param name="author">Optional override for the author</param>
        /// <param name="committer">Optional override for the committer</param>
        /// <param name="message">Optional override for the message</param>
        /// <returns>A new <see cref="CommitRewriteInfo"/> object that matches the info for the
        /// <paramref name="commit"/> with the optional parameters replaced..</returns>
        public static CommitRewriteInfo From(Commit commit,
                                             Signature author = null,
                                             Signature committer = null,
                                             string message = null)
        {
            var cri = SameAs(commit);
            cri.Author = author ?? cri.Author;
            cri.Committer = committer ?? cri.Committer;
            cri.Message = message ?? cri.Message;
            return cri;
        }
    }
}
