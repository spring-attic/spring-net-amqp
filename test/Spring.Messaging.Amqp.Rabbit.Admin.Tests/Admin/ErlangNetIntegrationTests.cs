// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErlangNetIntegrationTests.cs" company="The original author or authors.">
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
using System;
using System.IO;
using System.Net;
using System.Text;
using Common.Logging;
using Erlang.NET;
using NUnit.Framework;
using Spring.Erlang.Connection;
using Spring.Erlang.Core;
using Spring.Messaging.Amqp.Rabbit.Tests.Test;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Admin
{
    /// <summary>
    /// Equivalent to JInterfaceIntegrationTests
    /// </summary>
    [TestFixture]
    [Category(TestCategory.Integration)]
    public class ErlangNetIntegrationTests
    {
        private static readonly ILog logger = LogManager.GetCurrentClassLogger();

        private static int counter;

        private static readonly string NODE_NAME = "rabbit@" + Dns.GetHostName().ToUpper();

        private OtpConnection connection;

        private RabbitBrokerAdmin brokerAdmin;

        public static EnvironmentAvailable environment = new EnvironmentAvailable("BROKER_INTEGRATION_TEST");

        /// <summary>The init.</summary>
        [SetUp]
        public void Init()
        {
            environment.Apply();
            this.brokerAdmin = BrokerTestUtils.GetRabbitBrokerAdmin(NODE_NAME);
            var status = this.brokerAdmin.GetStatus();
            if (!status.IsRunning)
            {
                this.brokerAdmin.StartBrokerApplication();
            }
        }

        /// <summary>The close.</summary>
        [TearDown]
        public void Close()
        {
            if (this.connection != null)
            {
                this.connection.close();
            }

            if (this.brokerAdmin != null)
            {
                this.brokerAdmin.StopNode();
            }
        }

        /// <summary>The test raw api.</summary>
        [Test]
        public void TestRawApi()
        {
            var self = new OtpSelf("rabbit-monitor");

            var hostName = NODE_NAME;
            var peer = new OtpPeer(hostName);
            this.connection = self.connect(peer);

            var encoding = new UTF8Encoding();
            OtpErlangObject[] objectArray = { new OtpErlangBinary(encoding.GetBytes("/")) };

            this.connection.sendRPC("rabbit_amqqueue", "info_all", new OtpErlangList(objectArray));

            var received = this.connection.receiveRPC();
            logger.Info(received);
            logger.Info(received.GetType().ToString());
        }

        /// <summary>
        /// Tests the OtpTemplate.
        /// </summary>
        [Test]
        public void OtpTemplate()
        {
            var selfNodeName = "rabbit-monitor";
            var peerNodeName = NODE_NAME;

            var cf = new SingleConnectionFactory(selfNodeName, peerNodeName);

            cf.AfterPropertiesSet();
            var template = new ErlangTemplate(cf);
            template.AfterPropertiesSet();

            var number = (long)template.ExecuteAndConvertRpc("erlang", "abs", -161803399);
            Assert.AreEqual(161803399, number);

            cf.Dispose();
        }

        /// <summary>
        /// Tests the raw otp connect.
        /// </summary>
        [Test]
        public void TestRawOtpConnect() { this.CreateConnection(); }

        /// <summary>The stress test.</summary>
        [Test]
        public void StressTest()
        {
            var cookie = this.ReadCookie();
            logger.Info("Cookie: " + cookie);
            var con = this.CreateConnection();
            var recycleConnection = false;
            for (int i = 0; i < 100; i++)
            {
                this.ExecuteRpc(con, recycleConnection, "rabbit", "status");
                this.ExecuteRpc(con, recycleConnection, "rabbit", "stop");
                this.ExecuteRpc(con, recycleConnection, "rabbit", "status");
                this.ExecuteRpc(con, recycleConnection, "rabbit", "start");
                this.ExecuteRpc(con, recycleConnection, "rabbit", "status");
                if (i % 10 == 0)
                {
                    logger.Debug("i = " + i);
                }
            }
        }

        /// <summary>Creates the connection.</summary>
        /// <returns>The Erlang.NET.OtpConnection.</returns>
        public OtpConnection CreateConnection()
        {
            var self = new OtpSelf("rabbit-monitor-" + counter++);
            var peer = new OtpPeer(NODE_NAME);
            return self.connect(peer);
        }

        /// <summary>Executes the RPC.</summary>
        /// <param name="con">The con.</param>
        /// <param name="recycleConnection">if set to <c>true</c> [recycle connection].</param>
        /// <param name="module">The module.</param>
        /// <param name="function">The function.</param>
        private void ExecuteRpc(OtpConnection con, bool recycleConnection, string module, string function)
        {
            con.sendRPC(module, function, new OtpErlangList());
            var response = con.receiveRPC();
            logger.Debug(module + " response received = " + response);
            if (recycleConnection)
            {
                con.close();
                con = this.CreateConnection();
            }
        }

        /// <summary>Reads the cookie.</summary>
        /// <returns>The System.String.</returns>
        private string ReadCookie()
        {
            var cookie = string.Empty;
            var dotCookieFilename = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".erlang.cookie");
            var cookieFile = new FileInfo(dotCookieFilename);

            if (!cookieFile.Exists)
            {
                logger.Info(string.Format("Could not find cookie file at path: {0}", cookieFile.FullName));
                Assert.Inconclusive("Could not read Erlang cookie file.");
            }

            try
            {
                cookie = File.ReadAllText(cookieFile.FullName);
            }
            catch (Exception ex)
            {
                logger.Error("Error occurred reading cookie file", ex);
                throw;
            }

            return cookie;
        }
    }
}
