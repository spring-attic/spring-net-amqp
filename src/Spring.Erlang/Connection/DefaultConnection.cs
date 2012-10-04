
using Erlang.NET;

namespace Spring.Erlang.Connection
{
    /// <summary>
    /// Basic implementation of {@link ConnectionProxy} that delegates to an underlying OtpConnection.
    /// </summary>
    /// <remarks></remarks>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class DefaultConnection : IConnectionProxy
    {
        /// <summary>
        /// The connection.
        /// </summary>
        private OtpConnection otpConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultConnection"/> class.
        /// </summary>
        /// <param name="otpConnection">The otp connection.</param>
        /// <remarks></remarks>
        public DefaultConnection(OtpConnection otpConnection)
        {
            this.otpConnection = otpConnection;
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        /// <remarks></remarks>
        public void Close()
        {
            this.otpConnection.close();
        }

        /// <summary>
        /// Sends the RPC.
        /// </summary>
        /// <param name="mod">The mod.</param>
        /// <param name="fun">The fun.</param>
        /// <param name="args">The args.</param>
        /// <remarks></remarks>
        public void SendRPC(string mod, string fun, OtpErlangList args)
        {
            this.otpConnection.sendRPC(mod, fun, args);
        }

        /// <summary>
        /// Receives the RPC.
        /// </summary>
        /// <returns>The second element of the tuple if the received message is a two-tuple, otherwise null. No further error checking is performed.</returns>
        /// <remarks></remarks>
        public OtpErlangObject ReceiveRPC()
        {
            return this.otpConnection.receiveRPC();
        }

        /// <summary>
        /// Gets the target connection.
        /// </summary>
        /// <returns>The connection.</returns>
        /// <remarks></remarks>
        public OtpConnection GetTargetConnection()
        {
            return this.otpConnection;
        }
    }
}
