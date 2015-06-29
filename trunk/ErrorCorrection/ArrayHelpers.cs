using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorCorrection
{
    public class ArrayHelpers
    {
        public static void CheckArrayEquals( byte[] left, byte[] right )
        {
            bool good = false;

            try
            {
                if( left.Length != right.Length )
                {
                    return;
                }

                for( int i = 0; i < left.Length; i++ )
                {
                    if( left[i] != right[i] )
                    {
                        return;
                    }
                }

                good = true;
            }
            finally
            {
                if( good == false )
                {
                    throw new Exception();
                }
            }
        }


        public static void CheckArrayEquals( int[] left, int[] right )
        {
            bool good = false;

            try
            {
                if( left.Length != right.Length )
                {
                    return;
                }

                for( int i = 0; i < left.Length; i++ )
                {
                    if( left[i] != right[i] )
                    {
                        return;
                    }
                }

                good = true;
            }
            finally
            {
                if( good == false )
                {
                    throw new Exception();
                }
            }
        }

    }
}
