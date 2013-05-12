namespace LibGit2Sharp
{
    public class CommitRewriteInfo
    {
        public Signature Author { get; set; }
        public Signature Committer { get; set; }
        public string Message { get; set; }
        public string Encoding { get; set; }

        public static CommitRewriteInfo From(Commit c)
        {
            return new CommitRewriteInfo
                {
                    Author = c.Author,
                    Committer = c.Committer,
                    Message = c.Message,
                    Encoding = c.Encoding,
                };
        }
    }
}
