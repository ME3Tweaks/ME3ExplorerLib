using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gammtek.Conduit;
using Gammtek.Conduit.Extensions.IO;
using ME3Explorer.Packages;
using ME3Explorer.Unreal;
using StreamHelpers;

namespace ME3Explorer
{
    public static class EnumerableExtensions
    {
        public static int FindOrAdd<T>(this List<T> list, T element)
        {
            int idx = list.IndexOf(element);
            if (idx == -1)
            {
                list.Add(element);
                idx = list.Count - 1;
            }
            return idx;
        }

        /// <summary>
        /// Searches for the specified object and returns the index of its first occurence, or -1 if it is not found
        /// </summary>
        /// <param name="array">The one-dimensional array to search</param>
        /// <param name="value">The object to locate in <paramref name="array" /></param>
        /// <typeparam name="T">The type of the elements of the array.</typeparam>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="array" /> is null.</exception>
        public static int IndexOf<T>(this T[] array, T value)
        {
            return Array.IndexOf(array, value);
        }

        public static int IndexOf<T>(this LinkedList<T> list, LinkedListNode<T> node)
        {
            LinkedListNode<T> temp = list.First;
            for (int i = 0; i < list.Count; i++)
            {
                if (node == temp)
                {
                    return i;
                }
                temp = temp.Next;
            }
            return -1;
        }

        public static int IndexOf<T>(this LinkedList<T> list, T node)
        {
            LinkedListNode<T> temp = list.First;
            for (int i = 0; i < list.Count; i++)
            {
                if (node.Equals(temp.Value))
                {
                    return i;
                }
                temp = temp.Next;
            }
            return -1;
        }

        public static LinkedListNode<T> Node<T>(this LinkedList<T> list, T node)
        {
            LinkedListNode<T> temp = list.First;
            for (int i = 0; i < list.Count; i++)
            {
                if (node.Equals(temp.Value))
                {
                    return temp;
                }
                temp = temp.Next;
            }
            return null;
        }

        public static void RemoveAt<T>(this LinkedList<T> list, int index)
        {
            list.Remove(list.NodeAt(index));
        }

        public static LinkedListNode<T> NodeAt<T>(this LinkedList<T> list, int index)
        {
            LinkedListNode<T> temp = list.First;
            for (int i = 0; i < list.Count; i++)
            {
                if (i == index)
                {
                    return temp;
                }
                temp = temp.Next;
            }
            throw new ArgumentOutOfRangeException();
        }

        public static T First<T>(this LinkedList<T> list)
        {
            return list.First.Value;
        }
        public static T Last<T>(this LinkedList<T> list)
        {
            return list.Last.Value;
        }

        /// <summary>
        /// Overwrites a portion of an array starting at offset with the contents of another array.
        /// Accepts negative indexes
        /// </summary>
        /// <typeparam name="T">Content of array.</typeparam>
        /// <param name="dest">Array to write to</param>
        /// <param name="offset">Start index in dest. Can be negative (eg. last element is -1)</param>
        /// <param name="source">data to write to dest</param>
        public static void OverwriteRange<T>(this IList<T> dest, int offset, IList<T> source)
        {
            if (offset < 0)
            {
                offset = dest.Count + offset;
                if (offset < 0)
                {
                    throw new IndexOutOfRangeException("Attempt to write before the beginning of the array.");
                }
            }
            if (offset + source.Count > dest.Count)
            {
                throw new IndexOutOfRangeException("Attempt to write past the end of the array.");
            }
            for (int i = 0; i < source.Count; i++)
            {
                dest[offset + i] = source[i];
            }
        }

        public static byte[] Slice(this byte[] src, int start, int length)
        {
            var slice = new byte[length];
            Buffer.BlockCopy(src, start, slice, 0, length);
            return slice;
        }

        public static T[] Slice<T>(this T[] src, int start, int length)
        {
            var slice = new T[length];
            Array.Copy(src, start, slice, 0, length);
            return slice;
        }

        public static T[] TypedClone<T>(this T[] src)
        {
            return (T[])src.Clone();
        }

