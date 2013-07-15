using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ErrorCorrection
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            List<int> field;
            List<int> inverse;
            // GF(2^4), with field generator poly p(x) = x^4 + x + 1 --> 10011 == 19.
            field = MakeField( 16, 19 );
            inverse = MakeInverseField( field );

            // GF(2^8) with field generatory poly p(x) = x^8 + x^4 + x^3 + x^2 + 1 ---> 100011101 == 285
            //field = MakeField( 256, 285 );


            Console.Out.WriteLine( "Field: " );
            PrintList( field );

            Console.Out.WriteLine( "Inverse: " );
            PrintList( inverse );

            Console.Out.WriteLine( "Double inverse gets field: " );

            for( int i = 0; i < field.Count; i++ )
            {
                Console.Out.WriteLine( "{0}: {1}", i, field[inverse[i]] );
            }
            
            Console.Out.Flush();
        }

        public static void PrintList( List<int> list )
        {
            for( int i = 0; i < list.Count; i++ )
            {
                Console.Out.WriteLine( "{0}: {1}", i, list[i] );
            }
        }

        public static List<int> MakeInverseField( List<int> field )
        {
            List<int> inverse = new List<int>( field );

            // In GF(2^8) with p(x) = x^4 + x + 1, the field has elements 0, a^0, .., a^15:
            //   0  1  2  3  4  5  6   7   8  9  10  11 12  13 14  15
            // { 0, 1, 2, 4, 8, 3, 6, 12, 11, 5, 10, 7, 14, 15 13, 9}

            // In the above, we have the elements of the field by their index.
            // What about going from the element to its index, for multiplying?
            // In above, we have element 15 at index 13.    ^^
            // The field inverse gives us element 13 at index 15.
           
            // field[13] = 15;
            // inverse[15] = 13;

            // inverse[ field[13] ] = 13;
            // inverse[ 15        ] = 13;
            

            for( int i = 0; i < field.Count; i++ )
            {
                inverse[field[i]] = i;
            }

            return inverse;
        }


        public static List<int> MakeField( int size, int fieldGenPoly )
        {
            List<int> field;
            int next;
            int last;

            field = new List<int>( size );

            field.Add( 0 );
            field.Add( 1 );
            last = 1;

            for( int i = 0; i < size - 2; i++ )
            {
                next = last << 1;

                if( next >= size )
                {
                    next = next ^ fieldGenPoly;
                }

                field.Add( next );
                last = next;
            }

            return field;
        }

        public static void MultiplyTest()
        {
            int[] left = new[] { 1, 1 };   // (x + 1)
            int[] right = new[] { 1, 2 };  // (x + 2)
            int[] result = new[] { 1, 3, 2 }; // (x^2 + 3x + 2)

        }

    }
}
