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
            Stopwatch watch = new Stopwatch();
            Thread.Sleep( 250 );
            Thread.SpinWait( 1000 );


            // GF(2^4), with field generator poly p(x) = x^4 + x + 1 --> 10011 == 19 == 0x13
            // size = 16, n = 15, k = 11, 2t = 4
            AntiduhEncoder encoder = new AntiduhEncoder( 4, 11, 0x13 );
            int[] message = new int[] { 0, 0, 0, 0, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

            // GF(2^8) with field generatory poly p(x) = x^8 + x^4 + x^3 + x^2 + 1 ---> 100011101 == 285 == 0x011D
            // n = 255, k = 239, 2t = 16
            //AntiduhEncoder encoder = new AntiduhEncoder( 8, 239, 0x011D );


            //GaloisField field = new GaloisField( 16, 0x13 );


            watch.Start();

            for( int i = 0; i < 1; i++ )
            {
                encoder.Encode( message );
            }


            watch.Stop();

            Console.Out.WriteLine( GaloisField.PolyPrint( message ) );

            Console.Out.WriteLine( "Elapsed: " + watch.ElapsedMilliseconds );
            
            
            Console.Out.Flush();
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
