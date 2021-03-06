﻿using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator
{
    static class DictionaryExtensions
    {
        public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default(TValue))
        {
            return dict.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }
}
