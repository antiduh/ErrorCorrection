// -----------------------------------------------------------------------
// <copyright file="RsEncoderStream.cs" company="">
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

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class RsEncoderStream : Stream
    {
        private Stream readStream;

        private AntiduhEncoder encoder;

        private int[] chunkBuffer;

        private int remainingChunk;

        public RsEncoderStream( Stream readStream, AntiduhEncoder encoder ) : base()
        {
            this.readStream = readStream;
            this.encoder = encoder;

            this.chunkBuffer = new int[encoder.MessageSize];
            this.remainingChunk = 0;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanTimeout
        {
            get { return readStream.CanTimeout; }
        }

        public override void Close()
        {
            base.Close();
            readStream.Close();
        }

        public override long Position
        {
            get
            {
                throw new InvalidOperationException();
            }
            set
            {
                throw new InvalidOperationException();
            }
        }

        public override long Length
        {
            get { throw new InvalidOperationException(); }
        }

        public override void SetLength( long value )
        {
            throw new InvalidOperationException();
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            throw new InvalidOperationException();
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            throw new InvalidOperationException();
        }

        public override void WriteByte( byte value )
        {
            throw new InvalidOperationException();
        }

        public override void Flush()
        {
            
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            // We can only read from the encoder in fixed-length chunks, eg, 15 bytes for GF(2^4). 
            // Call this quanity 'chunkSize'.
            // If the reader ever asks for data from us with length shorter than chunkSize or 
            // not an exact multiple of chunkSize, we have to read the whole chunk, give them the 
            // portion they asked for, and buffer the rest.
            // The next time they call us, we'll first give them what's left and then anything left 
            // over we'll handle the same way.

            // Furthermore, the encoder doesn't support offset encoding, so we have to read into
            // chunkBuffer and then array copy into the destination buffer at the requested index.

            if( remainingChunk == 0 )
            {
                // Chunk buffer is empty. Fill it up, give the user something out of it.

                //readStream.Read( chunkBuffer, 0, chunkBuffer.Length );
            }

            return 0;
        }
    }
}
