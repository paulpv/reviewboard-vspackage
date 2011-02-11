using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace org.reviewboard.ReviewBoardVs.UI
{
    public partial class FormProgress : Form
    {
        public object Result { get; protected set; }
        public Exception Error { get; protected set; }

        public FormProgress(string title, string label, DoWorkEventHandler handler)
        {
            InitializeComponent();

            this.Text = title;
            this.label1.Text = label;

            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.DoWork += handler;
            backgroundWorker1.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker1_ProgressChanged);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
        }

        private void FormSubmitProgress_Load(object sender, EventArgs e)
        {
            CenterToParent();
            backgroundWorker1.RunWorkerAsync();
        }

        private void FormSubmitProgress_FormClosing(object sender, FormClosingEventArgs e)
        {
            lock (backgroundWorker1)
            {
                if (backgroundWorker1.IsBusy)
                {
                    Cancel();

                    // Cancel this close & let the background worker close the form later
                    e.Cancel = true;
                }
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Cancel();
        }

        private void Cancel()
        {
            buttonCancel.Enabled = false;
            DialogResult = DialogResult.Cancel;
            label1.Text = "Canceling...";
            backgroundWorker1.CancelAsync();
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            label1.Text = e.UserState as string;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lock (backgroundWorker1)
            {
                //
                // Reference:
                //  http://msdn.microsoft.com/en-us/library/system.componentmodel.backgroundworker.runworkercompleted.aspx
                //
                //  "Your RunWorkerCompleted event handler should always check the 
                //  AsyncCompletedEventArgs.Error and AsyncCompletedEventArgs.Cancelled
                //  properties before accessing the RunWorkerCompletedEventArgs.Result property.
                //  If an exception was raised or if the operation was canceled, accessing the
                //  RunWorkerCompletedEventArgs.Result property raises an exception."
                //
                // See also:
                //  http://www.developerdotstar.com/community/node/671
                //

                if (e.Error != null)
                {
                    Error = e.Error;
                    Result = null;
                }
                else
                {
                    Error = null;
                    Result = (e.Cancelled) ? null : e.Result;
                }

                Close();
            }
        }
    }
}
