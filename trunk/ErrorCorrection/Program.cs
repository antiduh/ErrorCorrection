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

            GaloisField256 field = new GaloisField256( 256, 0x011d );
            
            VerifyField( field );

            Rs256Encoder encoder = new Rs256Encoder( 256, 239, 0x011d );

            byte[] message = new byte[encoder.EncodedSize];

            for ( int i = encoder.CheckWords; i < encoder.EncodedSize; i++ )
            {
                message[i] = (byte)1; 
            }

            encoder.Encode( message );
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

    }

}
