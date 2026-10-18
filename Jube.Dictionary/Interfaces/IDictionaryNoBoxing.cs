/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Dictionary.Interfaces
{
    public interface IDictionaryNoBoxing<TKey> where TKey : notnull
    {
        InternalValue this[TKey key] { get; set; }
        int Count { get; }
        bool ContainsKey(TKey key);
        bool Remove(TKey key);
        bool TryGetValue(TKey key, out InternalValue value);
        bool TryAdd(TKey key, string? value);
        bool TryAdd(TKey key, double value);
        bool TryAdd(TKey key, bool value);
        bool TryAdd(TKey key, DateTime value);
        bool TryAdd(TKey key, int value);
        void Clear();
        DictionaryNoBoxing<TKey>.Enumerator GetEnumerator();
    }
}