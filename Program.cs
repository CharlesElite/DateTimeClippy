#region Copyright (c) 2015 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace DateTimeClippy
{
    #region Imports

    using System;
    using System.Drawing;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;

    #endregion

    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            const string guid = "48CE0057-4846-4E28-B2D1-62CF8D39F7D7";
            bool isNewMutex;
            using (new Mutex(initiallyOwned: false, name: guid,
                             createdNew: out isNewMutex))
            {
                if (!isNewMutex) // Already exists so exit
                    return;

                Wain(args);
            }
        }

        static void Wain(string[] args)
        {
            args = args.Where(arg => !string.IsNullOrEmpty(arg)).ToArray();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ReSharper disable once RedundantAssignment

            var contextMenu = new ContextMenu();

            var patternQuery =
                from e in
                    // ReSharper disable once PossibleNullReferenceException
                    from ps in new[]
                    {
                        args.Length > 0
                        ? args
                        : DateTimeFormatInfo.CurrentInfo
                                            .GetAllDateTimePatterns()
                    }
                    from e in ps.Select((p, i) => new { Position = i + 1, Pattern = p })
                    group e by e.Pattern into g
                    select g.First() into e
                    orderby e.Position
                    select e
                select new
                {
                    e.Pattern,
                    e.Position,
                    LastUseTime = new DateTime[1]
                };

            var patterns = patternQuery.ToArray();

            var separator = new
            {
                Text        = default(string),
                Pattern     = default(string),
                LastUseTime = default(DateTime[]),
                Position    = default(int),
            };

            contextMenu.Popup += delegate
            {
                var now = DateTimeOffset.Now;

                var menus =
                    from fs in new[]
                    {
                        from e in patterns
                        select new
                        {
                            Text = TryFormat(now, e.Pattern),
                            e.Pattern, e.LastUseTime, e.Position
                        }
                        into e
                        where e.Text != null
                        select e
                    }
                    select fs.ToArray() into fs
                    let mrus = fs.Where(e => e.LastUseTime[0] > DateTime.MinValue).ToArray()
                    from ms in new[]
                    {
                        from e in fs.Except(mrus)
                        orderby e.Position
                        select e,
                        Enumerable.Repeat(separator, mrus.Any() ? 1 : 0),
                        from e in mrus
                        orderby e.LastUseTime[0], e.Position
                        select e,
                    }
                    from e in ms
                    select e != separator
                         ? new MenuItem($"{e.Text} \u00ab {e.Pattern}", delegate
                         {
                             e.LastUseTime[0] = DateTime.Now;
                             Clipboard.SetText(e.Text);
                         })
                         : new MenuItem("-");

                var contextMenuItems = contextMenu.MenuItems;
                contextMenuItems.Clear();
                contextMenuItems.AddRange(menus.ToArray());
                contextMenuItems.AddRange(new[]
                {
                    new MenuItem("-"),
                    new MenuItem("Exit", delegate { Application.Exit(); })
                });
            };

            using (var icon = new Icon(typeof(Program), "App.ico"))
            using (new NotifyIcon { Icon = icon, Visible = true, ContextMenu = contextMenu })
            using (var app = new ApplicationContext())
                Application.Run(app);
        }

        static string TryFormat(DateTimeOffset dateTime, string format)
        {
            try
            {
                return dateTime.ToString(format);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
