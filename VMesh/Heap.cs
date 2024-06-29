using System;
using System.IO;

namespace VirtualMeshCreator.VMesh
{
    public class Heap : IDisposable
    {
        private uint heap_size;
        private int num_index;
        private uint[] heap;
        private float[] keys;
        private uint[] heap_indexes;

        public Heap()
        {
            heap = null;
            keys = null;
            heap_indexes = null;
            heap_size = 0;
            num_index = 0;
        }

        public Heap(int numIndex)
        {
            heap_size = 0;
            num_index = numIndex;
            heap = new uint[numIndex];
            keys = new float[numIndex];
            heap_indexes = new uint[numIndex];
        }

        private void PushUp(uint i)
        {
            uint idx = heap[i];
            int fa = ((int)i - 1) >> 1;
            while(i > 0 && keys[idx] < keys[heap[fa]])
            {
                heap[i] = heap[fa];
                heap_indexes[heap[i]] = i;
                i = (uint)fa;
                fa = ((int)i - 1) >> 1;
            }
            heap[i] = idx;
            heap_indexes[heap[i]] = i;
        }

        private void PushDown(uint i)
        {
            uint idx = heap[i];
            int ls = ((int)i << 1) + 1;
            int rs = ls + 1;
            while(ls < heap_size)
            {
                int t = ls;
                if(rs < heap_size && keys[heap[rs]] < keys[heap[ls]])
                    t = rs;
                if(keys[heap[t]] < keys[idx])
                {
                    heap[i] = heap[t];
                    heap_indexes[heap[i]] = i;
                    i = (uint)t;
                    ls = ((int)i << 1) + 1;
                    rs = ls + 1;
                }
                else break;
            }
            heap[i] = idx;
            heap_indexes[heap[i]] = i;
        }

        public void Resize(int size)
        {
            Dispose();
            heap_size = 0;
            num_index = size;
            heap = new uint[size];
            keys = new float[size];
            heap_indexes = new uint[size];
        }

        public float GetKey(uint index)
        {
            return keys[index];
        }

        public void Clear()
        {
            heap_size = 0;
        }

        public bool Empty => heap_size == 0;

        public bool IsPresent(uint idx) => heap_indexes[idx] != ~0u;

        public uint Num() => heap_size;

        public uint Top()
        {
            return heap[0];
        }

        public void Pop()
        {
            uint idx = heap[0];
            heap[0] = heap[--heap_size];
            heap_indexes[heap[0]] = 0;
            heap_indexes[idx] = ~0u;
            PushDown(0);
        }

        public void Add(float key, uint idx)
        {
            uint i = heap_size++;
            heap[i] = idx;
            keys[idx] = key;
            heap_indexes[idx] = i;
            PushUp(i);
        }

        public void Update(float key, int idx)
        {
            keys[idx] = key;
            uint i = heap_indexes[idx];
            if(i > 0 && key < keys[heap[(i - 1) >> 1]]) PushUp(i);
            else PushDown(i);
        }

        public void Remove(uint idx)
        {
            float key = keys[idx];
            uint i = heap_indexes[idx];

            if(i == heap_size - 1)
            {
                --heap_size;
                heap_indexes[idx] = ~0u;
                return;
            }

            heap[i] = heap[--heap_size];
            heap_indexes[heap[i]] = i;
            heap_indexes[idx] = ~0u;
            if(key < keys[heap[i]]) PushDown(i);
            else PushUp(i);
        }

        public void Dispose()
        {
            heap_size = 0;
            num_index = 0;
            heap = null;
            keys = null;
            heap_indexes = null;
        }
    }
}