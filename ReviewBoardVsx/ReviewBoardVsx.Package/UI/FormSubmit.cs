using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using ReviewBoardVsx.Package;

namespace ReviewBoardVsx.UI
{
    public partial class FormSubmit : Form
    {
        public class SubmitItem
        {
            public string FullPath { get; protected set; }
            public string Project { get; protected set; }
            public PostReview.DiffType DiffType { get; protected set; }
            public string Diff { get; protected set; }

            public SubmitItem(string fullPath, string project, PostReview.DiffType diffType, string diff)
            {
                FullPath = fullPath;
                Project = project;
                DiffType = diffType;
                Diff = diff;
            }
        }

        public PostReview.ReviewInfo Review { get; protected set; }

        MyPackage package;

        public FormSubmit(MyPackage package)
        {
            InitializeComponent();

            this.package = package;
        }

        private void FormSubmit_Load(object sender, EventArgs e)
        {
            if (DesignMode)
                return;

            Point location = Properties.Settings.Default.Location;
            Size size = Properties.Settings.Default.Size;
            if (!location.IsEmpty && !size.IsEmpty)
            {
                Rectangle rect = new Rectangle(location, size);
                if (MyUtils.IsOnScreen(rect))
                    DesktopBounds = rect;
            }

            // Enabled by listPaths_ItemChecked validation
            buttonOk.Enabled = false;

            InitializeReviewIds(false);
        }

        private void FormSubmit_Shown(object sender, EventArgs e)
        {
            // DoFindSolutionChanges will finish initializing remaining controls as and after it finishes crawling the solution
            DoFindSolutionChanges(this);
        }

        private void buttonClearReviewIds_Click(object sender, EventArgs e)
        {
            InitializeReviewIds(true);
        }

        private void FormSubmit_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If the post-review was successful, save the review info to the list for next time.
            if (DialogResult == DialogResult.OK)
            {
                PostReview.ReviewInfo reviewInfo = Review;
                if (reviewInfo != null)
                {
                    // Always insert new items just below the "<New>" entry
                    if (!comboReviewIds.Items.Contains(reviewInfo))
                    {
                        comboReviewIds.Items.Insert(1, Review);
                        Properties.Settings.Default.reviewIdHistory = new ArrayList(comboReviewIds.Items);
                    }
                }
            }
            else
            {
                Review = null;
            }

            Properties.Settings.Default.Location = this.DesktopBounds.Location;
            Properties.Settings.Default.Size = this.DesktopBounds.Size;
            Properties.Settings.Default.Save();
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            // DoPostReview will call FormSubmit_FormClosing after PostReview has finished
            DoPostReview(this);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void buttonOk_UpdateEnable(object sender, EventArgs e)
        {
            buttonOk.Enabled = listPaths.CheckedItems.Count > 0;
            buttonOk.Enabled &= !String.IsNullOrEmpty(textBoxUsername.Text);
            buttonOk.Enabled &= !String.IsNullOrEmpty(textBoxPassword.Text);
        }

        /// <summary>
        /// The 0th item is special; it is a hard coded string, NOT a ReviewInfo type.
        /// All the other items should be a ReviewInfo type.
        /// </summary>
        /// <param name="clear"></param>
        private void InitializeReviewIds(bool clear)
        {
            ArrayList values = Properties.Settings.Default.reviewIdHistory;
            if (values == null)
            {
                values = new ArrayList();
            }

            if (values.Count == 0 || clear)
            {
                values.Clear();
                values.Add(Resources.ReviewIdNew);
                Properties.Settings.Default.reviewIdHistory = new ArrayList(values);
            }

            object[] items = values.ToArray();

            comboReviewIds.BeginUpdate();
            comboReviewIds.Items.Clear();
            comboReviewIds.Items.AddRange(items);
            // TODO:(pv) Remember last selected review id/index?
            if (comboReviewIds.Items.Count > 0)
                comboReviewIds.SelectedIndex = 0;
            comboReviewIds.EndUpdate();
        }

        #region Private property getters

        private int GetSelectedReviewId()
        {
            int reviewId;
            switch (comboReviewIds.SelectedIndex)
            {
                case -1:
                    // Pre-validated comboReviewIds_KeyDown in and comboReviewIds_TextUpdate
                    // Should never throw an exception
                    reviewId = int.Parse(comboReviewIds.Text);
                    break;
                case 0:
                    reviewId = 0;
                    break;
                default:
                    // Should never throw InvalidCastException
                    PostReview.ReviewInfo reviewInfo = (PostReview.ReviewInfo)comboReviewIds.SelectedItem;
                    reviewId = reviewInfo.Id;
                    break;
            }
            return reviewId;
        }

