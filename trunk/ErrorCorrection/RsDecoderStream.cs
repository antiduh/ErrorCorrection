// -----------------------------------------------------------------------
// <copyright file="RsDecoderStream.cs" company="">
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
    public class RsDecoderStream : Stream
    {
        private Stream stream;
        private Decoder decoder;

        private int[] blockBuffer;

        private byte[] inputBuffer;

        private int checkSymbols;

        public RsDecoderStream( Stream stream, Decoder decoder )
        {
            this.stream = stream;
            this.decoder = decoder;

            this.inputBuffer = new byte[decoder.BlockSize];
            this.blockBuffer = new int[decoder.BlockSize];

            this.checkSymbols = decoder.BlockSize - decoder.MessageSize;
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            if( count != decoder.MessageSize )
            {
                throw new InvalidOperationException();
            }

            int bytesRead = stream.Read( this.inputBuffer, 0, decoder.BlockSize );

            if( bytesRead == 0 )
            {
                return 0;
            }
            else if( bytesRead != decoder.BlockSize )
            {
                throw new IOException( "Didn't read a whole block" );
            }

            Array.Copy( this.inputBuffer, this.blockBuffer, this.decoder.BlockSize );

            this.decoder.Decode( this.blockBuffer );

            for( int i = 0; i < decoder.MessageSize; i++ )
            {
                buffer[offset + i] = (byte)this.blockBuffer[checkSymbols + i];
            }

            return count;
        }

        public override void Write( byte[] buffer, int offset, int count )
        {
            throw new NotSupportedException();
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

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Seek( long offset, SeekOrigin origin )
        {
            throw new NotSupportedException ();
        }

        public override void Flush()
        {
            this.stream.Flush();
        }

        public override void SetLength( long value )
        {
            throw new NotSupportedException();
        }
    }

}
