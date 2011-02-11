using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.reviewboard.ReviewBoardVs.PostReview
{
    /// <summary>
    /// A representation of a SVN source code repository. This version knows how to
    /// find a matching repository on the server even if the URLs differ.
    /// </summary>
    class SvnRepositoryInfo : RepositoryInfo
    {
        string uuid;

        public SvnRepositoryInfo(string path, string basePath, string uuid)
            : this(path, basePath, uuid, false)
        {
        }

        public SvnRepositoryInfo(string path, string basePath, string uuid, bool supportsParentDiffs)
            : base(path, basePath, false, supportsParentDiffs)
        {
            this.uuid = uuid;
        }

        /// <summary>
        /// The point of this function is to find a repository on the server that
        /// matches self, even if the paths aren't the same. (For example, if self
        /// uses an 'http' path, but the server uses a 'file' path for the same
        /// repository.) It does this by comparing repository UUIDs. If the
        /// repositories use the same path, you'll get back self, otherwise you'll
        /// get a different SvnRepositoryInfo object (with a different path).
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public override RepositoryInfo FindServerRepositoryInfo(ReviewBoardServer server)
        {
            Hashtable repositories = server.GetRepositories();
            foreach(Hashtable repository in repositories)
            {
                if ("Subversion" != repository["tool"] as string)
                    continue;
#if PYTHON
                info = self._get_repository_info(server, repository)

                if not info or self.uuid != info['uuid']:
                    continue

                repos_base_path = info['url'][len(info['root_url']):]
                relpath = self._get_relative_path(self.base_path, repos_base_path)
                if relpath:
                    return SvnRepositoryInfo(info['url'], relpath, self.uuid)
#else
            }

            //# We didn't find a matching repository on the server. We'll just return
            //# self and hope for the best.
            return this;
        }
#endif

        public RepositoryInfo GetRepositoryInfo(ReviewBoardServer server, Hashtable repository)
        {
            try
            {
                string id = repository["id"] as string;
                return server.GetRepositoryInfo(int.Parse(id));
            }
            catch
            {
#if PYTHON
            except APIError, e:
                //# If the server couldn't fetch the repository info, it will return
                //# code 210. Ignore those.
                //# Other more serious errors should still be raised, though.
                if e.error_code == 210:
                    return None

                raise e
#endif
                throw;
            }
        }

#if PYTHON
        def _get_relative_path(self, path, root):
            pathdirs = self._split_on_slash(path)
            rootdirs = self._split_on_slash(root)

            //# root is empty, so anything relative to that is itself
            if len(rootdirs) == 0:
                return path

            //# If one of the directories doesn't match, then path is not relative
            //# to root.
            if rootdirs != pathdirs:
                return None

            //# All the directories matched, so the relative path is whatever
            //# directories are left over. The base_path can't be empty, though, so
            //# if the paths are the same, return '/'
            if len(pathdirs) == len(rootdirs):
                return '/'
            else:
                return '/'.join(pathdirs[len(rootdirs):])

        def _split_on_slash(self, path):
            //# Split on slashes, but ignore multiple slashes and throw away any
            //# trailing slashes.
            split = re.split('/*', path)
            if split[-1] == '':
                split = split[0:-1]
            return split
#endif
    }
}
