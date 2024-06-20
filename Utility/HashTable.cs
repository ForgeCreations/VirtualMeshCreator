using System;

namespace VirtualMeshCreator.Utility
{
    public class HashTable : IDisposable
    {
        private uint hash_size;
        private uint hash_mask;
        private uint index_size;
        private uint[] hash;
        private uint[] next_index;

        public HashTable(uint indexSize = 0)
        {
            hash = null;
            next_index = null;
            Resize(indexSize, indexSize);
        }

        public HashTable(uint hashSize, uint indexSize)
        {
            hash = null;
            next_index = null;
            Resize(hashSize, indexSize);
        }

        private void ResizeIndex(uint _index_size)
        {
            index_size = _index_size;
            next_index = new uint[index_size];
            Array.Clear(next_index, 0, next_index.Length);
        }

        public void Resize(uint indexSize)
        {
            ResizeIndex(MeshUtility.lower_nearest_2_power(indexSize));
        }

        public void Resize(uint hashSize, uint indexSize)
        {
            Dispose();
            Console.WriteLine((hash_size & (hash_size - 1)) == 0);

            hash_size = hashSize;
            hash_mask = hashSize - 1;
            hash = new uint[hashSize];
            next_index = new uint[indexSize];
            Array.Resize(ref hash, (int)(hashSize * 4));
            Array.Clear(hash, 0, (int)(hashSize * 4));
        }

        public void Add(uint key, uint idx)
        {
            if(idx >= index_size)
            {
                ResizeIndex(System.Math.Max(32u, MeshUtility.upper_nearest_2_power(idx + 1u)));
            }

            key &= hash_mask;
            next_index[idx] = hash[key];
            hash[key] = idx;
        }

        //Safe for many threads to add concurrently.
        //Not safe to search the table while other threads are adding.
        //Will not resize. Only use for presized tables.
        public void AddConcurrent(uint key, int idx)
        {
            key &= hash_mask;
            //next_index[idx] = FPlatformAtomics::InterlockedExchange((int)hash[key], idx);
            next_index[idx] = hash[key];
        }

        public void Remove(uint key, uint idx)
        {
            if(idx >= index_size) return;
            key &= hash_mask;
            if(hash[key] == idx) hash[key] = next_index[idx];
            
            else
            {
                for(uint i = hash[key]; IsValid(i); i = next_index[i])
                {
                    if(next_index[i] == idx)
                    {
                        next_index[i] = next_index[idx];
                        break;
                    }
                }
            }
        }

        public void Clear()
        {
            if(index_size > 0)
            {
                Array.Resize(ref hash, (int)(hash_size * 4));
                Array.Clear(hash, 0, (int)(hash_size * 4));
            }

            if(next_index != null)
                Array.Clear(next_index, 0, next_index.Length);
        }

        public void Clear(uint inHashSize, uint inIndexSize)
        {
            Dispose();

            hash_size = inHashSize;
            index_size = inIndexSize;

            //check(HashSize > 0);
            Console.WriteLine(hash_size > 0);
            //check(FMath::IsPowerOfTwo(HashSize));
            Console.WriteLine(MathUtil.IsPowerOfTwo(hash_size));

            if(index_size > 0)
            {
                hash_mask = hash_size - 1;

                hash = new uint[hash_size];
                next_index = new uint[index_size];

                //FMemory::Memset(Hash, 0xff, HashSize * 4);
                Array.Resize(ref hash, (int)(hash_size * 4));
                Array.Clear(hash, 0, (int)(hash_size * 4));
            }
        }

        public void Dispose()
        {
            if(index_size > 0)
            {
                hash_mask = 0;
                index_size = 0;

                hash = new uint[1];

                next_index = null;
            }
        }

        public uint First(uint Key)
        {
            Key &= hash_mask;
            return hash[Key];
        }

        public bool IsValid(uint Index)
        {
            return Index != ~0u;
        }

        public uint Next(uint Index)
        {
            //checkSlow(Index < IndexSize);
            //Console.WriteLine(Index < index_size);
            //checkSlow(NextIndex[Index] != Index); // check for corrupt tables
            //Console.WriteLine("Corrupt Tables? [" + (next_index[Index] != Index) + "]");
            return next_index[Index];
        }

        public float AverageSearch() 
        {
            uint SumAvgSearch = 0;
            uint NumElements = 0;
	        for(uint Key = 0; Key < hash_size; Key++)
	        {
		        uint NumInBucket = 0;
		        for(uint i = First(Key); IsValid(i); i = Next(i))
		        {
			        NumInBucket++;
		        }

                SumAvgSearch += NumInBucket * (NumInBucket + 1);
		        NumElements  += NumInBucket;
	        }
            return (SumAvgSearch >> 1) / NumElements;
        }
    }
}
