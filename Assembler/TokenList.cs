using System.Collections.Generic;

namespace Assembler
{
    public class TokenList<T> : List<T>
    {
        private readonly T outOfRange;

        public TokenList(IEnumerable<T> collection, T outOfRange)
            : base(collection)
        {
            this.outOfRange = outOfRange;
        }

        public new T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    return outOfRange;
                return base[index];
            }
        }
    }
}
