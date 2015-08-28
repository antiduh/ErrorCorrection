namespace ErrorCorrection
{
    using System.Text;

    /// <summary>
    /// Implements polynomial math for polynomials over a galois field with characteristic 2 (a binary
    /// galois field), with restrictions on the practical size of the field.
    /// </summary>
    /// <remarks>
    /// This paper has a Reed Solomon-relevant intro to GFs:
    /// http://downloads.bbc.co.uk/rd/pubs/whp/whp-pdf-files/WHP031.pdf
    ///
    /// A Galois field is an arithmetical field that is:
    ///   - Finite: Arithmetic operations are defined for only the elements defined to be in the field,
    ///             and there are a finite number of elements.
    ///   - Closed: Arithmetic operations on elements of the field return a value also in the field.
    ///
    /// A Galois field's size is a parameter of the field, but only certain sizes can exist. The
    /// field must be a size = p^k where p is prime and k is a positive integer. p is also called the
    /// characteristic of the field.
    ///
    /// A Galois field is composed of an additive group and a multiplicative group - what it means to
    /// perform addition or multiplication in the field depends on the definitions of these groups for
    /// the field in question - the choice of what meaning to use is up to the designer of the field.
    /// However, both groups must meet the requirements for operations in a Galois field - they must be
    /// finite and closed.
    ///
    /// One such possible definition for addition over GF(p) (where p is prime) could be `z = (x + y) mod p`.
    /// Another might be `z = a XOR b` where XOR represents the bitwise exclusive-or operation. Both of these
    /// definitions are sufficient for defining the meaning of addition in a Galois field.
    ///
    /// The same is true for defining multiplication - in GF(p), multiplication could be defined as
    /// `z = (x * y ) mod p` - such a definition is finite and closed.
    ///
    /// A Galois field need not be defined simply over a prime number of elements - it is possible
    /// to define a Galois field GF(p^n) (where p is prime and n is > 1), however, providing definitions for
    /// addition and multiplication prove a bit tricky - particularly, multiplication is the trouble maker.
    /// Our go-to definition for multiplication over such a field might be `z = (x * y) mod p^k`, however
    /// such a definition is not sufficient - the ring of integers modulo p^k doesn't meet the definition of
    /// a field.
    ///
    /// Instead, however, we can define the multiplicative group for our finite field in a different manner.
    /// What if we treated the elements of GF(p^n) as polynomials over the field GF(p)? Each element in GF(p^n)
    /// would be decomposed into a set of elements in GF(p), and those elements would be used as coefficients
    /// for a polynomial over GF(p).
    ///
    /// Consider a galois field that uses characteristic '2', such as GF(2^4). Such a field has 16 elements, and
    /// we could consider each element as a set of elements from GF(2) - we could consider them by their bits.
    ///
    /// For instance, the element '14' from GF(2^4) could be decomposed into binary '1110'. Now we could
    /// represent the element '14' from GF(2^4) as the polynomial '1x^3 + 1x^2 + 1x^1 + 0x^0' from GF(2).
    ///
    /// Let's keep that in our pocket for now.
    ///
    /// In Galois Fields, non-zero elements of the field form a multiplicative group that is also a cyclic
    /// group. For certain elements of the field, successive powers of these elements form a cyclic group
    /// that contains the entire set of non-zero elements in the field. These elements are called primitive
    /// elements, usually denoted as some value 'a'. Since successive powers of these elements forms
    /// a complete cyclic group containing all non-zero elements of the field - all elements are
    /// represented, and represented only once - we can represent the elements of the field indexed
    /// by the power of some primitive element a, since we can guarantee that there is a one-to-one
    /// mapping from some power a^i to some element in the field. The field can be represented in
    /// an alternate representation { 0, a^0, a^1, ..., a^(n-2) }, which is particularly useful when
    /// defining multiplication in some GF(p^n) as polynomials over GF(p).
    ///
    /// For example, consider GF(5) - this is a prime group, so we can choose to define arithmetic as
    /// simply being addition modulo p and multiplication modulo p.
    ///
    /// The elements of GF(5) are {0, 1, 2, 3, 4}. 2 + 4 = 6 mod 5 = 1. 2 * 4 = 8 mod 5 = 3.
    ///
    /// Lets test to see if the element 2 in GF(5) is also a primitive element:
    ///   - 2^1 =  2
    ///   - 2^2 =  4
    ///   - 2^3 =  8 mod 5 = 3
    ///   - 2^5 = 16 mod 5 = 1
    ///
    /// Each element was generated, and generated only once, thus the element 2 in GF(5) is primitive in GF(5).
    /// We can now write the elements of GF(5) indexed by a=2:
    ///  {0, a^0, a^1, a^2, a^3} =
    ///  {0    1    2,   4,   3}
    ///
    /// Consider the field GF(7) equipped with addition/multiplication modulo 7, and see if 2 is a primitive element:
    ///  - 2^0 = 1
    ///  - 2^1 = 2
    ///  - 2^2 = 4
    ///  - 2^3 = 8 mod 7 = 1    xxx
    ///
    /// 2 in GF(7) forms a cyclic group {2, 4, 1}, but isn't a complete cyclic group,
    /// and thus is not a primitive element in GF(7).
    ///
    /// --
    ///
    /// Recall that we don't currently have a definition for how to perform arithmetic on elements
    /// from some arbitrary field GF(p^k), since our go-to operator modulus doesn't fit the bill -
    /// the ring of integers modulo p^k doesn't meet the definitions for a field.
    ///
    /// However, we *can* use modulus to define arthimetic on Galois Fields of the form GF(p). Then, we
    /// can use polynomials over GF(p), which we are now equiped to compute, as the basis for defining
    /// arithmetic over some arbitrary GF(p^k).
    ///
    /// A polynomial over some arbitrary GF(x) has polynomial coefficients that are elements of GF(x).
    /// Consider the following equation of polynomials over the GF(5) that is using modulus to define
    /// arithmetic:
    ///    ( 3x^2 + x + 2 ) + ( 3x^2 + x + 3) = ( 6x^2 + 2x + 5 ) = ( x^2 + 2x + 0 )
    ///
    /// We can represent *elements* of GF(p^k) as *polynomials* over GF(p).
    /// For instance, element '7' in GF(2^4) would be the following polynomial over GF(2):
    ///    0x^3 + 1x^2 + 1x + 1.
    ///
    /// And element '10' would be the polynomial:
    ///    1x^3 + 0x^2 + 1x + 0
    ///
    /// We can then add those two polynomials together according to our definition of arithmetic in GF(2):
    ///    1x^3 + 1x^2 + 2x + 1  which reduces modulo 2 to
    ///    1x^3 + 1x^2 + 0x + 1
    ///
    /// Which, when converted back to an element in GF(2^4) would be 13.
    /// Thus, we've defined elemental addition in GF(2^4) to be polynomial addition in
    /// the system of GF(2) equipped with elemental addition modulu 2.
    ///
    /// We can similarly use GF(2) with modulu to define multiplication in GF(2^4), however, we
    /// run into one little snag - we end up with polynomials with higher-power terms than defined
    /// in GF(2^4):
    ///
    /// Multiply 7 * 6 in GF(2^4):
    ///   (x^2 + x + 1) * (x^2 + x) =
    ///   (x^4 + x^3) + (x^3 + x^2) + (x^2 + x) =
    ///   x^4 + 2x^3 + 2x^2 + x + 0 =
    ///   x^4 + 0x^3 + 0x^2 + x + 0 =
    ///   x^4 + x
    ///
    /// We're left with this x^4 term that has no representation in GF(2^4).
    ///
    /// However, if we can find a irreducible polynomial in GF(p) that has degree k, then we can
    /// perform polynomial division and use the remainder as the result, effectively, implmenting
    /// polynomial modulus. The irreducible polynomial must have a non-zero term of degree k (it must be monic)
    /// for it to have the reducing strength required to remove polynomial terms that are too high order.
    ///
    /// For GF(2^4), such a polynomial might be x^4 + x + 1
    ///
    /// Thus, we could take our earlier result x^4 + x and reduce it x^4 + x + 1 via polynomial division:
    ///               1
    ///             __________
    /// x^4 + x + 1 | x^4 + x + 0
    ///             - x^4 + x + 1
    ///                 0 + 0 + 1
    ///
    /// And thus, we're left with just 1. This was a polynomial in GF(2), which
    /// we can convert back to an element in GF(2^4), which is simply element 1.
    ///
    /// Thus, for the field GF(2^4), with reducing polynomial x^4 + x + 1,
    ///  7 * 6 = 1.
    ///
    /// We call this reducing polynomial the field generator polynomial p(x).
    ///
    /// --
    ///
    /// The primitive elements of the field GF(p^k) also form roots of all generator polynomials.
    /// For instance, the generator polynomial we chose above:
    ///   p(x) = x^4 + x + 1     -->
    ///   p(a) = a^4 + a + 1 = 0 --->
    ///    a^4 = a + 1
    ///
    /// We have defined addition and multiplication for non-prime field of the form GF(p^k)
    /// by using polynomal representation in GF(p) equipped with modular arithmetic.
    ///
    /// We can use this fact to generate the field of GF(p^k) in terms of the elements { 0, a^0, .., a^(n-2) }
    ///
    /// Suppose we pick 2 as our primitive element for GF(2^4), and use reducing polynomial x^4 + x + 1, and thus
    /// know that a^4 = a + 1
    ///
    ///
    /// a^0 =        = 1
    /// a^1 =        = 2
    /// a^2 =        = 4
    /// a^3 =        = 8
    /// a^4 = a + 1  = 3
    /// a^5 = a(a^4 + 1) = a(a+1) = a^2 + a = 6
    /// ...
    ///
    /// And thus, we can represent the entire field indexed by the power of our primitive element, and implement
    /// multiplication and division using logarithms.
    ///
    /// --
    ///
    /// This design uses the following optimizating restrictions:
    ///  - The field characteristic must be 2; that is the field must be of the form GF(2^m).
    ///  - The generator polynomial is specified in the form of a 32-bit 'int' variable,
    ///    limiting the length of the polynomial to 32 coefficients. This limit will never be
    ///    practically hit, due to the next restriction.
    ///  - Multiplication and division are implemented using a look-up table, for performance.
    ///    Large galois fields require n^2 memory to store the table. For GF(2^8), this is
    ///    256 * 256 == 65536 entries, times 4 bytes per entry = 256 kiB.
    ///    GF(2^16) would require 16 GiB of memory.
    /// </remarks>
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

        /// <summary>
        /// The elements of the field, ordered as generated by the field generator polynomial.
        /// </summary>
        public readonly int[] Field;

        /// <summary>
        /// Stores the multiplicative inverses of the elements of the field, eg
        /// the value of 1/a^9 is stored
        /// </summary>
        public readonly int[] Inverses;

        /// <summary>
        /// Stores the values of the field indexed by their multiplicative logarithm.
        /// </summary>
        public readonly int[] Logarithms;

        /// <summary>
        /// The primitive polynomial used to generate the elements of the field by taking successive powers of
        /// the polynomial.
        /// </summary>
        private int fieldGenPoly;

        /// <summary>
        /// Caches multiplication values for field elements.
        /// </summary>
        private int[,] multTable;

        /// <summary>
        /// Stores the total number of elements in the field. As a consequence of the structure of
        /// Galois Feilds, this value must be of the form p^k where p is a prime number and k is a
        /// positive integer.
        /// </summary>
        private int size;

        /// <summary>
        /// Constructs a new instance of the GaloisField class.
        /// </summary>
        /// <param name="size">The total number of elements in the field. Must be a power of two.</param>
        /// <param name="fieldGenPoly">A primitive element of the field, represented in polynomial form,
        /// that is used to generate the elements of the field in an ordered manner. The bits of the value
        /// indicate the coefficients of the polynomial, eg, 0b1011 indicate 1*x^3 + 0*x^2 + 1*x^1 + 1*x^0.</param>
        public GaloisField( int size, int fieldGenPoly )
        {
            this.size = size;
            this.fieldGenPoly = fieldGenPoly;

            this.Field = new int[size];
            this.Logarithms = new int[this.Field.Length];
            this.Inverses = new int[this.Field.Length];

            BuildField();
            BuildLogarithms();
            BuildMultTable();
            BuildInverses();
        }

        /// <summary>
        /// Pretty-prints a polynomial with the given coefficients.
        /// </summary>
        /// <param name="poly"></param>
        /// <returns></returns>
        public static string PolyPrint( int[] poly )
        {
            StringBuilder builder = new StringBuilder( poly.Length * 3 );

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

        /// <summary>
        /// Performs division, as defined by by this Galois field implementation, on the
        /// given operands.
        /// </summary>
        /// <param name="dividend">The value to act as the dividend.</param>
        /// <param name="divisor">The value to act as the divisor.</param>
        /// <returns></returns>
        public int Divide( int dividend, int divisor )
        {
            // Using the original computation is the same speed as the multiplication table.
            // I don't know why.
            return this.multTable[dividend, Inverses[divisor]];
        }

        /// <summary>
        /// Performs the multiplication operation, as defined by this Galois field implemention,
        /// on the given operands.
        /// </summary>
        /// <param name="left">The first value to multiply.</param>
        /// <param name="right">The second value to multiply.</param>
        /// <returns></returns>
        public int Multiply( int left, int right )
        {
            // Using the multiplication table is a lot faster than the original computation.
            return this.multTable[left, right];
        }

        /// <summary>
        /// Evaluates the polynomial for the given value of x
        /// </summary>
        /// <param name="poly">The polynomial to evaluate</param>
        /// <param name="x">The value with which to evaluate the polynomail.</param>
        /// <returns></returns>
        public int PolyEval( int[] poly, int x )
        {
            int sum;
            int xLog;
            int coeffLog;
            int power;

            // The constant term in the poly requires no multiplication.
            sum = poly[0];

            xLog = Logarithms[x];

            for( int i = 1; i < poly.Length; i++ )
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
                power = ( coeffLog + xLog * i ) % ( size - 1 );
                sum ^= Field[power + 1];
            }

            return sum;
        }

        /// <summary>
        /// Multiplies two polynomials of degrees X and Y to produce a single polynomial
        /// of degree X + Y, where 'degree' means the exponent of the highest order monomial.
        /// Eg, x^2 + x + 1 is of degree 2, and has 3 monomials.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public int[] PolyMult( int[] left, int[] right )
        {
            int[] result;

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

            // Now, in our galois fields, coefficient multiplication and addition are different.
            // In our GF(2^x), addition is simple XOR and multiplication is best represented as being logarithm based.
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

            int coeff;
            result = new int[left.Length + right.Length - 1];

            for( int i = 0; i < left.Length; i++ )
            {
                for( int j = 0; j < right.Length; j++ )
                {
                    coeff = InternalMult( left[i], right[j] );

                    result[i + j] = result[i + j] ^ coeff;
                }
            }

            return result;
        }

        private void BuildField()
        {
            int next;
            int last;

            this.Field[0] = 0;
            this.Field[1] = 1;
            last = 1;

            for( int i = 2; i < this.Field.Length; i++ )
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

        private void BuildInverses()
        {
            // Build the mulitiplicative inverses.

            // if a^9 = 10, then what is 1/a^9 aka a^(-9) ?

            this.Inverses[0] = 0;
            for( int i = 1; i < this.Inverses.Length; i++ )
            {
                this.Inverses[this.Field[i]] = InternalDivide( 1, this.Field[i] );
            }
        }

        private void BuildLogarithms()
        {
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

            for( int i = 0; i < this.Field.Length; i++ )
            {
                this.Logarithms[this.Field[i]] = i - 1;
            }
        }

        private void BuildMultTable()
        {
            this.multTable = new int[this.size, this.size];

            for( int left = 0; left < size; left++ )
            {
                for( int right = 0; right < size; right++ )
                {
                    this.multTable[left, right] = InternalMult( left, right );
                }
            }
        }

        private int InternalDivide( int dividend, int divisor )
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

        private int InternalMult( int left, int right )
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
    }
}