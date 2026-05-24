using FactoryPlanner.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactoryPlanner.Core.Stores
{
    public interface IObjectCache<T, V>
    {
        // Public Functions
        OperationResult TryAdd(T key, V instance);
        OperationResult TryUpdate(T key, V instance);
        OperationResult TryGet(T key, out V value);
        OperationResult TryGetRange(T[] keys, out V[] values);
        OperationResult TryDelete(T key);
        bool ContainsKey(T key);
        OperationResult Clear();

        IEnumerable<V> GetAll();

        // Properties
        IEnumerable<V> Values { get; }
        IEnumerable<T> Keys { get; }
        int Count { get; }
    }
}
