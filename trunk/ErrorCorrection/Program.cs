using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.IO;
using ErrorCorrection.ByteImpl;
using ErrorCorrection.IntImpl;

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
            //IntImplTester.PrimeFinder();
            Galois256Tester();
            Console.Out.Flush();
        }




        private static void Galois256Tester()
        {
            ByteImplTester.DoTests();
            //IntImplTester.DoTests();
        }

        private static void OldEncoderTest()
        {
            Rs256Decoder decoder = new Rs256Decoder( 16, 11, 0x13 );
            {
                byte[] origMessage = { 0, 0, 0, 0, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
                byte[] encodedMessage = { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
                byte[] errorMessage = { 12, 12, 1, 1, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

                decoder.Decode( errorMessage );
                ArrayHelpers.CheckArrayEquals( errorMessage, encodedMessage );
            }

            AntiduhDecoder oldDecoder = new AntiduhDecoder( 16, 11, 0x13 );
            {
                int[] origMessage = { 0, 0, 0, 0, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
                int[] encodedMessage = { 12, 12, 3, 3, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
                int[] errorMessage = { 12, 12, 1, 1, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };

                oldDecoder.Decode( errorMessage );

                ArrayHelpers.CheckArrayEquals( errorMessage, encodedMessage );
            }
        }

    }

}