        /// <summary>
        /// Creates a shallow copy
        /// </summary>
        public static List<T> Clone<T>(this IEnumerable<T> src)
        {
            return new List<T>(src);
        }

        //https://stackoverflow.com/a/26880541
        /// <summary>
        /// Searches for the specified array and returns the index of its first occurence, or -1 if it is not found
        /// </summary>
        /// <param name="haystack">The one-dimensional array to search</param>
        /// <param name="needle">The object to locate in <paramref name="haystack" /></param>
        /// <param name="start">The index to start searching at</param>
        public static int IndexOfArray<T>(this T[] haystack, T[] needle, int start = 0) where T : IEquatable<T>
        {
            var len = needle.Length;
            var limit = haystack.Length - len;
            for (var i = start; i <= limit; i++)
            {
                var k = 0;
                for (; k < len; k++)
                {
                    if (!needle[k].Equals(haystack[i + k])) break;
                }
                if (k == len) return i;
            }
            return -1;
        }

        public static bool IsEmpty<T>(this ICollection<T> list)
        {
            return list.Count == 0;
        }

        public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
        {
            return !enumerable.Any();
        }
        /// <summary>
        /// Creates a sequence of tuples by combining the two sequences. The resulting sequence will length of the shortest of the two.
        /// </summary>
        public static IEnumerable<(TFirst, TSecond)> ZipTuple<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second)
        {
            return first.Zip(second, ValueTuple.Create);
        }

        public static bool HasExactly<T>(this IEnumerable<T> src, int count)
        {
            if (count < 0) return false;
            foreach (var _ in src)
            {
                if (count <= 0)
                {
                    return false;
                }

                --count;
            }

            return count == 0;
        }

        public static IEnumerable<T> NonNull<T>(this IEnumerable<T> src) where T : class
        {
            return src.Where(obj => obj != null);
        }

        public static string StringJoin<T>(this IEnumerable<T> values, string separator)
        {
            return string.Join(separator, values);
        }

        public static T MaxBy<T, R>(this IEnumerable<T> en, Func<T, R> evaluate) where R : IComparable<R>
        {
            return en.Select(t => (obj: t, key: evaluate(t)))
                .Aggregate((max, next) => next.key.CompareTo(max.key) > 0 ? next : max).obj;
        }

        public static T MinBy<T, R>(this IEnumerable<T> en, Func<T, R> evaluate) where R : IComparable<R>
        {
            return en.Select(t => (obj: t, key: evaluate(t)))
                .Aggregate((max, next) => next.key.CompareTo(max.key) < 0 ? next : max).obj;
        }

