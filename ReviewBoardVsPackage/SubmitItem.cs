using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SharpSvn;

namespace org.reviewboard.ReviewBoardVs
{
    public class SubmitItem
    {
        public enum PathStatus
        {
            None,
            Added,
            Changed,
            Modified,
        }

        string fullPath;
        PathStatus status;
        string project;

        public SubmitItem(string fullPath, PathStatus status, string project)
        {
            this.fullPath = fullPath;
            this.status = status;
            this.project = project;
        }

        public string FullPath
        {
            get
            {
                return fullPath;
            }
        }

        public PathStatus Status
        {
            get
            {
                return status;
            }
        }

        public string Project
        {
            get
            {
                return project;
            }
        }
    }
}
