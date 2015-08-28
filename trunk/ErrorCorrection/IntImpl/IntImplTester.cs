using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorCorrection.IntImpl
{
    public class IntImplTester
    {
        public static void DoTests()
        {
            EncoderTest();
            DecoderValidTest();
            DecoderErrorTest();

            PerformanceTest_GF16_4();
            PerformanceTest_GF256_16();
            PerformanceTest_GF2048_16();
        }

        public static void EncoderTest()
        {
            Encoder encoder = new Encoder( 16, 11, 0x13 );
            int[] message = { 0, 0, 0, 0, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
            int[] encodedMessage = { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

            encoder.Encode( message );

            ArrayHelpers.CheckArrayEquals( message, encodedMessage );
        }

        public static void DecoderValidTest()
        {
            Decoder decoder = new Decoder( 16, 11, 0x13 );
            int[] message = { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
            int[] cleanMessage = { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

            decoder.Decode( message );

            ArrayHelpers.CheckArrayEquals( message, cleanMessage );
        }

        public static void DecoderErrorTest()
        {
            Decoder decoder = new Decoder( 16, 11, 0x13 );
            // Note the errors:             v                   v
            int[] message = { 12, 12, 1, 3, 11, 10, 9, 8, 1, 6, 5, 4, 3, 2, 1 };
            int[] cleanMessage = { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

            decoder.Decode( message );

            ArrayHelpers.CheckArrayEquals( message, cleanMessage );
        }

        public static void PerformanceTest_GF16_4()
        {
            PerformanceTest( "RSINT_16/4", 16, 11, 0x13 );
        }

        public static void PerformanceTest_GF256_16()
        {
            PerformanceTest( "RSINT_256/16", 256, 255 - 16, 0x011d );
        }

        public static void PerformanceTest_GF2048_16()
        {
            PerformanceTest( "RSINT_2048/16", 2048, 2048 - 1 - 16, 0x82b );
        }

        private static void PerformanceTest( string name, int size, int dataBytes, int poly )
        {
            Stopwatch watch = new Stopwatch();

            RS256Test test = new RS256Test( size, dataBytes, poly, watch );

            // Prime the JIT.
            test.RoundTripTest();
            test.RoundTripTest();

            watch.Reset();

            for( int i = 0; i < 1000; i++ )
            {
                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();

                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();
                test.RoundTripTest();
            }

            double average = watch.Elapsed.TotalMilliseconds / ( 10 * 1000 * 1.0 );
            double throughput = 8.0 * ( size - 1 ) / ( average * 1000.0 );

            Console.Out.WriteLine(
                "{0}, 100k iterations: total time: {1:0.000}, average time: {2:0.000} ms/iteration, {3:0.000} mbit/sec",
                name,
                watch.Elapsed.TotalMilliseconds,
                average,
                throughput
            );
        }

        public static void PrimeFinder()
        {
            GaloisField field;

            for( int power = 1; power <= 16; power++ )
            {
                int size = (int)Math.Round( Math.Pow( 2, power ) );

                for( int primeElement = 2; primeElement <= size * 2; primeElement++ )
                {
                    try
                    {
                        field = new GaloisField( size, primeElement );

                        if( VerifyField( field ) )
                        {
                            Console.Out.WriteLine( "Found - size: {0}; prime: {1}", size, primeElement );
                        }
                    }
                    catch { } 
                }
            }
        }

        public static bool VerifyField( GaloisField field )
        {
            HashSet<int> values = new HashSet<int>();

            for( int i = 0; i < field.Field.Length; i++ )
            {
                if( values.Contains( field.Field[i] ) )
                {
                    return false;
                }

                values.Add( field.Field[i] );
            }

            for( int i = 0; i < field.Field.Length; i++ )
            {
                if( values.Contains( i ) == false )
                {
                    return false;
                }
            }

            return true;
        }

        private class RS256Test
        {
            private readonly int size;
            private readonly int checkBytes;
            private readonly int maxCorruption;
            private readonly int[] message;
            private readonly int[] cleanMessage;
            private readonly Encoder encoder;
            private readonly Decoder decoder;
            private readonly Random rand;
            private readonly Stopwatch watch;

            public RS256Test( int size, int dataBytes, int poly, Stopwatch watch )
            {
                this.size = size;
                this.watch = watch;

                checkBytes = size - 1 - dataBytes;
                maxCorruption = checkBytes / 2;
                message = new int[size - 1];
                cleanMessage = new int[size - 1];

                rand = new Random();
                encoder = new Encoder( size, dataBytes, poly );
                decoder = new Decoder( size, dataBytes, poly );

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
                ArrayHelpers.CheckArrayEquals( message, cleanMessage );
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
        }
    }
}
