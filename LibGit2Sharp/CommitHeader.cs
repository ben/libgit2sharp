namespace LibGit2Sharp
{
    public class CommitHeader
    {
        public Signature Author { get; set; }
        public Signature Committer { get; set; }
        public string Message { get; set; }
        public string Encoding { get; set; }

        public static CommitHeader From(Commit c)
        {
            return new CommitHeader
                {
                    Author = c.Author,
                    Committer = c.Committer,
                    Message = c.Message,
                    Encoding = c.Encoding,
                };
        }
    }
}
