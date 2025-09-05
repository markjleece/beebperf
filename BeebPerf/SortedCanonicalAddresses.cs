namespace BeebPerf
{
    public class SortedCanonicalAddresses
    {
        private readonly List<CanonicalAddress> _List = new();

        public SortedCanonicalAddresses()
        { 
        }

        public SortedCanonicalAddresses(List<CanonicalAddress> items)
        {
            _List = items.ToList();
            _List.Sort();
        }

        public void Add(CanonicalAddress value)
        {
            int index = _List.BinarySearch(value);
            if (index < 0)
            {
                index = ~index; // insertion point
                _List.Insert(index, value);
            }
        }

        public bool Contains(CanonicalAddress value)
        {
            return _List.BinarySearch(value) >= 0;
        }

        public CanonicalAddress Find(CanonicalAddress value)
        {
            if (_List.Count > 0)
            {
                int index = _List.BinarySearch(value);

                index = ~index;

                if (index > 0)
                {
                    index--;
                    CanonicalAddress result = _List[index];
                    if (result.Page == value.Page)
                        return result; // partial match
                }
            }

            return new CanonicalAddress(0, value.Page); // no match
        }
    }
}