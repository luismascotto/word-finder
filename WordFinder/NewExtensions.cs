using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace WordFinder;

public static class NewExtensions
{
    extension<T>(IEnumerable<T> source)
    {
        /// <summary>
        /// Helper method to split an IEnumerable into a specified number of chunks instead of chunk size.
        /// </summary>
        /// <param name="chunks"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>

        public IEnumerable<T[]> ChunkedIn(int chunks)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (chunks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(chunks), "Size must be greater than 0.");
            }
            if (!source.Any())
            {
                return [];
            }
            int chunkSize = (int)Math.Ceiling((double)source.Count() / chunks);
            return source.Chunk(chunkSize);
        }

        /// <summary>
        /// Copied from .NET 10's Enumerable.Chunk implementation, with modification to limit the number of samples per chunk.
        /// Divides the input sequence into a specified number of chunks, each containing at most <paramref name="maxSamples"/> 
        /// (instead of approximately equal elements), maintaining original data distribution, for sampling purposes.
        /// </summary>
        /// <param name="chunks">The number of chunks to split the input sequence into. Must be greater than 0.</param>
        /// <param name="maxSamples">The maximum number of samples to consider from each chunk.</param>
        /// <returns>An enumerable collection of arrays, where each array represents a chunk of the input sequence. Each chunk
        /// contains approximately the same number of elements.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="chunks"/> is less than 1.</exception>
        public IEnumerable<T[]> ChunkForSample(int chunks, int maxSamples)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentOutOfRangeException.ThrowIfLessThan(chunks, 1, nameof(chunks));
            //if (source is not ICollection<T> _)
            //{
            //    throw new NotSupportedException("Source must implement ICollection<T> or IReadOnlyCollection<T> for Count");
            //}
            int size;
            if (source is T[] array)
            {
                if (array.Length == 0)
                {
                    return [];
                }
                size = (int)Math.Ceiling((double)array.Length / chunks);

                return ArrayChunkIteratorForSample(array, size, Math.Min(size, maxSamples));
            }
            else if (source is ICollection<T> collection)
            {
                if (collection.Count == 0)
                {
                    return [];
                }

                size = (int)Math.Ceiling((double)collection.Count / chunks);
                return CollectionChunkIteratorForSample(collection, size, Math.Min(size, maxSamples));
            }
            else
            {
                if (!source.Any())
                {
                    return [];
                }

                int count = source.Count();
                //if (!source.TryGetNonEnumeratedCount(out int count))
                //{
                //    count = source.Count();
                //}

                size = (int)Math.Ceiling((double)count / chunks);
                return EnumerableChunkIteratorForSample(source, size, Math.Min(size, maxSamples));
            }
            //Deciding if accepting only arrays or lists for the Sample Helper
            //throw new NotSupportedException("Source must be an array or implement ICollection<T> or IReadOnlyCollection<T> for Count");
        }

        public IEnumerable<T> SamplesByChunk(int chunks, int maxSamples)
        {
            return source.ChunkedIn(chunks).Select(chk => chk.First()).Take(maxSamples);
        }
    }


    private static IEnumerable<T[]> ArrayChunkIteratorForSample<T>(T[] source, int size, int sampleSize)
    {
        int index = 0;
        int blockSize;
        int allockSize;
        while (index < source.Length)
        {
            blockSize = Math.Min(size, source.Length - index);
            allockSize = sampleSize > 0 ? Math.Min(sampleSize, blockSize) : blockSize;
            // alloc sample
            T[] chunk = new ReadOnlySpan<T>(source, index, allockSize).ToArray();
            // walk block
            index += blockSize;
            yield return chunk;
        }
    }

    private static IEnumerable<T[]> CollectionChunkIteratorForSample<T>(ICollection<T> source, int size, int sampleSize)
    {
        int index = 0;
        int blockSize;
        int allockSize;
        while (index < source.Count)
        {
            blockSize = Math.Min(size, source.Count - index);
            allockSize = sampleSize > 0 ? Math.Min(sampleSize, blockSize) : blockSize;
            // alloc sample
            //T[] chunk = new ReadOnlySpan<T>(source, index, allockSize).ToArray();
            T[] chunk = [.. source.Skip(index).Take(allockSize)];

            // walk block
            index += blockSize;
            yield return chunk;
        }
    }

    private static IEnumerable<T[]> EnumerableChunkIteratorForSample<T>(IEnumerable<T> source, int size, int sampleSize)
    {
        using IEnumerator<T> e = source.GetEnumerator();

        // Before allocating anything, make sure there's at least one element.
        if (e.MoveNext())
        {
            // Now that we know we have at least one item, allocate an initial storage array. This is not
            // the array we'll yield.  It starts out small in order to avoid significantly overallocating
            // when the source has many fewer elements than the chunk size.
            int arraySize = Math.Min(sampleSize, 4);
            int i;
            do
            {
                var array = new T[arraySize];

                // Store the first item.
                array[0] = e.Current;
                i = 1;

                if (sampleSize != array.Length)
                {
                    // This is the first chunk. As we fill the array, grow it as needed.
                    for (; i < sampleSize && e.MoveNext(); i++)
                    {
                        if (i >= array.Length)
                        {
                            arraySize = (int)Math.Min((uint)sampleSize, 2 * (uint)array.Length);
                            Array.Resize(ref array, arraySize);
                        }

                        array[i] = e.Current;
                    }
                }
                else
                {
                    // For all but the first chunk, the array will already be correctly sized.
                    // We can just store into it until either it's full or MoveNext returns false.
                    T[] local = array; // avoid bounds checks by using cached local (`array` is lifted to iterator object as a field)
                    Debug.Assert(local.Length == sampleSize);
                    for (; (uint)i < (uint)local.Length && e.MoveNext(); i++)
                    {
                        local[i] = e.Current;
                    }
                }
                // Deal with missing remainder items
                if (i != array.Length)
                {
                    Array.Resize(ref array, i);
                }

                for (; (uint)i < (uint)size && e.MoveNext(); i++)
                {
                    // just advance the enumerator to skip items beyond the sample size
                }


                yield return array;
            }
            while (i >= size && e.MoveNext());
        }
    }



    extension<T>(IEnumerable<T[]> chunks)
    {
        /// <summary>
        /// Returns a flattened sequence containing up to the specified number of items from each chunk in the input
        /// collection.
        /// Useful for picking samples from each chunk, specially when processing large datasets. Whether the original data is ordered/sorted or not,
        /// picking one sample from each chunk helps to maintain the overall data distribution, without recurring to shuffling/randomizing the entire 
        /// collection (that would need to be reassembled in this case).
        /// Use this when you already have the data chunked (e.g., via the Chunk extension method) and want to pick a few samples from each chunk.
        /// </summary>
        /// <param name="maxSamplesByChunk">The maximum number of items to include from each chunk. Must be greater than zero.</param>
        /// <returns>An enumerable sequence of items, with up to <paramref name="maxSamplesByChunk"/> items taken from each chunk
        /// in the input collection.</returns>
        public IEnumerable<T> CollectSamples(int maxSamplesByChunk)
        {
            ArgumentNullException.ThrowIfNull(chunks);
            foreach (var arr in chunks)
            {
                foreach (var item in arr.Take(maxSamplesByChunk))
                {
                    yield return item!;
                }
            }
        }
    }
}