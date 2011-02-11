﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace org.reviewboard.ReviewBoardVs
{
    public static class MyUtils
    {
        /// <summary>
        /// From http://stackoverflow.com/questions/2070356/find-common-prefix-of-strings
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> Transpose<T>(
            this IEnumerable<IEnumerable<T>> source)
        {
            var enumerators = source.Select(e => e.GetEnumerator()).ToArray();
            try
            {
                while (enumerators.All(e => e.MoveNext()))
                {
                    yield return enumerators.Select(e => e.Current).ToArray();
                }
            }
            finally
            {
                Array.ForEach(enumerators, e => e.Dispose());
            }
        }

        public static string PathCombine(params string[] paths)
        {
            string path = paths[0];
            for (int i = 1; i < paths.Length; i++)
            {
                path = Path.Combine(path, paths[i]);
            }
            return Path.GetFullPath(path);
        }

        public static string GetCommonRoot(List<SubmitItem> paths)
        {
            return GetCommonRoot(new List<string>(paths.Select(p => p.FullPath)));
        }

        public static string GetCommonRoot(List<string> paths)
        {
            if (paths == null)
            {
                return null;
            }

            string[] xs = paths.ToArray();
            if (xs.Length == 0)
            {
                return null;
            }

            string x;

            if (xs.Length == 1)
            {
                x = xs[0];
                while (!String.IsNullOrEmpty(x) && !Directory.Exists(x))
                {
                    x = x.Substring(0, x.LastIndexOf('\\'));
                }
                return x;
            }
            else
            {
                x = string.Join("\\", xs.Select(s => s.Split('\\').AsEnumerable())
                                              .Transpose()
                                              .TakeWhile(s => s.All(d => d == s.First()))
                                              .Select(s => s.First()).ToArray());
                return x;
            }
        }

        public static bool IsOnScreen(Rectangle rect)
        {
            Screen[] screens = Screen.AllScreens;
            foreach (Screen screen in screens)
            {
                if (screen.WorkingArea.Contains(rect))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// For now this code ASSuMEs that cut/copy/paste keys are *NOT* localized.
        /// There may be a flaw in this code w/ different combinations of CTRL+SHIFT+X/C/V/INSERT/DELETE.
        /// For now I don't really care! :)
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="keyModifiers"></param>
        /// <returns></returns>
        public static bool IsCutCopyPaste(int keyValue, Keys keyModifiers)
        {
            if ((keyModifiers & Keys.Control) == Keys.Control)
            {
                switch ((Keys)keyValue)
                {
                    case Keys.X: // CTRL-X Cut
                    case Keys.C: // CTRL-C Copy
                    case Keys.Insert: // CTRL-INSERT Copy
                    case Keys.V: // CTRL-V Paste
                        return true;
                }
            }

            if ((keyModifiers & Keys.Shift) == Keys.Shift)
            {
                switch ((Keys)keyValue)
                {
                    case Keys.Delete: // SHIFT-DELETE Cut
                    case Keys.Insert: // SHIFT-INSERT Paste
                        return true;
                }
            }

            return false;
        }

        public static bool IsDigit(int keyValue)
        {
            if (Char.IsDigit((Char)keyValue))
            {
                return true;
            }

            if (Control.IsKeyLocked(Keys.NumLock))
            {
                switch ((Keys)keyValue)
                {
                    case Keys.NumPad0:
                    case Keys.NumPad1:
                    case Keys.NumPad2:
                    case Keys.NumPad3:
                    case Keys.NumPad4:
                    case Keys.NumPad5:
                    case Keys.NumPad6:
                    case Keys.NumPad7:
                    case Keys.NumPad8:
                    case Keys.NumPad9:
                        return true;
                }
            }

            return false;
        }

        public static string[] GetLastXLines(string s, int lineWant, out int linesTotal)
        {
            // Replace "\r\n" with "\n", and work with '\n' only from here on out
            s = s.Replace(Environment.NewLine, "\n");

            // Leading & trailing new lines don't count
            s = s.Trim('\n');

            linesTotal = 0;
            int lineCount = 0;
            int cursorTotal = s.Length - 1;
            int startIndex = cursorTotal;
            while (cursorTotal > 0)
            {
                cursorTotal = s.LastIndexOf('\n', cursorTotal - 1);
                if (cursorTotal != -1)
                {
                    if (lineCount < lineWant)
                    {
                        startIndex = cursorTotal;
                        lineCount++;
                    }
                    linesTotal++;
                }
            }

            s = s.Substring(startIndex).Trim('\n');

            return s.Split('\n');
        }

        public static string GetLastXLines(string s, int lineCount, string dontStartWith, out int linesTotal, out int linesReturned)
        {
            string[] lastXLines = MyUtils.GetLastXLines(s, lineCount, out linesTotal);
            int startIndex = 0;
            while (lastXLines[startIndex].StartsWith(dontStartWith))
            {
                startIndex++;
            }
            linesReturned = lastXLines.Length - startIndex;
            string lastLines = string.Join(Environment.NewLine, lastXLines, startIndex, linesReturned);
            return lastLines;
        }

        /// <summary>
        /// Walks up the tree to the root directory.
        /// </summary>
        /// <param name="path"></param>
        public static IEnumerable<string> WalkParents(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            while (di.Parent != null)
            {
                yield return di.FullName;
                di = di.Parent;
            }
        }
    }
}
