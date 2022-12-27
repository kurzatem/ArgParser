// /*
//    Copyright 2022 PrioritizedList.cs
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// */

using System.Collections;

namespace ArgParser;

public class PrioritizedList<T> : IReadOnlyList<T>
{
    private readonly IComparer<T> _comparer;

    private readonly List<T> _list;

    public int Count => this._list.Count;
    
    public T this[int index] => this._list[index];

    public PrioritizedList(IComparer<T> comparer)
    {
        this._comparer = comparer;
        this._list = new List<T>();
    }

    public void InsertOrAdd(T obj)
    {
        var oldCount = this.Count;
        for (var i = oldCount - 1; i > -1; i--)
        {
            if (this._comparer.Compare(this._list[i], obj) <= 0)
            {
                this._list.Insert(i + 1, obj);
                break;
            }
        }
        
        if (this.Count == oldCount)
            if (this.Count == 0)
                this._list.Add(obj);
            else
                this._list.Insert(0, obj);
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        return this._list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}