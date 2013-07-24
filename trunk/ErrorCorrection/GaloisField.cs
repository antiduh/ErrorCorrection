// -----------------------------------------------------------------------
// <copyright file="GaloisField.cs" company="">
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
    public sealed class GaloisField
    {
        // Note: Most examples in the comments for this class are made in GF(2^4) with p(x) = x^4 + x + 1
        // The field looks like:
        //   0  1  2  3  4  5  6   7   8  9  10  11 12  13 14  15
        // { 0, 1, 2, 4, 8, 3, 6, 12, 11, 5, 10, 7, 14, 15 13, 9}
        //
        // Eg:
        //      0   = field[0] = 0
        //      a^0 = field[1] = 1; 
        //      a^1 = field[2] = 2;
        //      a^2 = feild[3] = 4;
        //      a^3 = field[4] = 8;
        //      ...
        //      a^7 = field[8] = 11.

        private uint size;

        private uint fieldGenPoly;

        private uint[] multTable;

        public GaloisField( uint size, uint fieldGenPoly )
        {
            this.size = size;
            this.fieldGenPoly = fieldGenPoly;


            BuildField();
            BuildLogarithms();
            BuildMultTable();
            BuildInverses();
            
        }

        public uint[] Field { get; private set; }

        public uint[] Inverses { get; private set; }

        public uint[] Logarithms { get; private set; }

        private void BuildField()
        {
            uint next;
            uint last;

            this.Field = new uint[ size ];

            this.Field[0] = 0;
            this.Field[1] = 1;
            last = 1;

            for( uint i = 2; i < this.Field.Length; i++ )
            {
                next = last << 1;

                if( next >= size )
                {
                    next = next ^ fieldGenPoly;
                }

                this.Field[i] = next;
                last = next;

            }
        }

        private void BuildLogarithms()
        {
            // Yes, we start by initializing the Inverses by *copying* from the regular field.
            // This is because we want to randomly index into the list while initializing it.
            this.Logarithms = new uint[this.Field.Length];

            // In GF(2^8) with p(x) = x^4 + x + 1, the field has elements 0, a^0, .., a^15:
            //   0  1  2  3  4  5  6   7   8  9  10  11 12  13 14  15
            // { 0, 1, 2, 4, 8, 3, 6, 12, 11, 5, 10, 7, 14, 15 13, 9}

            // In the above, we have the elements of the field by their index.
            // What about going from the element to its power of a, for multiplying?
            // In above, we have element 15 at index 13, but 15 is a^12 - it's inverse is one higher
            // than its power, because 0 is in there.
            
            // field[13] = 15;
            // logarithms[15] = 13 - 1;

            // logarithms[ field[13] ] = 13 - 1;
            // logarithms[ 15        ] = 13 - 1;
            // logarithms[ 15        ] = 12;

            // This means that zero will be stored with a logarithm of -1.
            // This is intentional, but we have to be careful to handle it specially when we actually
            // do multiplication.

            for( uint i = 0; i < this.Field.Length; i++ )
            {
                this.Logarithms[this.Field[i]] = i - 1;
            }
        }

        private void BuildMultTable()
        {
            this.multTable = new uint[this.size * this.size];

            for( uint left = 0; left < size; left++ )
            {
                for( uint right = 0; right < size; right++ )
                {
                    this.multTable[left + right * size] = InternalMult( left, right );
                }
            }
        }

        private void BuildInverses()
        {
            // Build the mulitiplicative inverses.

            // if a^9 = 10, then what is 1/a^9 aka a^(-9) ? 

            this.Inverses = new uint[this.Field.Length];
            this.Inverses[0] = 0;
            for( uint i = 1; i < this.Inverses.Length; i++ )
            {
                this.Inverses[this.Field[i]] = InternalDivide( 1, this.Field[i] );
            }
        }

        public uint Multiply( uint left, uint right )
        {
            // Using the multiplication table is a lot faster than the original computation.
            return this.multTable[left + right*size];
        }

        public uint Divide( uint dividend, uint divisor )
        {
            // Using the original computation is the same speed as the multiplication table.
            // I don't know why.
            return multTable[dividend + Inverses[divisor] * size];
        }

        private uint InternalMult( uint left, uint right )
        {
            // Conceptual notes:
            // If 
            //      a^9 = 10 and a^13 = 13 in GF(16) p(x) = x^4 + x + 1, 
            // Then 
            //      10 * 13 == 
            //      a^9 * a^13 == 
            //      a^((9+13) mod 15) == 
            //      a^(22 mod 15) == a^7 == 
            //      11
            //
            // Implementation notes:
            //  Our logarithms table stores a^9 at logarithms[9] = 10;
            //  Our field table stores a^9 at field[10] (0 is the first element, a^0 is the second).
            //  a^i is stored at field[i+1];
            //
            // Plan:
            // Convert each field element to its logarithm:
            //  left  = a^i --> i
            //  right = a^j --> j
            //
            // Sum the logarithms to perform the multiplication. 
            // Modulus the sum to convert from, eg, a^15 back to a^0
            //  k = (i + j) mod (size-1).
            //
            // Convert k back to a^k. Remember that a^k is stored at field[k+1];
            //  a^k = field[k+1];
            // 
            // Return a^k.

            // Handle the special case 0
            if( left == 0 || right == 0 ) { return 0; }

            // Convert each to their logarithm;
            left = Logarithms[left];
            right = Logarithms[right];

            // Sum the logarithms, using left to store the result.
            left = ( left + right ) % ( size - 1 );

            // Convert the logarithm back to the field value.
            return this.Field[left + 1];
        }

        private uint InternalDivide( uint dividend, uint divisor )
        {
            // Same general concept as Mult. Convert to logarithms and subtract.

            if( dividend == 0 ) { return 0; }

            dividend = Logarithms[dividend];

            divisor = Logarithms[divisor];

            // Note the extra '... + size - 1' term. This is to push the subtraction above
            // zero, so that the modulus operator will do the right thing.
            // (10 - 11) % 5 == -1  wrong
            // ((10 - 11) + 5) % 5 == 4 right
            dividend = ( dividend - divisor + ( size - 1 ) ) % ( size - 1 );

            return this.Field[dividend + 1];
        }

        /// <summary>
        /// Multiplies two polynomials of degrees X and Y to produce a single polynomial 
        /// of degree X + Y, where 'degree' means the exponent of the highest order monomial.
        /// Eg, x^2 + x + 1 is of degree 2, and has 3 monomials.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public uint[] PolyMult( uint[] left, uint[] right )
        {
            uint[] result;
            
            // Lets say we have the following two polynomials:
            //  3x^2 + 0x + 1   == int[]{1, 0, 3}
            //  1x^2 + 1x + 0   == int[]{0, 1, 1}
            // 
            // The naive result (ignoring galois fields) will be:
            //      (3x^4 + 3x^3 + 0) + (0x^3 + 0x^2 + 0) + (1x^2 + 1x + 0)  ==
            //      3x^4 + (3 + 0)x^3 + (0+1)x^2 + 1x + 0 ==
            //      3x^4 + 3x^3 + 1x^2 + 1x + 0  == {3, 3, 1, 1, 0}
            
            // The result is of degree X + Y == 2 + 2 = 4. The number of monomials, and thus the number of 
            // coefficients is degree + 1. Thus, for two polynomials with degree X and Y, the resultant 
            // polynomial has degree X + Y but has X + Y - 1 coefficents.
            // This establishes our basis for the degree of the resultant polynomial.

            // Now, in galois fields, coefficient multiplication and addition are different.
            // In GF(2^x), addition is simple XOR and multiplication is best represented as being logarithm based.
            // If 
            //      a^9 = 10 and a^13 = 13 in GF(16) p(x) = x^4 + x + 1, 
            // Then 
            //      10 * 13 == 
            //      a^9 * a^13 == 
            //      a^((9+13) mod 15) == 
            //      a^(22 mod 15) == a^7 == 
            //      11
            // 
            // Taking another example:
            //      5x + 1  == {1, 5}
            //      7x + 10 == {10, 7}
            //
            // (5x^1 + 2x^0)(7x^1 + 10x^0) ==
            //  [(5*7)x^2 + (5*10)x^1 ] + [(2*7)x^1 + (2*10)x^0]
            //
            // --> Perform multiplications:
            //     5*7 = a^8 * a^10 = a^18 = a^3 = 8
            //     5*10 = a^8 * a^9 = a^17 = a^2 = 4
            //     2*7 = a^1 * a^10 = a^11 = a^11 = 14
            //     2*10 = a^1 * a^9 = a^10 = a^10 = 7
            //  [8x^2 + 4x^1 ] + [14x^1 + 7x^0]
            //
            // --> Combine like terms
            //  8x^2 + (4+14)x^1 + 7x^0
            //
            // --> Perform sums
            //     4+14 = 4 XOR 14 = 10
            //  8x^2 + 10x^1 + 7x^0
            //
            // Done.
            

            uint coeff;
            result = new uint[left.Length + right.Length - 1];

            for( uint i = 0; i < left.Length; i++ )
            {
                for( uint j = 0; j < right.Length; j++ )
                {
                    coeff = InternalMult( left[i], right[j] );

                    result[i + j] = result[i + j] ^ coeff;
                }
            }

            return result;
        }

        public uint PolyEval( uint[] poly, uint x )
        {
            uint sum;
            uint xLog;
            uint coeffLog;
            uint power;

            // The constant term in the poly requires no multiplication.
            sum = poly[0];

            xLog = Logarithms[x];

            for( uint i = 1; i < poly.Length; i++ )
            {
                // The polynomial at this spot has some coefficent, call it a^j.
                // x itself has some value, call it a^k.
                // x is raised to some power, which is 'i' in the loop.
                // a^j * (a^k)^i 
                //    == a^j * a^(k*i)
                //    == a^(j+k+i) 
                // Remember that exponents are added modulo 'size - 1', because the field elements
                // are {0, a^0, a^1, ... } - we use size - 1 because we don't use 0.

                // If the coeff is 0, then this monomial contributes no value to the sum.
                if( poly[i] == 0 ) { continue; }

                coeffLog = Logarithms[poly[i]];

                // Add the powers together, then lookup which a^L you ended up with.
                power = ( coeffLog + xLog * i) % ( size - 1 );
                sum ^= Field[power+1];
            }

            return sum;
        }

        public static string PolyPrint( uint[] poly )
        {
            StringBuilder builder = new StringBuilder(poly.Length * 3);

            for( int i = poly.Length - 1; i >= 0; i-- )
            {
                //if( poly[i] > 0 )
                {
                    if( i > 1 )
                    {
                        builder.Append( poly[i] ).Append( "x^" ).Append( i ).Append( " + " );
                    }
                    else if( i == 1 )
                    {
                        builder.Append( poly[i] ).Append( "x" ).Append( " + " );
                    }
                    else
                    {
                        builder.Append( poly[i] );
                    }
                }
            }

            return builder.ToString();
        }

    }
}
