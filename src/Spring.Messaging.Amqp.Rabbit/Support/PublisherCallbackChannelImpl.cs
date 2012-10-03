// -----------------------------------------------------------------------
// <copyright file="PublisherCallbackChannelImpl.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Common.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Framing.Impl.v0_9_1;
using RabbitMQ.Client.Impl;
using Spring.Util;

namespace Spring.Messaging.Amqp.Rabbit.Support
{
    /// <summary>
    /// Channel wrapper to allow a single listener able to handle confirms from multiple channels.
    /// </summary>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald</author>
    public class PublisherCallbackChannelImpl : IPublisherCallbackChannel // , BasicAckEventHandler, BasicNackEventHandler
    {
        private readonly ILog logger = LogManager.GetCurrentClassLogger();

        private readonly IModel channelDelegate;

        private readonly ConcurrentDictionary<string, IListener> listeners = new ConcurrentDictionary<string, IListener>();

        private readonly ConcurrentDictionary<IListener, SortedList<long, PendingConfirm>> pendingConfirms = new ConcurrentDictionary<IListener, SortedList<long, PendingConfirm>>();

        private readonly ConcurrentDictionary<long, IListener> listenerForSeq = new ConcurrentDictionary<long, IListener>();

        public PublisherCallbackChannelImpl(IModel channelDelegate) { this.channelDelegate = channelDelegate; }

        #region Delegate Methods
        public void AddShutdownListener(ModelShutdownEventHandler listener) { this.channelDelegate.ModelShutdown += listener; }

        public void RemoveShutdownListener(ModelShutdownEventHandler listener) { this.channelDelegate.ModelShutdown -= listener; }

        public ShutdownEventArgs CloseReason { get { return this.channelDelegate.CloseReason; } }

        public void NotifyListeners() { ((ModelBase)this.channelDelegate).m_session.Notify(); }

        public bool IsOpen { get { return this.channelDelegate.IsOpen; } }

        public int ChannelNumber { get { return ((ModelBase)this.channelDelegate).m_session.ChannelNumber; } }

        public IConnection Connection { get { return ((ModelBase)this.channelDelegate).m_session.Connection; } }

        public void Close(ushort closeCode, string closeMessage) { this.channelDelegate.Close((ushort)closeCode, closeMessage); }

        public void ChannelFlow(bool active) { this.channelDelegate.ChannelFlow(active); }

        // public ChannelFlowOk Flow() { return this.channelDelegate.ChannelFlow(); }

        public void Abort() { this.channelDelegate.Abort(); }

        public void Abort(ushort closeCode, string closeMessage) { this.channelDelegate.Abort((ushort)closeCode, closeMessage); }

        private IList<FlowControlEventHandler> flowListeners = new List<FlowControlEventHandler>();

        public event FlowControlEventHandler FlowControl;

        public void AddFlowListener(FlowControlEventHandler listener)
        {
            this.flowListeners.Add(listener);
            this.channelDelegate.FlowControl += listener;
        }

        public void RemoveFlowListener(FlowControlEventHandler listener)
        {
            if (this.flowListeners.Contains(listener))
            {
                this.flowListeners.Remove(listener);
                this.channelDelegate.FlowControl -= listener;
            }
        }

        public void ClearFlowListeners()
        {
            foreach (var listener in this.flowListeners)
            {
                this.channelDelegate.FlowControl -= listener;
            }
        }

        public IBasicConsumer DefaultConsumer { get { return this.channelDelegate.DefaultConsumer; } set { this.channelDelegate.DefaultConsumer = value; } }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global) { this.channelDelegate.BasicQos((uint)prefetchSize, (ushort)prefetchCount, global); }

