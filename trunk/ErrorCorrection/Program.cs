using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.IO;
using ErrorCorrection.ByteImpl;

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
            Galois256Tester();
            Console.Out.Flush();
        }

        private static void Galois256Tester()
        {
            // Taken from the DVB-T standard
            // p(x) = x^8 + x^4 + x^3 + x^2 + x^0 == 100011101 = 0x011d
            
            Rs256Encoder encoder = new Rs256Encoder( 256, 239, 0x011d );
            Rs256Decoder decoder = new Rs256Decoder( 256, 239, 0x011d );

            byte[] message = new byte[encoder.EncodedSize];
            byte[] origMessage = new byte[encoder.EncodedSize];

            for ( int i = encoder.CheckWords; i < encoder.EncodedSize; i++ )
            {
                origMessage[i] = (byte)i; 
            }

            encoder.Encode( origMessage );

            Array.Copy( origMessage, message, origMessage.Length );

            message[100] = 253;
            message[101] = 199;

            decoder.Decode( message );

            CheckArrayEquals( origMessage, message );
            
        }

        private static void OldEncoderTest()
        {
            Rs256Decoder decoder = new Rs256Decoder( 16, 11, 0x13 );
            {
                byte[] origMessage = { 0, 0, 0, 0, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
                byte[] encodedMessage = { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
                byte[] errorMessage = { 12, 12, 1, 1, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

                decoder.Decode( errorMessage );
                CheckArrayEquals( errorMessage, encodedMessage );
            }

            AntiduhDecoder oldDecoder = new AntiduhDecoder( 16, 11, 0x13 );
            {
                int[] origMessage = { 0, 0, 0, 0, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
                int[] encodedMessage = { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
                int[] errorMessage = { 12, 12, 1, 1, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

                oldDecoder.Decode( errorMessage );

                CheckArrayEquals( errorMessage, encodedMessage );
            }
        }

        private static void PrimeFinder()
        {
            GaloisField256 field;

            for( int size = 2; size <= 256; size++ )
            {
                for( int primeElement = 2; primeElement <= 512; primeElement++ )
                {
                    field = new GaloisField256( size, primeElement );

                    if( VerifyField( field ) )
                    {
                        Console.Out.WriteLine( "Found - size: {0}; prime: {1}", size, primeElement );
                    }
                }
            }
        }

        private static bool VerifyField( GaloisField256 field )
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


        private static void CheckArrayEquals( byte[] left, byte[] right )
        {
            bool good = false;

            try
            {
                if ( left.Length != right.Length )
                {
                    return;
                }

                for ( int i = 0; i < left.Length; i++ )
                {
                    if ( left[i] != right[i] )
                    {
                        return;
                    }
                }

                good = true;
            }
            finally
            {
                if ( good == false )
                {
                    Console.Out.WriteLine( "New is broken" );
                    Console.Out.Flush();
                }
            }
        }


        private static void CheckArrayEquals( int[] left, int[] right )
        {
            if ( left.Length != right.Length )
            {
                throw new Exception();
            }

            for ( int i = 0; i < left.Length; i++ )
            {
                if ( left[i] != right[i] )
                {
                    throw new Exception();
                }
            }
        }

    }

}
