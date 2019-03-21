// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PublisherCallbackChannelImpl.cs" company="The original author or authors.">
//   Copyright 2002-2012 the original author or authors.
//   
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with
//   the License. You may obtain a copy of the License at
//   
//   https://www.apache.org/licenses/LICENSE-2.0
//   
//   Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on
//   an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the
//   specific language governing permissions and limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region Using Directives
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Common.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Impl;
using Spring.Util;
#endregion

namespace Spring.Messaging.Amqp.Rabbit.Support
{
    /// <summary>
    /// Channel wrapper to allow a single listener able to handle confirms from multiple channels.
    /// </summary>
    /// <author>Gary Russell</author>
    /// <author>Joe Fitzgerald</author>
    public class PublisherCallbackChannelImpl : IPublisherCallbackChannel
    {
        // , BasicAckEventHandler, BasicNackEventHandler
        private readonly ILog logger = LogManager.GetCurrentClassLogger();

        private readonly IModel channelDelegate;

        private readonly ConcurrentDictionary<string, IPublisherCallbackChannelListener> listeners = new ConcurrentDictionary<string, IPublisherCallbackChannelListener>();

        private readonly SortedDictionary<IPublisherCallbackChannelListener, SortedDictionary<long, PendingConfirm>> pendingConfirms = new SortedDictionary<IPublisherCallbackChannelListener, SortedDictionary<long, PendingConfirm>>();

        private readonly ConcurrentDictionary<long, IPublisherCallbackChannelListener> listenerForSeq = new ConcurrentDictionary<long, IPublisherCallbackChannelListener>();

        /// <summary>Initializes a new instance of the <see cref="PublisherCallbackChannelImpl"/> class.</summary>
        /// <param name="channelDelegate">The channel delegate.</param>
        public PublisherCallbackChannelImpl(IModel channelDelegate) { this.channelDelegate = channelDelegate; }

        #region Delegate Methods

        /// <summary>The add shutdown listener.</summary>
        /// <param name="listener">The listener.</param>
        public void AddShutdownListener(ModelShutdownEventHandler listener) { this.channelDelegate.ModelShutdown += listener; }

        /// <summary>The remove shutdown listener.</summary>
        /// <param name="listener">The listener.</param>
        public void RemoveShutdownListener(ModelShutdownEventHandler listener) { this.channelDelegate.ModelShutdown -= listener; }

        /// <summary>Gets the close reason.</summary>
        public ShutdownEventArgs CloseReason { get { return this.channelDelegate.CloseReason; } }

        /// <summary>The notify listeners.</summary>
        public void NotifyListeners() { ((ModelBase)this.channelDelegate).m_session.Notify(); }

        /// <summary>Gets a value indicating whether is open.</summary>
        public bool IsOpen { get { return this.channelDelegate.IsOpen; } }

        /// <summary>Gets the channel number.</summary>
        public int ChannelNumber { get { return ((ModelBase)this.channelDelegate).m_session.ChannelNumber; } }

        /// <summary>Gets the connection.</summary>
        public IConnection Connection { get { return ((ModelBase)this.channelDelegate).m_session.Connection; } }

        /// <summary>The close.</summary>
        /// <param name="closeCode">The close code.</param>
        /// <param name="closeMessage">The close message.</param>
        public void Close(ushort closeCode, string closeMessage) { this.channelDelegate.Close(closeCode, closeMessage); }

        /// <summary>The channel flow.</summary>
        /// <param name="active">The active.</param>
        public void ChannelFlow(bool active) { this.channelDelegate.ChannelFlow(active); }

        // public ChannelFlowOk Flow() { return this.channelDelegate.ChannelFlow()); }

        /// <summary>The abort.</summary>
        public void Abort() { this.channelDelegate.Abort(); }

        /// <summary>The abort.</summary>
        /// <param name="closeCode">The close code.</param>
        /// <param name="closeMessage">The close message.</param>
        public void Abort(ushort closeCode, string closeMessage) { this.channelDelegate.Abort(closeCode, closeMessage); }

        private readonly List<FlowControlEventHandler> flowListeners = new List<FlowControlEventHandler>();

