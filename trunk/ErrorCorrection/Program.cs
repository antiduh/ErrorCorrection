using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.IO;

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
            ReadStreamTest();
            Console.Out.Flush();
        }

        private static void ReadStreamTest()
        {
            FileStream file = new FileStream( "test.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite );

            AntiduhDecoder decoder = new AntiduhDecoder( 256, 239, 0x011D );

            RsDecoderStream decoderStream = new RsDecoderStream( file, decoder );

            BlockStreamReadAdapter readAdapter = new BlockStreamReadAdapter( decoderStream, decoder.PlainTextSize );

            Random rand = new Random();

            byte[] buffer = new byte[200];

            long position = 0;

            Array.Clear( buffer, 0, buffer.Length );

            for( int test = 0; test < 150 * 1000; test++ )
            {
                int length = rand.Next( 1, buffer.Length + 1 );
                int amountRead = readAdapter.Read( buffer, 0, length );

                if( amountRead == 0 )
                {
                    return;
                }

                for( int i = 0; i < amountRead; i++ )
                {
                    if( buffer[i] != position % 256 )
                    {
                        throw new Exception();
                    }

                    position++;
                }
            }

        }

        private static void WriteBlockStreamTest()
        {
            if( File.Exists( "block.bin" ) )
            {
                File.Delete( "block.bin" );
            }

            FileStream file = new FileStream( "block.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite );
                        
            BlockStreamWriteAdapter writeAdapter = new BlockStreamWriteAdapter( file, 239 );

            Random rand = new Random();

            byte[] buffer = new byte[255 * 5];
            int position = 0;


            for( int test = 0; test < 5; test++ )
            {
                int length = rand.Next( 1, buffer.Length + 1 );

                for( int i = 0; i < length; i++ )
                {
                    buffer[i] = (byte)position;
                    position = ( position + 1 ) % 256;
                }

                writeAdapter.Write( buffer, 0, length );
            }

            writeAdapter.Close();
            file.Close();

        }

        private static void ReadBlockStreamTest()
        {
            FileStream file = new FileStream( "block.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite );

            BlockStreamReadAdapter readAdapter = new BlockStreamReadAdapter( file, 239 );

            Random rand = new Random();

            byte[] buffer = new byte[200];

            long position = 0;

            Array.Clear( buffer, 0, buffer.Length );

            for( int test = 0; test < 150 * 1000; test++ )
            {
                int length = buffer.Length;

                int amountRead = readAdapter.Read( buffer, 0, length );

                if( amountRead == 0 )
                {
                    Console.Out.WriteLine( "Block read test successful" );
                    return;
                }

                for( int i = 0; i < amountRead; i++ )
                {
                    if( buffer[i] != position % 256 )
                    {
                        throw new Exception();
                    }

                    position++;
                }
            }
        }


        private static void WriteStreamTest()
        {
            if( File.Exists( "test.bin" ) )
            {
                File.Delete( "test.bin" );
            }

            FileStream file = new FileStream( "test.bin", FileMode.OpenOrCreate, FileAccess.ReadWrite );

            // GF(2^8), k = 239, 2t = 16.
            AntiduhEncoder encoder = new AntiduhEncoder( 256, 239, 0x011D );

            RsEncoderStream encoderStream = new RsEncoderStream( file, encoder );

            BlockStreamWriteAdapter writeAdapter = new BlockStreamWriteAdapter( encoderStream, encoder.PlainTextSize );

            Random rand = new Random();

            byte[] buffer = new byte[encoder.CodeWordSize * 5];
            int position = 0;

            
            for( int test = 0; test <  5; test++ )
            {
                int length = rand.Next( 1, buffer.Length + 1 );

                for( int i = 0; i < length; i++ )
                {
                    buffer[i] = (byte)position;
                    position = ( position + 1 ) % 256;
                }

                writeAdapter.Write( buffer, 0, length );
            }
        }

        private static void PerformanceTest()
        {
            // Multidim:
            //      16,  11, 0x13  -- 01.45
            //     256  251, 0x11d -- 22.49, 22.24
            //     256, 239, 0x11d -- 90.67, 84.41

            // Singledim left + right * size
            //      16,  11, 0x13  -- 01.38, 01.291
            //     256, 251, 0x11d -- 14.50, 14.46
            //     256, 239, 0x11d -- 53.41, 53.44

            // Ints instead of uints
            //      16,  11, 0x13  -- 01.21, 01.20
            //     256, 251, 0x11d -- 13.20, 13.26
            //     256, 239, 0x11d -- 51.31, 51.74

            // ChienCache
            //      16,  11, 0x13  -- 01.198, 01.177
            //     256, 251, 0x11d -- 12.915, 12.923
            //     256, 239, 0x11d -- 50.286, 50.274


            Stopwatch watch = new Stopwatch();
            ReedSolomonTest test;
            int testNum = 1;
            int hitsPerIter = 20;
            int iters;

            if( testNum == 1 )
            {
                iters = 150 * 1000;
                test = new ReedSolomonTest( 16, 11, 0x13, watch );
            }
            else if( testNum == 2 )
            {
                iters = 50 * 1000;
                test = new ReedSolomonTest( 256, 251, 0x011D, watch );
            }
            else if( testNum == 3 )
            {
                iters = 10 * 1000;
                test = new ReedSolomonTest( 256, 239, 0x011D, watch );
            }
            else
            {
                throw new Exception();
            }

            test.RoundTripTest();
            test.RoundTripTest();
            watch.Reset();

            for( int i = 0; i < iters; i++ )
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
            int[] errorMessage = new int[] { 12, 12, 1, 3, 11, 10, 9, 8, 7, 11, 5, 4, 3, 2, 1 };
            int[] cleanMessage = new int[] { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
            //int[] cleanMessage = new int[] { 14, 11, 10, 8, 12, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
            //int[] errorMessage = new int[] { 14, 11, 10, 8, 12, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

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
            //AntiduhEncoder encoder = new AntiduhEncoder( 16, 11, 0x13 );
            //int[] message = new int[] { 0, 0, 0, 0, 12, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

            // GF(2^8) with field generatory poly p(x) = x^8 + x^4 + x^3 + x^2 + 1 ---> 100011101 == 285 == 0x011D
            // n = 255, k = 239, 2t = 16
            AntiduhEncoder encoder = new AntiduhEncoder( 256, 239, 0x011D );
            int[] message = new int[255];
            for( int i = 16; i < message.Length; i++ )
            {
                message[i] = i;
            }

            
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
        int size;
        int checkBytes;
        int maxCorruption;
        int[] message;
        int[] cleanMessage;
        AntiduhEncoder encoder;
        AntiduhDecoder decoder;
        Random rand;
        Stopwatch watch;

        public ReedSolomonTest( int size, int dataBytes, int poly, Stopwatch watch )
        {
            this.size = size;
            this.watch = watch;

            checkBytes = size - 1 - dataBytes;
            maxCorruption = checkBytes / 2;
            message = new int[size - 1];
            cleanMessage = new int[size - 1];

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
