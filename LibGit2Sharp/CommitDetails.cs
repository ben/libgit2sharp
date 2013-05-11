namespace LibGit2Sharp
{
    public class CommitDetails
    {
        public Signature Author { get; set; }
        public Signature Committer { get; set; }
        public string Message { get; set; }
    }
}