        private List<string> GetCheckedFullPaths()
        {
            ListView.CheckedListViewItemCollection checkedItems = listPaths.CheckedItems;
            List<string> checkedFullPaths = new List<string>(checkedItems.Count);
            foreach (ListViewItem item in checkedItems)
            {
                checkedFullPaths.Add(item.SubItems["FullPath"].Text);
            }
            return checkedFullPaths;
        }

        #endregion Private property getters

        #region comboReviewIds keyboard/mouse input handlers

        private void comboReviewIds_MouseClick(object sender, MouseEventArgs e)
        {
            if (comboReviewIds.SelectedIndex == 0)
            {
                comboReviewIds.SelectAll();
            }
        }

        /// <summary>
        /// Very ugly nazi function that has the audacity to try to control the keys that are allowed to be pressed.
        /// I don't like doing this, but I couldn't find any better way to prevent users from entering invalid data.
        /// The road to hell is paved with good intentions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboReviewIds_KeyDown(object sender, KeyEventArgs e)
        {
            bool allow = true;

            // The spirit of this method is to have comboReviewIds accept only keys '0'-'9'.
            // In reality this is too strict; we allow cut/copy/paste [and perhaps a few others if needed].

            if (comboReviewIds.SelectedIndex == -1 || comboReviewIds.SelectedIndex == 0)
            {
                // Both free-hand-edit-mode and hard-coded "<New>" allow 0-9 or cut/copy/paste
                allow = (MyUtils.IsDigit((Char)e.KeyValue)) || MyUtils.IsCutCopyPaste(e.KeyValue, e.Modifiers);

                switch ((Keys)e.KeyValue)
                {
                    case Keys.NumLock:
                        //case Keys.Up:
                        //case Keys.Down:
                        //case Keys.PageUp:
                        //case Keys.PageDown:
                        // TODO:(pv) Allow up/down/pgup/pgdn to pull up dropdown and navigate items...
                        allow = true;
                        break;
                }

                if (comboReviewIds.SelectedIndex == -1)
                {
                    // free-hand-edit-mode adds allowing horizontal cursor movement keys
                    switch ((Keys)e.KeyValue)
                    {
                        case Keys.Back:
                        case Keys.Insert:
                        case Keys.Home:
                        case Keys.Delete:
                        case Keys.End:
                        case Keys.Left:
                        case Keys.Right:
                            allow = true;
                            break;
                    }
                }

                e.SuppressKeyPress = !allow;
            }

            if (!allow)
            {
                // Play a rejection sound if they pressed a printable character without pressing CTRL
                if (comboReviewIds.SelectedIndex == 0 || !Char.IsControl((Char)e.KeyValue) && !e.Control)
                {
                    string path = MyUtils.PathCombine(Environment.SystemDirectory, "..", "Media", "Windows Ding.wav");
                    if (File.Exists(path))
                    {
                        // TODO:(pv) Uh, why can I not hear this play?
                        new SoundPlayer(path).Play();
                    }
                }
            }
        }

