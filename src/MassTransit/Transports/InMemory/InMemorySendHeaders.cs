// Copyright 2007-2015 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Transports.InMemory
{
    using System;
    using System.Collections;
    using System.Collections.Generic;


    public class InMemorySendHeaders :
        SendHeaders
    {
        readonly IDictionary<string, object> _headers;

        public InMemorySendHeaders()
        {
            _headers = new Dictionary<string, object>();
        }

        public void Set(string key, string value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                _headers.Remove(key);
            else
                _headers[key] = value;
        }

        public void Set(string key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (value == null)
                _headers.Remove(key);
            else
                _headers[key] = value;
        }

        public bool TryGetHeader(string key, out object value)
        {
            return _headers.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _headers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}