using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace org.reviewboard.ReviewBoardVsx
{
    /// <summary>
    /// These Package Load Key settings are required for VS 2008 and earlier.
    /// If any of the below values change then a new PLK will need to be requested from:
    /// http://msdn.microsoft.com/en-us/vstudio/cc655795
    /// The resulting PLK then must be updated in VSPackage.resx.
    /// 
    /// To create an installer:
    ///     1) Start->"System Definition Model Command Prompt"
    ///     2) "cd ReviewBoardVsx\bin\Debug" (or wherever the primary output directory is)
    ///     3) regpkg /regfile:ReviewBoardVsx.reg "%CD%\ReviewBoardVsx.dll"
    ///         NOTE: the "%CD%" is important! (due to an apparent bug in the regpkg tool)
    ///
    /// TODO:(pv) Consider packaging this as a VISX and putting on http://visualstudiogallery.msdn.microsoft.com:
    ///     http://blogs.msdn.com/b/visualstudio/archive/2010/01/19/using-the-vsix-manifest-editor.aspx
    /// </summary>
    static class MyPackageLoadKey
    {
        public const string PackageId = "bbffe0ea-3383-4ea2-a281-528706f79d57";
        public const string MinimumVsEdition = "Standard";
        public const string Version = "0.1";
        public const string Product = "ReviewBoardVsx";
        public const string Company = "reviewboard.org";
        public const int KeyResourceId = 1; // resource id in VSPackage.resx (mandatory resource file for VS Packages)
    }

    static class MyPackageConstants
    {
        public const string PackageDescription = MyPackageLoadKey.Product + " - ReviewBoard Support for Visual Studio";

        //**********************************************************************************
        public const string AssemblyCopyright = "Copyright © " + MyPackageLoadKey.Company + " 2011";
        public const string AssemblyProduct = PackageDescription;
        public const string AssemblyCompany = MyPackageLoadKey.Company;

        //**********************************************************************************
        // Items for the VS 2010 Extension registration
        public const string ExtensionTitle = AssemblyProduct;
        public const string ExtensionAuthor = AssemblyCompany;
        public const string ExtensionDescription = "Open Source ReviewBoard Support for Visual Studio 2005, 2008 and 2010.";
        public const string ExtensionMoreInfoUrl = "http://www.reviewboard.org/";
        public const string ExtensionGettingStartedUrl = "http://www.reviewboard.org/";

        //**********************************************************************************
        // Required guids for the menu item(s) used by this package
        public const string CommandGroupId = "ed66aa69-7606-4fe2-852f-ecda6208097d";
        public const string CommandSetId = "d98ce002-7ba4-42ec-83d6-08492024ec22";

        //public static readonly Guid PackageIdGuid = new Guid(PackageLoadKey.PackageId);
        //public static readonly Guid CommandGroupIdGuid = new Guid(CommandGroupId);
        public static readonly Guid CommandSetIdGuid = new Guid(CommandSetId);
    }

    static class MyPackageCommandIds
    {
        public const int cmdIdReviewBoard = 0x100;
    }

    static class MyVsConstants
    {
        /// <summary>
        /// Found in "%ProgramFiles(x86)%\Microsoft Visual Studio 2008 SDK\VisualStudioIntegration\Common\inc\vsshlids.h"
        /// </summary>
        public const string UICONTEXT_SolutionExists = "f1536ef8-92ec-443c-9ed7-fdadf150da82";

        // Borrowed from AnkhSvn; no idea where these came from...
        /// <summary>The SCC Provider guid (used as SCC active marker by VS)</summary>
        public const string SccProviderId = "8770915b-b235-42ec-bbc6-8e93286e59b5";
        /// <summary>The GUID of the SCC Service</summary>
        public const string SccServiceId = "d8c473d2-9634-4513-91d5-e1a671fe2df4";
    }

    [ComVisible(true)]
    public class MyPackage : Package
    {
        protected void TraceEnter(String methodName)
        {
            if (!methodName.Equals("()") && !methodName.StartsWith("."))
            {
                methodName = "." + methodName;
            }
            Trace.WriteLine(string.Format("+{0}{1}", this.ToString(), methodName));
        }

        protected void TraceLeave(String methodName)
        {
            if (!methodName.Equals("()") && !methodName.StartsWith("."))
            {
                methodName = "." + methodName;
            }
            Trace.WriteLine(string.Format("-{0}{1}", this.ToString(), methodName));
        }

        /// <summary>
        /// Gets the item id.
        /// </summary>
        /// <param name="pvar">VARIANT holding an itemid.</param>
        /// <returns>Item Id of the concerned node</returns>
        public uint GetItemId(object pvar)
        {
            if (pvar == null) return VSConstants.VSITEMID_NIL;
            if (pvar is int) return (uint)(int)pvar;
            if (pvar is uint) return (uint)pvar;
            if (pvar is short) return (uint)(short)pvar;
            if (pvar is ushort) return (uint)(ushort)pvar;
            if (pvar is long) return (uint)(long)pvar;
            return VSConstants.VSITEMID_NIL;
        }

        public T GetService<T>() where T : class
        {
            return GetService<T>(typeof(T));
        }

        public T GetService<T>(Type type) where T : class
        {
            return GetService(type) as T;
        }

        public new Object GetService(Type type)
        {
            return base.GetService(type);
        }

        public IVsSolution GetSolution()
        {
            return GetService<SVsSolution>() as IVsSolution;
        }

        public IVsLaunchPad GetLaunchPad()
        {
            return GetService<SVsLaunchPad>() as IVsLaunchPad;
        }

        public IVsMonitorSelection GetMonitorSelection()
        {
            return Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
        }

        public static IVsOutputWindow GetOutputWindow()
        {
            return Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
        }

        public static IVsOutputWindowPane GetOutputWindowPaneGeneral()
        {
            IVsOutputWindowPane outWindowGeneralPane = null;
            IVsOutputWindow outputWindow = GetOutputWindow();
            if (outputWindow != null)
            {
                Guid guidGeneral = VSConstants.GUID_OutWindowGeneralPane;
                outputWindow.GetPane(ref guidGeneral, out outWindowGeneralPane);
            }
            return outWindowGeneralPane;
        }

        /// <summary>
        /// Prints to debug ouput and on the generic pane of the VS output window.
        /// </summary>
        /// <param name="text">text to send to Output Window.</param>
        public void OutputGeneral(string text)
        {
            Debug.WriteLine("OutputGeneral: " + text);

            // Build the string to write on the debugger and output window.
            StringBuilder outputText = new StringBuilder(text);
            outputText.AppendLine();

            IVsOutputWindowPane outputWindowPaneGeneral = GetOutputWindowPaneGeneral();
            if (outputWindowPaneGeneral == null)
            {
                Trace.WriteLine("Failed to get a reference to IVsOutputWindow");
                return;
            }

            if (ErrorHandler.Failed(outputWindowPaneGeneral.OutputString(outputText.ToString())))
            {
                Trace.WriteLine("Failed to write on the output window");
            }
        }

        public IVsWebBrowsingService GetWebBrowsingService()
        {
            return GetService<SVsWebBrowsingService>() as IVsWebBrowsingService;
        }

        public int VsBrowseUrl(Uri uri)
        {
            if (uri == null)
            {
                OutputGeneral("ERROR: url cannot be null");
                ErrorHandler.ThrowOnFailure(VSConstants.E_POINTER);
            }

            IVsWebBrowsingService browserService = GetWebBrowsingService();
            if (browserService == null)
            {
                OutputGeneral("ERROR: Cannot create browser service");
                ErrorHandler.ThrowOnFailure(VSConstants.E_UNEXPECTED);
            }

            Guid guidNull = Guid.Empty;
            IVsWindowFrame frame;
            IVsWebBrowser browser;
            uint flags = (uint)(__VSCREATEWEBBROWSER.VSCWB_AutoShow | __VSCREATEWEBBROWSER.VSCWB_StartCustom | __VSCREATEWEBBROWSER.VSCWB_ReuseExisting);
            return browserService.CreateWebBrowser(flags, ref guidNull, "", uri.AbsoluteUri, null, out browser, out frame);
        }

        /// <summary>
        /// Blocks until the command finishes executing
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="commandLine"></param>
        /// <param name="workingDirectory"></param>
        /// <returns></returns>
        public string VsExecCommand(string fileName, string arguments, string workingDirectory)
        {
            IVsLaunchPad lp = GetService(typeof(SVsLaunchPad)) as IVsLaunchPad;
            if (lp == null)
            {
                OutputGeneral("Failed to create launch pad");
                return null;
            }

            IVsOutputWindowPane owp = GetOutputWindowPaneGeneral();
            if (owp == null)
            {
                OutputGeneral("Failed to get output window general pane");
                return null;
            }

            string commandLine;
            if (String.IsNullOrEmpty(arguments))
            {
                commandLine = fileName;
            }
            else
            {
                StringBuilder sb = new StringBuilder(fileName);
                sb.Append(" ").Append(arguments);
                commandLine = sb.ToString();
            }

            uint exitCode = 0;
            string[] output = new string[1];
            int hr = lp.ExecCommand(fileName, commandLine, workingDirectory, (uint)_LAUNCHPAD_FLAGS.LPF_PipeStdoutToOutputWindow, owp, (uint)VSTASKCATEGORY.CAT_USER, 0, "", null, out exitCode, output);
            if (ErrorHandler.Failed(hr))
            {
                OutputGeneral(fileName + " failed to launch: hr=0x" + hr.ToString("X8"));
                return null;
            }

            OutputGeneral(fileName + " exited with exitCode " + exitCode);

            if (exitCode != 0)
            {
                return null;
            }

            return output[0];
        }
    }
}
