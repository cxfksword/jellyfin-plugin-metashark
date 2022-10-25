using System;
using System.Collections;
using System.Collections.Generic;

namespace TMDbLib.Utilities
{
    public class SimpleNamedValueCollection : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly List<KeyValuePair<string, string>> _list;

        public SimpleNamedValueCollection()
        {
            _list = new List<KeyValuePair<string, string>>();
        }

        public string this[string index]
        {
            get { return Get(index); }
            set { Add(index, value); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (KeyValuePair<string, string> pair in _list)
                yield return pair;
        }

        public void Add(string key, string value)
        {
            Remove(key);

            _list.Add(new KeyValuePair<string, string>(key, value));
        }

        public string Get(string key, string @default = null)
        {
            foreach (KeyValuePair<string, string> pair in _list)
            {
                if (pair.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }

            return @default;
        }

        public bool Remove(string key)
        {
            return _list.RemoveAll(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) > 0;
        }
    }
}