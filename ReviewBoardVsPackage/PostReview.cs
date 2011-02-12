using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace org.reviewboard.ReviewBoardVs
{
    public class PostReview
    {
        public static readonly string PostReviewExe = "post-review.exe";
        public static readonly string PostReviewRegExSubmitOk = @"Review request #(?<id>\d*?) posted\..*(?<uri>http(s?)://.*?/r/\d*)";
        public static readonly string PostReviewRegExDiffExternal = "[\"svn: '(.*?)' is not under version control\n\"]";

        public class PostReviewException : Exception
        {
            public int ExitCode { get; protected set; }
            public string StdOut { get; protected set; }
            public string StdErr { get; protected set; }

            public PostReviewException(string message, Exception e)
                : base(message, e)
            {
                ExitCode = 0;
                StdOut = null;
                StdErr = null;
            }

            public PostReviewException(string message, int exitCode, string stdout, string stderr)
                : base(message)
            {
                ExitCode = exitCode;
                StdOut = stdout;
                StdErr = stderr;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine(Message);
                if (InnerException != null)
                {
                    sb.AppendLine();
                    sb.AppendLine(InnerException.Message);
                }
                sb.AppendLine();
                sb.Append("ExitCode: ").Append(ExitCode).AppendLine();
                sb.AppendLine();
                sb.AppendLine(FormatOutput("Standard Output", StdOut, 10));
                sb.AppendLine();
                sb.AppendLine(FormatOutput("Error Output", StdErr, 10));

                return sb.ToString();
            }

            private static string FormatOutput(string name, string output, int lineCount)
            {
                StringBuilder message = new StringBuilder(name);
                if (String.IsNullOrEmpty(output))
                {
                    message.Append(": \"\"");
                }
                else
                {
                    int linesTotal;
                    int linesReturned;
                    string lastTenPreferredLines = MyUtils.GetLastXLines(output, lineCount, "    ", out linesTotal, out linesReturned);
                    message.Append(" (Last ").Append(linesReturned).AppendLine(" lines):");
                    if (linesTotal > linesReturned)
                    {
                        message.AppendLine("...");
                    }
                    message.Append(lastTenPreferredLines);
                }
                return message.ToString();
            }
        }

        protected static string TrimOutput(string output)
        {
            if (output != null)
            {
                output = output.Trim(new char[] { ' ', '\t', '\r', '\n' });
                if (output.Length == 0)
                {
                    output = null;
                }
            }
            return output;
        }

        #region DiffFile

        public enum DiffType
        {
            Normal,
            Added,
            Changed,
            Modified,
            External,
        }

        public static DiffType DiffFile(string path, out string diff)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new PostReviewException("PostReview.DiffFile", new ArgumentNullException("path"));
            }

            if (!File.Exists(path))
            {
                throw new PostReviewException("PostReview.DiffFile", new FileNotFoundException(path));
            }

            string directory = Path.GetDirectoryName(path);

            string stdout;
            string stderr;
            int exitCode;

            try
            {
                exitCode = MyUtils.ExecCommand(null, PostReviewExe, "-n --server=DUMMY " + path, directory, out stdout, out stderr);
            }
            catch (Exception e)
            {
                string message = String.Format("PostReview.DiffFile: Unexpected error executing {0}", PostReviewExe);
                throw new PostReviewException(message, e);
            }

            stdout = TrimOutput(stdout);
            stderr = TrimOutput(stderr);

            DiffType diffType;

            switch (exitCode)
            {
                case 0:
                    if (String.IsNullOrEmpty(stdout))
                    {
                        diffType = DiffType.Normal;
                        diff = null;
                    }
                    else
                    {
                        // TODO:(pv) Parse diff and determine if Change, Modified, Added (etc?)
                        diffType = DiffType.Changed;
                        diff = stdout;
                    }
                    break;

                case 1:
                    // Example: "["svn: 'filename.ext' is not under version control\n"]"
                    // TODO:(pv) make this even stricter by inserting path in to the regex
                    if (!Regex.IsMatch(stdout, PostReviewRegExDiffExternal, RegexOptions.Singleline))
                    {
                        goto default;
                    }

                    diffType = DiffType.External;
                    diff = null;
                    break;

                default:
                    string message = String.Format("PostReview.DiffFile: Error executing {0}", PostReview.PostReviewExe);
                    throw new PostReview.PostReviewException(message, exitCode, stdout, stderr);
            }

            return diffType;
        }

        #endregion DiffFile

        #region Submit

        public enum PostReviewOpen
        {
            None,
            Internal,
            External
        }

        public class ReviewInfo
        {
            public int Id { get; protected set; }
            public Uri Uri { get; protected set; }

            public ReviewInfo(int id, Uri uri)
            {
                Id = id;
                Uri = uri;
            }

            public override string ToString()
            {
                return new StringBuilder().Append(Id).Append(" - ").Append(Uri.AbsoluteUri).ToString();
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }

                if (obj.GetType() != this.GetType())
                {
                    return false;
                }

                ReviewInfo other = (ReviewInfo)obj;

                return Id.Equals(other.Id) && string.Compare(Uri.AbsoluteUri, other.Uri.AbsoluteUri, true) == 0;
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode() ^ Uri.GetHashCode();
            }
        }

        public static ReviewInfo Submit(BackgroundWorker worker, MyPackage package,
            string server, string username, string password, string submitAs,
            int reviewId, List<string> changes, bool publish, PostReviewOpen open, bool debug)
        {
            if (String.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException("username");
            }

            if (String.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException("password");
            }

            StringBuilder argumentsBuilder = new StringBuilder();

            if (!String.IsNullOrEmpty(server))
            {
                argumentsBuilder.Append("--server=").Append(server).Append(' ');
            }

            argumentsBuilder.Append("--username=").Append(username).Append(' ');
            argumentsBuilder.Append("--password=").Append(password).Append(' ');

            if (!String.IsNullOrEmpty(submitAs))
            {
                argumentsBuilder.Append("--submit-as=").Append(submitAs).Append(' ');
            }

            if (publish)
            {
                argumentsBuilder.Append("--publish ");
            }

            if (open == PostReviewOpen.External)
            {
                argumentsBuilder.Append("--open ");
            }

            if (debug)
            {
                argumentsBuilder.Append("--debug ");
            }

            if (reviewId > 0)
            {
                argumentsBuilder.Append("--review-request-id=").Append(reviewId).Append(" ");
            }
            for (int i = 0; i < changes.Count; i++)
            {
                if (i > 0)
                {
                    argumentsBuilder.Append(' ');
                }
                argumentsBuilder.Append(changes[i]);
            }

            string workingDirectory = MyUtils.GetCommonRoot(changes);
            string arguments = argumentsBuilder.ToString();

            StringBuilder commandLine = new StringBuilder();
            commandLine.Append(workingDirectory).Append('>').Append(PostReviewExe);
            if (!String.IsNullOrEmpty(arguments))
            {
                commandLine.Append(' ').Append(arguments);
            }
            package.OutputGeneral("Running: " + commandLine);

            string stdout;
            string stderr;
            int exitCode;

            try
            {
                exitCode = MyUtils.ExecCommand(worker, PostReviewExe, arguments, workingDirectory, out stdout, out stderr);
            }
            catch (Exception e)
            {
                string message = String.Format("PostReview.Submit: Unexpected error executing {0}", PostReviewExe);
                throw new PostReviewException(message, e);
            }

            if (exitCode != 0)
            {
                string message = String.Format("Error executing {0} submit", PostReviewExe);
                throw new PostReviewException(message, exitCode, stdout, stderr);
            }

            // Example: "Review request #145 posted.\r\n\r\nhttp://10.100.30.227/r/145\r\n"
            Match m = Regex.Match(stdout, PostReviewRegExSubmitOk, RegexOptions.Singleline);
            if (!m.Success)
            {
                string message = String.Format("Output does not match expected format {0}", PostReviewRegExSubmitOk);
                throw new PostReviewException(message, exitCode, stdout, stderr);
            }

            try
            {
                string id = m.Groups["id"].Value;
                string uri = m.Groups["uri"].Value;

                // The default page leaves the user wondering what to do next.
                // Direct the user the url to the more useful "diff" page.
                uri += "/diff/";

                if (reviewId != 0)
                {
                    // TODO:(pv) Compare the requested review id with the resulting review id.
                }

                reviewId = int.Parse(id);
                Uri reviewUri = new Uri(uri);

                return new ReviewInfo(reviewId, reviewUri);
            }
            catch (Exception e)
            {
                throw new PostReviewException("Error parsing id and url from output", e);
            }
        }

        #endregion Submit
    }
}
