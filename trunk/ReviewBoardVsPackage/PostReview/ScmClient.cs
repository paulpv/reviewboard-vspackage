using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace org.reviewboard.ReviewBoardVs.PostReview
{
    /// <summary>
    /// A base representation of an SCM tool for fetching repository information
    /// and generating diffs.
    /// </summary>
    public abstract class ScmClient
    {
        protected PostReview postReview;

        public ScmClient(PostReview postReview)
        {
            this.postReview = postReview;
        }

        public abstract RepositoryInfo GetRepositoryInfo(string dirPath);

        /// <summary>
        /// Scans the current directory on up to find a .reviewboardrc file
        /// containing the server path.
        /// </summary>
        /// <param name="repositoryInfo"></param>
        /// <returns></returns>
        public virtual string ScanForServer(RepositoryInfo repositoryInfo, string dirPath)
        {
            string serverUrl = GetServerFromConfig(postReview.UserConfig, repositoryInfo);
            if (!String.IsNullOrEmpty(serverUrl))
            {
                return serverUrl;
            }

            if (File.Exists(dirPath))
            {
                dirPath = Path.GetDirectoryName(dirPath);
            }

            foreach(string path in MyUtils.WalkParents(dirPath))
            {
                string filename = Path.Combine(path, ".reviewboardrc");
                if (File.Exists(filename))
                {
                    Hashtable config = postReview.LoadConfigFile(filename);
                    serverUrl = GetServerFromConfig(config, repositoryInfo);
                    if (!String.IsNullOrEmpty(serverUrl))
                    {
                        return serverUrl;
                    }
                }
            }

            return null;
        }

        protected string GetServerFromConfig(Hashtable config, RepositoryInfo repositoryInfo)
        {
            if (config.ContainsKey("REVIEWBOARD_URL"))
            {
                return config["REVIEWBOARD_URL"] as string;
            }
            else if (config.ContainsKey("TREES"))
            {
                Hashtable trees = config["TREES"] as Hashtable;
                if (trees == null)
                {
                    throw new Exception("Warning: 'TREES' in config file is not a dict!");
                }

                foreach (string path in repositoryInfo.Paths)
                {
                    if (trees.ContainsKey(path))
                    {
                        Hashtable tree = trees[path] as Hashtable;
                        if (tree != null && tree.ContainsKey("REVIEWBOARD_URL"))
                        {
                            return tree["REVIEWBOARD_URL"] as string;
                        }
                    }
                }
            }

            return null;
        }

        public abstract void Diff(out string diffString, out string parentDiffString, string file);

        public abstract void Diff(out string diffString, out string parentDiffString, IEnumerable<string> files);
    }
}
