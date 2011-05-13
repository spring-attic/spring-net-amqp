using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spring.Messaging.Amqp.Rabbit.Test
{
    /**
 * Global convenience class for all integration tests, carrying constants and other utilities for broker set up.
 * 
 * @author Dave Syer
 * 
 */
public class BrokerTestUtils {

	public static readonly int DEFAULT_PORT = 5672;

	public static readonly int TRACER_PORT = 5673;

    public static readonly string ADMIN_NODE_NAME = "spring@localhost";

	
	/**
	 * The port that the broker is listening on (e.g. as input for a {@link ConnectionFactory}).
	 * 
	 * @return a port number
	 */
	public static int GetPort() {
		return DEFAULT_PORT;
	}

	/**
	 * The port that the tracer is listening on (e.g. as input for a {@link ConnectionFactory}).
	 * 
	 * @return a port number
	 */
	public static int GetTracerPort() {
		return TRACER_PORT;
	}

	/**
	 * An alternative port number than can safely be used to stop and start a broker, even when one is already running
	 * on the standard port as a privileged user. Useful for tests involving {@link RabbitBrokerAdmin} on UN*X.
	 * 
	 * @return a port number
	 */
	public static int GetAdminPort() {
		return 15672;
	}

	/**
	 * Convenience factory for a {@link RabbitBrokerAdmin} instance that will usually start and stop cleanly on all
	 * systems.
	 * 
	 * @return a {@link RabbitBrokerAdmin} instance
	 */
	public static RabbitBrokerAdmin GetRabbitBrokerAdmin() {
		return getRabbitBrokerAdmin(ADMIN_NODE_NAME, GetAdminPort());
	}

	/**
	 * Convenience factory for a {@link RabbitBrokerAdmin} instance that will usually start and stop cleanly on all
	 * systems.
	 * 
	 * @param nodeName the name of the node
	 * 
	 * @return a {@link RabbitBrokerAdmin} instance
	 */
	public static RabbitBrokerAdmin GetRabbitBrokerAdmin(string nodeName) {
		return getRabbitBrokerAdmin(nodeName, GetAdminPort());
	}

	/**
	 * Convenience factory for a {@link RabbitBrokerAdmin} instance that will usually start and stop cleanly on all
	 * systems.
	 * 
	 * @param nodeName the name of the node
	 * @param port the port to listen on
	 * 
	 * @return a {@link RabbitBrokerAdmin} instance
	 */
    public static RabbitBrokerAdmin GetRabbitBrokerAdmin(string nodeName, int port)
    {
		RabbitBrokerAdmin brokerAdmin = new RabbitBrokerAdmin(nodeName, port);
		brokerAdmin.setRabbitLogBaseDirectory("target/rabbitmq/log");
		brokerAdmin.setRabbitMnesiaBaseDirectory("target/rabbitmq/mnesia");
		brokerAdmin.setStartupTimeout(10000L);
		return brokerAdmin;
	}

}
}