        /// <summary>The flow control.</summary>
        public event FlowControlEventHandler FlowControl;

        /// <summary>The add flow listener.</summary>
        /// <param name="listener">The listener.</param>
        public void AddFlowListener(FlowControlEventHandler listener)
        {
            this.flowListeners.Add(listener);
            this.channelDelegate.FlowControl += listener;
        }

        /// <summary>The remove flow listener.</summary>
        /// <param name="listener">The listener.</param>
        public void RemoveFlowListener(FlowControlEventHandler listener)
        {
            if (this.flowListeners.Contains(listener))
            {
                this.flowListeners.Remove(listener);
                this.channelDelegate.FlowControl -= listener;
            }
        }

        /// <summary>The clear flow listeners.</summary>
        public void ClearFlowListeners()
        {
            foreach (var listener in this.flowListeners)
            {
                this.channelDelegate.FlowControl -= listener;
            }
        }

        /// <summary>Gets or sets the default consumer.</summary>
        public IBasicConsumer DefaultConsumer { get { return this.channelDelegate.DefaultConsumer; } set { this.channelDelegate.DefaultConsumer = value; } }

        /// <summary>The basic qos.</summary>
        /// <param name="prefetchSize">The prefetch size.</param>
        /// <param name="prefetchCount">The prefetch count.</param>
        /// <param name="global">The global.</param>
        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global) { this.channelDelegate.BasicQos(prefetchSize, prefetchCount, global); }

        /// <summary>The basic qos.</summary>
        /// <param name="prefetchCount">The prefetch count.</param>
        public void BasicQos(int prefetchCount) { this.channelDelegate.BasicQos(0, (ushort)prefetchCount, false); }

        /// <summary>The basic publish.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="props">The props.</param>
        /// <param name="body">The body.</param>
        public void BasicPublish(string exchange, string routingKey, IBasicProperties props, byte[] body) { this.channelDelegate.BasicPublish(exchange, routingKey, props, body); }

        /// <summary>The basic publish.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="mandatory">The mandatory.</param>
        /// <param name="immediate">The immediate.</param>
        /// <param name="props">The props.</param>
        /// <param name="body">The body.</param>
        public void BasicPublish(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties props, byte[] body) { this.channelDelegate.BasicPublish(exchange, routingKey, mandatory, immediate, props, body); }

        /// <summary>The exchange declare.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="type">The type.</param>
        public void ExchangeDeclare(string exchange, string type) { this.channelDelegate.ExchangeDeclare(exchange, type); }

        /// <summary>The exchange declare.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="type">The type.</param>
        /// <param name="durable">The durable.</param>
        public void ExchangeDeclare(string exchange, string type, bool durable) { this.channelDelegate.ExchangeDeclare(exchange, type, durable); }

