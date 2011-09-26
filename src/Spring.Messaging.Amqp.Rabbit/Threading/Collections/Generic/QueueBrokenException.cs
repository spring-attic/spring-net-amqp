using System;

namespace Spring.Threading.Collections.Generic
{
    /// <summary>
    /// Exception to indicate a queue is already broken.
    /// </summary>
    [Serializable]
    public class QueueBrokenException : Exception
    {
    }
}