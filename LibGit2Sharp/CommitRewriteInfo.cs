namespace LibGit2Sharp
{
    public class CommitRewriteInfo
    {
        public Signature Author { get; set; }
        public Signature Committer { get; set; }
        public string Message { get; set; }
        public string Encoding { get; set; }

        public static CommitRewriteInfo SameAs(Commit c)
        {
            return new CommitRewriteInfo
                {
                    Author = c.Author,
                    Committer = c.Committer,
                    Message = c.Message,
                    Encoding = c.Encoding,
                };
        }

        public static CommitRewriteInfo From(Commit c, Signature author = null, Signature committer = null,
                                             string message = null, string encoding = null)
        {
            var cri = SameAs(c);
            cri.Author = author ?? cri.Author;
            cri.Committer = committer ?? cri.Committer;
            cri.Message = message ?? cri.Message;
            cri.Encoding = encoding ?? cri.Encoding;
            return cri;
        }
    }
}
