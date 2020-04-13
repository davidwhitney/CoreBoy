using System;
using System.Collections.Generic;

namespace eu.rekawek.coffeegb.gpu
{
    public class IntQueue
    {
        private Queue<int> _inner;

        public IntQueue(int capacity)
        {
            _inner = new Queue<int>(capacity);
        }

        public int size()
        {
            return _inner.Count;
        }

        public void enqueue(int value)
        {
            _inner.Enqueue(value);
        }

        public int dequeue()
        {
            return _inner.Dequeue();
        }

        public int get(int index)
        {
            return _inner.ToArray()[index];
        }

        public void set(int index, int value)
        {
            lock (_inner)
            {
                var asArray = _inner.ToArray();
                asArray[index] = value;
                _inner = new Queue<int>(asArray);
            }
            /*
            if (index >= size) {
                throw new IndexOutOfBoundsException();
            }
            array[(offset + index) % array.length] = value;*/
        }

        public void clear()
        {
            _inner.Clear();
        }
    }
}