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
        private Stream stream;

        private AntiduhEncoder encoder;

        private int[] blockBuffer;
        private byte[] outputBuffer;

        private int checkSymbols;
        
        public RsEncoderStream( Stream stream, AntiduhEncoder encoder ) : base()
        {
            if( stream.CanWrite == false )
            {
                throw new ArgumentException( "Must be a writable stream", "stream" );
            }

            this.stream = stream;
            this.encoder = encoder;

            this.blockBuffer = new int[encoder.CodeWordSize];
            this.checkSymbols = encoder.CodeWordSize - encoder.PlainTextSize;

            this.outputBuffer = new byte[encoder.CodeWordSize];
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanTimeout
        {
            get { return stream.CanTimeout; }
        }

        public override void Close()
        {
            base.Close();
            stream.Close();
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
            if( count != this.encoder.PlainTextSize )
            {
                throw new InvalidOperationException( "RsEncoderStream only accepts writes that are exactly the size " + 
                    "of a single reed-solmon block. Use BlockStreamWriteAdapter to buffer writes of differing sizes." );
            }

            Array.Clear( this.blockBuffer, 0, this.checkSymbols );

            Array.Copy( buffer, offset, this.blockBuffer, this.checkSymbols, count );

            this.encoder.Encode( this.blockBuffer );

            for( int i = 0; i < this.blockBuffer.Length; i++ )
            {
                this.outputBuffer[i] = (byte)this.blockBuffer[i];
            }

            this.stream.Write( this.outputBuffer, 0, this.outputBuffer.Length );
        }

        public override void WriteByte( byte value )
        {
            throw new InvalidOperationException();
        }

        public override void Flush()
        {
            this.stream.Flush();
        }

        public override int Read( byte[] buffer, int offset, int count )
        {
            throw new InvalidOperationException();
        }
    }
}
