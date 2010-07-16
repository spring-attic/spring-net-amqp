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

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    /// <summary>
    /// Converter that understands the responses from the rabbit control module and related functionality. 
    /// </summary>
    /// <author>Mark Pollack</author>
    public class RabbitControlErlangConverter : SimpleErlangConverter
    {
        protected static readonly ILog logger = LogManager.GetLogger(typeof(RabbitControlErlangConverter));

        private IDictionary<string, IErlangConverter> converterMap = new Dictionary<string, IErlangConverter>();

        public RabbitControlErlangConverter()
        {
            InitializeConverterMap();
        }

        public override object FromErlangRpc(string module, string function, OtpErlangObject erlangObject)
        {
            IErlangConverter converter = GetConverter(module, function);
            if (converter != null)
            {
                return converter.FromErlang(erlangObject);
            } else
            {
                return base.FromErlangRpc(module, function, erlangObject);
            }
        }

        private void InitializeConverterMap()
        {
            RegisterConverter("rabbit_access_control", "list_users", new ListUsersConverter());	
            RegisterConverter("rabbit", "status", new StatusConverter());
            RegisterConverter("rabbit_amqqueue", "info_all", new QueueInfoAllConverter());
        }

        private void RegisterConverter(string module, string function, IErlangConverter converter)
        {
            converterMap.Add(GenerateKey(module, function), converter);	   
        }

        private string GenerateKey(string module, string function)
        {
            return module + "%" + function;
        }

        protected virtual IErlangConverter GetConverter(string module, string function)
        {
            string key = GenerateKey(module, function);
            if (converterMap.ContainsKey(key))
            {
                return converterMap[key];
            }
            return null;
        }
    }

    public class ListUsersConverter : SimpleErlangConverter
    {
        public override object FromErlang(OtpErlangObject erlangObject)
        {
            IList<string> users = new List<string>();
            if (erlangObject is OtpErlangList)
            {
                OtpErlangList erlangList = (OtpErlangList) erlangObject;
                foreach (OtpErlangObject obj in erlangList)
                {
                    if (obj is OtpErlangBinary)
                    {
                        OtpErlangBinary binary = (OtpErlangBinary) obj;
                        users.Add(new ASCIIEncoding().GetString(binary.binaryValue()));
                    }

                }
            }
            return users;
        }
    }

    public class QueueInfoAllConverter : SimpleErlangConverter
    {
        public override object FromErlang(OtpErlangObject erlangObject)
        {
            IList<QueueInfo> queueInfoList = new List<QueueInfo>();
            if (erlangObject is OtpErlangList)
            {
                OtpErlangList erlangList = (OtpErlangList) erlangObject;
                foreach (OtpErlangObject element in erlangList)
                {
                    QueueInfo queueInfo = new QueueInfo();
                    OtpErlangList itemList = (OtpErlangList) element;
                    foreach (OtpErlangObject item in itemList)
                    {
                        OtpErlangTuple tuple = (OtpErlangTuple) item;
                        if (tuple.arity() == 2)
                        {
                            string key = tuple.elementAt(0).ToString();
                            OtpErlangObject value = tuple.elementAt(1);
                            switch (key)
                            {
                                case "name":
                                    queueInfo.Name = ExtractNameValueFromTuple((OtpErlangTuple) value);
                                    break;
                                case "transactions":
                                    queueInfo.Transactions = ExtractLong(value);
                                    break;
                                case "acks_uncommitted":
                                    queueInfo.AcksUncommitted = ExtractLong(value);
                                    break;
                                case "consumers":
                                    queueInfo.Consumers = ExtractLong(value);
                                    break;
                                case "pid":
                                    queueInfo.Pid = ExtractPid(value);
                                    break;
                                case "durable":
                                    queueInfo.Durable = ExtractAtomBoolean(value);                                    
                                    break;
                                case "messages":
                                    queueInfo.Messages = ExtractLong(value);
                                    break;
                                case "memory":
                                    queueInfo.Memory = ExtractLong(value);
                                    break;
                                case "auto_delete":
                                    queueInfo.AutoDelete = ExtractAtomBoolean(value);
                                    break;
                                case "messages_ready":
                                    queueInfo.MessagesReady = ExtractLong(value);
                                    break;
                                case "arguments":
                                    OtpErlangList list = (OtpErlangList)value;
                                    if (list != null)
                                    {
                                        String[] args = new String[list.arity()];
                                        for (int i = 0; i < list.arity(); i++)
                                        {
                                            OtpErlangObject obj = list.elementAt(i);
                                            args[i] = obj.ToString();
                                        }
                                        queueInfo.Arguments = args;
                                    }
                                    break;
                                case "messages_unacknowledged":
                                    queueInfo.MessagesUnacknowledged = ExtractLong(value);
                                    break;
                                case "messages_uncommitted":
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

        private bool ExtractAtomBoolean(OtpErlangObject value)
        {
            return ((OtpErlangAtom)value).boolValue();
        }

        private string ExtractNameValueFromTuple(OtpErlangTuple value)
        {
            Object nameElement = value.elementAt(3);
            return new ASCIIEncoding().GetString(((OtpErlangBinary) nameElement).binaryValue());
        }
    }

    public class StatusConverter : SimpleErlangConverter
    {
        public override object FromErlang(OtpErlangObject erlangObject)
        {
            IList<Application> applications = new List<Application>();
            IList<Node> nodes = new List<Node>();
            IList<Node> runningNodes = new List<Node>();
            if (erlangObject is OtpErlangList)
            {
                OtpErlangList erlangList = (OtpErlangList)erlangObject;

                OtpErlangTuple runningAppTuple = (OtpErlangTuple)erlangList.elementAt(0);
                OtpErlangList appList = (OtpErlangList)runningAppTuple.elementAt(1);
                ExtractApplications(applications, appList);

                OtpErlangTuple nodesTuple = (OtpErlangTuple)erlangList.elementAt(1);
                OtpErlangList nodesList = (OtpErlangList)nodesTuple.elementAt(1);
                ExtractNodes(nodes, nodesList);


                OtpErlangTuple runningNodesTuple = (OtpErlangTuple)erlangList.elementAt(2);
                nodesList = (OtpErlangList)runningNodesTuple.elementAt(1);
                ExtractNodes(runningNodes, nodesList);
            }

            return new RabbitStatus(applications, nodes, runningNodes);
        }

        private void ExtractNodes(IList<Node> nodes, OtpErlangList nodesList)
        {
            foreach (OtpErlangObject erlangNodeName in nodesList)
            {
                string nodeName = erlangNodeName.ToString();
                nodes.Add(new Node(nodeName));
            }
        }

        private void ExtractApplications(IList<Application> applications, OtpErlangList appList)
        {
            foreach (OtpErlangObject appDescription in appList)
            {
                OtpErlangTuple appDescriptionTuple = (OtpErlangTuple)appDescription;
                string name = appDescriptionTuple.elementAt(0).ToString();
                String description = appDescriptionTuple.elementAt(1).ToString();
                String version = appDescriptionTuple.elementAt(2).ToString();
                applications.Add(new Application(name, description, version));
            }           
        }
    }
}