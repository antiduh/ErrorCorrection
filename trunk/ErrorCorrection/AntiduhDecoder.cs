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
            int[] errorLocator;
            int[] omega;
            int[] errorIndexes;

            for( int i = 0; i < syndroms.Length; i++ )
            {
                syndroms[i] = CalcSyndrom( message, gf.Field[i + 1] );
            }

            errorLocator = BerklekampErrorLocator( syndroms );
            omega = CalcOmega( syndroms, errorLocator );

            errorIndexes = ChienSearch( errorLocator );

            Console.Out.Write( omega[0] );
        }

        private int[] BerklekampErrorLocator(int[] syndroms)
        {
            // Explanation of terms:
            // S  = S(x)     - syndrom polynomial
            // C  = C(x)     - correction polynomial
            // D  = 'lamba'(x) - error locator estimate polynomial.
            // D* = 'lambda-star'(x) - a new error locator estimate polynomial.
            // S_x = the element at index 'x' in S, eg, if S = {5,6,7,8}, then S_0 = 5, S_1 = 6, etc.
            // 2T  = the number of error correction symbols, eg, numCheckBytes.
            //       T must be >= 1, so 2T is guarenteed to be at least 2. 
            //
            // Start with 
            //   K = 1;
            //   L = 0; 
            //   C = 0x^n + ... + 0x^2 + x + 0 aka {0,1,0, ...};
            //   D = 0x^n + ... + 0x^2 + 0x + 1 aka {1,0,0,...};
            //     Both C and D are guarenteed to be at least 2 elements, which is why they can have
            //     hardcoded initial values.

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

            // the C(x) polynomial.
            int[] corr = new int[numCheckBytes - 1];
            
            // The lamda(x) aka D(x) polynomial
            int[] dPoly = new int[numCheckBytes - 1];

            // The lambda-star(x) aka D*(x) polynomial.
            int[] dStarPoly = new int[numCheckBytes - 1];

            int k;
            int l;
            int e;
            int eInv; // temp to store calculation of 1 / e aka e^(-1)

            // --- Initial conditions ----
            k = 1;
            l = 0;
            corr[1] = 1;
            dPoly[0] = 1;


            while( k <= numCheckBytes )
            {            
                // --- Calculate e ---
                e = syndroms[k - 1];

                for( int i = 1; i <= l; i++ )
                {
                    e ^= gf.Mult( dPoly[i], syndroms[k - 1 - i]);
                }

                // --- Update estimate if e != 0 ---
                if( e != 0 )
                {
                    // D*(x) = D(x) + e * C(x);
                    for( int i = 0; i < dStarPoly.Length; i++ )
                    {
                        dStarPoly[i] = dPoly[i] ^ gf.TableMult( e, corr[i] );
                    }

                    if( 2 * l < k )
                    {
                        // L = K - L;
                        l = k - l;

                        // C(x) = D(x) * e^(-1);
                        eInv = gf.Divide( 1, e );
                        for( int i = 0; i < corr.Length; i++ )
                        {
                            corr[i] = gf.TableMult( dPoly[i], eInv );
                        }
                    }
                }

                // --- Advance C(x) ---

                // C(x) = C(x) * x
                for( int i = corr.Length - 1; i >= 1; i-- )
                {
                    corr[i] = corr[i - 1];
                }
                corr[0] = 0;

                if( e != 0 )
                {
                    // D(x) = D*(x);
                    Array.Copy( dStarPoly, dPoly, dPoly.Length );
                }

                k += 1;

            }

            return dPoly;
        }

        private int[] CalcOmega(int[] syndroms, int[] lambda)
        {
            // O(x) is shorthand for Omega(x).
            // L(x) is shorthand for Lambda(x).
            // 
            // O_i is the coefficient of the term in omega with degree i. Ditto for L_i.
            // Eg, O(x) = 6x + 15;  O_0 = 15, O_1 = 6
            // 
            // From the paper:
            // O_0 = S_b
            //   ---> b in our implementation is 0.
            // O_1 = S_{b+1} + S_b * L_1
            //
            // O_{v-1} = S_{b+v-1} + S_{b+v-2} * L_1 + ... + S_b * L_{v-1}.
            // O_i = S_{b+i} + S_{b+ i-1} * L_1  + ... + S_{b+0} * L_i
 
            // Lets say :
            //   L(x) = 14x^2 + 14x + 1         aka {1, 14, 14}.
            //   S(x) = 12x^3 + 4x^2 + 3x + 15  aka {15, 3, 4, 12}
            //   b = 0;
            //   v = 2 because the power of the highest monomial in L(x), 14x^2, is 2.
            // 
            // O_0 = S_{b} = S_0 = 15
            // O_1 = S_{b+1} + S_b * L_1 = S_1 + S_0 * L_1 = 3 + 15 * 14 = 6.
            // 
            // O(x) = 6x + 15.

            // Lets make up another example (these are completely made up so they may not work):
            //   L(x) = 10x^3 + 9x^2 + 8x + 7       aka { 7, 8, 9, 10}
            //   S(x) = 2^4 + 3x^3 + 4x^2 + 5x + 6  aka { 6, 5, 4, 3, 2}
            //   b = 0 (design parameter)
            //   v = 3

            // O_i for i = 0 .. v - 1 = 2. Thus, O has form ax^2 + bx^1 + cx^0
            // Compute O_0, O_1, O_2

            // O_0 = S_{b+0}
            //     = S_0
            //
            // O_1 = S_{b+1} + S_{b+0} * L_1
            //     = S_1 + S_0 * L_1
            //
            // O_2 = S_{b+2} + S_{b+1} * L_1 + S_{b+0} * L_2
            //     + S_2 + S_1 * L_1 + S_0 * L_2



            int[] omega = new int[lambda.Length - 1];

            for ( int i = 0; i < omega.Length; i++ )
            {
                omega[i] = syndroms[i];

                for ( int lIter = 1; lIter <= i; lIter++ )
                {
                    omega[i] ^= gf.TableMult( syndroms[i - lIter], lambda[lIter] );
                }
            }

            return omega;
        }

        private int[] ChienSearch( int[] lambda )
        {
            int[] eLocs = new int[size - 1];

            // The chien search evaluates the lamba polynomial for the multiplicate inverse 
            // each element in the field other than 0.
            // Eg,
            // eLocs[i] = gf.EvalPoly(lambda, gf.Divide( 1, gf.Field[i] );
            //
            // This method does a significant amount of converting back-and-fourth between
            // representation of terms, and so instead, we evalute one term in the polynomial,
            // add it to the right spot in errorPositions, evaluate the next term, etc.
            //
            // eg, say that lamba was D(x) = 14x^2 + 14x + 1.
            // 

            for( int i = 0; i < eLocs.Length; i++ )
            {
                eLocs[i] = gf.PolyEval(
                    lambda,
                    gf.Divide( 1, gf.Field[i+1] )
                );
            }

            /*
            // Evaluate the constant term in the polynomial.
            for( int i = 0; i < eLocs.Length; i++ )
            {
                eLocs[i] = lambda[0];
            }

            // Evaluate each non-constant term.
            int lambdaLog;

            for( int lambdaIndex = 1; lambdaIndex < lambda.Length; lambdaIndex++ )
            {
                // Evaluate the nth monomial in lambda.
                // First, look up with logarithm the current lambda corresponds to.
                // If lambda[i] = 14, then 14 is a^11. so lambdaLog is 11.
                lambdaLog = gf.Logarithms[lambda[lambdaIndex]];
            }
            */


            return eLocs;
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
