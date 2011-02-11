using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace org.reviewboard.ReviewBoardVs.PostReview
{
    /// <summary>
    /// A representation of a source code repository.
    /// </summary>
    public class RepositoryInfo
    {
        public List<string> Paths { get; protected set; }

        string basePath;
        bool supportsChangeSets;
        bool supportsParentDiffs;

        public RepositoryInfo(string path, string basePath)
            : this(path, basePath, false, false)
        {
        }

        public RepositoryInfo(List<string> paths, string basePath)
            : this(paths, basePath, false, false)
        {
        }

        public RepositoryInfo(string path, string basePath, bool supportsChangeSets, bool supportsParentDiffs)
            : this(new List<string>() { path }, basePath, supportsChangeSets, supportsParentDiffs)
        {
        }

        public RepositoryInfo(List<string> paths, string basePath, bool supportsChangeSets, bool supportsParentDiffs)
        {
            Paths = new List<string>();

            this.Paths.AddRange(paths);
            this.basePath = basePath;
            this.supportsChangeSets = supportsChangeSets;
            this.supportsParentDiffs = supportsParentDiffs;
            Debug.WriteLine("repository info: " + this);
        }

        public override string ToString()
        {
            return String.Format("Path: {0}, Base path: {1}, Supports changesets: {2}, Supports parent diffs: {3}", Paths, basePath, supportsChangeSets, supportsParentDiffs);
        }

#if PYTHON
        def set_base_path(self, base_path):
            if not base_path.startswith('/'):
                base_path = '/' + base_path
            debug("changing repository info base_path from %s to %s" % \
                  (self.base_path, base_path))
            self.base_path = base_path
#endif

        /// <summary>
        /// Try to find the repository from the list of repositories on the server.
        /// For Subversion, this could be a repository with a different URL. For
        /// all other clients, this is a noop.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public virtual RepositoryInfo FindServerRepositoryInfo(ReviewBoardServer server)
        {
            return this;
        }
    }
}
