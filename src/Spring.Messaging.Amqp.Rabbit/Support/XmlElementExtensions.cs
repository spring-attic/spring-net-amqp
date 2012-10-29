// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XmlElementExtensions.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   http://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System.Collections.Generic;
using System.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Support
{
    /// <summary>
    /// XmlElement Extensions
    /// </summary>
    public static class XmlElementExtensions
    {
        public const string RabbitPrefix = "rabbit";
        public const string RabbitUri = "http://www.springframework.net/schema/rabbit";

        /// <summary>The get elements by tag name with optional prefix.</summary>
        /// <param name="node">The node.</param>
        /// <param name="prefix">The prefix.</param>
        /// <param name="tagName">The tag name.</param>
        /// <returns>The System.Collections.Generic.List`1[T -&gt; System.Xml.XmlNode].</returns>
        public static List<XmlNode> GetElementsByTagNameWithOptionalPrefix(this XmlElement node, string prefix, string tagName)
        {
            if (node == null)
            {
                return new List<XmlNode>();
            }

            var unprefixedList = node.GetElementsByTagName(tagName);
            var prefixedList = node.GetElementsByTagName(string.Format("{0}:{1}", prefix, tagName));
            var nodeList = new List<XmlNode>();
            foreach (XmlNode item in unprefixedList)
            {
                nodeList.Add(item);
            }

            foreach (XmlNode item in prefixedList)
            {
                nodeList.Add(item);
            }

            return nodeList;
        }

        public static List<XmlNode> SelectChildElementsByTagName(this XmlElement node, string tagName)
        {
            if (node == null || node.OwnerDocument == null)
            {
                return new List<XmlNode>();
            }

            var mgr = new XmlNamespaceManager(node.OwnerDocument.NameTable);
            mgr.AddNamespace(RabbitPrefix, RabbitUri);
            var list = node.SelectNodes(string.Format("{0}:{1}", RabbitPrefix, tagName), mgr);
            var result = new List<XmlNode>();
            if (list == null)
            {
                return result;
            }

            foreach (XmlNode item in list)
            {
                result.Add(item);
            }
            return result;
        }

        public static XmlElement SelectChildElementByTagName(this XmlElement node, string tagName)
        {
            if (node == null || node.OwnerDocument == null)
            {
                return default(XmlElement);
            }

            var mgr = new XmlNamespaceManager(node.OwnerDocument.NameTable);
            mgr.AddNamespace(RabbitPrefix, RabbitUri);
            var result = node.SelectSingleNode(string.Format("{0}:{1}", RabbitPrefix, tagName), mgr);
            if (result != null && result is XmlElement)
            {
                return result as XmlElement;
            }

            return default(XmlElement);
        }
    }
}
