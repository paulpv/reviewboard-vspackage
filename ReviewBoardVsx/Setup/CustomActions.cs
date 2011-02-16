using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ReviewBoardVsx.Setup.CustomActions
{
    [RunInstaller(true)]
    public partial class CustomActions : Installer
    {
        public CustomActions()
        {
            InitializeComponent();
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);

            // TODO:(pv) Detect installed VS versions and install the respective 2005/2008/2010 Package in to those...
            // 

            // TODO:(pv) Read the .pkgdef file and import in to registry? It is nearly identical to the output of regpkg.exe

            //
            // VS2010 is different from 2008 and earlier.
            // In VS2010 there is no RegKey registration *necessary* (although for the time being *I think* it is still supported).
            // From: http://msdn.microsoft.com/en-us/library/dd891105.aspx
            // "The .pkgdef files must be installed in one of the following locations: 
            //  %localappdata%\Microsoft\Visual Studio\10.0\Extensions\
            //  -or-
            //  %vsinstalldir%\Common7\IDE\Extensions\."
            //

            foreach (DevEnvInfo devEnvInfo in devEnvInfos)
            {
                DevEnvSetup(devEnvInfo);
            }
        }

        protected class DevEnvInfo
        {
            public string Name { get; protected set; }
            public string RegKeyPath { get; protected set; }
            public string RegKeyName { get; protected set; }
            public string Arguments { get; protected set; }

            public DevEnvInfo(string name, string regKeyPath, string regKeyName, string arguments)
            {
                Name = name;
                RegKeyPath = regKeyPath;
                RegKeyName = regKeyName;
                Arguments = arguments;
            }
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/bb166419.aspx
        /// http://blogs.msdn.com/b/aaronmar/archive/2007/07/19/devenv-setup-performance.aspx
        /// </summary>
        IList devEnvInfos = new List<DevEnvInfo>()
        {
            //{ new DevEnvSetupInfo(@"SOFTWARE\Microsoft\VisualStudio\8.0\Setup\VS", "EnvironmentPath", "/setup") },
            { new DevEnvInfo("VS2008", @"SOFTWARE\Microsoft\VisualStudio\9.0\Setup\VS", "EnvironmentPath", "/setup /nosetupvstemplates") },
            //{ new DevEnvSetupInfo(@"SOFTWARE\Microsoft\VisualStudio\10.0\Setup\VS", "EnvironmentPath", "/setup /nosetupvstemplates") },
        };

        protected void DevEnvSetup(DevEnvInfo devEnvInfo)
        {
            using (RegistryKey setupKey = Registry.LocalMachine.OpenSubKey(devEnvInfo.RegKeyPath))
            {
                if (setupKey != null)
                {
                    string devEnvRuntime = setupKey.GetValue(devEnvInfo.RegKeyName).ToString();
                    if (!string.IsNullOrEmpty(devEnvRuntime))
                    {
                        // TODO:(pv) Need to start this quietly?
                        //string message = String.Format("Running: \"{0}\" {1}", devEnvRuntime, devEnvInfo.Arguments);
                        //MessageBox.Show(message);
                        Process p = Process.Start(devEnvRuntime, devEnvInfo.Arguments);
                        p.WaitForExit();
                        int exitCode = p.ExitCode;
                        //message = String.Format("Returned: {0}", exitCode);
                        //MessageBox.Show(message);
                    }
                }
            }
        }
    }
}
