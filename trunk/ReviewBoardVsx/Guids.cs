using System;

namespace org.reviewboard.ReviewBoardVsx
{
    static class GuidList
    {
        /// <summary>
        /// Found in "%ProgramFiles(x86)%\Microsoft Visual Studio 2008 SDK\VisualStudioIntegration\Common\inc\vsshlids.h"
        /// </summary>
        public const String UICONTEXT_SolutionExists = "f1536ef8-92ec-443c-9ed7-fdadf150da82";

        // Borrowed from AnkhSvn; no idea where these came from...
        /// <summary>The SCC Provider guid (used as SCC active marker by VS)</summary>
        public const string SccProviderId = "8770915b-b235-42ec-bbc6-8e93286e59b5";
        /// <summary>The GUID of the SCC Service</summary>
        public const string SccServiceId = "d8c473d2-9634-4513-91d5-e1a671fe2df4";


        public const string guidReviewBoardVsxPkgString = "bbffe0ea-3383-4ea2-a281-528706f79d57";
        public const string guidReviewBoardVsxGrpString = "ed66aa69-7606-4fe2-852f-ecda6208097d";
        public const string guidReviewBoardVsxCmdSetString = "d98ce002-7ba4-42ec-83d6-08492024ec22";

        public static readonly Guid guidReviewBoardVsxPkg = new Guid(guidReviewBoardVsxPkgString);
        public static readonly Guid guidReviewBoardVsxGrp = new Guid(guidReviewBoardVsxGrpString);
        public static readonly Guid guidReviewBoardVsxCmdSet = new Guid(guidReviewBoardVsxCmdSetString);
    };
}