using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorCorrection
{
    public class BlockStreamReadAdapter : Stream
    {
        private Stream blockSource;

        private byte[] leftovers;

        private int leftoverSize;

        public BlockStreamReadAdapter( Stream blockSource, int blockSize )
        {
            this.blockSource = blockSource;
            this.BlockSize = blockSize;

            this.leftovers = new byte[blockSize];
            this.leftoverSize = 0;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public int BlockSize { get; private set; }

        public override int Read( byte[] buffer, int offset, int count )
        {
            int written = 0;

            // We have to try to service as much of the read as possible.
            // The underlying stream will return either an exact block, or zero bytes to indicate end of stream.

            // Step 1: Consume leftovers
            // If there's any leftovers, fullfill as much of the read as possible from
            // there. We store the earliest bytes at the beginning of the leftovers, so if
            // we don't use all of the leftovers, we'll have to copy them down to the beginning.
            // This could happen if, eg, leftovers contains 5 bytes and the read request is for
            // 4 bytes.

            if( this.leftoverSize > 0 )
            {
                // Keep in mind that they could be asking to read a single byte.
                int writeSize = Math.Min( this.leftoverSize, count );

                Array.Copy( this.leftovers, 0, buffer, offset, writeSize );

                count -= writeSize;
                offset += writeSize;
                written += writeSize;

                if( writeSize < this.leftoverSize )
                {
                    // They asked for a read that was less than what we had leftover.
                    // Fixup the leftovers to move the remaining bits down to the beginning
                    // of the array, and then return since we've completely satisfied the request.
                    for( int i = 0; i < this.leftoverSize - writeSize; i++ )
                    {
                        this.leftovers[i] = this.leftovers[i + writeSize];
                    }

                    this.leftoverSize -= writeSize;

                    return written;
                }
                else
                {
                    this.leftoverSize = 0;
                }
            }


            // Step 2: Copy whole blocks input to output.
            // Try to fulfill as much of the request as possible using direct reads from the block
            // stream, in single block sizes. If we need to copy less than a whole block, stop.
            while( count >= this.BlockSize )
            {
                int bytesRead = this.blockSource.Read( buffer, offset, this.BlockSize );

                if( bytesRead == 0 )
                {
                    // We hit end of stream.
                    return written;
                }
                else
                {
                    offset += this.BlockSize;
                    count -= this.BlockSize;
                    written += this.BlockSize;
                }
            }

            // Step 3: Complete any partial chunks.
            // In step 2, we only read and write whole blocks. If the request has bytes left that aren't
            // exactly a whole block, then we'll read a whole block, write as much as we can to the request,
            // and the store the remaining bits from the block we just read as leftovers.

            if( count >= this.BlockSize )
            {
                throw new Exception();
            }

            if( count > 0 )
            {
                // The condition `BlockSize > count > 0` must be true - we have an incomplete block.
                // Also, leftovers must be empty, else we would never have gotten this far.
                // Perform a read directly into the leftovers buffer, copy what we can, and then
                // copy down leftovers.

                // Step 1: Read a whole block from the underlying stream into leftovers.
                int amountRead = this.blockSource.Read( this.leftovers, 0, this.BlockSize );

                if( amountRead == 0 )
                {
                    return written;
                }


                // Step 2: Copy a partial block from leftovers to the request.
                Array.Copy( this.leftovers, 0, buffer, offset, count );

                int newLeftoverSize = this.BlockSize - count;

                // Step 3: Move the remainder of leftovers down so that the first leftover byte is at index 0.
                for( int i = 0; i < newLeftoverSize; i++ )
                {
                    this.leftovers[i] = this.leftovers[i + count];
                }

                // Step 4: Update statistics
                written += count;
                this.leftoverSize = newLeftoverSize;
            }

            return written;
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            throw new NotSupportedException();
        }

        public override void SetLength( long value )
        {
            throw new NotSupportedException();
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            this.blockSource.Flush();
        }
    }
}
