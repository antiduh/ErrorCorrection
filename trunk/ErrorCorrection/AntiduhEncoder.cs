namespace ErrorCorrection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Implements a Reed-Solomon encoder over the finite field GF(2^N).
    /// </summary>
    /// <remarks>
    /// A block is defined as the set of data that is ultimately produced by the encoding process, such
    /// as would be transmitted or stored. A block is composed of a string of symbols. A symbol is a single 
    /// value that is an element from the underlying Galois field. A block is split into two parts - symbols
    /// containing original message data, and symbols containing parity data.
    /// 
    /// This implemention assumes a binary Galois field - that is, one of the form GF(2^N).
    /// Since the /characteristic/ of the field is 2, symbols are binary and thus are composed of N bits.
    /// A consequence of the design of finite fields is that the width of each symbol is necessarily
    /// linked to the length of the block.
    /// This implementation defines addition for elements in GF(2^n) as the bitwise XOR operation, 
    /// and multiplication by way of logarithms.
    /// 
    /// The size of the field is the number of elements in the Galois field. GF(2^4) can
    /// also be represented as GF(16), and thus has 16 elements. 
    /// 
    /// The block size produced by Reed-Solomon encoding when using a field GF(2^N) is 
    /// 2^n - 1; GF(2^4) == GF(16) has 15 symbols per block, and each symbol is 4 bits for a total
    /// of 60 bits per block.
    /// The choice of how many symbols are used to represent original data versus parity data 
    /// is a choice of the invoker. The choice of the number of bits per symbol depends on the exponent
    /// parameter of the field size. GF(2^N) has 2^N - 1 symbols per block and N bits per symbol. Thus
    /// a block has N * (2^N - 1) bits.
    ///
    /// This implementation is compatible with "code shortening" schemes where a block of N symbols
    /// is filled with some number of zeros, parity is computed, and only the original and parity 
    /// chunks are transmitted. This implementation, however, does not perform any of the work to 
    /// implement code shortening; it is the responsibility of the invoker to do so if desired.
    /// 
    /// BlockSize 
    ///     - the number of symbols that is ultimately produced by the encoding process.
    ///     
    /// MessageSize 
    ///     - the number of symbols in the block that store original data
    ///
    /// ParitySize
    ///     - The number of symbols in the block that store parity data.
    /// 
    /// FieldSize 
    ///     - The number of elements in the field.
    /// 
    /// The following relationships must hold:
    /// 
    ///   * BlockSize = FieldSize - 1
    ///   * BlockSize = MessageSize + ParitySize
    /// 
    /// This class is not multi-thread safe, because it caches buffers used for the encoding process. 
    /// This allows the encoder to perform the decoding process without allocating any memory beyond 
    /// initial construction.
    /// </remarks>
    public sealed class AntiduhEncoder
    {
        /// <summary>
        /// The number of elements in the field.
        /// </summary>
        private readonly int fieldSize;

        /// <summary>
        /// The number of symbols in each output block that store the original 
        /// message data.
        /// </summary>
        private readonly int messageSymbols;

        /// <summary>
        /// The number of symbols in each output block that store parity data.
        /// </summary>
        private readonly int paritySymbols;

        /// <summary>
        /// Stores the code generator polynomial.
        /// </summary>
        /// <remarks>
        /// The highest-degree monomial in the code generator polynomial is not used
        /// during the encoding process.
        /// </remarks>
        private readonly int[] codeGenPoly;

        /// <summary>
        /// Buffer used to store the accumulating result during the encoding/modulus 
        /// operation.
        /// </summary>
        private readonly int[] modulusResult;

        /// <summary>
        /// A reference to the underlying galois field implementation used to perform
        /// arithmetic on the Reed-Solomon blocks.
        /// </summary>
        private readonly GaloisField gf;

        /// <summary>
        /// Initializes a new instance of the encoder.
        /// </summary>
        /// <param name="fieldSize">The size of the Galois field to create. Must be a value that is 
        /// a power of two. The length of the output block is set to `fieldSize - 1`.</param>
        /// <param name="messageSymbols">The number of original message symbols per block.</param>
        /// <param name="paritySymbols">The number of parity symbols per block.</param>
        /// <param name="fieldGenPoly">A value representing the field generator polynomial, 
        /// which must be order N for a field GF(2^N).</param>
        /// <remarks>
        /// BlockSize is equal to `fieldSize - 1`. messageSymbols plus paritySymbols must equal BlockSize.
        /// </remarks>
        public AntiduhEncoder( int fieldSize, int messageSymbols, int paritySymbols, int fieldGenPoly )
        {
            if( fieldSize - 1 != messageSymbols + paritySymbols )
            {
                throw new ArgumentOutOfRangeException(
                    "Invalid reed-solomon block parameters were provided - " +
                    "the number of message symbols plus the number of parity symbols " +
                    "does not add up to the size of a block"
                );
            }

            this.fieldSize = fieldSize;
            this.messageSymbols = messageSymbols;
            this.paritySymbols = paritySymbols;
            this.BlockSize = fieldSize - 1;

            this.gf = new GaloisField( fieldSize, fieldGenPoly );

            this.codeGenPoly = BuildCodeGenPoly();

            this.modulusResult = new int[paritySymbols];
        }


        /// <summary>
        /// The number of symbols that make up an entire encoded message. An encoded message is composed of the
        /// original data symbols plus parity symbols.
        /// </summary>
        public int BlockSize { get; private set; }

        /// <summary>
        /// The number of symbols that make up the original message data.
        /// </summary>
        public int MessageSize
        {
            get
            {
                return this.messageSymbols;
            }
        }

        /// <summary>
        /// Initializes the code generator polynomial according to reed-solomon encoding.
        /// </summary>
        /// <remarks>
        /// The code generator polynomial is a polynomial over GF(2) of order N. 
        /// The parity symbols are calculated by treating the message symbols as another
        /// polynomial with coefficients from GF(2), and then performing polynomial division
        /// by the code generator polynomial. The remainder polynomial from the division 
        /// process comprises the parity symbols. In a block that contains zero errors,
        /// performing polynomial multiplication of the parity symbols with the message
        /// symbols returns exactly 0.
        /// 
        /// ...
        /// 
        /// Say that we were producing the code generator polynomial for the galois field over 
        /// 2^4 with p(x) = x^4 + x + 1, and for the reed-solomon system n = 15, k = 11, 2t = 4. 
        /// 
        /// In that system, the natual code generator polynomial would be 
        /// (x + a^0)(x + a^1)(x + a^2)(x + a^3), which would be
        /// x^4 + 15x^3 + 3x^2 + x + 12, aka {12, 1, 3, 15, 1}.
        /// 
        /// The encoding process ignores the highest-degree monomial in the code generation 
        /// polynomial. The extra term is still produced and stored, however it is not used.
        /// </remarks>
        private int[] BuildCodeGenPoly()
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


            int numElements = fieldSize - messageSymbols - 1;

            List<int[]> polys = new List<int[]>( (int)numElements );

            // Build the degree-1 polynomials (we need 2t = numElements of them).
            // Eg 2t = 4, need four of them:
            //   (x + 1) is {1, 1}
            //   (x + 2) is {2, 1}
            //   (x + 4) is {4, 1}
            //   (x + 8) is {8, 1}

            // Remember that field[0] is 0, field[1] is a^0.
            for( int i = 0; i < numElements; i++ )
            {
                polys.Add( new int[] { gf.Field[i + 1], 1 } );
            }

            // Multiply them one at a time to produce the field generator poly.
            int[] current = polys[0];
            for( int i = 1; i < numElements; i++ )
            {
                current = gf.PolyMult( current, polys[(int)i] );
            }

            return current;
        }

        /// <summary>
        /// Transforms the provided input buffer by computing and inserting the reed-solomon
        /// error check symbols into the array. The first `paritySymbols` elements of the array must be 
        /// empty, as these are where the check symbols will be inserted. The array must be MessageSize 
        /// elements long.
        /// </summary>
        /// <param name="message2"></param>
        /// <remarks>
        /// The design of this method requires that the message leave room for the parity symbols
        /// because the '0' values of the parity symbols are actually used during the encoding process.
        /// This is actually a result of modulus over finite (galois) fields - if those symbols are
        /// zero while performing modulus with divisor X, then remainder Y is returned.
        /// If those bytes are then filled with the value of the remainder Y, and the modulus is repeated,
        /// then the modulus returns those zero bytes. This is by definition and is part of the design of 
        /// RS error correction.
        /// </remarks>
        public void Encode( int[] message )
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

            int z_0;
            int r;
            int[] z = this.modulusResult;
            int[] g = this.codeGenPoly;
            
            // ------------ Init ----------

            // Clear the bytes that are supposed to be zero in the message.
            Array.Clear( message, 0, paritySymbols );

            // Step 0.1 -- Z = {0,0,0,0};
            Array.Clear( z, 0, z.Length );

            // Step 0.2 -- z_0 = 0.
            z_0 = 0;

            // Loop from, eg, x^14 to x^5
            // Note, we don't run the loop on the last message byte.
            // Thats because after we process the last message byte, we don't do the z fudge crap.
            // So we do that manually after the loop, instead of putting a conditional in the loop.
            for( int i = message.Length - 1; i > paritySymbols; i-- )
            {
                // Step 1 -- r = z_0 - x^i
                r = z_0 ^ message[i];

                // Step 2 -- Y = g'(x) * r
                // Step 3 -- Z = Z - Y
                for( int zIter = 0; zIter < z.Length; zIter++ )
                {
                    z[zIter] ^= gf.Multiply( g[zIter], r );
                }

                // Step 4 -- z_0 and Z fudge.
                // Extract z_0 from Z.
                z_0 = z[z.Length - 1];

                // Shift Z.
                for( int zIter = z.Length - 1; zIter >= 1; zIter-- )
                {
                    z[zIter] = z[zIter - 1];
                }

                z[0] = 0;
            }

            // Perform the first part of the algorithm for the last byte.
            // We don't mess with z after processing the last message byte, so we have to 
            // perform this logic outside of the loop, or put a conditional in the loop.
            r = z_0 ^ message[paritySymbols];

            // Step 2 -- Y = g'(x) * r
            // Step 3 -- Z = Z - Y
            for( int zIter = 0; zIter < z.Length; zIter++ )
            {
                z[zIter] ^= gf.Multiply( g[zIter], r );
            }


            // Write z to the zero-bytes in the message.
            for( int i = 0; i < z.Length; i++ )
            {
                message[i] = z[i];
            }
        }
    }
}
