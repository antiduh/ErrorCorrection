// -----------------------------------------------------------------------
// <copyright file="BlockStreamWriteAdapter.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace ErrorCorrection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Diagnostics;

    /// <summary>
    /// Implements an adapter for the write-aspect of a stream where that stream supports writes that 
    /// must always be a certain block size. For instance, if some underlying stream only supported writes
    /// of 16 bytes, this adapter would provide buffering to automatically handle writes of any size, meanwhile
    /// it would always write to the underlying stream with requests that are always exactly 16 bytes.
    /// </summary>
    /// <remarks>
    /// Bytes are only written to the underlying stream when there are enough bytes to fill a complete block.
    /// This means that short writes (requests that are less than the block size) are buffered until enough
    /// has been buffered for a complete block to be sent down.
    /// 
    /// If a flush is requested and there are unwritten bytes buffered, then those bytes will be written
    /// out in a block padded with zeros. If a flush is requested when no bytes are buffered, the flush is
    /// ignored.
    /// </remarks>
    public class BlockStreamWriteAdapter : Stream
    {
        /// <summary>
        /// The underlying stream to write to, that must only be written to using writes of only a single size.
        /// </summary>
        private Stream blockSink;

        /// <summary>
        /// Stores bytes that haven't been written to a complete block yet in the underlying stream.
        /// </summary>
        private byte[] leftovers;

        /// <summary>
        /// Indicates the number of bytes that are stored in the buffer.
        /// </summary>
        private int leftoversConsumption;

        /// <summary>
        /// Inititializes a new instance of the BlockStreamWriteAdapter class.
        /// </summary>
        /// <param name="blockSink">The underlying stream to write to that has block semantics.</param>
        /// <param name="blockSize">The size of the block that must be written to the block sink.</param>
        public BlockStreamWriteAdapter( Stream blockSink, int blockSize )
        {
            if( blockSink.CanWrite == false )
            {
                throw new ArgumentException( "The provided stream does not support writing", "blockSink" );
            }

            this.blockSink = blockSink;
            this.BlockSize = blockSize;

            this.leftovers = new byte[blockSize];
            this.leftoversConsumption = 0;
        }

        public int BlockSize { get; private set; }

        public override void Write( byte[] buffer, int offset, int count )
        {
            // Count up how many bytes we have in the leftovers and in the write request.
            // If we have enough to perform a full write to blockSink, then do a write.
            
            // Doing a write:
            //  - If we have anything in the leftovers, then consume bytes from the write request
            //    to fill it up to a full block size, then write that to the blockSink.
            //
            //  - For as many full blocks that are left in the request, send requests directly to blockSink.
            //
            //  - If any bytes are left over from the request, they are saved to the leftovers for the next iteration.

            // ------ Step 1: Figure out if we have enough to do a write at all ------

            if( this.leftoversConsumption + count >= 16 )
            {

                // ------ Step 2: Write what we can from the leftovers ------

                // ------ Step 3: Write what we can from the request ------

            }

            // ------  Step 4: Save any unwritten bytes to the buffer ------
            if( count > 0 )
            {
                // Check to make sure we have enough room in the unwritten buffer to store the leftovers.
                // If we don't, we screwed up somewhere and should've written more.

                Debug.Assert(
                    count + this.leftoversConsumption < this.BlockSize,
                    "Attempted to save leftovers equal or larger than a block",
                    "The point of leftovers is to store bytes that couldn't be written because they didn't fill a full block. "
                      + "However, we found that we have enough leftovers to fill a full block which means we should've written one more "
                      + "block to the output, but didn't. The math is broken somewhere"
                );

                // If zero bytes are stored in the unwritten buffer, then the first index to write to is zero.
                // The first index to write to in the buffer is the bufferConsumption.

                Array.Copy( buffer, offset, this.leftovers, this.leftoversConsumption, count );
            }


        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override void Flush()
        {
            
        }
    }
}
