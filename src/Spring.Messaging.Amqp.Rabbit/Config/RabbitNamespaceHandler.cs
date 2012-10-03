// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitNamespaceHandler.cs" company="The original author or authors.">
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
using Spring.Objects.Factory.Xml;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// Namespace handler for Rabbit.
    /// </summary>
    /// <author>Mark Pollack</author>
    /// <author>Mark Fisher</author>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald</author>
    [NamespaceParser(Namespace = "http://www.springframework.net/schema/rabbit", SchemaLocationAssemblyHint = typeof(RabbitNamespaceHandler), SchemaLocation = "/Spring.Messaging.Amqp.Rabbit.Config/spring-rabbit-1.1.xsd")]
    public class RabbitNamespaceHandler : NamespaceParserSupport
    {
        /// <summary>The init.</summary>
        public override void Init()
        {
            this.RegisterObjectDefinitionParser("queue", new QueueParser());
            this.RegisterObjectDefinitionParser("direct-exchange", new DirectExchangeParser());
            this.RegisterObjectDefinitionParser("topic-exchange", new TopicExchangeParser());
            this.RegisterObjectDefinitionParser("fanout-exchange", new FanoutExchangeParser());
            this.RegisterObjectDefinitionParser("headers-exchange", new HeadersExchangeParser());
            this.RegisterObjectDefinitionParser("federated-exchange", new FederatedExchangeParser());
            this.RegisterObjectDefinitionParser("listener-container", new ListenerContainerParser());
            this.RegisterObjectDefinitionParser("admin", new AdminParser());
            this.RegisterObjectDefinitionParser("connection-factory", new ConnectionFactoryParser());
            this.RegisterObjectDefinitionParser("template", new TemplateParser());
            this.RegisterObjectDefinitionParser("queue-arguments", new QueueArgumentsParser());
        }
    }
}
