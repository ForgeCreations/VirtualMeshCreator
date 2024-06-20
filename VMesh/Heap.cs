using System;
using System.IO;
using System.Collections;

namespace VirtualMeshCreator.VMesh
{
    public class Heap : IDisposable
    {
        private int heap_size;
        private int num_index;
        private uint[] heap;
        private float[] keys;
        private int[] heap_indexes;

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
            heap_indexes = new int[numIndex];
        }

        private void PushUp(int i)
        {
            uint idx = heap[i];
            int fa = (i - 1) >> 1;
            while(i > 0 && keys[idx] < keys[heap[fa]])
            {
                heap[i] = heap[fa];
                heap_indexes[heap[i]] = i;
                i = fa;
                fa = (i - 1) >> 1;
            }
            heap[i] = idx;
            heap_indexes[heap[i]] = i;
        }

        private void PushDown(int i)
        {
            uint idx = heap[i];
            int ls = (i << 1) + 1;
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
                    i = t;
                    ls = (i << 1) + 1;
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
            heap_indexes = new int[size];
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

        public bool IsPresent(uint idx) => heap_indexes[idx] != ~0;

        public uint Top()
        {
            return heap[0];
        }

        public void Pop()
        {
            uint idx = heap[0];
            heap[0] = heap[--heap_size];
            heap_indexes[heap[0]] = 0;
            heap_indexes[idx] = ~0;
            PushDown(0);
        }

        public void Add(float key, uint idx)
        {
            int i = heap_size++;
            heap[i] = idx;
            keys[idx] = key;
            heap_indexes[idx] = i;
            PushUp(i);
        }

        public void Update(float key, int idx)
        {
            keys[idx] = key;
            int i = heap_indexes[idx];
            if(i > 0 && key < keys[heap[(i - 1) >> 1]]) PushUp(i);
            else PushDown(i);
        }

        public void Remove(uint idx)
        {
            float key = keys[idx];
            int i = heap_indexes[idx];

            if(i == heap_size - 1)
            {
                --heap_size;
                heap_indexes[idx] = ~0;
                return;
            }

            heap[i] = heap[--heap_size];
            heap_indexes[heap[i]] = i;
            heap_indexes[idx] = ~0;
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