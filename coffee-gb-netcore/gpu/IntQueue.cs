using System;
using System.Collections.Generic;

namespace eu.rekawek.coffeegb.gpu
{
    public class IntQueue
    {
        private readonly Queue<int> _inner;

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
            throw new NotImplementedException("Implement lock and set of queue");
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