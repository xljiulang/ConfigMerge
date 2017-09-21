using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ConfigMerge
{
    /// <summary>
    /// 提供xml文档的合并
    /// </summary>
    public static class XmlMerger
    {
        /// <summary>
        /// 将valueXml的值与markXml的结构合并
        /// 返回得到的新的xml
        /// </summary>
        /// <param name="valueXml">带有值的xml</param>
        /// <param name="markXml">带有标记的xml</param>    
        /// <returns></returns>
        public static string MergeXml(string valueXml, string markXml)
        {
            var valueDoc = XDocument.Parse(valueXml);
            var markDoc = XDocument.Parse(markXml);
            var markElements = markDoc.Descendants().ToArray();

            foreach (var markElement in markElements)
            {
                var mergeMark = markElement.GetMergeMark();
                if (mergeMark == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(mergeMark.By) == false)
                {
                    var byAttr = markElement.Attribute(mergeMark.By);
                    var xPath = markElement.GetAbsoluteXPath(byAttr);
                    valueDoc.XPathSelectElement(xPath).CopyAttrValuesTo(markElement, mergeMark.Attr);
                }
                else
                {
                    var xPath = markElement.GetAbsoluteXPath();
                    valueDoc.XPathSelectElement(xPath).CopyAttrValuesTo(markElement, mergeMark.Attr);
                }
            }
            return markDoc.ToString();
        }

        /// <summary>
        /// 获取元素的标记匹配节点
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private static MergeMark GetMergeMark(this XElement element)
        {
            var prevNode = element.PreviousNode;
            while (prevNode != null)
            {
                if (prevNode.NodeType == XmlNodeType.ProcessingInstruction)
                {
                    var piNode = prevNode as XProcessingInstruction;
                    if (piNode.Target == "merge")
                    {
                        piNode.Remove();
                        return new MergeMark(element, piNode.Data);
                    }
                    else
                    {
                        prevNode = prevNode.PreviousNode;
                    }
                }
                else if (prevNode.NodeType == XmlNodeType.Comment)
                {
                    prevNode = prevNode.PreviousNode;
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// 复制元素的属性值到目标元素
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target">目标元素</param>
        /// <param name="attrs">特性的属性</param>
        private static void CopyAttrValuesTo(this XElement source, XElement target, IEnumerable<XAttribute> attrs)
        {
            if (source == null || source.HasAttributes == false)
            {
                return;
            }

            foreach (var attr in attrs)
            {
                var oldAttr = source.Attribute(attr.Name);
                if (oldAttr != null)
                {
                    attr.SetValue(oldAttr.Value);
                }
            }
        }

        /// <summary>
        /// 获取元素的xPath表示
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private static string GetAbsoluteXPath(this XElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            var paths = element
                .Ancestors().Reverse()
                .Concat(new[] { element })
                .Select(item => item.GetRelativeXPath());

            return string.Concat(paths);
        }

        /// <summary>
        /// 获取元素的xPath表示
        /// </summary>
        /// <param name="element"></param>
        /// <param name="byAttr">唯一标识特性</param>
        /// <returns></returns>
        private static string GetAbsoluteXPath(this XElement element, XAttribute byAttr)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            var paths = element
                .Ancestors().Reverse()
                .Select(item => item.GetRelativeXPath())
                .Concat(new[] { element.GetRelativeXPath(byAttr) });

            return string.Concat(paths);
        }

        /// <summary>
        /// 获取元素xPath相对路径
        /// </summary>
        /// <param name="element"></param>
        /// <param name="byAttr">唯一标识特性</param>
        /// <returns></returns>
        private static string GetRelativeXPath(this XElement element, XAttribute byAttr)
        {
            return string.Format("/{0}[@{1}='{2}']", element.Name, byAttr.Name, byAttr.Value);
        }

        /// <summary>
        /// 获取元素xPath相对路径
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private static string GetRelativeXPath(this XElement element)
        {
            var index = element.GetXPathIndex();
            var name = element.Name.LocalName;
            if (index < 0)
            {
                return "/" + name;
            }
            else
            {
                return string.Format("/{0}[{1}]", name, index);
            }
        }

        /// <summary>
        /// 获取元素xPath索引
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private static int GetXPathIndex(this XElement element)
        {
            if (element.Parent == null)
            {
                return -1;
            }

            var elements = element.Parent.Elements(element.Name).ToArray();
            for (var i = 0; i < elements.Length; i++)
            {
                if (elements[i] == element)
                {
                    return i + 1;
                }
            }
            throw new NotSupportedException("元素已被移除");
        }


        /// <summary>
        /// 表示合并标记
        /// </summary>
        class MergeMark
        {
            /// <summary>
            /// 获取通过什么属性合并
            /// </summary>
            public string By { get; private set; }

            /// <summary>
            /// 获取合并的属性
            /// </summary>
            public IEnumerable<XAttribute> Attr { get; private set; }

            /// <summary>
            /// 合并标记
            /// </summary>
            /// <param name="element"></param>
            /// <param name="data"></param>
            public MergeMark(XElement element, string data)
            {
                this.By = this.GetValue(data, "by");
                this.Attr = this.GetMarkAttrs(element, data);
            }

            /// <summary>
            /// 从data获取标记的特性
            /// </summary>
            /// <param name="element"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            private IEnumerable<XAttribute> GetMarkAttrs(XElement element, string data)
            {
                var attrValues = this.GetValue(data, "attr");
                if (string.IsNullOrEmpty(attrValues))
                {
                    return element.Attributes();
                }

                return
                    from name in attrValues.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    let attribute = element.Attribute(name.Trim())
                    where attribute != null
                    select attribute;
            }

            /// <summary>
            /// 获取值
            /// </summary>
            /// <param name="data">数据</param>
            /// <param name="name">名称</param>
            /// <returns></returns>
            private string GetValue(string data, string name)
            {
                if (string.IsNullOrEmpty(data))
                {
                    return null;
                }
                return Regex.Match(data, string.Format(@"(?<={0}\s*=\s*"").+?(?="")", name), RegexOptions.IgnoreCase).Value;
            }
        }
    }
}
