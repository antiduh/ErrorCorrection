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
    /// TODO: Update summary.
    /// </summary>
    public class AntiduhEncoder
    {
        private int symbolWidth;

        private int numDataSymbols;

        private int[] codeGenPoly;

        private GaloisField gf;

        public AntiduhEncoder( int symbolWidth, int numDataSymbols, int fieldGenPoly )
        {
            this.symbolWidth = symbolWidth;
            this.numDataSymbols = numDataSymbols;

            // symbolWidth is the number of bits per symbol, eg, GF(2^symbolWidth);
            // Code word size n is n = 2^symbolWidth - 1

            // Say we choose symbolWidth = 4, numDataSymbols = 11.
            // That gives GF(2^4), n = 15, k = 11, 2t = 4.

            this.gf = new GaloisField( 1 << symbolWidth, fieldGenPoly );

            BuildCodeGenPoly();

            Console.Out.WriteLine( GaloisField.PolyPrint( this.codeGenPoly ) );
        }

        private void BuildCodeGenPoly()
        {
            // Example:
            //   - GF(2^4) with p(x) = x^4 + x + 1.
            //   - n = 2^4 - 1 = 15
            //   - k is given to be 11 (user provides it, say).
            //   - 2t = n - k = 4
            // 
            //  The code gen poly g(x) is then g(x) = (x+a^0)(x+a^1)(x+a^2)(x+a^3)
            //  Multiply that out, reduce mod 

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


            int numElements = (1 << symbolWidth) - numDataSymbols - 1;

            List<int[]> polys = new List<int[]>( numElements );

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
                current = gf.PolyMult( current, polys[i] );
            }

            this.codeGenPoly = current;
        }
    }
}
