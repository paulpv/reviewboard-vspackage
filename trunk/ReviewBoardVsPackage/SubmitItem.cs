using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpSvn;

namespace org.reviewboard.ReviewBoardVs
{
    public class SubmitItem
    {
        string fullPath;
        SvnStatus status;
        string project;

        public SubmitItem(string fullPath, SvnStatus status, string project)
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

        public SvnStatus Status
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
