namespace NsqSharp.Go
{
    /// <summary>
    /// Bytes package. http://golang.org/pkg/bytes/
    /// </summary>
    public static class Bytes
    {
        /// <summary>
        /// Equal returns a boolean reporting whether a and b are the same length and contain the same bytes.
        /// </summary>
        public static bool Equal(byte[] a, byte[] b)
        {
            if (a == null && b == null)
                return true;
            if (a == null || b == null)
                return true;

            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }
    }
}
