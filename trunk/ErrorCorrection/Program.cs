using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

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
            EncoderTest();

            DecoderTest();

            /*
            GaloisField field = new GaloisField( 16, 0x13 );
            int[] errorPoly = new int[] { 1, 14, 14 };

            // a^-14 = a^1 = 2
            // a^-13 = a^2 = 4
            int eval = field.PolyEval( errorPoly, 4 );
            
            Console.Out.WriteLine( eval );
             */

            Console.Out.Flush();
        }

        private static void DecoderTest()
        {
            AntiduhDecoder decoder = new AntiduhDecoder( 16, 11, 0x13 );
            //                                       V                      V
            // Note the following errors:    12, 12, 3, 3, 11, 10, 9, 8, 7, 6 , 5, 4, 3, 2, 1 
            //int[] errorMessage = new int[] { 12, 12, 1, 3, 11, 10, 9, 8, 7, 11, 5, 4, 3, 2, 1 };
            //int[] cleanMessage = new int[] { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
            int[] cleanMessage = new int[] { 14, 11, 10, 8, 12, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
            int[] errorMessage = new int[] { 14, 11, 10, 8, 12, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

            decoder.Decode( errorMessage );

            CheckArrayEquals( errorMessage, cleanMessage );

            Console.Out.WriteLine( errorMessage[0] );
        }

        private static void CheckArrayEquals( int[] left, int[] right )
        {
            if( left.Length != right.Length )
            {
                throw new Exception();
            }

            for( int i = 0; i < left.Length; i++ )
            {
                if( left[i] != right[i] )
                {
                    throw new Exception();
                }
            }
        }

        private static void EncoderTest()
        {

            // GF(2^4), with field generator poly p(x) = x^4 + x + 1 --> 10011 == 19 == 0x13
            // size = 16, n = 15, k = 11, 2t = 4
            AntiduhEncoder encoder = new AntiduhEncoder( 16, 11, 0x13 );
            int[] message = new int[] { 0, 0, 0, 0, 12, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

            // GF(2^8) with field generatory poly p(x) = x^8 + x^4 + x^3 + x^2 + 1 ---> 100011101 == 285 == 0x011D
            // n = 255, k = 239, 2t = 16
            //AntiduhEncoder encoder = new AntiduhEncoder( 256, 239, 0x011D );

            encoder.Encode( message );
            ArrayPrint( message );
            Console.Out.WriteLine( GaloisField.PolyPrint( message ) );

        }

        private static void ArrayPrint( int[] array )
        {
            for ( int i = 0; i < array.Length; i++ )
            {
                Console.Out.Write( array[i] + ", " );
            }
            Console.Out.WriteLine();
        }

        private static void MultTestCase()
        {
            int[] left = new int[] { 1, 2 };
            int[] right = new int[] { 3, 4 };
            int[] result;
    
            GaloisField field = new GaloisField( 16, 0x13 );
            //GaloisField field = new GaloisField( 256, 0x011D );

            result = field.PolyMult( left, right );

            Console.Out.WriteLine( field.Mult(3,7) ); 

        }

        public static void PrintList( List<int> list )
        {
            for( int i = 0; i < list.Count; i++ )
            {
                Console.Out.WriteLine( "{0}: {1}", i, list[i] );
            }
        }

    }
}
