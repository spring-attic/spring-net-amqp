
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Config
{

    /// <summary>
    /// Namespace handler for Rabbit.
    /// </summary>
    [
    NamespaceParser(
        Namespace = "http://www.springframework.net/schema/rabbit",
        SchemaLocationAssemblyHint = typeof(RabbitNamespaceHandler),
        SchemaLocation = "/Spring.Messaging.Amqp.Rabbit.Config/spring-rabbit.xsd"
        )
    ]
    public class RabbitNamespaceHandler : NamespaceParserSupport
    {
        public override void Init()
        {
            this.RegisterObjectDefinitionParser("queue", new QueueParser());
            this.RegisterObjectDefinitionParser("direct-exchange", new DirectExchangeParser());
            this.RegisterObjectDefinitionParser("topic-exchange", new TopicExchangeParser());
            this.RegisterObjectDefinitionParser("fanout-exchange", new FanoutExchangeParser());
            this.RegisterObjectDefinitionParser("headers-exchange", new HeadersExchangeParser());
            this.RegisterObjectDefinitionParser("listener-container", new ListenerContainerParser());
            this.RegisterObjectDefinitionParser("admin", new AdminParser());
            this.RegisterObjectDefinitionParser("connection-factory", new ConnectionFactoryParser());
            this.RegisterObjectDefinitionParser("template", new TemplateParser());
        }
    }
}
