using System.Windows.Input;

namespace NsqMon.Common.Wpf
{
    /// <summary>
    /// MouseHelper
    /// </summary>
    public static class MouseHelper
    {
        private static int _mouseCursorWaitCount;

        /// <summary>Sets the wait cursor.</summary>
        public static void SetWaitCursor()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _mouseCursorWaitCount++;
        }

        /// <summary>Resets the cursor if all other waits have finished.</summary>
        public static void ResetCursor()
        {
            _mouseCursorWaitCount--;
            if (_mouseCursorWaitCount <= 0)
            {
                _mouseCursorWaitCount = 0;
                Mouse.OverrideCursor = null;
            }
        }
    }
}
