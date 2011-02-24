using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReviewBoardVsx.Ids
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
    ///     
    /// http://msdn.microsoft.com/en-us/library/bb166239(v=VS.90).aspx
    /// 
    /// </summary>
    public static class MyPackageLoadKey
    {
        public const string PackageId = "bbffe0ea-3383-4ea2-a281-528706f79d57";
        public const string MinimumVsEdition = "Standard";
        public const string Version = "0.1";
        public const string Product = "ReviewBoardVsx";
        public const string Company = "reviewboard.org";
        public const int KeyResourceId = 1; // resource id of PLK in VSPackage.resx (mandatory file for VS Packages)
    }

    public static class MyPackageConstants
    {
        public const string PackageDescription = MyPackageLoadKey.Product + " - ReviewBoard Support for Visual Studio";

#if VS2010
        public const string DefaultRegistryRoot = @"Software\Microsoft\VisualStudio\10.0";
        public const string MenuResourceId = "Menus.ctmenu";
#elif VS2008
        public const string DefaultRegistryRoot = @"Software\Microsoft\VisualStudio\9.0";
        public const int MenuResourceId = 1000;
#else
        //public const string DefaultRegistryRoot = @...;
        //public const string MenuResourceId = ...;
#endif

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

    public static class MyPackageCommandIds
    {
        public const int cmdIdReviewBoard = 0x100;
    }

    public static class MyVsConstants
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
}
