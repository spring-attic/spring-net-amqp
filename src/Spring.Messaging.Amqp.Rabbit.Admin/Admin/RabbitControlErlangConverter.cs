#region License

/*
 * Copyright 2002-2010 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Common.Logging;
using Erlang.NET;
using Spring.Erlang.Core;
using Spring.Erlang.Support.Converter;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    /// <summary>
    /// Converter that understands the responses from the rabbit control module and related functionality. 
    /// </summary>
    /// <author>Mark Pollack</author>
    public class RabbitControlErlangConverter : SimpleErlangConverter
    {
        /// <summary>
        /// The logger
        /// </summary>
        protected static readonly ILog logger = LogManager.GetLogger(typeof(RabbitControlErlangConverter));

        /// <summary>
        /// The converter map.
        /// </summary>
        private readonly IDictionary<string, IErlangConverter> converterMap = new Dictionary<string, IErlangConverter>();

        /// <summary>
        /// The module adapter.
        /// </summary>
        private readonly IDictionary<string, string> moduleAdapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitControlErlangConverter"/> class.
        /// </summary>
        /// <param name="moduleAdapter">The module adapter.</param>
        /// <remarks></remarks>
        public RabbitControlErlangConverter(IDictionary<string, string> moduleAdapter)
        {
            this.moduleAdapter = moduleAdapter;
            this.InitializeConverterMap();
        }

        /// <summary>
        /// The return value from executing the Erlang RPC.
        /// </summary>
        /// <param name="module">The module to call</param>
        /// <param name="function">The function to invoke</param>
        /// <param name="erlangObject">The erlang object that is passed in as a parameter</param>
        /// <returns>The converted .NET object return value from the RPC call.</returns>
        /// <exception cref="ErlangConversionException">in case of conversion failures</exception>
        /// <remarks></remarks>
        public override object FromErlangRpc(string module, string function, OtpErlangObject erlangObject)
        {
            var converter = this.GetConverter(module, function);
            if (converter != null)
            {
                return converter.FromErlang(erlangObject);
            } 
            else
            {
                return base.FromErlangRpc(module, function, erlangObject);
            }
        }

        /// <summary>
        /// Gets the converter.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <returns>The converter.</returns>
        /// <remarks></remarks>
        protected virtual IErlangConverter GetConverter(string module, string function)
        {
            var key = this.GenerateKey(module, function);
            if (this.converterMap.ContainsKey(key))
            {
                return this.converterMap[key];
            }

            return null;
        }

        /// <summary>
        /// Initializes the converter map.
        /// </summary>
        /// <remarks></remarks>
        protected void InitializeConverterMap()
        {
            this.RegisterConverter("rabbit_auth_backend_internal", "list_users", new ListUsersConverter());
            this.RegisterConverter("rabbit", "status", new StatusConverter());
            this.RegisterConverter("rabbit_amqqueue", "info_all", new QueueInfoAllConverter());
        }

        /// <summary>
        /// Registers the converter.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <param name="listUsersConverter">The list users converter.</param>
        /// <remarks></remarks>
        protected void RegisterConverter(string module, string function, IErlangConverter listUsersConverter)
        {
            var key = this.GenerateKey(module, function);
            if (this.moduleAdapter.ContainsKey(key))
            {
                var adapter = this.moduleAdapter[key];
                var values = adapter.Split("%".ToCharArray());
                AssertUtils.State(values.Length == 2, "The module adapter should be a map from 'module%function' to 'module%function'. " + "This one contained [" + adapter + "] which cannot be parsed to a module, function pair.");
                module = values[0];
                function = values[1];
            }

            this.converterMap.Add(this.GenerateKey(module, function), listUsersConverter);
        }

        /// <summary>
        /// Generates the key.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        /// <returns>The key.</returns>
        /// <remarks></remarks>
        protected string GenerateKey(string module, string function)
        {
            return module + "%" + function;
        }
    }

    /// <summary>
    /// List users converter.
    /// </summary>
    /// <remarks></remarks>
    public class ListUsersConverter : SimpleErlangConverter
    {
        /// <summary>
        /// Convert from an Erlang data type to a .NET data type.
        /// </summary>
        /// <param name="erlangObject">The erlang object.</param>
        /// <returns>The converted .NET object</returns>
        /// <exception cref="ErlangConversionException">in case of conversion failures</exception>
        /// <remarks></remarks>
        public override object FromErlang(OtpErlangObject erlangObject)
        {
            var users = new List<string>();
            if (erlangObject is OtpErlangList)
            {
                var erlangList = (OtpErlangList)erlangObject;
                foreach (var obj in erlangList)
                {
                    var value = this.ExtractString(obj);
                    if (value != null)
                    {
                        users.Add(value);
                    }
                }
            }

            return users;
        }

        /// <summary>
        /// Extracts the string.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>The string.</returns>
        /// <remarks></remarks>
        private string ExtractString(OtpErlangObject obj)
        {
            if (obj is OtpErlangBinary)
            {
                var binary = (OtpErlangBinary)obj;
                return new UTF8Encoding().GetString(binary.binaryValue());
            }
            else if (obj is OtpErlangTuple)
            {
                var tuple = (OtpErlangTuple)obj;
                return this.ExtractString(tuple.elementAt(0));
            }

            return null;
        }
    }

    /// <summary>
    /// A status converter.
    /// </summary>
    /// <remarks></remarks>
    public class StatusConverter : SimpleErlangConverter
    {
        /// <summary>
        /// Convert from an Erlang data type to a .NET data type.
        /// </summary>
        /// <param name="erlangObject">The erlang object.</param>
        /// <returns>The converted .NET object</returns>
        /// <exception cref="ErlangConversionException">in case of conversion failures</exception>
        /// <remarks></remarks>
        public override object FromErlang(OtpErlangObject erlangObject)
        {
            var applications = new List<Application>();
            var nodes = new List<Node>();
            var runningNodes = new List<Node>();
            if (erlangObject is OtpErlangList)
            {
                var erlangList = (OtpErlangList)erlangObject;

                var runningAppTuple = (OtpErlangTuple)erlangList.elementAt(1);
                var appList = (OtpErlangList)runningAppTuple.elementAt(1);
                this.ExtractApplications(applications, appList);

                var nodesTuple = (OtpErlangTuple)erlangList.elementAt(1);
                var nodesList = (OtpErlangList)nodesTuple.elementAt(1);
                this.ExtractNodes(nodes, nodesList);


                var runningNodesTuple = (OtpErlangTuple)erlangList.elementAt(2);
                nodesList = (OtpErlangList)runningNodesTuple.elementAt(1);
                this.ExtractNodes(runningNodes, nodesList);
            }

            return new RabbitStatus(applications, nodes, runningNodes);
        }

        /// <summary>
        /// Extracts the nodes.
        /// </summary>
        /// <param name="nodes">The nodes.</param>
        /// <param name="nodesList">The nodes list.</param>
        /// <remarks></remarks>
        private void ExtractNodes(IList<Node> nodes, OtpErlangList nodesList)
        {
            foreach (var erlangNodeName in nodesList)
            {
                var nodeName = erlangNodeName.ToString();
                nodes.Add(new Node(nodeName));
            }
        }

        /// <summary>
        /// Extracts the applications.
        /// </summary>
        /// <param name="applications">The applications.</param>
        /// <param name="appList">The app list.</param>
        /// <remarks></remarks>
        private void ExtractApplications(IList<Application> applications, OtpErlangList appList)
        {
            foreach (var appDescription in appList)
            {
                var appDescriptionTuple = (OtpErlangTuple)appDescription;
                var name = appDescriptionTuple.elementAt(0).ToString();
                var description = appDescriptionTuple.elementAt(1).ToString();
                var version = appDescriptionTuple.elementAt(2).ToString();
                applications.Add(new Application(name, description, version));
            }
        }
    }

    /// <summary>
    /// Queue info field enumeraton.
    /// </summary>
    /// <remarks></remarks>
    public enum QueueInfoField
    {
        /// <summary>
        /// Transactions value
        /// </summary>
        transactions,

        /// <summary>
        /// Acks Uncommitted value
        /// </summary>
        acks_uncommitted,

        /// <summary>
        /// Consumers value
        /// </summary>
        consumers,

        /// <summary>
        /// Pid value
        /// </summary>
        pid,

        /// <summary>
        /// Durable value
        /// </summary>
        durable,

        /// <summary>
        /// Messages value
        /// </summary>
        messages,

        /// <summary>
        /// Memory value
        /// </summary>
        memory,

        /// <summary>
        /// Auto delete value
        /// </summary>
        auto_delete,

        /// <summary>
        /// Messages ready value
        /// </summary>
        messages_ready,

        /// <summary>
        /// Arguments value
        /// </summary>
        arguments,

        /// <summary>
        /// Name value
        /// </summary>
        name,

        /// <summary>
        /// Messages unacknowledged value
        /// </summary>
        messages_unacknowledged,

        /// <summary>
        /// Messages uncommitted value
        /// </summary>
        messages_uncommitted,

        /// <summary>
        /// No value
        /// </summary>
        NOVALUE
    }

    /// <summary>
    /// A queue info all converter.
    /// </summary>
    /// <remarks></remarks>
    public class QueueInfoAllConverter : SimpleErlangConverter
    {
        /// <summary>
        /// Toes the queue info field.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The queue info field value.</returns>
        /// <remarks></remarks>
        public static QueueInfoField ToQueueInfoField(string str)
        {
            try
            {
                return (QueueInfoField)Enum.Parse(typeof(QueueInfoField), str, true);
            }
            catch (Exception ex)
            {
                return QueueInfoField.NOVALUE;
            }
        }

        public override object FromErlang(OtpErlangObject erlangObject)
        {
            var queueInfoList = new List<QueueInfo>();
            if (erlangObject is OtpErlangList)
            {
                var erlangList = (OtpErlangList)erlangObject;
                foreach (var element in erlangList)
                {
                    var queueInfo = new QueueInfo();
                    var itemList = (OtpErlangList)element;
                    foreach (var item in itemList)
                    {
                        var tuple = (OtpErlangTuple)item;
                        if (tuple.arity() == 2)
                        {
                            var key = tuple.elementAt(0).ToString();
                            var value = tuple.elementAt(1);
                            switch (ToQueueInfoField(key))
                            {
                                case QueueInfoField.name:
                                    queueInfo.Name = this.ExtractNameValueFromTuple((OtpErlangTuple)value);
                                    break;
                                case QueueInfoField.transactions:
                                    queueInfo.Transactions = ExtractLong(value);
                                    break;
                                case QueueInfoField.acks_uncommitted:
                                    queueInfo.AcksUncommitted = ExtractLong(value);
                                    break;
                                case QueueInfoField.consumers:
                                    queueInfo.Consumers = ExtractLong(value);
                                    break;
                                case QueueInfoField.pid:
                                    queueInfo.Pid = ExtractPid(value);
                                    break;
                                case QueueInfoField.durable:
                                    queueInfo.Durable = this.ExtractAtomBoolean(value);                                    
                                    break;
                                case QueueInfoField.messages:
                                    queueInfo.Messages = ExtractLong(value);
                                    break;
                                case QueueInfoField.memory:
                                    queueInfo.Memory = ExtractLong(value);
                                    break;
                                case QueueInfoField.auto_delete:
                                    queueInfo.AutoDelete = this.ExtractAtomBoolean(value);
                                    break;
                                case QueueInfoField.messages_ready:
                                    queueInfo.MessagesReady = ExtractLong(value);
                                    break;
                                case QueueInfoField.arguments:
                                    var list = (OtpErlangList)value;
                                    if (list != null)
                                    {
                                        var args = new string[list.arity()];
                                        for (var i = 0; i < list.arity(); i++)
                                        {
                                            var obj = list.elementAt(i);
                                            args[i] = obj.ToString();
                                        }

                                        queueInfo.Arguments = args;
                                    }

                                    break;
                                case QueueInfoField.messages_unacknowledged:
                                    queueInfo.MessagesUnacknowledged = ExtractLong(value);
                                    break;
                                case QueueInfoField.messages_uncommitted:
                                    queueInfo.MessageUncommitted = ExtractLong(value);
                                    break;
                                default:
                                    break;    
                                
                            }

                            queueInfoList.Add(queueInfo);
                        }
                    }
                }
            }

            return queueInfoList;
        }

        /// <summary>
        /// Extracts the atom boolean.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The atom boolean.</returns>
        /// <remarks></remarks>
        private bool ExtractAtomBoolean(OtpErlangObject value)
        {
            return ((OtpErlangAtom)value).boolValue();
        }

        /// <summary>
        /// Extracts the name value from tuple.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The name value.</returns>
        /// <remarks></remarks>
        private string ExtractNameValueFromTuple(OtpErlangTuple value)
        {
            object nameElement = value.elementAt(3);
            return new UTF8Encoding().GetString(((OtpErlangBinary)nameElement).binaryValue());
        }
    }


}