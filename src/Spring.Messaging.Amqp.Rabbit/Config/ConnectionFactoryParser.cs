
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using Spring.Messaging.Amqp.Rabbit.Connection;
using Spring.Objects.Factory.Support;
using Spring.Objects.Factory.Xml;

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    /// <summary>
    /// A connection factory parser.
    /// </summary>
    public class ConnectionFactoryParser : AbstractSingleObjectDefinitionParser
    {
        private static readonly string CONNECTION_FACTORY_ATTRIBUTE = "connection-factory";

	    private static readonly string CHANNEL_CACHE_SIZE_ATTRIBUTE = "channel-cache-size";

	    private static readonly string HOST_ATTRIBUTE = "host";

	    private static readonly string PORT_ATTRIBUTE = "port";

	    private static readonly string VIRTUAL_HOST_ATTRIBUTE = "virtual-host";

	    private static readonly string USER_ATTRIBUTE = "username";

	    private static readonly string PASSWORD_ATTRIBUTE = "password";

        protected override Type GetObjectType(XmlElement element)
        {
            return typeof(CachingConnectionFactory);
        }

        protected override bool ShouldGenerateId
        {
            get
            {
                return false;
            }
        }

        protected override bool ShouldGenerateIdAsFallback
        {
            get
            {
                return true;
            }
        }

        protected override void DoParse(XmlElement element, ParserContext parserContext, ObjectDefinitionBuilder builder)
        {
            NamespaceUtils.AddConstructorArgParentRefIfAttributeDefined(builder, element, CONNECTION_FACTORY_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, CHANNEL_CACHE_SIZE_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, HOST_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, PORT_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, USER_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, PASSWORD_ATTRIBUTE);
            NamespaceUtils.SetValueIfAttributeDefined(builder, element, VIRTUAL_HOST_ATTRIBUTE);
        }
    }
}