        /// <summary>The exchange declare.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="type">The type.</param>
        /// <param name="durable">The durable.</param>
        /// <param name="autoDelete">The auto delete.</param>
        /// <param name="arguments">The arguments.</param>
        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary arguments) { this.channelDelegate.ExchangeDeclare(exchange, type, durable, autoDelete, arguments); }

        /// <summary>The exchange declare.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="type">The type.</param>
        /// <param name="durable">The durable.</param>
        /// <param name="autoDelete">The auto delete.</param>
        /// <param name="isInternal">The is internal.</param>
        /// <param name="arguments">The arguments.</param>
        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, bool isInternal, IDictionary arguments) { this.channelDelegate.ExchangeDeclare(exchange, type, durable, autoDelete /*, isInternal */, arguments); }

        /// <summary>The exchange declare passive.</summary>
        /// <param name="name">The name.</param>
        public void ExchangeDeclarePassive(string name) { this.channelDelegate.ExchangeDeclarePassive(name); }

        /// <summary>The exchange delete.</summary>
        /// <param name="exchange">The exchange.</param>
        /// <param name="ifUnused">The if unused.</param>
        public void ExchangeDelete(string exchange, bool ifUnused) { this.channelDelegate.ExchangeDelete(exchange, ifUnused); }

        /// <summary>The exchange delete.</summary>
        /// <param name="exchange">The exchange.</param>
        public void ExchangeDelete(string exchange) { this.channelDelegate.ExchangeDelete(exchange); }

        /// <summary>The exchange bind.</summary>
        /// <param name="destination">The destination.</param>
        /// <param name="source">The source.</param>
        /// <param name="routingKey">The routing key.</param>
        public void ExchangeBind(string destination, string source, string routingKey) { this.channelDelegate.ExchangeBind(destination, source, routingKey); }

        /// <summary>The exchange bind.</summary>
        /// <param name="destination">The destination.</param>
        /// <param name="source">The source.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="arguments">The arguments.</param>
        public void ExchangeBind(string destination, string source, string routingKey, IDictionary arguments) { this.channelDelegate.ExchangeBind(destination, source, routingKey, arguments); }

        /// <summary>The exchange unbind.</summary>
        /// <param name="destination">The destination.</param>
        /// <param name="source">The source.</param>
        /// <param name="routingKey">The routing key.</param>
        public void ExchangeUnbind(string destination, string source, string routingKey) { this.channelDelegate.ExchangeUnbind(destination, source, routingKey); }

        /// <summary>The exchange unbind.</summary>
        /// <param name="destination">The destination.</param>
        /// <param name="source">The source.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="arguments">The arguments.</param>
        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary arguments) { this.channelDelegate.ExchangeUnbind(destination, source, routingKey, arguments); }

        /// <summary>The queue declare.</summary>
        /// <returns>The RabbitMQ.Client.QueueDeclareOk.</returns>
        public QueueDeclareOk QueueDeclare() { return this.channelDelegate.QueueDeclare(); }

        /// <summary>The queue declare.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="durable">The durable.</param>
        /// <param name="exclusive">The exclusive.</param>
        /// <param name="autoDelete">The auto delete.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The RabbitMQ.Client.QueueDeclareOk.</returns>
        public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary arguments) { return this.channelDelegate.QueueDeclare(queue, durable, exclusive, autoDelete, arguments); }

        /// <summary>The queue declare passive.</summary>
        /// <param name="queue">The queue.</param>
        /// <returns>The RabbitMQ.Client.QueueDeclareOk.</returns>
        public QueueDeclareOk QueueDeclarePassive(string queue) { return this.channelDelegate.QueueDeclarePassive(queue); }

        /// <summary>The queue delete.</summary>
        /// <param name="queue">The queue.</param>
        /// <returns>The System.UInt32.</returns>
        public uint QueueDelete(string queue) { return this.channelDelegate.QueueDelete(queue); }

        /// <summary>The queue delete.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="ifUnused">The if unused.</param>
        /// <param name="ifEmpty">The if empty.</param>
        /// <returns>The System.UInt32.</returns>
        public uint QueueDelete(
            string queue, 
            bool ifUnused, 
            bool ifEmpty) { return this.channelDelegate.QueueDelete(queue, ifUnused, ifEmpty); }

        /// <summary>The queue bind.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        public void QueueBind(
            string queue, 
            string exchange, 
            string routingKey) { this.channelDelegate.QueueBind(queue, exchange, routingKey); }

        /// <summary>The queue bind.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="arguments">The arguments.</param>
        public void QueueBind(
            string queue, 
            string exchange, 
            string routingKey, 
            IDictionary arguments) { this.channelDelegate.QueueBind(queue, exchange, routingKey, arguments); }

        /// <summary>The queue unbind.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        public void QueueUnbind(
            string queue, 
            string exchange, 
            string routingKey) { this.channelDelegate.QueueUnbind(queue, exchange, routingKey, new Dictionary<string, object>()); }

        /// <summary>The queue unbind.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="arguments">The arguments.</param>
        public void QueueUnbind(
            string queue, 
            string exchange, 
            string routingKey, 
            IDictionary arguments) { this.channelDelegate.QueueUnbind(queue, exchange, routingKey, arguments); }

        /// <summary>The queue purge.</summary>
        /// <param name="queue">The queue.</param>
        /// <returns>The System.UInt32.</returns>
        public uint QueuePurge(string queue) { return this.channelDelegate.QueuePurge(queue); }

        /// <summary>The basic get.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="autoAck">The auto ack.</param>
        /// <returns>The RabbitMQ.Client.BasicGetResult.</returns>
        public BasicGetResult BasicGet(string queue, bool autoAck) { return this.channelDelegate.BasicGet(queue, autoAck); }

        /// <summary>The basic ack.</summary>
        /// <param name="deliveryTag">The delivery tag.</param>
        /// <param name="multiple">The multiple.</param>
        public void BasicAck(ulong deliveryTag, bool multiple) { this.channelDelegate.BasicAck(deliveryTag, multiple); }

        /// <summary>The basic nack.</summary>
        /// <param name="deliveryTag">The delivery tag.</param>
        /// <param name="multiple">The multiple.</param>
        /// <param name="requeue">The requeue.</param>
        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue) { this.channelDelegate.BasicNack(deliveryTag, multiple, requeue); }

        /// <summary>The basic reject.</summary>
        /// <param name="deliveryTag">The delivery tag.</param>
        /// <param name="requeue">The requeue.</param>
        public void BasicReject(ulong deliveryTag, bool requeue) { this.channelDelegate.BasicReject(deliveryTag, requeue); }

        /// <summary>The basic consume.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>The System.String.</returns>
        public string BasicConsume(string queue, IBasicConsumer callback) { return this.channelDelegate.BasicConsume(queue, true, callback); }

        /// <summary>The basic consume.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="autoAck">The auto ack.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>The System.String.</returns>
        public string BasicConsume(string queue, bool autoAck, IBasicConsumer callback) { return this.channelDelegate.BasicConsume(queue, autoAck, callback); }

        /// <summary>The basic consume.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="autoAck">The auto ack.</param>
        /// <param name="consumerTag">The consumer tag.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>The System.String.</returns>
        public string BasicConsume(string queue, bool autoAck, string consumerTag, IBasicConsumer callback) { return this.channelDelegate.BasicConsume(queue, autoAck, consumerTag, callback); }

        /// <summary>The basic consume.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="autoAck">The auto ack.</param>
        /// <param name="consumerTag">The consumer tag.</param>
        /// <param name="noLocal">The no local.</param>
        /// <param name="exclusive">The exclusive.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>The System.String.</returns>
        public string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary arguments, IBasicConsumer callback) { return this.channelDelegate.BasicConsume(queue, autoAck, consumerTag, noLocal, exclusive, arguments, callback); }

        /// <summary>The basic cancel.</summary>
        /// <param name="consumerTag">The consumer tag.</param>
        public void BasicCancel(string consumerTag) { this.channelDelegate.BasicCancel(consumerTag); }

        /// <summary>The basic recover.</summary>
        public void BasicRecover() { this.channelDelegate.BasicRecover(true); }

        /// <summary>The basic recover.</summary>
        /// <param name="requeue">The requeue.</param>
        public void BasicRecover(bool requeue) { this.channelDelegate.BasicRecover(requeue); }

        /// <summary>The basic recover async.</summary>
        /// <param name="requeue">The requeue.</param>
        [Obsolete("Deprecated", true)]
        public void BasicRecoverAsync(bool requeue) { this.channelDelegate.BasicRecoverAsync(requeue); }

        /// <summary>The tx select.</summary>
        public void TxSelect() { this.channelDelegate.TxSelect(); }

        /// <summary>The tx commit.</summary>
        public void TxCommit() { this.channelDelegate.TxCommit(); }

        /// <summary>The tx rollback.</summary>
        public void TxRollback() { this.channelDelegate.TxRollback(); }

        /// <summary>The confirm select.</summary>
        public void ConfirmSelect() { this.channelDelegate.ConfirmSelect(); }

        /// <summary>Gets the next publish seq no.</summary>
        public ulong NextPublishSeqNo { get { return this.channelDelegate.NextPublishSeqNo; } }

        /// <summary>The wait for confirms.</summary>
        /// <returns>The System.Boolean.</returns>
        public bool WaitForConfirms() { return this.channelDelegate.WaitForConfirms(); }

        /// <summary>The wait for confirms.</summary>
        /// <param name="timeout">The timeout.</param>
        /// <param name="timedOut">The timed out.</param>
        /// <returns>The System.Boolean.</returns>
        public bool WaitForConfirms(TimeSpan timeout, out bool timedOut) { return this.channelDelegate.WaitForConfirms(timeout, out timedOut); }

        /// <summary>The wait for confirms or die.</summary>
        public void WaitForConfirmsOrDie() { this.channelDelegate.WaitForConfirmsOrDie(); }

        /// <summary>The wait for confirms or die.</summary>
        /// <param name="timeout">The timeout.</param>
        public void WaitForConfirmsOrDie(TimeSpan timeout) { this.channelDelegate.WaitForConfirmsOrDie(timeout); }

        // public void AsyncRpc(Method method) { this.channelDelegate.AsyncRpc(method); }

        // public Command Rpc(Method method) { return this.channelDelegate.Rpc(method); }
        private readonly List<BasicAckEventHandler> ackListeners = new List<BasicAckEventHandler>();

        private readonly List<BasicNackEventHandler> nackListeners = new List<BasicNackEventHandler>();

        /// <summary>The basic acks.</summary>
        public event BasicAckEventHandler BasicAcks;

        /// <summary>The basic nacks.</summary>
        public event BasicNackEventHandler BasicNacks;

        /// <summary>The add confirm listener.</summary>
        /// <param name="ackListener">The ack listener.</param>
        /// <param name="nackListener">The nack listener.</param>
        public void AddConfirmListener(BasicAckEventHandler ackListener, BasicNackEventHandler nackListener)
        {
            this.ackListeners.Add(ackListener);
            this.nackListeners.Add(nackListener);
            this.channelDelegate.BasicAcks += ackListener;
            this.channelDelegate.BasicNacks += nackListener;
        }

        /// <summary>The remove confirm listener.</summary>
        /// <param name="ackListener">The ack listener.</param>
        /// <param name="nackListener">The nack listener.</param>
        public void RemoveConfirmListener(BasicAckEventHandler ackListener, BasicNackEventHandler nackListener)
        {
            if (this.ackListeners.Contains(ackListener))
            {
                this.ackListeners.Remove(ackListener);
                this.channelDelegate.BasicAcks -= ackListener;
            }

            if (this.nackListeners.Contains(nackListener))
            {
                this.nackListeners.Remove(nackListener);
                this.channelDelegate.BasicNacks -= nackListener;
            }
        }

        /// <summary>The clear confirm listeners.</summary>
        public void ClearConfirmListeners()
        {
            foreach (var listener in this.ackListeners)
            {
                this.channelDelegate.BasicAcks -= listener;
            }

            foreach (var listener in this.nackListeners)
            {
                this.channelDelegate.BasicNacks -= listener;
            }
        }

        private readonly List<BasicReturnEventHandler> returnListeners = new List<BasicReturnEventHandler>();

        /// <summary>The basic returns.</summary>
        public event BasicReturnEventHandler BasicReturns;

        /// <summary>The add return listener.</summary>
        /// <param name="listener">The listener.</param>
        public void AddReturnListener(BasicReturnEventHandler listener)
        {
            this.returnListeners.Add(listener);
            this.channelDelegate.BasicReturn += listener;
        }

        /// <summary>The remove return listener.</summary>
        /// <param name="listener">The listener.</param>
        public void RemoveReturnListener(BasicReturnEventHandler listener)
        {
            if (this.returnListeners.Contains(listener))
            {
                this.returnListeners.Remove(listener);
                this.channelDelegate.BasicReturn -= listener;
            }
        }

        /// <summary>The clear return listeners.</summary>
        public void ClearReturnListeners()
        {
            foreach (var listener in this.returnListeners)
            {
                this.channelDelegate.BasicReturn -= listener;
            }
        }

        #endregion

        /// <summary>The close.</summary>
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

        /// <summary>The add listener.</summary>
        /// <param name="listener">The listener.</param>
        /// <returns>The System.Collections.Generic.SortedDictionary`2[TKey -&gt; System.Int64, TValue -&gt; Spring.Messaging.Amqp.Rabbit.Support.PendingConfirm].</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public SortedDictionary<long, PendingConfirm> AddListener(IPublisherCallbackChannelListener listener)
        {
            AssertUtils.ArgumentNotNull(listener, "Listener cannot be null");
            if (this.listeners.Count == 0)
            {
                this.AddConfirmListener((model, args) => this.HandleAck((long)args.DeliveryTag, args.Multiple), (model, args) => this.HandleNack((long)args.DeliveryTag, args.Multiple));
                this.AddReturnListener((model, args) => this.HandleReturn(args.ReplyCode, args.ReplyText, args.Exchange, args.RoutingKey, args.BasicProperties, args.Body));
            }

            if (!this.listeners.Values.Contains(listener))
            {
                this.listeners.Add(listener.Uuid, listener);
                this.pendingConfirms.Add(listener, new SortedDictionary<long, PendingConfirm>());
                this.logger.Debug(m => m("Added listener " + listener));
            }

            return this.pendingConfirms.Get(listener);
        }

        /// <summary>The remove listener.</summary>
        /// <param name="listener">The listener.</param>
        /// <returns>The System.Boolean.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool RemoveListener(IPublisherCallbackChannelListener listener)
        {
            var mappedListener = this.listeners.GetAndRemove(listener.Uuid);
            var result = mappedListener != null;
            if (result && this.listeners.Count == 0)
            {
                this.RemoveConfirmListener((model, args) => this.HandleAck((long)args.DeliveryTag, args.Multiple), (model, args) => this.HandleNack((long)args.DeliveryTag, args.Multiple));
                this.RemoveReturnListener((model, args) => this.HandleReturn(args.ReplyCode, args.ReplyText, args.Exchange, args.RoutingKey, args.BasicProperties, args.Body));
            }

            var list = this.listenerForSeq.ToList();
            foreach (var item in list)
            {
                if (item.Value == listener)
                {
                    this.listenerForSeq.GetAndRemove(item.Key);
                }
            }

            this.pendingConfirms.Remove(listener);
            return result;
        }

        // ConfirmListener

        /// <summary>The handle ack.</summary>
        /// <param name="seq">The seq.</param>
        /// <param name="multiple">The multiple.</param>
        public void HandleAck(long seq, bool multiple)
        {
            this.logger.Debug(m => m(this.ToString() + " PC:Ack:" + seq + ":" + multiple));

            this.ProcessAck(seq, true, multiple);
        }

        /// <summary>The handle nack.</summary>
        /// <param name="seq">The seq.</param>
        /// <param name="multiple">The multiple.</param>
        public void HandleNack(long seq, bool multiple)
        {
            this.logger.Debug(m => m(this.ToString() + " PC:Nack:" + seq + ":" + multiple));

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
                    var involvedListeners = (from l in this.listenerForSeq where l.Key < (seq + 1) select l.Value).ToList();

                    // eliminate duplicates
                    var listeners = new HashSet<IPublisherCallbackChannelListener>(involvedListeners);
                    foreach (var involvedListener in listeners)
                    {
                        // find all unack'd confirms for this listener and handle them
                        var confirmsMap = this.pendingConfirms.Get(involvedListener);
                        if (confirmsMap != null)
                        {
                            var confirms = (from m in confirmsMap where m.Key < (seq + 1) select m).ToList();

                            foreach (var confirm in confirms)
                            {
                                confirmsMap.Remove(confirm.Key);
                                this.DoHandleConfirm(ack, involvedListener, confirm.Value);
                            }
                        }
                    }
                }
            }
            else
            {
                var listener = this.listenerForSeq.Get(seq);
                if (listener != null)
                {
                    var pendingConfirm = this.pendingConfirms.Get(listener).GetAndRemove(seq);
                    if (pendingConfirm != null)
                    {
                        this.DoHandleConfirm(ack, listener, pendingConfirm);
                    }
                }
                else
                {
                    this.logger.Error(m => m("No listener for seq:" + seq));
                }
            }
        }

        private void DoHandleConfirm(bool ack, IPublisherCallbackChannelListener listener, PendingConfirm pendingConfirm)
        {
            try
            {
                if (listener.IsConfirmListener)
                {
                    this.logger.Debug(m => m("Sending confirm " + pendingConfirm));
                    listener.HandleConfirm(pendingConfirm, ack);
                }
            }
            catch (Exception e)
            {
                this.logger.Error(m => m("Exception delivering confirm"), e);
            }
        }

        /// <summary>The add pending confirm.</summary>
        /// <param name="listener">The listener.</param>
        /// <param name="seq">The seq.</param>
        /// <param name="pendingConfirm">The pending confirm.</param>
        public void AddPendingConfirm(IPublisherCallbackChannelListener listener, long seq, PendingConfirm pendingConfirm)
        {
            var pendingConfirmsForListener = this.pendingConfirms[listener];
            AssertUtils.ArgumentNotNull(pendingConfirmsForListener, "Listener not registered");
            lock (this.pendingConfirms)
            {
                pendingConfirmsForListener.Add(seq, pendingConfirm);
            }

            this.listenerForSeq.TryAdd(seq, listener);
        }

        // ReturnListener

        /// <summary>The handle return.</summary>
        /// <param name="replyCode">The reply code.</param>
        /// <param name="replyText">The reply text.</param>
        /// <param name="exchange">The exchange.</param>
        /// <param name="routingKey">The routing key.</param>
        /// <param name="properties">The properties.</param>
        /// <param name="body">The body.</param>
        public void HandleReturn(int replyCode, string replyText, string exchange, string routingKey, IBasicProperties properties, byte[] body)
        {
            var uuidObject = properties.Headers["spring_return_correlation"].ToString();
            var listener = this.listeners[uuidObject];
            if (listener == null || !listener.IsReturnListener)
            {
                this.logger.Warn(m => m("No Listener for returned message"));
            }
            else
            {
                listener.HandleReturn(replyCode, replyText, exchange, routingKey, properties, body);
            }
        }

        /// <summary>The basic consume.</summary>
        /// <param name="queue">The queue.</param>
        /// <param name="noAck">The no ack.</param>
        /// <param name="consumerTag">The consumer tag.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="consumer">The consumer.</param>
        /// <returns>The System.String.</returns>
        public string BasicConsume(string queue, bool noAck, string consumerTag, IDictionary arguments, IBasicConsumer consumer) { return this.channelDelegate.BasicConsume(queue, noAck, consumerTag, arguments, consumer); }

        /// <summary>The basic publish.</summary>
        /// <param name="addr">The addr.</param>
        /// <param name="basicProperties">The basic properties.</param>
        /// <param name="body">The body.</param>
        public void BasicPublish(PublicationAddress addr, IBasicProperties basicProperties, byte[] body) { this.channelDelegate.BasicPublish(addr, basicProperties, body); }

        /// <summary>The basic recover ok.</summary>
        public event BasicRecoverOkEventHandler BasicRecoverOk;

        /// <summary>The basic return.</summary>
        public event BasicReturnEventHandler BasicReturn;

        /// <summary>The callback exception.</summary>
        public event CallbackExceptionEventHandler CallbackException;

        /// <summary>The create basic properties.</summary>
        /// <returns>The RabbitMQ.Client.IBasicProperties.</returns>
        public IBasicProperties CreateBasicProperties() { return this.channelDelegate.CreateBasicProperties(); }

        /// <summary>The create file properties.</summary>
        /// <returns>The RabbitMQ.Client.IFileProperties.</returns>
        public IFileProperties CreateFileProperties() { return this.channelDelegate.CreateFileProperties(); }

        /// <summary>The create stream properties.</summary>
        /// <returns>The RabbitMQ.Client.IStreamProperties.</returns>
        public IStreamProperties CreateStreamProperties() { return this.channelDelegate.CreateStreamProperties(); }

        /// <summary>The dtx select.</summary>
        public void DtxSelect() { this.channelDelegate.DtxSelect(); }

        /// <summary>The dtx start.</summary>
        /// <param name="dtxIdentifier">The dtx identifier.</param>
        public void DtxStart(string dtxIdentifier) { this.channelDelegate.DtxStart(dtxIdentifier); }

        /// <summary>The model shutdown.</summary>
        public event ModelShutdownEventHandler ModelShutdown;

        /// <summary>The dispose.</summary>
        public void Dispose() { }
    }
}
