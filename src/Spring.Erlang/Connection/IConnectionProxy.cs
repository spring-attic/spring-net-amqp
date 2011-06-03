
using Erlang.NET;

namespace Spring.Erlang.Connection
{
    /// <summary>
    /// Subinterface of {@link IConnection} to be implemented by Connection proxies.
    /// Allows access to the underlying target Connection.
    /// </summary>
    /// <remarks></remarks>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald</author>
    public interface IConnectionProxy : IConnection
    {
        /// <summary>
        /// Gets the target connection.
        /// </summary>
        /// <returns>The connection.</returns>
        /// <remarks></remarks>
        OtpConnection GetTargetConnection();
    }
}
