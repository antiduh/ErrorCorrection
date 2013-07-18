// -----------------------------------------------------------------------
// <copyright file="AntiduhDecoder.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace ErrorCorrection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class AntiduhDecoder
    {
        private readonly GaloisField gf;
        private readonly int size;
        private readonly int fieldGenPoly;
        private readonly int numDataSymbols;
        private readonly int numCheckBytes;

        public AntiduhDecoder( int size, int numDataSymbols, int fieldGenPoly )
        {
            this.size = size;
            this.numDataSymbols = numDataSymbols;
            this.fieldGenPoly = fieldGenPoly;
            this.numCheckBytes = (size - 1) - numDataSymbols;

            this.gf = new GaloisField( size, fieldGenPoly );
        }

        public void Decode( int[] message )
        {
            int[] syndroms = new int[numCheckBytes];

            for( int i = 0; i < syndroms.Length; i++ )
            {
                syndroms[i] = CalcSyndrom( message, gf.Field[i + 1] );
            }
        }

        private int[] BerklekampErrorLocator(int[] syndroms)
        {
            // S  = S(x)     - syndrom polynomial
            // C  = C(x)     - correction polynomial
            // D  = 'lamba'(x) - error locator estimate polynomial.
            // D* = 'lambda-star'(x) - a new error locator estimate polynomial.
            // S_x = the element at index 'x' in S, eg, if S = {5,6,7,8}, then S_0 = 5, S_1 = 6, etc.
            // 
            //
            // Start with 
            //   K = 1;
            //   L = 0; 
            //   C = 0x^n + ... + 0x^2 + x + 0 aka {0,1,0, ...};
            //   D = 0x^n + ... + 0x^2 + 0x + 1 aka {1,0,0,...}

            // Step 1: Calculate e.
            // --------------
            //  e = S_(K-1) + sum(from i=1 to L: D_i * S_(K-1-i)
            //
            // Example
            //                           0   1   2         0  1  2   3
            // K = 4, L = 2; D = {1, 11, 15}; S = {15, 3, 4, 12}
            //         
            //  e = S_3 + sum(from i = 1 to 2: D_i * S_(3- i)
            //    = 12 + D_1 * S_2 + D_2 * S_1
            //    = 12 +  11 *  4  +  15 *  3
            //    = 12 +  10       +   2
            //    = 12 XOR 10 XOR 2 
            //    = 4
            
            // Step 2: Update estimate if e != 0
            //
            // If e != 0 { 
            //      D*(x) = D(x) + e * C(X)  -- Note that this just assigns D(x) to D*(x) if e is zero.
            //      If 2L < k {
            //          L = K - L
            //          C(x) = D(x) * e^(-1) -- Multiply D(x) times the multiplicative inverse of e.
            //      }
            // }

            // Step 3: Advance C(x):
            //   C(x) = C(x) * x  
            //     This just shifts the coeffs down; eg, x + 1 {1, 1, 0} turns into x^2 + x {0, 1, 1}
            //   
            //   D(x) = D*(x) (only if a new D*(x) was calulated)
            //  
            //   K = K + 1

            // Step 4: Compute end conditions
            //   If K <= 2T goto 1
            //   Else, D(x) is the error locator polynomial.

            int[] corr = new int[numCheckBytes - 1];
            int[] dPoly = new int[numCheckBytes - 1];
            int[] nextDPoly = new int[numCheckBytes - 1];

            int k;
            int l;
            int e;



            return dPoly;
        }

        // EG, if g(x) = (x+a^0)(x+a^1)(x+a^2)(x+a^3) 
        //             = (x+1)(x+2)(x+4)(x+8),
        // Then for the first syndrom S_0, we would provide root = a^0 = 1.
        // S_1 --> root = a^1 = 2 etc.
        private int CalcSyndrom( int[] message, int root )
        {
            int syndrom = 0;

            for( int i = message.Length - 1; i > 0; i-- )
            {
                syndrom = gf.TableMult( ( syndrom ^ message[i] ), root );
            }

            syndrom ^= message[0];

            return syndrom;
        }
    }
}
