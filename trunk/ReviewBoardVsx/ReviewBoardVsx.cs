using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using org.reviewboard.ReviewBoardVsx.UI;

namespace org.reviewboard.ReviewBoardVsx
{
    // This attribute tells the registration utility (regpkg.exe) that this class needs to be registered as package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Description(MyPackageConstants.PackageDescription)]
    // A Visual Studio component can be registered under different regitry roots.
    // For instance, when you debug your package you want to register it in the experimental hive.
    // This attribute specifies the registry root to use if one is not provided to regpkg.exe with the /root switch.
    // TODO:(pv) Is this "default" OK on non-VS2K8 installs?
    [DefaultRegistryRoot(@"Software\Microsoft\VisualStudio\9.0")]
    [InstalledProductRegistration(false, "#110", "#112", "0.1", IconResourceID = 400)]
    [Guid(MyPackageLoadKey.PackageId)]
    // A Package Load Key is required for Visual Studio 2008 and earlier on a machine that does not have the VS SDK installed.
    // A PLK can be requested at http://msdn.microsoft.com/vstudio/extend/
    // This attributes tells the shell that this package has a Package Load Key embedded in its resources.
    [ProvideLoadKey(MyPackageLoadKey.MinimumVsEdition, MyPackageLoadKey.Version, MyPackageLoadKey.Product, MyPackageLoadKey.Company, MyPackageLoadKey.KeyResourceId)]
    [ProvideAutoLoad(MyVsConstants.UICONTEXT_SolutionExists)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource(1000, 1)]
    public sealed class ReviewBoardVsx : MyPackage
    {
        public ReviewBoardVsx()
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
                CommandID idReviewBoard = new CommandID(MyPackageConstants.CommandSetIdGuid, PkgCmdIdList.cmdIdReviewBoard);

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
            // TODO:(pv) Preselect most of the changed files according to the items selected in the Solution Explorer.
            // See below "GetCurrentSelection()" code.
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
