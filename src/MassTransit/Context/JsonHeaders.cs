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
namespace MassTransit.Context
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Serialization;


    /// <summary>
    /// The context headers are sourced from the IContextHeaderProvider, with the use of a Json deserializer
    /// to convert data types to objects as required. If the original headers are Json objects, those headers
    /// are deserialized as well
    /// </summary>
    public class JsonHeaders :
        Headers
    {
        readonly JsonSerializer _deserializer;
        readonly IHeaderProvider _provider;

        public JsonHeaders(IHeaderProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _provider = provider;

            _deserializer = JsonMessageSerializer.Deserializer;
        }

        public IEnumerable<KeyValuePair<string, object>> GetAll()
        {
            return _provider.GetAll();
        }

        T Headers.Get<T>(string key, T defaultValue)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            object obj;
            if (!_provider.TryGetHeader(key, out obj))
                return defaultValue;

            if (obj == null)
                return defaultValue;

            JToken token = obj as JToken ?? new JValue(obj);

            if (token.Type == JTokenType.Null)
                return defaultValue;

            using (JsonReader jsonReader = token.CreateReader())
                return (T)_deserializer.Deserialize(jsonReader, typeof(T)) ?? defaultValue;
        }

        T? Headers.Get<T>(string key, T? defaultValue)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            object obj;
            if (!_provider.TryGetHeader(key, out obj))
                return defaultValue;

            if (obj == null)
                return defaultValue;

            JToken token = obj as JToken ?? new JValue(obj);

            if (token.Type == JTokenType.Null)
                token = new JObject();

            if (token.Type == JTokenType.Null)
                return defaultValue;

            using (JsonReader jsonReader = token.CreateReader())
                return (T)_deserializer.Deserialize(jsonReader, typeof(T));
        }

        bool Headers.TryGetHeader(string key, out object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return _provider.TryGetHeader(key, out value);
        }
    }
}