using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace g3
{
    public class Bitmap2 : IEquatable<Bitmap2>
    {
        private readonly BitArray _bits;
        private readonly Vector2i _dimensions;

        public Vector2i Dimensions
        {
            get => _dimensions;
        }

        public Bitmap2(Vector2i dims)
        {
            _dimensions = dims;
            _bits = new BitArray(dims.x * dims.y);
        }

        public Bitmap2(int width, int height)
        {
            _dimensions = new Vector2i(width, height);
            _bits = new BitArray(width * height);
        }

        public Bitmap2(Bitmap2 bitmapToCopy)
        {
            _dimensions = bitmapToCopy.Dimensions;
            _bits = (BitArray)bitmapToCopy._bits.Clone();
        }

        public Bitmap2(bool[,] input)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            int rowCount = input.GetLength(0);
            int columnCount = input.GetLength(1);
            _dimensions = new Vector2i(x: columnCount, y: rowCount);
            _bits = new BitArray(_dimensions.x * _dimensions.y);
            for (int row = 0; row < rowCount; ++row)
            {
                for (int col = 0; col < columnCount; ++col)
                {
                    this[row, col] = input[row, col];
                }
            }
        }

        public AxisAlignedBox2i GridBounds => new AxisAlignedBox2i(Vector2i.Zero, Dimensions);

        public bool this[int i]
        {
            get { return _bits[i]; }
            set { _bits[i] = value; }
        }

        public bool this[int r, int c]
        {
            get { return _bits[r * _dimensions.x + c]; }
            set { _bits[r * _dimensions.x + c] = value; }
        }

        public bool this[Vector2i idx]
        {
            get
            {
                int i = idx.y * _dimensions.x + idx.x;
                return _bits[i];
            }
            set
            {
                int i = idx.y * _dimensions.x + idx.x;
                _bits[i] = value;
            }
        }

        public void Set(Vector2i idx, bool val)
        {
            int i = idx.y * _dimensions.x + idx.x;
            _bits[i] = val;
        }

        public bool Get(Vector2i idx)
        {
            int i = idx.y * _dimensions.x + idx.x;
            return _bits[i];
        }


        public Vector2i ToIndex(int i)
        {
            int b = i / _dimensions.x;
            i -= b * _dimensions.x;
            return new Vector2i(i, b);
        }

        public int ToLinear(Vector2i idx)
        {
            return idx.y * _dimensions.x + idx.x;
        }

        public bool[,] ToArray()
        {
            int rowCount = _dimensions.y;
            int columnCount = _dimensions.x;
            bool[,] @out = new bool[rowCount, columnCount];
            for (int row = 0; row < rowCount; ++row)
            {
                for (int col = 0; col < columnCount; ++col)
                {
                    @out[row, col] = this[row, col];
                }
            }

            return @out;
        }

        public string ToDebugString()
        {
            var sb = new StringBuilder();
            int rowCount = _dimensions.y;
            int columnCount = _dimensions.x;
            for (int row = 0; row < rowCount; ++row)
            {
                var innerSb = new StringBuilder("");
                for (int col = 0; col < columnCount; ++col)
                {
                    innerSb.Append(this[row, col] ? "1," : "0,");
                }

                sb.AppendLine(innerSb.ToString());
            }

            return sb.ToString();
        }

        public IEnumerable<Vector2i> Indices()
        {
            for (int y = 0; y < Dimensions.y; ++y)
            {
                for (int x = 0; x < Dimensions.x; ++x)
                {
                    yield return new Vector2i(x, y);
                }
            }
        }

        public IEnumerable<Vector2i> NonZeros()
        {
            for (int i = 0; i < _bits.Count; ++i)
            {
                if (_bits[i])
                {
                    yield return ToIndex(i);
                }
            }
        }

        public bool Equals(Bitmap2 other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return _dimensions.Equals(other._dimensions) &&
                   ((BitArray)_bits.Clone()).Xor(other._bits).OfType<bool>().All(e => !e);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Bitmap2)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = _dimensions.GetHashCode();
                hashCode = (hashCode * 397) ^ (_bits?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
}