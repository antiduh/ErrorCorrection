// -----------------------------------------------------------------------
// <copyright file="AntiduhEncoder.cs" company="">
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
    /// Implements Reed-solomon encoding.
    /// </summary>
    /// <remarks>
    /// This class is not thread safe.
    /// </remarks>
    public sealed class AntiduhEncoder
    {
        private readonly uint size;

        private readonly uint numDataSymbols;

        private readonly uint checkBytes;

        /// <summary>
        /// Stores the code generator polynomial.
        /// </summary>
        /// <remarks>
        /// The highest-degree monomial in the code generator polynomial is not used
        /// during the encoding process.
        /// </remarks>
        private readonly uint[] codeGenPoly;

        /// <summary>
        /// Buffer used to store the accumulating result during the encoding/modulus 
        /// operation.
        /// </summary>
        private readonly uint[] modulusResult;

        private readonly GaloisField gf;

        public AntiduhEncoder( uint size, uint numDataSymbols, uint fieldGenPoly )
        {
            this.size = size;
            this.numDataSymbols = numDataSymbols;
            this.checkBytes = (size - 1) - numDataSymbols;

            // symbolWidth is the number of bits per symbol, eg, GF(2^symbolWidth);
            // Code word size n is n = 2^symbolWidth - 1

            // Say we choose symbolWidth = 4, numDataSymbols = 11.
            // That gives GF(2^4), n = 15, k = 11, 2t = 4.

            this.gf = new GaloisField( size, fieldGenPoly );

            codeGenPoly = BuildCodeGenPoly();

            this.modulusResult = new uint[checkBytes];
        }

        /// <summary>
        /// Initializes the code generator polynomial according to reed-solomon encoding.
        /// </summary>
        /// <remarks>
        /// As an example, say that we were producing the code generator polynomial for 
        /// the galois field over 2^4 with p(x) = x^4 + x + 1, and for the reed-solomon
        /// system n = 15, k = 11, 2t = 4. 
        /// 
        /// In that system, the natual code generator polynomial would be 
        /// (x + a^0)(x + a^1)(x + a^2)(x + a^3), which would be
        /// x^4 + 15x^3 + 3x^2 + x + 12, aka {12, 1, 3, 15, 1}.
        /// 
        /// The encoding process ignores the highest-degree monomial in the code generation 
        /// polynomial. The extra term is still produced and stored, however it is not used.
        /// </remarks>
        private uint[] BuildCodeGenPoly()
        {
            // Example:
            //   - GF(2^4) with p(x) = x^4 + x + 1.
            //   - n = 2^4 - 1 = 15
            //   - k is given to be 11 (user provides it, say).
            //   - 2t = n - k = 4
            // 
            //  The code gen poly g(x) is then g(x) = (x+a^0)(x+a^1)(x+a^2)(x+a^3)

            // For RS with n=15 and k=11 on GF(2^4), the code generator needs 2t = n-k = 4 elements
            // By definition:
            //      n = (2^symbolWidth) - 1; 
            //      k = numDataSymbols;
            //
            // 2t = n - k == (2 ^ symbolWidth) - numDataSymbols - 1.
            //
            // The code generator will have degree 2t = n - k, that is n - k + 1 monomials.
            //
            // Example: GF(2^4) on p(x) = x^4 + x + 1; n = 15, k = 11, 2t = 4.
            // a^0 = 1; a^1 = 2; a^2 = 4; a^3 = 8;
            //
            // Then g(x) = (x + a^0)(x + a^1)(x + a^2)(x + a^3)
            //           = (x + 1)(x + 2)(x + 4)(x + 8)
            //
            // When multiplied/combined (according to multiplication/addition in GF(2^4) over p(x) = x^4 + x + 1)
            // Then g(x) = x^4 + 15x^3 + 3x^2 + x + 12
            // 
            // This is a polynomial of degree 4, but has 5 monomials.


            uint numElements = size - numDataSymbols - 1;

            List<uint[]> polys = new List<uint[]>( (int)numElements );

            // Build the degree-1 polynomials (we need 2t = numElements of them).
            // Eg 2t = 4, need four of them:
            //   (x + 1) is {1, 1}
            //   (x + 2) is {2, 1}
            //   (x + 4) is {4, 1}
            //   (x + 8) is {8, 1}

            // Remember that field[0] is 0, field[1] is a^0.
            for( uint i = 0; i < numElements; i++ )
            {
                polys.Add( new uint[] { gf.Field[i + 1], 1 } );
            }

            // Multiply them one at a time to produce the field generator poly.
            uint[] current = polys[0];
            for( uint i = 1; i < numElements; i++ )
            {
                current = gf.PolyMult( current, polys[(int)i] );
            }

            return current;
        }

        /// <summary>
        /// Transforms the provided input buffer by computing and inserting the reed-solomon
        /// error check symbols into the array. The first 2t bytes of the array must be empty,
        /// as these are where the check symbols will be inserted. The array must be n bytes long.
        /// </summary>
        /// <param name="message2"></param>
        /// <remarks>
        /// The design of this method requires that the message leave room for the check bytes
        /// because the '0' values of the check bytes are actually used during the encoding process.
        /// This is actually a result of modulus over finite (galois) fields - if those bytes are
        /// zero while performing modulus with divisor X, then remainder Y is returned.
        /// If those bytes are then filled with the value of the remainder Y, and the modulus is repeated,
        /// then the modulus returns those zero bytes. This is by definition and is part of the design of 
        /// RS error correction.
        /// </remarks>
        public void Encode( uint[] message )
        {
            // Lets assume:
            //   GF(2^4)
            //   p = 2
            //   m = 4
            //   p(x) = x^4 + x + 1  = {1, 1, 4}
            //
            // Dividend is:
            //  {0, 0, 0, 0, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1}
            // that is, x^0 = 0 and x^14 = 1.
            // 
            // Divisor is g(x):
            //   g(x) = x^4 + 15x^3 + 3x^2 + x + 12 = {12, 1, 3, 15, 1}

            // Our accumulator needs to have degree one less than our divisor, and will be our 
            // result.
            //
            //  - We define our accumulator: holds the current value of the modulus as we do each
            //    term one at a time.
            //  - A single int to hold the highest order coefficent that gets shifted off of 
            //    the accumulator during each iteration of the algorithm.
            //  - A modified g'(x) that is missing the highest order term.
            //  - A variable to store the intermediate result of multiplying g'(x) by the 
            //    highest-order coefficent

            // Process:
            //
            // 0.0) Set i = degree of dividend, ie, 14 in our example. i is standard arithmetic, not part of GF.
            // 0.1) Set Z = {0, 0, 0, 0}. This is our accumulator.
            // 0.2) Set z_0 = 0  -- z_0 is the variable to store the highest-level coeff that gets 
            //      shifted off Z.
            //      EG, if Z = {12, 1, 3, 15}, then after shifting,
            //      z_0 = 15 and Z = { 0, 12, 1, 3}
            // 0.3) Ignore the existance of the highest order monomial in g(x). 
            //      This changes the loop constraints used in the actual implementation.
            // 
            // 1) r = z_0 - x^i
            // 2) Y = g'(x) * r
            // 3) Z = Z - Y
            // 4) Store z_0 and shift Z
            // 5) i = i - 1.
            // 6) repeat for i = degree .. m, eg, i = 14 .. 4 (inclusive).

            // Example:
            // Dividend is:
            //  x^0 x^1 x^2 x^3 x^4 x^5 x^6 x^7 x^8 x^9 x^10 x^11 x^12 x^13 x^14
            // {0,  0,  0,  0,  11, 10, 9,  8,  7,  6,  5,   4,   3,   2,   1}
            //
            // 0.0) i   = 14
            // 0.1) Z   = {0, 0, 0, 0}
            // 0.2) z_0 = 0.
            // 0.3) g(x) = {12, 1, 3, 15, 1}, thus, g'(x) would be = {12, 1, 3, 15}.
            //      We just ignore the last value.
            //
            // 1) r = z_0 - x^14        = 0 - 1 = 1
            // 2) Y = g'(x) * r         = {12, 1, 3, 15} * 1 = {12, 1, 3, 15}
            // 3) Z = Z - Y             = {0,0,0,0} - {12, 1, 3, 15} = {12, 1, 3, 15}
            // 4) z_0 and Z fudge   z_0 = 15
            //                        Z = {0, 12, 1, 3}
            // 5) i = i - 1             = 14 - 1 = 13
            //
            // 6) r = z_0 - x^13        = 15 - 2 = 13
            // 7) Y = g'(x) * r         = {12, 1, 3, 15} * 13 = {3, 13, 4, 7}
            // 8) Z = Z - Y             = {0, 12, 1, 3} - {3, 13, 4, 7} = {3, 1, 5, 4}
            // 9) z_0 and Z fudge   z_0 = 4
            //                        Z = {0, 3, 1, 5}
            //10) i = i - 1 =           = 13 - 1 = 12

            // Actual application:
            // Since this algorithm is used to perform the encoding procedure,
            // 'dividend' is the message, and divisor is the code generator polynomial.
            // Z and Y are small temporary buffers used during this process, and so we allocate 
            // them when the class is constructed so that they can be re-used.

            uint z_0;
            uint r;
            uint[] z = this.modulusResult;
            uint[] g = this.codeGenPoly;
            
            // ------------ Init ----------

            // Clear the bytes that are supposed to be zero in the message.
            Array.Clear( message, 0, (int)checkBytes );

            // Step 0.1 -- Z = {0,0,0,0};
            Array.Clear( z, 0, z.Length );

            // Step 0.2 -- z_0 = 0.
            z_0 = 0;

            // Loop from, eg, x^14 to x^5
            // Note, we don't run the loop on the last message byte.
            // Thats because after we process the last message byte, we don't do the z fudge crap.
            // So we do that manually after the loop, instead of putting a conditional in the loop.
            for( uint i = (uint)(message.Length - 1); i > checkBytes; i-- )
            {
                // Step 1 -- r = z_0 - x^i
                r = z_0 ^ message[i];

                // Step 2 -- Y = g'(x) * r
                // Step 3 -- Z = Z - Y
                for( uint zIter = 0; zIter < z.Length; zIter++ )
                {
                    z[zIter] ^= gf.Multiply( g[zIter], r );
                }

                // Step 4 -- z_0 and Z fudge.
                // Extract z_0 from Z.
                z_0 = z[z.Length - 1];

                // Shift Z.
                for( uint zIter = (uint)(z.Length - 1); zIter >= 1; zIter-- )
                {
                    z[zIter] = z[zIter - 1];
                }

                z[0] = 0;
            }

            // Perform the first part of the algorithm for the last byte.
            // We don't mess with z after processing the last message byte, so we have to 
            // perform this logic outside of the loop, or put a conditional in the loop.
            r = z_0 ^ message[checkBytes];

            // Step 2 -- Y = g'(x) * r
            // Step 3 -- Z = Z - Y
            for( uint zIter = 0; zIter < z.Length; zIter++ )
            {
                z[zIter] ^= gf.Multiply( g[zIter], r );
            }


            // Write z to the zero-bytes in the message.
            for( uint i = 0; i < z.Length; i++ )
            {
                message[i] = z[i];
            }
        }
    }
}
