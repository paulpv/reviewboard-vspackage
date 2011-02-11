using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Procurios.Public; // JSON

namespace org.reviewboard.ReviewBoardVs.PostReview
{
    /// <summary>
    /// 
    /// Rememeber, post-review.exe is run from the command-line of a current working directory.
    /// You can never have subdirs using different SCMs or traversing outside of the current working directory.
    /// 
    /// A Visual Studio Solution is a lot more complicated!
    /// A VS Solution, especially C++ solutions with "filters", can pull files from arbitrary file paths.
    /// As an added complication, each file path could potentially be under a different SCM.
    /// EACH FILE MUST BE CHECKED FOR SCM AND CHANGE!
    /// 
    /// This code will try to cache file status and SCM lookups so that future lookups in that file's folder will be faster.
    /// 
    /// </summary>
    public class PostReview
    {
        public static string ADD_REPOSITORY_DOCS_URL = "http://www.reviewboard.org/docs/manual/dev/admin/management/repositories/";
        public static string GNU_DIFF_WIN32_URL = "http://gnuwin32.sourceforge.net/packages/diffutils.htm";

        public string HomePath
        {
            get
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
        }

        public string CookieFilePath
        {
            get
            {
                return Path.Combine(HomePath, ".post-review-cookies.txt");
            }
        }

        ScmClient[] SCMCLIENTS;

        public Hashtable UserConfig { get; protected set; }

        public PostReview()
        {
            UserConfig = LoadConfigFile(Path.Combine(HomePath, ".reviewboardrc"));

            SCMCLIENTS = new ScmClient[]
            {
                new SvnClient(this),
            };
        }

        public void Debug(string format, params object[] args)
        {
            System.Diagnostics.Debug.WriteLine(String.Format(format, args));
        }

        public Hashtable LoadConfigFile(string filePath)
        {
            Hashtable config = new Hashtable();
            config.Add("TREES", new Hashtable());

            if (File.Exists(filePath))
            {
                TextReader file = File.OpenText(filePath);
                string json = file.ReadToEnd();

                bool success = true;
                config = (Hashtable)JSON.JsonDecode(json, ref success);
            }

            return config;
        }

        public void DetermineClient(out RepositoryInfo repositoryInfo, out ScmClient tool, string dirPath)
        {
            repositoryInfo = null;
            tool = null;

            //# Try to find the SCM Client we're going to be working with.
            for (int i = 0; i < SCMCLIENTS.Length; i++)
            {
                tool = SCMCLIENTS[i];
                repositoryInfo = tool.GetRepositoryInfo(dirPath);
                if (repositoryInfo != null)
                    break;
            }

            if (repositoryInfo == null)
            {
                tool = null;

                return;
            }
#if false
            if not repository_info:
                if options.repository_url:
                    print "No supported repository could be access at the supplied url."
                else:
                    print "The current directory does not contain a checkout from a"
                    print "supported source code repository."
                sys.exit(1)

            //# Verify that options specific to an SCM Client have not been mis-used.
            if options.change_only and not repository_info.supports_changesets:
                sys.stderr.write("The --change-only option is not valid for the "
                                 "current SCM client.\n")
                sys.exit(1)

            if options.parent_branch and not repository_info.supports_parent_diffs:
                sys.stderr.write("The --parent option is not valid for the "
                                 "current SCM client.\n")
                sys.exit(1)

            if ((options.p4_client or options.p4_port) and \
                not isinstance(tool, PerforceClient)):
                sys.stderr.write("The --p4-client and --p4-port options are not valid "
                                 "for the current SCM client.\n")
                sys.exit(1)
#endif
        }
    }
}
