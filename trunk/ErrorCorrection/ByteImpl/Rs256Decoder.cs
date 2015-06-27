using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErrorCorrection.ByteImpl;

namespace ErrorCorrection
{
    /// <summary>
    /// Implements a Reed-solomon decoder that operates on galois fields with size at most GF(2^8), eg, a byte-oriented
    /// decoder.
    /// </summary>
    public class Rs256Decoder
    {
        private readonly GaloisField256 gf;
        private readonly int size;
        private readonly int fieldGenPoly;
        private readonly int numDataSymbols;
        private readonly int numCheckBytes;

        private readonly byte[] syndroms;

        private readonly byte[] lambda;
        private readonly byte[] corrPoly;
        private readonly byte[] lambdaStar;

        private readonly byte[] lambdaPrime;

        private readonly byte[] omega;

        private readonly byte[] errorIndexes;

        private readonly byte[] chienCache;

        public Rs256Decoder( int size, int numDataSymbols, int fieldGenPoly )
        {
            this.size = size;
            this.numDataSymbols = numDataSymbols;
            this.fieldGenPoly = fieldGenPoly;
            this.numCheckBytes = (size - 1) - numDataSymbols;

            this.CodeWordSize = size - 1;

            this.gf = new GaloisField256( size, fieldGenPoly );

            // Syndrom calculation buffers
            this.syndroms = new byte[numCheckBytes];

            // Lamda calculation buffers
            this.lambda = new byte[numCheckBytes - 1];
            this.corrPoly = new byte[numCheckBytes - 1];
            this.lambdaStar = new byte[numCheckBytes - 1];

            // LambdaPrime calculation buffers
            this.lambdaPrime = new byte[numCheckBytes - 2];

            // Omega calculation buffers
            this.omega = new byte[numCheckBytes - 2];
            
            // Error position calculation
            this.errorIndexes = new byte[size - 1];

            // Cache of the lookup used in the ChienSearch process.
            this.chienCache = new byte[size - 1];

            for( int i = 0; i < this.chienCache.Length; i++ )
            {
                this.chienCache[i] = gf.Inverses[gf.Field[i + 1]];
            }
        }

        /// <summary>
        /// The number of symbols that make up an entire received codeword, which includes parity symbols
        /// and original message symbols.
        /// </summary>
        public int CodeWordSize { get; private set; }

        /// <summary>
        /// How many symbols per code word are used for storing original message symbols.
        /// </summary>
        public int PlainTextSize
        {
            get { return this.numDataSymbols; }
        }

        public void Decode( byte[] message )
        {
            CalcSyndromPoly( message );
            CalcLambda();
            CalcLambdaPrime();
            CalcOmega();

            ChienSearch();

            RepairErrors( message, errorIndexes, omega, lambdaPrime );
        }

        private void RepairErrors( byte[] message, byte[] errorIndexes, byte[] omega, byte[] lp )
        {
            byte top;
            byte bottom;
            byte x;
            byte xInverse;
            int messageLen = message.Length;

            for( int i = 0; i < messageLen; i++ )
            {
                if( errorIndexes[i] == 0 )
                {
                    x = gf.Field[i + 1];

                    xInverse = gf.Inverses[x];

                    top = gf.PolyEval( omega, xInverse );
                    top = gf.Multiply( top, x );
                    bottom = gf.PolyEval( lp, xInverse );
                    
                    message[i] ^= gf.Divide( top, bottom );
                }
            }
        }

        private void CalcLambda()
        {
            int k;
            int l;
            byte e;
            byte eInv; // temp to store calculation of 1 / e aka e^(-1)

            // --- Initial conditions ----
            // Need to clear lambda and corrPoly, but not lambdaStar. lambda and corrPoly 
            // are used and initialized iteratively in the algorithm, whereas lambdaStar isn't.
            Array.Clear( corrPoly, 0, corrPoly.Length );
            Array.Clear( lambda, 0, lambda.Length );
            k = 1;
            l = 0;
            corrPoly[1] = 1;
            lambda[0] = 1;


            while( k <= numCheckBytes )
            {            
                // --- Calculate e ---
                e = syndroms[k - 1];

                for( int i = 1; i <= l; i++ )
                {
                    e ^= gf.Multiply( lambda[i], syndroms[k - 1 - i] );
                }

                // --- Update estimate if e != 0 ---
                if( e != 0 )
                {
                    // D*(x) = D(x) + e * C(x);
                    for( int i = 0; i < lambdaStar.Length; i++ )
                    {
                        lambdaStar[i] = (byte)(lambda[i] ^ gf.Multiply( e, corrPoly[i] ));
                    }

                    if( 2 * l < k )
                    {
                        // L = K - L;
                        l = k - l;

                        // C(x) = D(x) * e^(-1);
                        eInv = gf.Inverses[e];
                        for( int i = 0; i < corrPoly.Length; i++ )
                        {
                            corrPoly[i] = gf.Multiply( lambda[i], eInv );
                        }
                    }
                }

                // --- Advance C(x) ---

                // C(x) = C(x) * x
                for( int i = corrPoly.Length - 1; i >= 1; i-- )
                {
                    corrPoly[i] = corrPoly[i - 1];
                }
                corrPoly[0] = 0;

                if( e != 0 )
                {
                    // D(x) = D*(x);
                    Array.Copy( lambdaStar, lambda, lambda.Length );
                }

                k += 1;

            }
        }

        private void CalcLambdaPrime()
        {
            // Forney's says that we can just set even powers to 0 and then take the rest and 
            // divide it by x (shift it down one). 
            
            // No need to clear this.lambdaPrime between calls; full assignment is done every call.

            for( int i = 0; i < lambdaPrime.Length; i++ )
            {
                if( ( i & 0x1 ) == 0 )
                {
                    lambdaPrime[i] = lambda[i + 1];
                }
                else
                {
                    lambdaPrime[i] = 0;
                }
            }
        }

        private void CalcOmega()
        {
            for ( int i = 0; i < omega.Length; i++ )
            {
                omega[i] = syndroms[i];

                for ( int lIter = 1; lIter <= i; lIter++ )
                {
                    omega[i] ^= gf.Multiply( syndroms[i - lIter], lambda[lIter] );
                }
            }
        }

        private void ChienSearch( )
        {
            for( int i = 0; i < errorIndexes.Length; i++ )
            {
                errorIndexes[i] = gf.PolyEval(
                    lambda,
                    chienCache[i]
                );
            }
        }


        private void CalcSyndromPoly( byte[] message )
        {
            byte syndrome;
            byte root;

            // Don't need to zero this.syndromes first - it's not used before its assigned to.

            for( int synIndex = 0; synIndex < syndroms.Length; synIndex++ )
            {
                // EG, if g(x) = (x+a^0)(x+a^1)(x+a^2)(x+a^3) 
                //             = (x+1)(x+2)(x+4)(x+8),
                // Then for the first syndrom S_0, we would provide root = a^0 = 1.
                // S_1 --> root = a^1 = 2 etc.

                root = gf.Field[synIndex + 1];
                syndrome = 0;

                for( int i = message.Length - 1; i > 0; i-- )
                {
                    syndrome = gf.Multiply( (byte)( syndrome ^ message[i] ), root );
                }

                syndroms[synIndex] = (byte)(syndrome ^ message[0]);
            }
        }

    }
}
