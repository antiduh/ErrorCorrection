using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErrorCorrection.ByteImpl
{
    public class GaloisField256
    {
        private int size;

        private byte[,] multTable;

        public GaloisField256( int size, int fieldGenPoly )
        {
            if( size > 256 )
            {
                throw new ArgumentOutOfRangeException( 
                    "size", "must be less than or equal to '8', the largest field this " + 
                    "particular implementation is able to handle." 
                );
            }

            if( size < 2 )
            {
                throw new ArgumentOutOfRangeException( "size", "must be >= 2" );
            }

            this.size = size;

            BuildField( fieldGenPoly );
            BuildLogarithms();
            BuildMultTable();
            BuildInverses();

        }

        public byte[] Field { get; private set; }

        public byte[] Inverses { get; private set; }

        public byte[] Logarithms { get; private set; }

        public byte Multiply( byte left, byte right )
        {
            return this.multTable[left, right];
        }

        public byte Divide( byte dividend, byte divisor )
        {
            return this.multTable[dividend, this.Inverses[divisor]];
        }

        public byte[] PolyMult( byte[] left, byte[] right )
        {
            byte[] result;
            byte coeff;

            result = new byte[left.Length + right.Length - 1];

            for( int leftIndex = 0; leftIndex < left.Length; leftIndex++ )
            {
                for( int rightIndex = 0; rightIndex < right.Length; rightIndex++ )
                {
                    coeff = InternalMult( left[leftIndex], right[rightIndex] );

                    result[leftIndex + rightIndex] = (byte)(result[leftIndex + rightIndex] ^ coeff);
                }
            }

            return result;
        }

        public byte PolyEval( byte[] poly, byte x )
        {
            byte sum;
            byte xLog = this.Logarithms[x];
            byte coeffLog;
            byte power;

            sum = poly[0];

            for( int i = 1; i < poly.Length; i++ )
            {
                if( poly[i] == 0 ) { continue; }

                coeffLog = this.Logarithms[poly[i]];

                power = (byte)( ( coeffLog + xLog * i ) % ( size - 1 ) );
                sum ^= this.Field[power];
            }

            return sum;
        }

        private void BuildField( int fieldGenPoly )
        {
            this.Field = new byte[this.size];

            int curr;
            int last;

            this.Field[0] = 0;
            this.Field[1] = 1;

            last = 1;

            for ( int i = 2; i < this.size; i++ )
            {
                curr = last << 1;

                if ( curr >= size )
                {
                    curr = curr ^ fieldGenPoly;
                }

                this.Field[i] = (byte)curr;

                last = curr;
            }
        }

        private void BuildLogarithms()
        {
            this.Logarithms = new byte[this.size];

            for( int i = 0; i < this.Field.Length; i++ )
            {
                this.Logarithms[this.Field[i]] = (byte)( i - 1 );
            }
        }

        private void BuildMultTable()
        {
            this.multTable = new byte[this.size, this.size];

            // These loop indexes, and InternalMult, all take `int`s. This is required.
            // If I used byte for this, then when size == 256 (valid for an 8-bit field), the iteration
            // variables overflow from 255 --> 0, and so the loop runs for infinity. Using ints still gives
            // the right values.
            for( int left = 0; left < this.size; left++ )
            {
                for( int right = 0; right < this.size; right++ )
                {
                    this.multTable[left, right] = InternalMult( left, right );
                }
            }
        }

        private void BuildInverses()
        {
            this.Inverses = new byte[this.size];
        }

        private byte InternalMult( int left, int right )
        {
            if( left == 0 || right == 0 ) { return 0; }

            int value = Logarithms[left] + Logarithms[right];

            value = value % ( size - 1 );

            return this.Field[value + 1];
        }

        private byte InternalDivide( byte dividend, byte divisor )
        {
            if( dividend == 0 ) { return 0; }

            int value = this.Logarithms[dividend] - this.Logarithms[divisor] + ( size - 1 );

            value = value % (size - 1);

            return this.Field[ value + 1 ];
        }
    }
}
