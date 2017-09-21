using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Xml.XPath;

namespace ConfigMerge
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("已添加到右键菜单");
                RightMenu.AddSelf();
                return;
            }

            var web = "Web.config";
            var markConfig = args.FirstOrDefault();
            if (string.Equals(Path.GetFileName(markConfig), web, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var webConfig = Path.Combine(Path.GetDirectoryName(markConfig), web);
            if (File.Exists(webConfig) == false)
            {
                return;
            }

            Console.WriteLine("正在合并{0}到{1}", Path.GetFileName(markConfig), web);

            var valueXml = File.ReadAllText(webConfig, Encoding.UTF8);
            var markXml = File.ReadAllText(markConfig, Encoding.UTF8);

            var merge = XmlMerger.MergeXml(valueXml, markXml);

            var bak = Path.ChangeExtension(webConfig, ".config." + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            File.Delete(bak);
            File.Move(webConfig, bak);
            File.WriteAllText(webConfig, merge, Encoding.UTF8);

            Console.WriteLine("合并完成");
        }
    }
}
