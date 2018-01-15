using System;
using System.Collections.Generic;

namespace TransmissionHandler
{
    /// <summary>
    /// Event arguments containing event data.
    /// </summary>
    public class DataTransmissionEventArgs : EventArgs
    {
        protected byte[] data;

        public byte[] EventData
        {
            get { return this.data; }
        }

        /// <summary>
        ///Copies the data starting at the specified index and paste them to the inner array
        /// </summary>
        /// <param name="result">Data raised in the event.</param>
        /// <param name="index">the index in the sourceArray at which copying begins.</param>
        /// <param name="length"> the number of elements to copy</param>
        public DataTransmissionEventArgs(byte[] result, int index, int length)
        {
            data = new byte[length];
            Array.Copy(result, index, data, 0, length);
        }
        /// <summary>
        /// Copies the data to the inner array 
        /// </summary>
        /// <param name="result">Data raised in the event.</param>
        public DataTransmissionEventArgs(byte[] result)
        {
            data = new byte[result.Length];
            Array.Copy(result, data, result.Length);
        }
        /// <summary>
        /// Override of Object.ToString
        /// </summary>
        /// <returns>String with ConnectionEventArgs parameters</returns>
        public override string ToString()
        {
            return EventData.ToString();
        }
    }
}
