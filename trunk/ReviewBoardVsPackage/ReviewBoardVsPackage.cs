using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using org.reviewboard.ReviewBoardVs.UI;

namespace org.reviewboard.ReviewBoardVs
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [DefaultRegistryRoot(@"Software\Microsoft\VisualStudio\9.0")]
    [InstalledProductRegistration(false, "#110", "#112", "0.1", IconResourceID = 400)]
    [ProvideLoadKey("Standard", "0.1", "ReviewBoardVs", "reviewboard.org", 1)]
    [ProvideMenuResource(1000, 1)]
    //[ProvideAutoLoad(GuidList.UICONTEXT_SolutionExists)] // Load on Solution activated
    [ProvideAutoLoad(GuidList.SccProviderId)] // Load on 'Scc active'
    [Guid(GuidList.guidReviewBoardVsPkgString)]
    public sealed class ReviewBoardVsPackage : MyPackage
    {
        private void TraceEnter(String methodName)
        {
            if (!methodName.Equals("()") && !methodName.StartsWith("."))
            {
                methodName = "." + methodName;
            }
            Trace.WriteLine(string.Format("+{0}{1}", this.ToString(), methodName));
        }

        private void TraceLeave(String methodName)
        {
            if (!methodName.Equals("()") && !methodName.StartsWith("."))
            {
                methodName = "." + methodName;
            }
            Trace.WriteLine(string.Format("-{0}{1}", this.ToString(), methodName));
        }

        public ReviewBoardVsPackage()
        {
            TraceEnter("()");
            TraceLeave("()");
        }

        protected override void Initialize()
        {
            TraceEnter("Initialize()");
            base.Initialize();

            OleMenuCommandService mcs = GetService<IMenuCommandService>() as OleMenuCommandService;
            if (null != mcs)
            {
                // Define commands ids as unique Guid/integer pairs...
                CommandID idReviewBoard = new CommandID(GuidList.guidReviewBoardVsCmdSet, PkgCmdIDList.cmdidReviewBoard);

                // Define the menu command callbacks...
                OleMenuCommand commandReviewBoard = new OleMenuCommand(new EventHandler(ReviewBoardCommand), idReviewBoard);
                // TODO:(pv) Only display ReviewBoard if svn status says selected item(s) have been changed
                //commandReviewBoard.BeforeQueryStatus += new EventHandler(commandReviewBoard_BeforeQueryStatus);

                // Add the menu commands to the command service...
                mcs.AddCommand(commandReviewBoard);
            }
            TraceLeave("Initialize()");
        }

        private void ReviewBoardCommand(object caller, EventArgs args)
        {
            // TODO:(pv) Show a dialog similar to a Tortoise/Ankh Commit dialog asking for the changed files to submit for code review.
            // TODO:(pv) Preselect most of the changed files according to the items selected in the Solution Explorer.
            // I am holding off doing this because it is a little complicated trying to figure out what the user intended to submit.
            // Does selecting a folder mean to also submit all files in that folder?
            // What if a few files/subfolders of that folder are also selected?
            // Should none of the other items be selected?
            // For now, just check *all* visible solution items for changes...

            IVsOutputWindowPane owp = GetOutputWindowPaneGeneral();
            if (owp != null)
            {
                owp.Activate();
            }

            FormSubmit form = new FormSubmit(this);
            if (form.ShowDialog() == DialogResult.OK)
            {
                PostReview.ReviewInfo reviewInfo = form.Review;
                if (reviewInfo != null)
                {
                    VsBrowseUrl(reviewInfo.Uri);
                }
            }
        }

        /*
        public IEnumerable<VSITEMSELECTION> GetCurrentSelection() 
        {
            IntPtr hierarchyPtr;
            uint itemid;
            IVsMultiItemSelect mis;
            IntPtr selectionContainer;

            // TODO:(pv) Remove/ignore any selected items that are children of another selected item...

            IVsMonitorSelection monitorSelection = GetMonitorSelection();
            if (ErrorHandler.Succeeded(monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out mis, out selectionContainer))) 
            { 
                uint count; 
                int singleHierarchy; 
 
                if ( mis != null && ErrorHandler.Succeeded( mis.GetSelectionInfo( out count, out singleHierarchy ) ) ) 
                { 
                    __VSGSIFLAGS options = 0; 
                    VSITEMSELECTION[] selection = new VSITEMSELECTION[count]; 
 
                    if ( ErrorHandler.Succeeded( mis.GetSelectedItems( (uint)options, count, selection ) ) ) 
                    { 
                        foreach ( VSITEMSELECTION item in selection ) 
                            yield return item; 
                    } 
                } 
                else 
                {
                    IVsHierarchy hierarchy = Marshal.GetTypedObjectForIUnknown(hierarchyPtr, typeof(IVsHierarchy)) as IVsHierarchy;
                    if ( hierarchy != null ) 
                    { 
                        yield return new VSITEMSELECTION() 
                        { 
                            pHier = hierarchy, 
                            itemid = itemid,
                        }; 
                    } 
                } 
            } 
        }
        */
    }
}
