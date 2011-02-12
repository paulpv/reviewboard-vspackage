using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections;
using System.ComponentModel;
using System.Data;
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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace org.reviewboard.ReviewBoardVs.UI
{
    public partial class FormSubmit : Form
    {
        public static readonly string PostReviewExe = "post-reviewfoo.exe";
        public static readonly string PostReviewSubmitRegEx = @"Review request #(?<id>\d*?) posted\..*(?<uri>http(s?)://.*?/r/\d*)";

        public ReviewInfo Review { get; protected set; }

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

            // FindSolutionChangesAsync will finish initializing remaining controls as and after it finishes crawling the solution
            FindSolutionChangesAsync(this);
        }

        private void FormSubmit_FormClosing(object sender, FormClosingEventArgs e)
        {
            // If the post-review was successful, save the review info to the list for next time.
            if (DialogResult == DialogResult.OK)
            {
                ReviewInfo reviewInfo = Review;
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
            // PostReviewAsync will call FormSubmit_FormClosing after PostReview has finished
            PostReviewAsync(this);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void listPaths_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            buttonOk.Enabled = listPaths.CheckedItems.Count > 0;
        }

        private void buttonClearReviewIds_Click(object sender, EventArgs e)
        {
            InitializeReviewIds(true);
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
                    ReviewInfo reviewInfo = (ReviewInfo)comboReviewIds.SelectedItem;
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

        void FindSolutionChangesAsync(FormSubmit form)
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

            FormProgress progress = new FormProgress("Processing...", "Finding solution changes...", handlerFindSolutionChanges);

            progress.FormClosed += (s, e) =>
            {
                Exception error = progress.Error;
                if (error != null)
                {
                    StringBuilder message = new StringBuilder();

                    message.AppendLine("Error finding solution changes:");
                    message.AppendLine();

                    if (error is PostReviewExecutionException)
                    {
                        PostReviewExecutionException pre = (PostReviewExecutionException)error;
                        message.AppendLine(pre.Message);
                        if (pre.InnerException != null)
                        {
                            message.AppendLine(pre.InnerException.Message);
                        }
                        message.AppendLine();
                        message.Append("ExitCode: ").Append(pre.ExitCode).AppendLine();
                        message.AppendLine(FormatOutput("Standard Output", pre.StdOut, 10));
                        message.AppendLine(FormatOutput("Error Output", pre.StdErr, 10));
                        message.AppendLine();
                        message.Append("Make sure ").Append(PostReviewExe).AppendLine(" is in your PATH");
                        message.AppendLine();
                        message.Append("Click \"OK\" to return to Visual Studio.");
                    }
                    else
                    {
                        message.Append(error.Message);
                    }

                    MessageBox.Show(this, message.ToString(), "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    form.DialogResult = DialogResult.Cancel;
                    form.Close();
                }

                List<SubmitItem> solutionChanges = (List<SubmitItem>)progress.Result;

                OnFindSolutionChangesDone(solutionChanges);
            };

            progress.Show(this);
        }

        private void OnFindSolutionChangesDone(List<SubmitItem> solutionChanges)
        {
            string commonRoot = MyUtils.GetCommonRoot(solutionChanges) + '\\';
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
                item.SubItems.Add(solutionChange.Status.ToString()).Name = "Change";
                item.SubItems.Add(pathFull).Name = "FullPath";
            }
            foreach (ColumnHeader columnHeader in listPaths.Columns)
            {
                columnHeader.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
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
            int hr;

            string itemName;
            hr = hierarchy.GetCanonicalName(itemId, out itemName);
            if (hr != VSConstants.E_NOTIMPL)
            {
                package.OutputGeneral("ERROR: Could not get canonical name of item #" + itemId);
                ErrorHandler.ThrowOnFailure(hr);
            }
            Debug.WriteLine("itemName=\"" + itemName + "\"");

            Guid guidTypeNode;
            hr = hierarchy.GetGuidProperty(itemId, (int)__VSHPROPID.VSHPROPID_TypeGuid, out guidTypeNode);
            if (hr != VSConstants.E_NOTIMPL)
            {
                package.OutputGeneral("ERROR: Could not get type guid of item #" + itemId + " \"" + itemName + "\"");
                ErrorHandler.ThrowOnFailure(hr);
            }
            Debug.WriteLine("guidTypeNode=" + guidTypeNode);

            string rootName = null;
            Object oRootName;
            hr = hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_Name, out oRootName);
            if (ErrorHandler.Succeeded(hr))
            {
                rootName = oRootName as string;
            }
            if (String.IsNullOrEmpty(rootName))
            {
                rootName = Resources.RootUnknown;
            }

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
                // future enumeration will handle the individual items in this folder...
            }
            else if (Guid.Equals(guidTypeNode, VSConstants.GUID_ItemType_VirtualFolder))
            {
                Debug.WriteLine("ignoring GUID_ItemType_VirtualFolder");
                // future enumeration will handle the individual items in this virtual folder...
            }
            else if (Guid.Equals(guidTypeNode, VSConstants.GUID_ItemType_SubProject))
            {
                Debug.WriteLine("ignoring GUID_ItemType_SubProject");
                // future enumeration will handle the individual items in this sub project...
            }
            else if (Guid.Equals(guidTypeNode, Guid.Empty))
            {
                Debug.WriteLine("ignoring itemName=" + itemName + "; guidTypeNode == Guid.Empty");
                // future enumeration will handle the individual items in this sub project...
            }
            else
            {
                package.OutputGeneral("ERROR: Unhandled node item/type itemName=" + itemName + ", guidTypeNode=" + guidTypeNode);
                ErrorHandler.ThrowOnFailure(VSConstants.E_UNEXPECTED);
            }
#endif
        }

        public void AddFilePathIfChanged(BackgroundWorker worker, string filePath, string project, List<SubmitItem> changes)
        {
            try
            {
                Debug.WriteLine("+AddFilePathIfChanged(\"" + filePath + "\", \"" + project + "\", changes(" + changes.Count + "))");

                filePath = MyUtils.GetCasedFilePath(filePath);
                if (String.IsNullOrEmpty(filePath))
                {
                    throw new FileNotFoundException(filePath);
                }

                // Percent == 0, since our progress is indeterminate
                worker.ReportProgress(0, filePath);

                string diff;
                string stderr;
                int exitCode;

                try
                {
                    exitCode = PostReviewDiff(filePath, out diff, out stderr);
                }
                catch (Exception e)
                {
                    string message = String.Format("Error executing {0} diff", Path.GetFileName(PostReviewExe));
                    throw new PostReviewExecutionException(message, e);
                }

                if (exitCode != 0)
                {
                    string message = String.Format("Error executing {0} diff", Path.GetFileName(PostReviewExe));
                    throw new PostReviewExecutionException(message, exitCode, diff, stderr);
                }

                SubmitItem.PathStatus status = SubmitItem.PathStatus.None;

                if (!String.IsNullOrEmpty(diff))
                {
                    // TODO:(pv) Parse diff and determine if Change, Modified, Added (etc?)
                    status = SubmitItem.PathStatus.Changed;
                }

                if (status != SubmitItem.PathStatus.None)
                {
                    SubmitItem change = new SubmitItem(filePath, SubmitItem.PathStatus.Changed, project);
                    changes.Add(change);
                }
            }
            finally
            {
                Debug.WriteLine("-AddFilePathIfChanged(\"" + filePath + "\", \"" + project + "\", changes(" + changes.Count + "))");
            }
        }

        #endregion FindSolutionChanges

        #region PostReview

        protected static void PostReviewAsync(FormSubmit form)
        {
            MyPackage package = form.package;

            string server = form.textBoxServer.Text;
            string username = form.textBoxUsername.Text;
            string password = form.textBoxPassword.Text;

            string submitAs = null;
            int reviewId = form.GetSelectedReviewId();
            List<string> changes = form.GetCheckedFullPaths();
            bool publish = false;
            PostReviewOpen open = PostReviewOpen.Internal;
            bool debug = false;

            DoWorkEventHandler handlerPostReview = (s, e) =>
            {
                BackgroundWorker bw = s as BackgroundWorker;

                e.Result = PostReviewSubmit(bw, package, server, username, password, submitAs, reviewId, changes, publish, open, debug);

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

                PostReviewExecutionException pre = (PostReviewExecutionException)progress.Error;
                if (pre != null)
                {
                    StringBuilder message = new StringBuilder();
                    message.AppendLine(pre.Message);
                    message.AppendLine();
                    if (pre.InnerException != null)
                    {
                        message.AppendLine(pre.InnerException.Message);
                    }
                    message.Append("ExitCode: ").Append(pre.ExitCode).AppendLine();
                    message.AppendLine(FormatOutput("Standard Output", pre.StdOut, 10));
                    message.AppendLine(FormatOutput("Error Output", pre.StdErr, 10));
                    message.AppendLine();
                    message.AppendLine("Click \"Retry\" to return to dialog, or \"Cancel\" to return to Visual Studio.");

                    if (MessageBox.Show(form, message.ToString(), "ERROR", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    {
                        close = false;
                    }
                }

                if (close)
                {
                    form.Review = progress.Result as ReviewInfo;
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                }
            };

            progress.Show(form);
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
                if (linesReturned < lineCount)
                {
                    message.AppendLine("...");
                }
                message.Append(lastTenPreferredLines);
            }
            return message.ToString();
        }

        public class PostReviewExecutionException : Exception
        {
            public int ExitCode { get; protected set; }
            public string StdOut { get; protected set; }
            public string StdErr { get; protected set; }

            public PostReviewExecutionException(string message, Exception e)
                : base(message, e)
            {
                ExitCode = 0;
                StdOut = null;
                StdErr = null;
            }

            public PostReviewExecutionException(string message, int exitCode, string stdout, string stderr)
                : base(message)
            {
                ExitCode = exitCode;
                StdOut = stdout;
                StdErr = stderr;
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

        protected static int PostReviewDiff(string path, out string stdout, out string stderr)
        {
            if (!String.IsNullOrEmpty(path) && File.Exists(path))
            {
                string directory = Path.GetDirectoryName(path);
                int exitCode = ExecCommand(null, PostReviewExe, "-n --server=DUMMY " + path, directory, out stdout, out stderr);
                stdout = TrimOutput(stdout);
                stderr = TrimOutput(stderr);
                return exitCode;
            }

            stdout = null;
            stderr = null;
            return 0;
        }

        protected enum PostReviewOpen
        {
            None,
            Internal,
            External
        }

        protected static ReviewInfo PostReviewSubmit(BackgroundWorker worker, MyPackage package,
            string server, string username, string password, string submitAs,
            int reviewId, List<string> changes, bool publish, PostReviewOpen open, bool debug)
        {
            StringBuilder argumentsBuilder = new StringBuilder();

            if (!String.IsNullOrEmpty(server))
            {
                argumentsBuilder.Append("--server=").Append(server).Append(' ');
            }

            if (!String.IsNullOrEmpty(username))
            {
                argumentsBuilder.Append("--username=").Append(username).Append(' ');
            }

            if (!String.IsNullOrEmpty(password))
            {
                argumentsBuilder.Append("--password=").Append(password).Append(' ');
            }

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

            int exitCode;
            string stdout;
            string stderr;
            exitCode = ExecCommand(worker, PostReviewExe, arguments, workingDirectory, out stdout, out stderr);
            if (exitCode != 0)
            {
                string message = String.Format("Error executing {0} submit", Path.GetFileName(PostReviewExe));
                throw new PostReviewExecutionException(message, exitCode, stdout, stderr);
            }

            // Example: "Review request #145 posted.\r\n\r\nhttp://10.100.30.227/r/145\r\n"
            Match m = Regex.Match(stdout, PostReviewSubmitRegEx, RegexOptions.Singleline);
            if (!m.Success)
            {
                string message = String.Format("Output does not match expected format {0}", PostReviewSubmitRegEx);
                throw new PostReviewExecutionException(message, exitCode, stdout, stderr);
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
                throw new PostReviewExecutionException("Error parsing id and url from output", e);
            }
        }

        public static int ExecCommand(BackgroundWorker worker, string fileName, string arguments, string workingDirectory, out string stdout, out string stderr)
        {
            stdout = null;
            stderr = null;

            ProcessStartInfo psi = new ProcessStartInfo(fileName, arguments);
            psi.WorkingDirectory = workingDirectory;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;

            StringBuilder bufferOut = new StringBuilder();
            StringBuilder bufferErr = new StringBuilder();

            Process process = Process.Start(psi);

            StreamReader streamOutput = process.StandardOutput;
            StreamReader streamError = process.StandardError;

            while (true)
            {
                bufferOut.Append(streamOutput.ReadToEnd());
                bufferErr.Append(streamError.ReadToEnd());

                // TODO:(pv) It would be cute if we parsed the output and updated the worker thread w/ the latest results in [near] realtime

                if (worker != null && worker.CancellationPending)
                {
                    break;
                }

                if (process.WaitForExit(500))
                {
                    break;
                }
            }

            bufferOut.Append(streamOutput.ReadToEnd());
            bufferErr.Append(streamError.ReadToEnd());

            stdout = bufferOut.ToString();
            stderr = bufferErr.ToString();

            int exitCode = process.ExitCode;

            process.Close();

            return exitCode;
        }

        #endregion PostReview
    }
}
