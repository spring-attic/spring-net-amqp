using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AopAlliance.Aop;
using Spring.Messaging.Amqp.Rabbit.Retry;

namespace Spring.Messaging.Amqp.Rabbit.Config
{
    // Spring Batch not implemented.
    public abstract class AbstractRetryOperationsInterceptorFactoryObject : FactoryObject<IAdvice>
    {

	private IMessageRecoverer messageRecoverer;

	private Spring.Aspects.RetryAdvice retryTemplate;

	public void setRetryOperations(RetryOperations retryTemplate) {
		this.retryTemplate = retryTemplate;
	}

	public void setMessageRecoverer(MessageRecoverer messageRecoverer) {
		this.messageRecoverer = messageRecoverer;
	}

	protected RetryOperations getRetryOperations() {
		return retryTemplate;
	}

	protected MessageRecoverer getMessageRecoverer() {
		return messageRecoverer;
	}

	public bool IsSingleton() {
		return true;
	}


    public object GetObject()
    {
        throw new NotImplementedException();
    }

    bool Objects.Factory.IFactoryObject.IsSingleton
    {
        get { throw new NotImplementedException(); }
    }

    public Type ObjectType
    {
        get { throw new NotImplementedException(); }
    }
    }
}
