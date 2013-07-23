using System;
using System.Collections.Generic;
using System.Linq;
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
            PerformanceTest();
            Console.Out.Flush();
        }

        private static void PerformanceTest()
        {
            Stopwatch watch = new Stopwatch();
            ReedSolomonTest test = new ReedSolomonTest( 16, 11, 0x13, watch );
            //ReedSolomonTest test = new ReedSolomonTest( 256, 251, 0x011D, watch );

            uint hitsPerIter = 20;
            uint iters = 50*1000;

            for( uint i = 0; i < iters; i++ )
            {
                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();

                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();

                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();

                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();
            }

            double timePerCall = watch.Elapsed.TotalMilliseconds;
            timePerCall = timePerCall / ( hitsPerIter * iters );
            timePerCall *= 1000.0; // convert to micro-secs.

            Console.Out.WriteLine(
                "Running time: {0} ms, {1} us/iter",
                watch.ElapsedMilliseconds,
                timePerCall );

        }

        private static void DecoderTest()
        {
            AntiduhDecoder decoder = new AntiduhDecoder( 16, 11, 0x13 );
            //                                       V                      V
            // Note the following errors:    12, 12, 3, 3, 11, 10, 9, 8, 7, 6 , 5, 4, 3, 2, 1 
            uint[] errorMessage = new uint[] { 12, 12, 1, 3, 11, 10, 9, 8, 7, 11, 5, 4, 3, 2, 1 };
            uint[] cleanMessage = new uint[] { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
            //int[] cleanMessage = new int[] { 14, 11, 10, 8, 12, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
            //int[] errorMessage = new int[] { 14, 11, 10, 8, 12, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

            decoder.Decode( errorMessage );

            CheckArrayEquals( errorMessage, cleanMessage );

            Console.Out.WriteLine( errorMessage[0] );
        }

        private static void CheckArrayEquals( uint[] left, uint[] right )
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
            uint[] message = new uint[] { 0, 0, 0, 0, 12, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

            // GF(2^8) with field generatory poly p(x) = x^8 + x^4 + x^3 + x^2 + 1 ---> 100011101 == 285 == 0x011D
            // n = 255, k = 239, 2t = 16
            //AntiduhEncoder encoder = new AntiduhEncoder( 256, 239, 0x011D );

            encoder.Encode( message );
            ArrayPrint( message );
            Console.Out.WriteLine( GaloisField.PolyPrint( message ) );

        }

        private static void ArrayPrint( uint[] array )
        {
            for ( uint i = 0; i < array.Length; i++ )
            {
                Console.Out.Write( array[i] + ", " );
            }
            Console.Out.WriteLine();
        }

        public static void PrintList( List<int> list )
        {
            for( int i = 0; i < list.Count; i++ )
            {
                Console.Out.WriteLine( "{0}: {1}", i, list[i] );
            }
        }

    }

    public class ReedSolomonTest
    {
        uint size;
        uint checkBytes;
        uint maxCorruption;
        uint[] message;
        uint[] cleanMessage;
        AntiduhEncoder encoder;
        AntiduhDecoder decoder;
        Random rand;
        Stopwatch watch;

        public ReedSolomonTest( uint size, uint dataBytes, uint poly, Stopwatch watch )
        {
            this.size = size;
            this.watch = watch;

            checkBytes = (uint)(size - 1 - dataBytes);
            maxCorruption = checkBytes / 2;
            message = new uint[size - 1];
            cleanMessage = new uint[size - 1];

            rand = new Random();
            encoder = new AntiduhEncoder( size, dataBytes, poly );
            decoder = new AntiduhDecoder( size, dataBytes, poly );

        }

        public void RoundTripTest()
        {
            for( int i = 0; i < message.Length; i++ )
            {
                // message[i] must be elements of the field. If size = 16, field elements are 0 .. 15.
                // rand.Next(0, 16) returns elements between 0 .. 15
                message[i] = (byte)rand.Next( 0, (int)size );
            }

            // ---- Encode the message ----
            watch.Start();
            encoder.Encode( message );
            watch.Stop();

            Array.Copy( message, cleanMessage, message.Length );

            // ---- Corrupt the TX message ----
            int corruptPosition;
            for( int i = 0; i < maxCorruption; i++ )
            {
                corruptPosition = rand.Next( 0, message.Length );
                message[corruptPosition] = (byte)rand.Next( 0, (int)size );
            }

            // ---- Repair the message ----
            watch.Start();
            decoder.Decode( message );
            watch.Stop();

            // ---- Compare ----
            CheckArrayEquals( message, cleanMessage );
        }


        private static void CheckArrayEquals( uint[] left, uint[] right )
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
    }
}
