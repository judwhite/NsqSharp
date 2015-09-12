#if NETFX_3_5
namespace System
{
    internal class Tuple<TItem1, TItem2>
    {
        private readonly TItem1 _item1;
        private readonly TItem2 _item2;

        public Tuple(TItem1 item1, TItem2 item2)
        {
            _item1 = item1;
            _item2 = item2;
        }

        public TItem1 Item1 { get { return _item1; } }
        public TItem2 Item2 { get { return _item2; } }
    }
}
#endif
