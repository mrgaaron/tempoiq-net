﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TempoIQ.Models;
using TempoIQ.Json;
using Newtonsoft.Json;

namespace TempoIQ.Queries
{
    /// <summary>
    /// Some utilities for working with <code>Selector</code>s
    /// </summary>
    public static class Select
    {
        [JsonConverter(typeof(SelectorTypeConverter))]
        public enum Type
        {
            Sensors,
            Devices
        }

        /// <summary>
        /// An <code>AllSelector</code> selects all objects within its scope
        /// </summary>
        public static AllSelector All()
        {
            return new AllSelector();
        }

        /// <summary>
        /// Combines multiple selectors, and yields only their intersection
        /// </summary>
        /// <param name="children"></param>
        public static AndSelector And(params Selector[] children)
        {
            return new AndSelector(children);
        }

        /// <summary>
        /// Selects <code>Device</code>s or <code>Sensor</code>s with the
        /// appropriate attribute key/value pairs
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static AttributesSelector Attribute(string key, string value)
        {
            return new AttributesSelector(key, value);
        }

        /// <summary>
        /// Selects <code>Device</code>s or <code>Sensor</code>s with the
        /// any value for the given attribute key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static AttributeKeySelector AttributeKey(string key)
        {
            return new AttributeKeySelector(key);
        }

        /// <summary>
        /// Selects <code>Device</code>s or <code>Sensor</code>s with the
        /// appropriate attribute key/value pairs
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static AttributesSelector Attributes(IDictionary<string, string> attrs)
        {
            return new AttributesSelector(attrs);
        }

        /// <summary>
        /// Selects <code>Device</code>s or <code>Sensor</code>s with the
        /// correct unique key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static KeySelector Key(string key)
        {
            return new KeySelector(key);
        }

        /// <summary>
        /// Combines several other selectors, and yields their union
        /// </summary>
        /// <param name="children"></param>
        public static OrSelector Or(params Selector[] children)
        {
            return new OrSelector(children);
        }
    }
}