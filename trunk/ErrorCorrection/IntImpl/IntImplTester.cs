using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

            StreamTest();
        }

        public static void EncoderTest()
        {
            Encoder encoder = new Encoder( 16, 11, 4, 0x13 );
            int[] message = { 0, 0, 0, 0, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
            int[] encodedMessage = { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

            encoder.Encode( message );

            ArrayHelpers.CheckArrayEquals( message, encodedMessage );
        }

        public static void DecoderValidTest()
        {
            Decoder decoder = new Decoder( 16, 11, 4, 0x13 );
            int[] message = { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
            int[] cleanMessage = { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

            decoder.Decode( message );

            ArrayHelpers.CheckArrayEquals( message, cleanMessage );
        }

        public static void DecoderErrorTest()
        {
            Decoder decoder = new Decoder( 16, 11, 4, 0x13 );
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
            int iterations = 10000;
            RS256Test test = new RS256Test( size, dataBytes, poly, watch );

            // Prime the JIT.
            test.RoundTripTest();
            test.RoundTripTest();

            watch.Reset();

            for ( int i = 0; i < iterations; i++ )
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

            double average = watch.Elapsed.TotalMilliseconds / ( 10.0 * iterations );
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

        public static void StreamTest()
        {
            //                 >----- thread --->                                                           >----- thread ---->
            //
            // Random --> inputFile.bin --> BlockAdapter --> encoder stream --> buffer --> decoder --> BlockAdapter --> outputFile.bin
            //                ^                                                             v
            //                |                                                             |
            //                I------------------------< Compare <--------------------------I

            // Chain 1:
            //   Random --> inputFile.bin
            //
            // Chain 2:
            //   inputFile.bin ---thread---> BlockAdapter --> encoder stream ---> buffer
            //                    ******
            // Chain 3:
            //   buffer --->  decoder ---> BlockAdapter ---thread---> outputFile.bin
            //                                             ******

            Encoder encoder = new Encoder( 256, 251, 4, 0x11d );
            Decoder decoder = new Decoder( 256, 251, 4, 0x11d );
            
            // Endpoints and connectors
            FileStream input = new FileStream( "inputFile.bin", FileMode.Create, FileAccess.ReadWrite );
            FileStream output = new FileStream( "outputFile.bin", FileMode.Create, FileAccess.ReadWrite );
            MemoryStream betweenBuffer = new MemoryStream( 300 * 1000 * 1000 );

            RsEncoderStream encStream = new RsEncoderStream( betweenBuffer, encoder );
            BlockStreamWriteAdapter writeAdapter = new BlockStreamWriteAdapter( encStream, encoder.MessageSize );

            RsDecoderStream decStream = new RsDecoderStream( betweenBuffer, decoder );
            BlockStreamReadAdapter readAdapter = new BlockStreamReadAdapter( decStream, decoder.MessageSize );
            
            Random rand = new Random();
            byte[] buffer = new byte[251];

            for ( int i = 0; i < 1000 * 1000; i++ )
            {
                FillBuffer( buffer, rand );

                input.Write( buffer, 0, buffer.Length );
            }

            input.Flush( true );
            input.Seek( 0, SeekOrigin.Begin );

            RandomPush( input, writeAdapter, rand );

            betweenBuffer.Flush();
            betweenBuffer.Seek( 0, SeekOrigin.Begin );

            RandomPush( readAdapter, output, rand );

            output.Flush( true );

            input.Seek( 0, SeekOrigin.Begin );
            output.Seek( 0, SeekOrigin.Begin );

            Compare( input, output );
        }

        private static void Compare( Stream left, Stream right)
        {
            int leftReadLen;
            int rightReadLen;

            byte[] leftBuffer = new byte[4096];
            byte[] rightBuffer = new byte[4096];

            while ( true )
            {
                leftReadLen = left.Read( leftBuffer, 0, leftBuffer.Length );
                rightReadLen = right.Read( rightBuffer, 0, rightBuffer.Length );

                if ( leftReadLen != rightReadLen )
                {
                    throw new Exception();
                }

                if ( leftReadLen == 0 )
                {
                    break;
                }

                ArrayHelpers.CheckArrayEquals( leftBuffer, rightBuffer );
            }
        }

        private static void RandomPush( Stream sourceStream, Stream destStream, Random rand )
        {
            byte[] buffer = new byte[4096];
            while ( true )
            {
                int desiredReadLen = rand.Next( 1, buffer.Length + 1 );
                int actualReadLen = sourceStream.Read( buffer, 0, desiredReadLen );

                if ( actualReadLen == 0 )
                {
                    break;
                }

                destStream.Write( buffer, 0, actualReadLen );
            }
        }

        private static void FillBuffer( byte[] buffer, Random rand )
        {
            for ( int i = 0; i < buffer.Length; i++ )
            {
                buffer[i] = (byte)rand.Next( 0, 256 );
            }
        }

        private class RS256Test
        {
            private readonly int fieldSize;
            private readonly int paritySymbols;
            private readonly int maxCorruption;
            private readonly int[] transmittedMessage;
            private readonly int[] receivedMessage;
            private readonly Encoder encoder;
            private readonly Decoder decoder;
            private readonly Random rand;
            private readonly Stopwatch watch;

            public RS256Test( int fieldSize, int messageSize, int poly, Stopwatch watch )
            {
                this.fieldSize = fieldSize;
                this.watch = watch;

                paritySymbols = fieldSize - 1 - messageSize;
                maxCorruption = paritySymbols / 2;
                transmittedMessage = new int[fieldSize - 1];
                receivedMessage = new int[fieldSize - 1];

                rand = new Random();
                encoder = new Encoder( fieldSize, messageSize, fieldSize - 1 - messageSize, poly );
                decoder = new Decoder( fieldSize, messageSize, fieldSize - 1 - messageSize, poly );

            }

            public void RoundTripTest()
            {
                for( int i = 0; i < transmittedMessage.Length; i++ )
                {
                    // message[i] must be elements of the field. If size = 16, field elements are 0 .. 15.
                    // rand.Next(0, 16) returns elements between 0 .. 15
                    transmittedMessage[i] = (byte)rand.Next( 0, (int)fieldSize );
                }

                // ---- Encode the message ----
                watch.Start();
                encoder.Encode( transmittedMessage );
                watch.Stop();

                Array.Copy( transmittedMessage, receivedMessage, transmittedMessage.Length );

                // ---- Corrupt the message ----
                int corruptPosition;
                for( int i = 0; i < maxCorruption; i++ )
                {
                    corruptPosition = rand.Next( 0, receivedMessage.Length );
                    receivedMessage[corruptPosition] = (byte)rand.Next( 0, (int)fieldSize );
                }

                // ---- Repair the message ----
                watch.Start();
                decoder.Decode( receivedMessage );
                watch.Stop();
                
                // ---- Compare ----
                ArrayHelpers.CheckArrayEquals( transmittedMessage, receivedMessage );
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