        public static bool SubsetOf<T>(this IList<T> src, IList<T> compare)
        {
            return src.All(compare.Contains);
        }
    }

    public static class DictionaryExtensions
    {
        /// <summary>
        /// Adds <paramref name="value"/> to List&lt;<typeparamref name="TValue"/>&gt; associated with <paramref name="key"/>. Creates List&lt;<typeparamref name="TValue"/>&gt; if neccesary.
        /// </summary>
        public static void AddToListAt<TKey, TValue>(this Dictionary<TKey, List<TValue>> dict, TKey key, TValue value)
        {
            if (!dict.TryGetValue(key, out List<TValue> list))
            {
                list = new List<TValue>();
                dict[key] = list;
            }
            list.Add(value);
        }
    }

    public static class StringExtensions
    {
        public static bool isNumericallyEqual(this string first, string second)
        {
            return double.TryParse(first, out double a)
                && double.TryParse(second, out double b)
                && (Math.Abs(a - b) < double.Epsilon);
        }

        /// <summary>Compares strings with culture and case sensitivity.</summary>
        /// <param name="str">Main string to check in.</param>
        /// <param name="toCheck">Substring to check for in Main String.</param>
        /// <param name="CompareType">Type of comparison.</param>
        /// <returns>True if toCheck found in str, false otherwise.</returns>
        public static bool Contains(this string str, string toCheck, StringComparison CompareType)
        {
            return str.IndexOf(toCheck, CompareType) >= 0;
        }

        //based on algorithm described here: http://www.codeproject.com/Articles/13525/Fast-memory-efficient-Levenshtein-algorithm
        public static int LevenshteinDistance(this string a, string b)
        {
            int n = a.Length;
            int m = b.Length;
            if (n == 0)
            {
                return m;
            }
            if (m == 0)
            {
                return n;
            }

            var v1 = new int[m + 1];
            for (int i = 0; i <= m; i++)
            {
                v1[i] = i;
            }

            for (int i = 1; i <= n; i++)
            {
                int[] v0 = v1;
                v1 = new int[m + 1];
                v1[0] = i;
                for (int j = 1; j <= m; j++)
                {
                    int above = v1[j - 1] + 1;
                    int left = v0[j] + 1;
                    int cost;
                    if (j > m || j > n)
                    {
                        cost = 1;
                    }
                    else
                    {
                        cost = a[j - 1] == b[j - 1] ? 0 : 1;
                    }
                    cost += v0[j - 1];
                    v1[j] = Math.Min(above, Math.Min(left, cost));

                }
            }

            return v1[m];
        }

        public static bool FuzzyMatch(this IEnumerable<string> words, string word, double threshold = 0.75)
        {
            foreach (string s in words)
            {
                int dist = s.LevenshteinDistance(word);
                if (1 - (double)dist / Math.Max(s.Length, word.Length) > threshold)
                {
                    return true;
                }
            }
            return false;
        }



        public static Guid ToGuid(this string src) //Do not edit this function!
        {
            byte[] stringbytes = Encoding.UTF8.GetBytes(src);
            byte[] hashedBytes = new System.Security.Cryptography.SHA1CryptoServiceProvider().ComputeHash(stringbytes);
            Array.Resize(ref hashedBytes, 16);
            return new Guid(hashedBytes);
        }
    }


    public static class IOExtensions
    {
        public static string ReadUnrealString(this Stream stream)
        {
            int length = stream.ReadInt32();
            if (length == 0)
            {
                return "";
            }
            return length < 0 ? stream.ReadStringUnicodeNull(length * -2) : stream.ReadStringASCIINull(length);
        }

        public static void WriteUnrealString(this Stream stream, string value, MEGame game)
        {
            if (game == MEGame.ME3)
            {
                stream.WriteUnrealStringUnicode(value);
            }
            else
            {
                stream.WriteUnrealStringASCII(value);
            }
        }

        public static void WriteUnrealStringASCII(this Stream stream, string value)
        {
            if (value?.Length > 0)
            {
                stream.WriteInt32(value.Length + 1);
                stream.WriteStringASCIINull(value);
            }
            else
            {
                stream.WriteInt32(0);
            }
        }

        public static void WriteUnrealStringUnicode(this Stream stream, string value)
        {
            if (value?.Length > 0)
            {
                stream.WriteInt32(-(value.Length + 1));
                stream.WriteStringUnicodeNull(value);
            }
            else
            {
                stream.WriteInt32(0);
            }
        }

        public static void WriteStream(this Stream stream, MemoryStream value)
        {
            value.WriteTo(stream);
        }

        /// <summary>
        /// Copies the inputstream to the outputstream, for the specified amount of bytes
        /// </summary>
        /// <param name="input">Stream to copy from</param>
        /// <param name="output">Stream to copy to</param>
        /// <param name="bytes">The number of bytes to copy</param>
        public static void CopyToEx(this Stream input, Stream output, int bytes)
        {
            var buffer = new byte[32768];
            int read;
            while (bytes > 0 &&
                   (read = input.Read(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
            {
                output.Write(buffer, 0, read);
                bytes -= read;
            }
        }

        public static NameReference ReadNameReference(this Stream stream, IMEPackage pcc)
        {
            return new NameReference(pcc.getNameEntry(stream.ReadInt32()), stream.ReadInt32());
        }

        public static void WriteNameReference(this Stream stream, NameReference name, IMEPackage pcc)
        {
            stream.WriteInt32(pcc.FindNameOrAdd(name.Name));
            stream.WriteInt32(name.Number);
        }
    }

    public static class ByteArrayExtensions
    {
        static readonly int[] Empty = new int[0];

        public static int[] Locate(this byte[] self, byte[] candidate)
        {
            if (IsEmptyLocate(self, candidate))
                return Empty;

            var list = new List<int>();

            for (int i = 0; i < self.Length; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }

        static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                   || candidate == null
                   || array.Length == 0
                   || candidate.Length == 0
                   || candidate.Length > array.Length;
        }
    }

    public static class UnrealExtensions
    {
        /// <summary>
        /// Converts an ME3Explorer index to an unreal index. (0 and up get incremented, less than zero stays the same)
        /// </summary>
        public static int ToUnrealIdx(this int i)
        {
            return i >= 0 ? i + 1 : i;
        }

        /// <summary>
        /// Converts an unreal index to an ME3Explorer index. (greater than zero gets decremented, less than zero stays the same,
        /// and 0 returns null)
        /// </summary>
        public static int? FromUnrealIdx(this int i)
        {
            if (i == 0)
            {
                return null;
            }
            return i > 0 ? i - 1 : i;
        }

        /// <summary>
        /// Converts Degrees to Unreal rotation units
        /// </summary>
        public static int ToUnrealRotationUnits(this float degrees) => Convert.ToInt32(degrees * 65536f / 360f);

        /// <summary>
        /// Converts Unreal rotation units to Degrees
        /// </summary>
        public static float ToDegrees(this int unrealRotationUnits) => unrealRotationUnits * 360f / 65536f;

        /// <summary>
        /// Checks if this object is of a specific generic type (e.g. List&lt;IntProperty&gt;)
        /// </summary>
        /// <param name="typeToCheck">typeof() of the item you are checking</param>
        /// <param name="genericType">typeof() of the value you are checking against</param>
        /// <returns>True if type matches, false otherwise</returns>
        public static bool IsOfGenericType(this Type typeToCheck, Type genericType)
        {
            return typeToCheck.IsOfGenericType(genericType, out Type _);
        }

        /// <summary>
        /// Checks if this object is of a specific generic type (e.g. List&lt;IntProperty&gt;)
        /// </summary>
        /// <param name="typeToCheck">typeof() of the item you are checking</param>
        /// <param name="genericType">typeof() of the value you are checking against</param>
        /// <param name="concreteGenericType">Concrete type output if this result is true</param>
        /// <returns>True if type matches, false otherwise</returns>
        public static bool IsOfGenericType(this Type typeToCheck, Type genericType, out Type concreteGenericType)
        {
            while (true)
            {
                concreteGenericType = null;

                if (genericType == null)
                    throw new ArgumentNullException(nameof(genericType));

                if (!genericType.IsGenericTypeDefinition)
                    throw new ArgumentException("The definition needs to be a GenericTypeDefinition", nameof(genericType));

                if (typeToCheck == null || typeToCheck == typeof(object))
                    return false;

                if (typeToCheck == genericType)
                {
                    concreteGenericType = typeToCheck;
                    return true;
                }

                if ((typeToCheck.IsGenericType ? typeToCheck.GetGenericTypeDefinition() : typeToCheck) == genericType)
                {
                    concreteGenericType = typeToCheck;
                    return true;
                }

                if (genericType.IsInterface)
                    foreach (var i in typeToCheck.GetInterfaces())
                        if (i.IsOfGenericType(genericType, out concreteGenericType))
                            return true;

                typeToCheck = typeToCheck.BaseType;
            }
        }

        public static float AsFloat16(this ushort float16bits)
        {
            int sign = (float16bits >> 15) & 0x00000001;
            int exp = (float16bits >> 10) & 0x0000001F;
            int mant = float16bits & 0x000003FF;
            switch (exp)
            {
                case 0:
                    return 0f;
                case 31:
                    return 65504f;
            }
            exp += (127 - 15);
            int i = (sign << 31) | (exp << 23) | (mant << 13);
            return BitConverter.ToSingle(BitConverter.GetBytes(i), 0);
        }

        public static ushort ToFloat16bits(this float f)
        {
            byte[] bytes = BitConverter.GetBytes((double)f);
            ulong bits = BitConverter.ToUInt64(bytes, 0);
            ulong exponent = bits & 0x7ff0000000000000L;
            ulong mantissa = bits & 0x000fffffffffffffL;
            ulong sign = bits & 0x8000000000000000L;
            int placement = (int)((exponent >> 52) - 1023);
            if (placement > 15 || placement < -14)
                return 0;
            ushort exponentBits = (ushort)((15 + placement) << 10);
            ushort mantissaBits = (ushort)(mantissa >> 42);
            ushort signBits = (ushort)(sign >> 48);
            return (ushort)(exponentBits | mantissaBits | signBits);
        }

        /// <summary>
        /// expects a float in range -1 to 1, anything outside that will be clamped to it.
        /// </summary>
        public static byte PackToByte(this float f)
        {
            return (byte)((int)(f * 127.5f + 128.0f)).Clamp(0, 255);
        }
    }

    public static class Enums
    {
        public static T[] GetValues<T>() where T : Enum
        {
            return (T[])Enum.GetValues(typeof(T));
        }
        public static string[] GetNames<T>() where T : Enum
        {
            return Enum.GetNames(typeof(T));
        }

        public static T Parse<T>(string val) where T : Enum
        {
            return (T)Enum.Parse(typeof(T), val);
        }
        public static T[] MaskToList<T>(this T mask, bool ignoreDefault = true) where T : Enum
        {
            var q = GetValues<T>().Where(t => mask.HasFlag(t));
            if (ignoreDefault)
            {
                q = q.Where(v => !v.Equals(default(T)));
            }
            return q.ToArray();
        }
    }

    public static class TypeExtension
    {
        public static object InvokeGenericMethod(this Type type, string methodName, Type genericType, object invokeOn, params object[] parameters)
        {
            return type.GetMethod(methodName).MakeGenericMethod(genericType).Invoke(invokeOn, parameters);
        }
    }

    /// <summary>
    /// For use with List initializers
    /// </summary>
    /// <example>
    /// <code>
    /// var intList = new List&lt;int&gt;
    /// {
    ///     1,
    ///     2,
    ///     3,
    ///     InitializerHelper.ConditionalAdd(shouldAdd465, () =&gt; new[]
    ///     {
    ///         4,
    ///         5,
    ///         6
    ///     }),
    ///     7,
    ///     8
    /// }
    /// //intList would only contain 4,5, and 6 if shouldAdd456 was true 
    /// </code>
    /// </example>
    public static class ListInitHelper
    {
        public class InitCollection<T> : List<T>
        {
            public InitCollection(IEnumerable<T> collection) : base(collection) { }
            public InitCollection() { }
        }

        public static InitCollection<T> ConditionalAddOne<T>(bool condition, Func<T> elem) => condition ? new InitCollection<T> { elem() } : null;

        public static InitCollection<T> ConditionalAddOne<T>(bool condition, Func<T> ifTrue, Func<T> ifFalse) => new InitCollection<T> { condition ? ifTrue() : ifFalse() };

        public static InitCollection<T> ConditionalAdd<T>(bool condition, Func<IEnumerable<T>> elems) => condition ? new InitCollection<T>(elems()) : null;

        public static InitCollection<T> ConditionalAdd<T>(bool condition, Func<IEnumerable<T>> ifTrue, Func<IEnumerable<T>> ifFalse) =>
            new InitCollection<T>(condition ? ifTrue() : ifFalse());

        //this may appear to have no references, but it is implicitly called whenever ConditionalAdd is used in a List initializer
        //VS's "Find All References" can't figure this out, but Resharper's "Find Usages" can 
        public static void Add<T>(this List<T> list, InitCollection<T> range)
        {
            if(range != null) list.AddRange(range);
        }
    }
}
