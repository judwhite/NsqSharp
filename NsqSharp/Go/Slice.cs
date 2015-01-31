using System;

namespace NsqSharp.Go
{
    /// <summary>
    /// Slice
    /// </summary>
    /// <typeparam name="T">The type of data stored in the slice</typeparam>
    public class Slice<T>
    {
        private readonly T[] _array;
        private int _offset;
        private int _maxIndex;

        /// <summary>
        /// Initializes a new Slice from a string
        /// </summary>
        /// <param name="value">The string</param>
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

        /// <summary>
        /// Initializes a new Slice from an array.
        /// </summary>
        /// <param name="array"></param>
        public Slice(T[] array)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            _array = array;
            _offset = 0;
            _maxIndex = array.Length;
        }

        /// <summary>
        /// Creates a new slice starting at <paramref name="start"/> and going to the end.
        /// </summary>
        /// <param name="start">The start index.</param>
        /// <returns>A new slice.</returns>
        public Slice<T> Slc(int start)
        {
            return Slc(start, Len());
        }

        /// <summary>
        /// Creates a new slice starting at <paramref name="start"/> and going to <paramref name="end"/>, exclusive.
        /// </summary>
        /// <param name="start">The start index.</param>
        /// <param name="end">The end index + 1. (ex: Slc(2, 5) returns a new slice of length 3 going from index 2 to 4)</param>
        /// <returns>A new slice.</returns>
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
            slice._maxIndex = _offset + end;

            return slice;
        }

        /// <summary>
        /// Returns the string if the slice is of type Slice&lt;char&gt; otherwise, returns a string representing the array.
        /// </summary>
        public override string ToString()
        {
            if (typeof(T) == typeof(char))
            {
                return new string((char[])(object)ToArray());
            }

            return string.Format("[ {0} ]", string.Join(" ", ToArray()));
        }

        /// <summary>
        /// Returns the slice as an array.
        /// </summary>
        public T[] ToArray()
        {
            T[] array = new T[Len()];

            if (array.Length != 0)
            {
                Array.Copy(_array, _offset, array, 0, array.Length);
            }

            return array;
        }

        /// <summary>
        /// Returns the length of the slice.
        /// </summary>
        public int Len()
        {
            return _maxIndex - _offset;
        }

        /// <summary>
        /// Gets the data at the specified index.
        /// </summary>
        /// <param name="index">The index of the data to return.</param>
        public T this[int index]
        {
            get
            {
                if (index >= Len())
                    throw new IndexOutOfRangeException();
                return _array[index + _offset];
            }
        }

        /// <summary>
        /// Checks if a slice and string are equal.
        /// </summary>
        /// <param name="s1">The first slice.</param>
        /// <param name="s2">The second slice.</param>
        /// <returns><c>true</c> if the slice and string are equal; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Checks if a slice and string are not equal.
        /// </summary>
        /// <param name="s1">The first slice.</param>
        /// <param name="s2">The second slice.</param>
        /// <returns><c>true</c> if the slice and string are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Slice<T> s1, string s2)
        {
            return !(s1 == s2);
        }

        /// <summary>
        /// Compares strings and references, otherwise always returns <c>false</c>. This implementation may change.
        /// </summary>
        /// <param name="obj">The object to check equality with.</param>
        /// <returns><c>true</c> if the strings or references are equal; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            string str = obj as string;
            if (this == str)
                return true;

            if (ReferenceEquals(this, obj))
                return true;

            return false;
            // TODO: check other slices, arrays.. maybe
        }

        /// <summary>
        /// Default GetHashCode implementation.
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
