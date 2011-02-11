using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using SharpSvn;

namespace org.reviewboard.ReviewBoardVs.PostReview
{
    public class SvnClient : ScmClient
    {
        SharpSvn.SvnClient svnClient;

        public SvnClient(PostReview postReview)
            : base(postReview)
        {
            svnClient = new SharpSvn.SvnClient();
        }

        public override RepositoryInfo GetRepositoryInfo(string dirPath)
        {
            RepositoryInfo repositoryInfo = null;

            SvnInfoEventArgs svnInfoEventArgs;
            if (svnClient.GetInfo(dirPath, out svnInfoEventArgs))
            {
                string path = svnInfoEventArgs.RepositoryRoot.AbsoluteUri;
                string basePath = svnInfoEventArgs.Uri.AbsoluteUri;
                string uuid = svnInfoEventArgs.RepositoryIdValue;

                if (!String.IsNullOrEmpty(path) && !String.IsNullOrEmpty(basePath) && !String.IsNullOrEmpty(uuid))
                {
                    repositoryInfo = new SvnRepositoryInfo(path, basePath, uuid);
                }
            }

            return repositoryInfo;
        }

        public override string ScanForServer(RepositoryInfo repositoryInfo, string dirPath)
        {
            //# Scan first for dot files, since it's faster and will cover the
            //# user's $HOME/.reviewboardrc
            string serverUrl = base.ScanForServer(repositoryInfo, dirPath);
            if (!String.IsNullOrEmpty(serverUrl))
            {
                return serverUrl;
            }

            return ScanForServerProperty(repositoryInfo, dirPath);
        }

        public string ScanForServerProperty(RepositoryInfo repositoryInfo, string dirPath)
        {
            string reviewBoardUrl;

            foreach (string path in MyUtils.WalkParents(dirPath))
            {
                if (!Directory.Exists(Path.Combine(path, ".svn")))
                {
                    break;
                }

                svnClient.GetProperty(path, "reviewboard:url", out reviewBoardUrl);
                if (!String.IsNullOrEmpty(reviewBoardUrl))
                {
                    return reviewBoardUrl;
                }
            }

            return null;
        }

        public override void Diff(out string diffString, out string parentDiffString, string file)
        {
            SvnRevisionRange svnRevisionRange = new SvnRevisionRange(SvnRevision.Head, SvnRevision.Working);

            SvnDiffArgs svnDiffArgs = new SvnDiffArgs();
            //svnDiffArgs.DiffArguments.Add("--diff-cmd=diff");

            using (MemoryStream stream = new MemoryStream())
            {
                svnClient.Diff(file, svnRevisionRange, svnDiffArgs, stream);
                stream.Flush();
                stream.Position = 0;

                using (StreamReader reader = new StreamReader(stream))
                {
                    diffString = reader.ReadToEnd();
                    parentDiffString = null;
                }
            }
        }

        public override void Diff(out string diffString, out string parentDiffString, IEnumerable<string> files)
        {
            throw new NotImplementedException();
        }

        public Collection<SvnStatusEventArgs> GetStatus(string path)
        {
            return SvnGetStatus(path, SvnDepth.Infinity);
        }

        public Collection<SvnStatusEventArgs> SvnGetStatus(string path, SvnDepth depth)
        {
            Collection<SvnStatusEventArgs> statuses = null;

            // Subversion is case-sensitive
            path = SvnTools.GetTruePath(path);
            if (!String.IsNullOrEmpty(path))
            {
                SvnStatusArgs args = new SvnStatusArgs();
                args.Depth = depth;

                // TODO:(pv) Test if path/folder is working copy

                svnClient.GetStatus(path, args, out statuses);
            }

            return statuses;
        }
    }
}