        private void comboReviewIds_TextUpdate(object sender, EventArgs e)
        {
            if (comboReviewIds.SelectedIndex == -1)
            {
                string text = comboReviewIds.Text;
                if (!String.IsNullOrEmpty(text))
                {
                    try
                    {
                        // Validate free-hand-edit-mode entered review # as integer
                        int.Parse(text);
                    }
                    catch
                    {
                        MessageBox.Show(this, "Invalid number entered", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        comboReviewIds.Text = text.Substring(0, text.Length - 1);
                        comboReviewIds.SelectAll();
                    }
                }
            }
        }

        private void comboReviewIds_TextChanged(object sender, EventArgs e)
        {
            if (comboReviewIds.SelectedIndex == -1)
            {
                if (String.IsNullOrEmpty(comboReviewIds.Text))
                {
                    // Empty string; go back to selecting "<New>" item (always index == 0)

                    // BUGBUG:(pv) Toggling DroppedDown is the only way I can get the SelectedIndex to stick...
                    comboReviewIds.DroppedDown = true;
                    comboReviewIds.SelectedIndex = 0;
                    comboReviewIds.DroppedDown = false;
                }
            }
        }

        #endregion comboReviewIds keyboard/mouse input handlers

        #region FindSolutionChanges

        // TODO:(pv) Make this static, like DoPostReview
        void DoFindSolutionChanges(FormSubmit form)
        {
            DoWorkEventHandler handlerFindSolutionChanges = (s, e) =>
            {
                BackgroundWorker bw = s as BackgroundWorker;

                IVsSolution solution = package.GetSolution();
                if (solution == null)
                {
                    package.OutputGeneral("ERROR: Cannot get solution object");
                    ErrorHandler.ThrowOnFailure(VSConstants.E_UNEXPECTED);
                }

                List<SubmitItem> changes = new List<SubmitItem>();

                EnumHierarchyItems(bw, (IVsHierarchy)solution, VSConstants.VSITEMID_ROOT, 0, true, true, changes);

                e.Result = changes;

                if (bw.CancellationPending)
                {
                    e.Cancel = true;
                }
            };

            FormProgress progress = new FormProgress("Finding solution changes...", "Finding solution changes...", handlerFindSolutionChanges);

            progress.FormClosed += (s, e) =>
            {
                bool cancel = false;

                Exception error = progress.Error;
                if (error != null)
                {
                    StringBuilder message = new StringBuilder();

                    message.AppendLine("Error finding solution changes:");
                    message.AppendLine();
                    if (error is PostReview.PostReviewException)
                    {
                        message.Append(error.ToString());
                        message.AppendLine();
                        message.Append("Make sure ").Append(PostReview.PostReviewExe).AppendLine(" is in your PATH!");
                    }
                    else
                    {
                        message.Append(error.Message);
                    }
                    message.AppendLine();
                    message.Append("Click \"OK\" to return to Visual Studio.");

                    MessageBox.Show(form, message.ToString(), "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    cancel = true;
                }
                else
                {
                    List<SubmitItem> solutionChanges = (List<SubmitItem>)progress.Result;
                    if (solutionChanges == null)
                    {
                        cancel = true;
                    }
                    else
                    {
                        OnFindSolutionChangesDone(solutionChanges);
                    }
                }

                if (cancel)
                {
                    form.DialogResult = DialogResult.Cancel;
                    form.Close();
                }
            };

            progress.ShowDialog(form);
        }

        private void OnFindSolutionChangesDone(List<SubmitItem> solutionChanges)
        {
            if (solutionChanges == null)
            {
                solutionChanges = new List<SubmitItem>();
            }

            string commonRoot = MyUtils.GetCommonRoot(new List<string>(solutionChanges.Select(p => p.FullPath))) + '\\';
            commonRoot = Regex.Escape(commonRoot);

            string pathFull;
            string pathShort;
            ListViewItem item;

            listPaths.BeginUpdate();
            listPaths.Items.Clear();
            foreach (SubmitItem solutionChange in solutionChanges)
            {
                pathFull = solutionChange.FullPath;
                pathShort = Regex.Replace(pathFull, commonRoot, "", RegexOptions.IgnoreCase);
                item = listPaths.Items.Add(pathShort);
                item.SubItems.Add(solutionChange.Project).Name = "Project";
                item.SubItems.Add(solutionChange.DiffType.ToString()).Name = "Change";
                item.SubItems.Add(pathFull).Name = "FullPath";
            }

            ColumnHeaderAutoResizeStyle resizeStyle = (solutionChanges.Count == 0) ? ColumnHeaderAutoResizeStyle.HeaderSize : ColumnHeaderAutoResizeStyle.ColumnContent;
            foreach (ColumnHeader columnHeader in listPaths.Columns)
            {
                columnHeader.AutoResize(resizeStyle);
            }
            // TODO:(pv) Sort Project by Solution, Solution Items, Project(s)...
            listPaths.EndUpdate();
        }

        /// <summary>
        /// Code almost 100% taken from VS SDK Example: SolutionHierarchyTraversal
        /// </summary>
        /// <param name="worker"></param>
        /// <param name="hierarchy"></param>
        /// <param name="itemid"></param>
        /// <param name="recursionLevel"></param>
        /// <param name="hierIsSolution"></param>
        /// <param name="visibleNodesOnly"></param>
        /// <param name="changes"></param>
        /// <returns>true if the caller should continue, false if the caller should stop</returns>
        private bool EnumHierarchyItems(BackgroundWorker worker, IVsHierarchy hierarchy, uint itemid, int recursionLevel, bool hierIsSolution, bool visibleNodesOnly, List<SubmitItem> changes)
        {
            if (worker != null && worker.CancellationPending)
            {
                return false;
            }

            int hr;
            IntPtr nestedHierarchyObj;
            uint nestedItemId;
            Guid hierGuid = typeof(IVsHierarchy).GUID;

            // Check first if this node has a nested hierarchy. If so, then there really are two 
            // identities for this node: 1. hierarchy/itemid 2. nestedHierarchy/nestedItemId.
            // We will recurse and call EnumHierarchyItems which will display this node using
            // the inner nestedHierarchy/nestedItemId identity.
            hr = hierarchy.GetNestedHierarchy(itemid, ref hierGuid, out nestedHierarchyObj, out nestedItemId);
            if (VSConstants.S_OK == hr && IntPtr.Zero != nestedHierarchyObj)
            {
                IVsHierarchy nestedHierarchy = Marshal.GetObjectForIUnknown(nestedHierarchyObj) as IVsHierarchy;
                Marshal.Release(nestedHierarchyObj);    // we are responsible to release the refcount on the out IntPtr parameter
                if (nestedHierarchy != null)
                {
                    // Display name and type of the node in the Output Window
                    EnumHierarchyItems(worker, nestedHierarchy, nestedItemId, recursionLevel, false, visibleNodesOnly, changes);
                }
            }
            else
            {
                object pVar;

                // Display name and type of the node in the Output Window
                ProcessNode(worker, hierarchy, itemid, recursionLevel, changes);

                recursionLevel++;

                // Get the first child node of the current hierarchy being walked
                // NOTE: to work around a bug with the Solution implementation of VSHPROPID_FirstChild,
                // we keep track of the recursion level. If we are asking for the first child under
                // the Solution, we use VSHPROPID_FirstVisibleChild instead of _FirstChild. 
                // In VS 2005 and earlier, the Solution improperly enumerates all nested projects
                // in the Solution (at any depth) as if they are immediate children of the Solution.
                // Its implementation _FirstVisibleChild is correct however, and given that there is
                // not a feature to hide a SolutionFolder or a Project, thus _FirstVisibleChild is 
                // expected to return the identical results as _FirstChild.
                hr = hierarchy.GetProperty(itemid,
                    ((visibleNodesOnly || (hierIsSolution && recursionLevel == 1) ?
                        (int)__VSHPROPID.VSHPROPID_FirstVisibleChild : (int)__VSHPROPID.VSHPROPID_FirstChild)),
                    out pVar);
                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
                if (VSConstants.S_OK == hr)
                {
                    // We are using Depth first search so at each level we recurse to check if the node has any children
                    // and then look for siblings.
                    uint childId = package.GetItemId(pVar);
                    while (childId != VSConstants.VSITEMID_NIL)
                    {
                        if (!EnumHierarchyItems(worker, hierarchy, childId, recursionLevel, false, visibleNodesOnly, changes))
                        {
                            break;
                        }

                        // NOTE: to work around a bug with the Solution implementation of VSHPROPID_NextSibling,
                        // we keep track of the recursion level. If we are asking for the next sibling under
                        // the Solution, we use VSHPROPID_NextVisibleSibling instead of _NextSibling. 
                        // In VS 2005 and earlier, the Solution improperly enumerates all nested projects
                        // in the Solution (at any depth) as if they are immediate children of the Solution.
                        // Its implementation   _NextVisibleSibling is correct however, and given that there is
                        // not a feature to hide a SolutionFolder or a Project, thus _NextVisibleSibling is 
                        // expected to return the identical results as _NextSibling.
                        hr = hierarchy.GetProperty(childId,
                            ((visibleNodesOnly || (hierIsSolution && recursionLevel == 1)) ?
                                (int)__VSHPROPID.VSHPROPID_NextVisibleSibling : (int)__VSHPROPID.VSHPROPID_NextSibling),
                            out pVar);
                        if (VSConstants.S_OK == hr)
                        {
                            childId = package.GetItemId(pVar);
                        }
                        else
                        {
                            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
                            break;
                        }
                    }
                }
            }

            return (worker == null || !worker.CancellationPending);
        }

        private void ProcessNode(BackgroundWorker worker, IVsHierarchy hierarchy, uint itemId, int recursionLevel, List<SubmitItem> changes)
        {
            try
            {
                Debug.WriteLine("+ProcessNode(...): itemId=" + itemId);

                int hr;

                Object oRootName;
                hr = hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_Name, out oRootName);
                if (ErrorHandler.Failed(hr))
                {
                    package.OutputGeneral("ERROR: Could not get root name of item #" + itemId);
                    ErrorHandler.ThrowOnFailure(hr);
                }
                string rootName = oRootName as string;
                if (String.IsNullOrEmpty(rootName))
                {
                    rootName = Resources.RootUnknown;
                }
                Debug.WriteLine("rootName=" + rootName);

                string itemName;
                hr = hierarchy.GetCanonicalName(itemId, out itemName);
                if (ErrorHandler.Failed(hr))
                {
                    switch(hr)
                    {
                        case VSConstants.E_NOTIMPL:
                            // ignore; Nothing to do if we cannot get the file name, but below logic can handle null/empty name...
                            itemName = null;
                            break;
                        default:
                            package.OutputGeneral("ERROR: Could not get canonical name of item #" + itemId);
                            ErrorHandler.ThrowOnFailure(hr);
                            break;
                    }
                }
                Debug.WriteLine("itemName=\"" + itemName + "\"");

#if DEBUG && false
                if (!String.IsNullOrEmpty(itemName))
                {
                    // Temporary until we call AddFilePathIfChanged after we find out what the item type is
                    worker.ReportProgress(0, itemName);
                }
#endif

                Guid guidTypeNode;
                hr = hierarchy.GetGuidProperty(itemId, (int)__VSHPROPID.VSHPROPID_TypeGuid, out guidTypeNode);
                if (ErrorHandler.Failed(hr))
                {
                    switch(hr)
                    {
                        case VSConstants.E_NOTIMPL:
                            Debug.WriteLine("Guid property E_NOTIMPL for item #" + itemId + " \"" + itemName + "\"; assuming virtual/reference item and ignoring");
                            // ignore; Below logic can handle Guid.Empty
                            guidTypeNode = Guid.Empty;
                            break;
                        case VSConstants.DISP_E_MEMBERNOTFOUND:
                            Debug.WriteLine("Guid property DISP_E_MEMBERNOTFOUND for item #" + itemId + " \"" + itemName + "\"; assuming reference item and ignoring");
                            guidTypeNode = Guid.Empty;
                            break;
                        default:
                            package.OutputGeneral("ERROR: Could not get type guid of item #" + itemId + " \"" + itemName + "\"");
                            ErrorHandler.ThrowOnFailure(hr);
                            break;
                    }
                }
                Debug.WriteLine("guidTypeNode=" + guidTypeNode);

                //
                // Intentionally ordered from most commonly expected to least commonly expected...
                //
                if (Guid.Equals(guidTypeNode, VSConstants.GUID_ItemType_PhysicalFile))
                {
                    AddFilePathIfChanged(worker, itemName, rootName, changes);
                }
                else if (itemId == VSConstants.VSITEMID_ROOT)
                {
                    IVsProject project = hierarchy as IVsProject;
                    if (project != null)
                    {
                        string projectFile;
                        project.GetMkDocument(VSConstants.VSITEMID_ROOT, out projectFile);
                        AddFilePathIfChanged(worker, projectFile, rootName, changes);
                    }
                    else
                    {
                        IVsSolution solution = hierarchy as IVsSolution;
                        if (solution != null)
                        {
                            rootName = Resources.RootSolution;

                            string solutionDirectory, solutionFile, solutionUserOptions;
                            ErrorHandler.ThrowOnFailure(solution.GetSolutionInfo(out solutionDirectory, out solutionFile, out solutionUserOptions));
                            AddFilePathIfChanged(worker, solutionFile, rootName, changes);
                        }
                        else
                        {
                            package.OutputGeneral("ERROR: itemid==VSITEMID_ROOT, but hierarchy is neither Solution or Project");
                            ErrorHandler.ThrowOnFailure(VSConstants.E_UNEXPECTED);
                        }
                    }
                }
#if DEBUG
                else if (Guid.Equals(guidTypeNode, VSConstants.GUID_ItemType_PhysicalFolder))
                {
                    Debug.WriteLine("ignoring GUID_ItemType_PhysicalFolder");
                    // future enumeration will handle any individual subitems in this folder...
                }
                else if (Guid.Equals(guidTypeNode, VSConstants.GUID_ItemType_VirtualFolder))
                {
                    Debug.WriteLine("ignoring GUID_ItemType_VirtualFolder");
                    // future enumeration will handle any individual subitems in this virtual folder...
                }
                else if (Guid.Equals(guidTypeNode, VSConstants.GUID_ItemType_SubProject))
                {
                    Debug.WriteLine("ignoring GUID_ItemType_SubProject");
                    // future enumeration will handle any individual subitems in this sub project...
                }
                else if (Guid.Equals(guidTypeNode, Guid.Empty))
                {
                    Debug.WriteLine("ignoring itemName=" + itemName + "; guidTypeNode == Guid.Empty");
                    // future enumeration will handle any individual subitems in this item...
                }
                else
                {
                    package.OutputGeneral("ERROR: Unhandled node item/type itemName=" + itemName + ", guidTypeNode=" + guidTypeNode);
                    ErrorHandler.ThrowOnFailure(VSConstants.E_UNEXPECTED);
                }
#endif
            }
            finally
            {
                Debug.WriteLine("-ProcessNode(...): itemId=" + itemId);
            }
        }

        public void AddFilePathIfChanged(BackgroundWorker worker, string filePath, string project, List<SubmitItem> changes)
        {
            try
            {
                Debug.WriteLine("+AddFilePathIfChanged(\"" + filePath + "\", \"" + project + "\", changes(" + changes.Count + "))");

                filePath = MyUtils.GetCasedFilePath(filePath);
                if (String.IsNullOrEmpty(filePath))
                {
                    // If we got this far then the *Solution* says there is a file.
                    // However, the file can be an external/symbolic link/reference, not an actual file.
                    // Ignore this situation and just continue the enumeration.
                    return;
                }

                // Percent is currently always 0, since our progress is indeterminate
                // TODO:(pv) Find some way to determine # of nodes in tree *before* processing
                // NOTE:(pv) I did have the debugger halt here complaining invalid state that the form is not active
                worker.ReportProgress(0, filePath);

                string diff;
                PostReview.DiffType diffType = PostReview.DiffFile(filePath, out diff);

                switch(diffType)
                {
                    case PostReview.DiffType.Added:
                    case PostReview.DiffType.Changed:
                    case PostReview.DiffType.Modified:
                        SubmitItem change = new SubmitItem(filePath, project, diffType, diff);
                        changes.Add(change);
                        break;
                    case PostReview.DiffType.External:
                        // TODO:(pv) Even add External items?
                        // Doesn't make much sense, since they won't diff during post-review.exe submit.
                        // Maybe could add them and group them at bottom as disabled.
                        break;
                    case PostReview.DiffType.Normal:
                        // TODO:(pv) Even add normal items?
                        // Doesn't make much sense, since they won't diff during post-review.exe submit.
                        // Maybe could add them and group them at bottom as disabled.
                        break;
                    default:
                        string message = String.Format("Unhandled DiffType={0}", diffType);
                        throw new ArgumentOutOfRangeException("diffType", message);
                }
            }
            finally
            {
                Debug.WriteLine("-AddFilePathIfChanged(\"" + filePath + "\", \"" + project + "\", changes(" + changes.Count + "))");
            }
        }

        #endregion FindSolutionChanges

        #region DoPostReview

        protected static void DoPostReview(FormSubmit form)
        {
            int reviewId = form.GetSelectedReviewId();
            List<string> changes = form.GetCheckedFullPaths();

            DoWorkEventHandler handlerPostReview = (s, e) =>
            {
                BackgroundWorker bw = s as BackgroundWorker;

                MyPackage package = form.package;

                string server = form.textBoxServer.Text;
                string username = form.textBoxUsername.Text;
                string password = form.textBoxPassword.Text;

                string submitAs = null;
                bool publish = false;
                PostReview.PostReviewOpen open = PostReview.PostReviewOpen.Internal;
                bool debug = false;

                e.Result = PostReview.Submit(bw, package, server, username, password, submitAs, reviewId, changes, publish, open, debug);

                if (bw.CancellationPending)
                {
                    e.Cancel = true;
                }
            };

            string label = String.Format("Uploading Code Review #{0} ({1} files)...", reviewId, changes.Count);

            FormProgress progress = new FormProgress("Uploading...", label, handlerPostReview);

            progress.FormClosed += (s, e) =>
            {
                bool close = true;

                PostReview.PostReviewException pre = (PostReview.PostReviewException)progress.Error;
                if (pre != null)
                {
                    StringBuilder message = new StringBuilder();
                    message.AppendLine(pre.ToString());
                    message.AppendLine();
                    message.AppendLine("Click \"Retry\" to return to dialog, or \"Cancel\" to return to Visual Studio.");

                    if (MessageBox.Show(form, message.ToString(), "ERROR", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    {
                        close = false;
                    }
                }

                if (close)
                {
                    form.Review = progress.Result as PostReview.ReviewInfo;
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                }
            };

            progress.ShowDialog(form);
        }

        #endregion DoPostReview
    }
}
