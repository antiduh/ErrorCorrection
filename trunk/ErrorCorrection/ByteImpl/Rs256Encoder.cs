using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErrorCorrection.ByteImpl;

namespace ErrorCorrection
{
    /// <summary>
    /// Implements a reed-solomon encoder that operates on galois fields no larger than GF(2^8), eg, byte-oriented
    /// encoder.
    /// </summary>
    public class Rs256Encoder
    {
        private readonly GaloisField256 gf;

        private readonly int size;

        private readonly int decodedSize;

        private readonly int checkwords;

        private byte[] codeGenPoly;

        private byte[] modTempResult;

        /// <summary>
        /// Instantiates a new instance of the Rs256Encoder.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="decodedSize"></param>
        /// <param name="fieldGeneratorPoly"></param>
        public Rs256Encoder( int size, int decodedSize, int fieldGeneratorPoly )
        {
            this.size = size;
            this.decodedSize = decodedSize;
            this.checkwords = (size - 1) - decodedSize;

            this.gf = new GaloisField256( size, fieldGeneratorPoly );
            this.codeGenPoly = BuildCodeGenPoly();
            this.modTempResult = new byte[this.checkwords];
        }

        public int CheckWords { get { return this.checkwords; } }

        public int EncodedSize { get { return this.size - 1; } }

        public int DecodedSize { get { return this.decodedSize; } }

        public void Encode( byte[] message )
        {
            byte z_0;
            byte r;
            byte[] z = this.modTempResult;
            byte[] g = this.codeGenPoly;

            // ------------ Init ----------

            Array.Clear( message, 0, this.checkwords );

            Array.Clear( z, 0, z.Length );

            z_0 = 0;

            for ( int i = message.Length - 1; i > this.checkwords; i-- )
            {
                r = (byte)(z_0 ^ message[i]);

                for ( int zIter = 0; zIter < z.Length; zIter++ )
                {
                    z[zIter] ^= gf.Multiply( g[zIter], r );
                }

                z_0 = z[z.Length - 1];

                for ( int zIter = z.Length - 1; zIter >= 1; zIter-- )
                {
                    z[zIter] = z[zIter - 1];
                }

                z[0] = 0;
            }

            r = (byte)(z_0 ^ message[this.checkwords]);

            for ( int zIter = 0; zIter < z.Length; zIter++ )
            {
                z[zIter] ^= gf.Multiply( g[zIter], r );
            }

            for ( int i = 0; i < z.Length; i++ )
            {
                message[i] = z[i];
            }
        }

        private byte[] BuildCodeGenPoly()
        {
            int numElements = size - decodedSize - 1;

            List<byte[]> polys = new List<byte[]>( (int)numElements );

            // Build the degree-1 polynomials (we need 2t = numElements of them).
            // Eg 2t = 4, need four of them:
            //   (x + 1) is {1, 1}
            //   (x + 2) is {2, 1}
            //   (x + 4) is {4, 1}
            //   (x + 8) is {8, 1}

            // Remember that field[0] is 0, field[1] is a^0.
            for ( int i = 0; i < numElements; i++ )
            {
                polys.Add( new byte[] { gf.Field[i + 1], 1 } );
            }

            // Multiply them one at a time to produce the field generator poly.
            byte[] current = polys[0];
            for ( int i = 1; i < numElements; i++ )
            {
                current = gf.PolyMult( current, polys[(int)i] );
            }

            return current;
        }
    }
}
