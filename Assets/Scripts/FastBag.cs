using System;

namespace Assets.Scripts
{
    public class FastBag<T>
    {
        public int Count { get; private set; }

        public T this[int index]
        {
            get => array[index];
            set
            {
                if (index >= Count) throw new ArgumentOutOfRangeException(nameof(index), "invalid index!");

                array[index] = value;
            }
        }

        private T[] array;

        public FastBag(int capacity)
        {
            array = new T[capacity];
        }

        public int Add(T item)
        {
            if (Count == array.Length)
            {
                var newArray = new T[array.Length * 2];

                Array.Copy(array, newArray, array.Length);

                array = newArray;
            }

            array[Count] = item;

            return Count++;
        }

        public bool TryReplaceByLastElement(int index)
        {
            if (Count == 1)
            {
                array[0] = default;
                Count = 0;

                return false;
            }

            Count--;

            if (index == Count)
            {
                array[index] = default;

                return false;
            }

            array[index] = array[Count];
            array[Count] = default;

            return true;
        }

        public void Clear(bool clearValues = true)
        {
            if (clearValues)
            {
                for (var i = 0; i < Count; i++)
                {
                    array[i] = default;
                }
            }

            Count = 0;
        }
    }
}