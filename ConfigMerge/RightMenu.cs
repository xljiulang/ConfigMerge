using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfigMerge
{
    static class RightMenu
    {
        public static void AddSelf()
        {
            var configSub = Registry.ClassesRoot.CreateSubKey(@"*\shell\webconfig");
            configSub.SetValue(null, "更新到Web.config");

            var cmd = string.Format(@"""{0}"" %1", Process.GetCurrentProcess().MainModule.FileName);
            configSub.CreateSubKey("command").SetValue(null, cmd);
        }

        public static void RemoveSelf()
        {
            Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\webconfig", false);
        }
    }
}
