
using Erlang.NET;
using Spring.Util;

namespace Spring.Erlang.Connection
{
    /// <summary>
    /// Encapsulate properties to create a OtpConnection
    /// </summary>
    /// <remarks></remarks>
    /// <author>Mark Pollack</author>
    /// <author>Joe Fitzgerald (.NET)</author>
    public class ConnectionParameters
    {
        /// <summary>
        /// The otp self.
        /// </summary>
        private OtpSelf otpSelf;

        /// <summary>
        /// The otp peer.
        /// </summary>
        private OtpPeer otpPeer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionParameters"/> class.
        /// </summary>
        /// <param name="otpSelf">The otp self.</param>
        /// <param name="otpPeer">The otp peer.</param>
        /// <remarks></remarks>
        public ConnectionParameters(OtpSelf otpSelf, OtpPeer otpPeer)
        {
            AssertUtils.ArgumentNotNull(otpSelf, "OtpSelf must be non-null");
            AssertUtils.ArgumentNotNull(otpPeer, "OtpPeer must be non-null");
            this.otpSelf = otpSelf;
            this.otpPeer = otpPeer;
        }

        /// <summary>
        /// Gets the otp self.
        /// </summary>
        /// <returns>The otp self.</returns>
        /// <remarks></remarks>
        public OtpSelf GetOtpSelf()
        {
            return this.otpSelf;
        }

        /// <summary>
        /// Gets the otp peer.
        /// </summary>
        /// <returns>The otp peer.</returns>
        /// <remarks></remarks>
        public OtpPeer GetOtpPeer()
        {
            return this.otpPeer;
        }
    }
}
