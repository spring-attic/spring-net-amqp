
using System.IO;
using Erlang.NET;

namespace Spring.Erlang
{
    /// <summary>
    /// RuntimeException wrapper for an {@link IOException} which
    /// can be commonly thrown from OTP operations.
    /// </summary>
    /// <remarks></remarks>
    /// <author>Mark Pollack</author>
    /// <author>Mark Fisher</author>
    /// <author>Joe Fitzgerald</author>
    public class OtpIOException : OtpException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OtpIOException"/> class.
        /// </summary>
        /// <param name="cause">The cause.</param>
        /// <remarks></remarks>
        public OtpIOException(IOException cause) : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OtpIOException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cause">The cause.</param>
        /// <remarks></remarks>
        public OtpIOException(string message, IOException cause) : base(message)
        {
        }
    }
}
