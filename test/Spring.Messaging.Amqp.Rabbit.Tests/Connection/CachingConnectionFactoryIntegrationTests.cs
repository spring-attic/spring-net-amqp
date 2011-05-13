using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Spring.Messaging.Amqp.Rabbit.Connection
{
    [TestFixture]
    public class CachingConnectionFactoryIntegrationTests
    {
        private CachingConnectionFactory connectionFactory = new CachingConnectionFactory();

	public BrokerRunning brokerIsRunning = BrokerRunning.isRunning();
	
	public ExpectedException exception = ExpectedException.none();
    }
}
