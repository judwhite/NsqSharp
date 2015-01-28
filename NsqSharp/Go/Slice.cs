using System;

namespace NsqSharp.Go
{
    internal class Slice<T>
    {
        private readonly T[] _array;
        private int _offset;
        private int _maxIndex;

        public Slice(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (typeof(T) != typeof(char))
                throw new Exception("string construcotr can only be used with Slice<char>");

            _array = (T[])(object)value.ToCharArray();
            _offset = 0;
            _maxIndex = _array.Length;
        }

        public Slice(T[] array)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            _array = array;
            _offset = 0;
            _maxIndex = array.Length;
        }

        public Slice<T> Slc(int start)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException("start", start, "start must be >= 0");
            if (start > Len())
                throw new ArgumentOutOfRangeException("start", start, string.Format("start must be <= {0}", Len()));

            var slice = new Slice<T>(_array);
            slice._offset = _offset + start;
            slice._maxIndex = _maxIndex;

            return slice;
        }

        public Slice<T> Slc(int start, int end)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException("start", start, "start must be >= 0");
            if (start > Len())
                throw new ArgumentOutOfRangeException("start", start, string.Format("start must be < Len() {0}", Len()));
            if (start > end)
                throw new ArgumentOutOfRangeException("start", start, string.Format("start must be <= end {0}", end));
            if (end > Len())
                throw new ArgumentOutOfRangeException("start", start, string.Format("end must be <= Len() {0}", Len()));

            var slice = new Slice<T>(_array);
            slice._offset = _offset + start;
            slice._maxIndex = slice._offset + end;

            return slice;
        }

        public override string ToString()
        {
            if (typeof(T) == typeof(char))
            {
                return new string((char[])(object)ToArray());
            }

            return base.ToString();
        }

        public T[] ToArray()
        {
            T[] array = new T[Len()];

            if (array.Length != 0)
            {
                Array.Copy(_array, _offset, array, 0, array.Length);
            }

            return array;
        }

        public int Len()
        {
            return _maxIndex - _offset;
        }

        public T this[int index]
        {
            get
            {
                if (index >= Len())
                    throw new IndexOutOfRangeException();
                return _array[index + _offset];
            }
        }

        public static bool operator ==(Slice<T> s1, string s2)
        {
            if (ReferenceEquals(s1, null) && s2 == null)
                return true;
            if (ReferenceEquals(s1, null) || s2 == null)
                return false;
            if (typeof(T) != typeof(char))
                return false;

            var str1 = s1.ToString();
            return str1 == s2;
        }

        public static bool operator !=(Slice<T> s1, string s2)
        {
            return !(s1 == s2);
        }

        public override bool Equals(object obj)
        {
            string str = obj as string;
            if (this == str)
                return true;

            return false;
            // TODO: check other slices, arrays.. maybe
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