        public void BasicQos(int prefetchCount) { this.channelDelegate.BasicQos(0, (ushort)prefetchCount, false); }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties props, byte[] body) { this.channelDelegate.BasicPublish(exchange, routingKey, props, body); }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties props, byte[] body) { this.channelDelegate.BasicPublish(exchange, routingKey, mandatory, immediate, props, body); }

        public void ExchangeDeclare(string exchange, string type) { this.channelDelegate.ExchangeDeclare(exchange, type); }

        public void ExchangeDeclare(string exchange, string type, bool durable) { this.channelDelegate.ExchangeDeclare(exchange, type, durable); }

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary arguments) { this.channelDelegate.ExchangeDeclare(exchange, type, durable, autoDelete, arguments); }

        // public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, bool isInternal, IDictionary arguments) { this.channelDelegate.ExchangeDeclare(exchange, type, durable, autoDelete, isInternal, arguments); }

        public void ExchangeDeclarePassive(string name) { this.channelDelegate.ExchangeDeclarePassive(name); }

        public void ExchangeDelete(string exchange, bool ifUnused) { this.channelDelegate.ExchangeDelete(exchange, ifUnused); }

        public void ExchangeDelete(string exchange) { this.channelDelegate.ExchangeDelete(exchange); }

        public void ExchangeBind(string destination, string source, string routingKey) { this.channelDelegate.ExchangeBind(destination, source, routingKey); }

        public void ExchangeBind(string destination, string source, string routingKey, IDictionary arguments) { this.channelDelegate.ExchangeBind(destination, source, routingKey, arguments); }

        public void ExchangeUnbind(string destination, string source, string routingKey) { this.channelDelegate.ExchangeUnbind(destination, source, routingKey); }

        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary arguments) { this.channelDelegate.ExchangeUnbind(destination, source, routingKey, arguments); }

        public RabbitMQ.Client.QueueDeclareOk QueueDeclare() { return this.channelDelegate.QueueDeclare(); }

        public RabbitMQ.Client.QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary arguments) { return this.channelDelegate.QueueDeclare(queue, durable, exclusive, autoDelete, arguments); }

        public RabbitMQ.Client.QueueDeclareOk QueueDeclarePassive(string queue) { return this.channelDelegate.QueueDeclarePassive(queue); }

        public uint QueueDelete(string queue) { return this.channelDelegate.QueueDelete(queue); }

        public uint QueueDelete(
            string queue,
            bool ifUnused,
            bool ifEmpty) { return this.channelDelegate.QueueDelete(queue, ifUnused, ifEmpty); }

        public void QueueBind(
            string queue,
            string exchange,
            string routingKey) { this.channelDelegate.QueueBind(queue, exchange, routingKey); }

        public void QueueBind(
            string queue,
            string exchange,
            string routingKey,
            IDictionary arguments) { this.channelDelegate.QueueBind(queue, exchange, routingKey, arguments); }

        public void QueueUnbind(
            string queue,
            string exchange,
            string routingKey) { this.channelDelegate.QueueUnbind(queue, exchange, routingKey, new Dictionary<string, object>()); }

        public void QueueUnbind(
            string queue,
            string exchange,
            string routingKey,
            IDictionary arguments) { this.channelDelegate.QueueUnbind(queue, exchange, routingKey, arguments); }

        public uint QueuePurge(string queue) { return this.channelDelegate.QueuePurge(queue); }

        public BasicGetResult BasicGet(string queue, bool autoAck) { return this.channelDelegate.BasicGet(queue, autoAck); }

        public void BasicAck(ulong deliveryTag, bool multiple) { this.channelDelegate.BasicAck(deliveryTag, multiple); }

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue) { this.channelDelegate.BasicNack(deliveryTag, multiple, requeue); }

        public void BasicReject(ulong deliveryTag, bool requeue) { this.channelDelegate.BasicReject(deliveryTag, requeue); }

        public string BasicConsume(string queue, IBasicConsumer callback) { return this.channelDelegate.BasicConsume(queue, true, callback); }

        public string BasicConsume(string queue, bool autoAck, IBasicConsumer callback) { return this.channelDelegate.BasicConsume(queue, autoAck, callback); }

        public string BasicConsume(string queue, bool autoAck, string consumerTag, IBasicConsumer callback) { return this.channelDelegate.BasicConsume(queue, autoAck, consumerTag, callback); }

        public string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary arguments, IBasicConsumer callback) { return this.channelDelegate.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, callback); }

        public void BasicCancel(string consumerTag) { this.channelDelegate.BasicCancel(consumerTag); }

        public void BasicRecover() { this.channelDelegate.BasicRecover(true); }

        public void BasicRecover(bool requeue) { this.channelDelegate.BasicRecover(requeue); }

        public void BasicRecoverAsync(bool requeue) { this.channelDelegate.BasicRecoverAsync(requeue); }

        public void TxSelect() { this.channelDelegate.TxSelect(); }

        public void TxCommit() { this.channelDelegate.TxCommit(); }

        public void TxRollback() { this.channelDelegate.TxRollback(); }

        public void ConfirmSelect() { this.channelDelegate.ConfirmSelect(); }

        public ulong NextPublishSeqNo { get { return this.channelDelegate.NextPublishSeqNo; } }

        public bool WaitForConfirms() { return this.channelDelegate.WaitForConfirms(); }

        public bool WaitForConfirms(TimeSpan timeout, out bool timedOut)
        {
            return this.channelDelegate.WaitForConfirms(timeout, out timedOut);
        }

        public void WaitForConfirmsOrDie() { this.channelDelegate.WaitForConfirmsOrDie(); }

        public void WaitForConfirmsOrDie(TimeSpan timeout) { this.channelDelegate.WaitForConfirmsOrDie(timeout); }

        // public void AsyncRpc(Method method) { this.channelDelegate.AsyncRpc(method); }

        // public Command Rpc(Method method) { return this.channelDelegate.Rpc(method); }

        private IList<BasicAckEventHandler> confirmListeners = new List<BasicAckEventHandler>();

        public event BasicAckEventHandler BasicAcks;

        public void AddConfirmListener(BasicAckEventHandler listener)
        {
            this.confirmListeners.Add(listener);
            this.channelDelegate.BasicAcks += listener;
        }

        public void RemoveConfirmListener(BasicAckEventHandler listener)
        {
            if (this.confirmListeners.Contains(listener))
            {
                this.confirmListeners.Remove(listener);
                this.channelDelegate.BasicAcks -= listener;
            }
        }

        public void ClearConfirmListeners()
        {
            foreach (var listener in this.confirmListeners)
            {
                this.channelDelegate.BasicAcks -= listener;
            }
        }

        private IList<BasicNackEventHandler> returnListeners = new List<BasicNackEventHandler>();

        public event BasicNackEventHandler BasicNacks;

        public void AddReturnListener(BasicNackEventHandler listener)
        {
            this.returnListeners.Add(listener);
            this.channelDelegate.BasicNacks += listener;
        }

        public void RemoveReturnListener(BasicNackEventHandler listener)
        {
            if (this.returnListeners.Contains(listener))
            {
                this.returnListeners.Remove(listener);
                this.channelDelegate.BasicNacks -= listener;
            }
        }

        public void ClearReturnListeners()
        {
            foreach (var listener in this.returnListeners)
            {
                this.channelDelegate.BasicNacks -= listener;
            }
        }
        #endregion

        public void Close()
        {
            this.channelDelegate.Close();
            foreach (var entry in this.pendingConfirms)
            {
                var listener = entry.Key;
                listener.RemovePendingConfirmsReference(this, entry.Value);
            }
            this.pendingConfirms.Clear();
            this.listenerForSeq.Clear();
        }

        public SortedList<long, PendingConfirm> AddListener(IListener listener)
        {
            AssertUtils.ArgumentNotNull(listener, "Listener cannot be null");
            if (this.listeners.Count == 0)
            {
                this.AddConfirmListener(this);
                this.AddReturnListener(this);
            }
            if (!this.listeners.Values.Contains(listener))
            {
                var listenersAdd = this.listeners.TryAdd(listener.GetUuid, listener);
                var pendingConfirmsAdd = this.pendingConfirms.TryAdd(listener, new SortedList<long, PendingConfirm>());
                logger.Debug(m => m("Added listener " + listener));
            }
            return this.pendingConfirms[listener];
        }

        public bool RemoveListener(IListener listener)
        {
            IListener mappedListener = null;
            var mappedListenerSuccess = this.listeners.TryRemove(listener.GetUuid, out mappedListener);
            var result = mappedListener != null;
            if (result && this.listeners.Count == 0)
            {
                this.RemoveConfirmListener(this);
                this.RemoveReturnListener(this);
            }
            var iterator = this.listenerForSeq.GetEnumerator();
            while (iterator.MoveNext())
            {
                var entry = iterator.Current;
                if (entry.Value == listener)
                {
                    IListener currentValue;
                    listenerForSeq.TryRemove(entry.Key, out currentValue);
                }
            }

            SortedList<long, PendingConfirm> currentConfirm;
            this.pendingConfirms.TryRemove(listener, out currentConfirm);
            return result;
        }

        //	ConfirmListener
        public void HandleAck(long seq, bool multiple)
        {
            logger.Debug(m => m(this.ToString() + " PC:Ack:" + seq + ":" + multiple));

            this.ProcessAck(seq, true, multiple);
        }

        public void HandleNack(long seq, bool multiple)
        {
            logger.Debug(m => m(this.ToString() + " PC:Nack:" + seq + ":" + multiple));

            this.ProcessAck(seq, false, multiple);
        }

        private void ProcessAck(long seq, bool ack, bool multiple)
        {
            if (multiple)
            {
                /*
                 * Piggy-backed ack - extract all Listeners for this and earlier
                 * sequences. Then, for each Listener, handle each of it's acks.
                 */
                lock (this.pendingConfirms)
                {
                    var involvedListeners = this.listenerForSeq.headMap(seq + 1);
                    // eliminate duplicates
                    var listeners = new HashSet<IListener>(involvedListeners.Values);
                    foreach (var involvedListener in listeners)
                    {
                        // find all unack'd confirms for this listener and handle them
                        var confirmsMap = this.pendingConfirms[involvedListener];
                        if (confirmsMap != null)
                        {
                            var confirms = confirmsMap.headMap(seq + 1);
                            var iterator = confirms.GetEnumerator();
                            while (iterator.MoveNext())
                            {
                                var entry = iterator.Current;
                                confirms.Remove(entry.Key);
                                this.DoHandleConfirm(ack, involvedListener, entry.Value);
                            }
                        }
                    }
                }
            }
            else
            {
                var listener = this.listenerForSeq[seq];
                if (listener != null)
                {
                    var pendingConfirm = this.pendingConfirms[listener].TryRemove(seq);
                    if (pendingConfirm != null)
                    {
                        this.DoHandleConfirm(ack, listener, pendingConfirm);
                    }
                }
                else
                {
                    logger.Error(m => m("No listener for seq:" + seq));
                }
            }
        }

        private void DoHandleConfirm(bool ack, IListener listener, PendingConfirm pendingConfirm)
        {
            try
            {
                if (listener.IsConfirmListener)
                {
                    logger.Debug(m => m("Sending confirm " + pendingConfirm));
                    listener.HandleConfirm(pendingConfirm, ack);
                }
            }
            catch (Exception e)
            {
                logger.Error(m => m("Exception delivering confirm"), e);
            }
        }

        public void AddPendingConfirm(IListener listener, long seq, PendingConfirm pendingConfirm)
        {
            var pendingConfirmsForListener = this.pendingConfirms[listener];
            AssertUtils.ArgumentNotNull(pendingConfirmsForListener, "Listener not registered");
            lock (this.pendingConfirms)
            {
                pendingConfirmsForListener.Add(seq, pendingConfirm);
            }
            this.listenerForSeq.TryAdd(seq, listener);
        }

        //  ReturnListener

        public void HandleReturn(
            int replyCode,
            String replyText,
            String exchange,
            String routingKey,
            IBasicProperties properties,
            byte[] body)
        {
            var uuidObject = properties.Headers["spring_return_correlation"].ToString();
            var listener = this.listeners[uuidObject];
            if (listener == null || !listener.IsReturnListener)
            {
                logger.Warn(m => m("No Listener for returned message"));

            }
            else
            {
                listener.HandleReturn(replyCode, replyText, exchange, routingKey, properties, body);
            }
        }


        public string BasicConsume(string queue, bool noAck, string consumerTag, IDictionary arguments, IBasicConsumer consumer)
        {
            throw new NotImplementedException();
        }
        
        public void BasicPublish(PublicationAddress addr, IBasicProperties basicProperties, byte[] body)
        {
            throw new NotImplementedException();
        }

        public event BasicRecoverOkEventHandler BasicRecoverOk;

        public event BasicReturnEventHandler BasicReturn;

        public event CallbackExceptionEventHandler CallbackException;
        
        public IBasicProperties CreateBasicProperties()
        {
            throw new NotImplementedException();
        }

        public IFileProperties CreateFileProperties()
        {
            throw new NotImplementedException();
        }

        public IStreamProperties CreateStreamProperties()
        {
            throw new NotImplementedException();
        }

        public void DtxSelect()
        {
            throw new NotImplementedException();
        }

        public void DtxStart(string dtxIdentifier)
        {
            throw new NotImplementedException();
        }

        public event ModelShutdownEventHandler ModelShutdown;

        public void Dispose()
        {
            
        }
    }
}
