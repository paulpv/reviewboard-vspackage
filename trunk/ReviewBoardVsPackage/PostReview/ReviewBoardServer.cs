using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace org.reviewboard.ReviewBoardVs.PostReview
{
    /// <summary>
    /// Should closely parallel postreview.py class ReviewBoardServer.
    /// </summary>
    public class ReviewBoardServer
    {
        Uri uri;
        Object info;
        string cookieFilePath;
        Object serverInfo;

        public ReviewBoardServer(string url, Object repositoryInfo, string cookieFilePath)
        {
            this.uri = new Uri(url);
            this.info = repositoryInfo;
            this.cookieFilePath = cookieFilePath;

            serverInfo = null;
        }

        public bool HasValidCookie()
        {
            string host = uri.Host;
            string path = uri.AbsolutePath;

            // TODO:(pv) Ensure no port # in host
            //host = host.Split(':', 1);

            string message;
            message = String.Format("Looking for '{0} {1}' cookie in {2}", host, path, cookieFilePath);
            Debug.WriteLine(message);

            return false;
        }

        /// <summary>
        /// Returns the list of repositories on this server.
        /// </summary>
        /// <returns></returns>
        public Hashtable GetRepositories()
        {
#if PYTHON
            rsp = self.api_get('/api/json/repositories/')
            return rsp['repositories']
#endif
            return null;
        }

        /// <summary>
        /// Returns detailed information about a specific repository.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public RepositoryInfo GetRepositoryInfo(int id)
        {
#if PYTHON
            rsp = self.api_get('/api/json/repositories/%s/info/' % rid)
            return rsp['info']
#endif
            return null;
        }
    }
}
