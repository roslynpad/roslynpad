//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;

    /// <summary>
    /// Allows property owners to control the lifetimes of the properties in the collection. 
    /// </summary>
    /// <remarks>This collection is synchronized in order to allow access by multiple threads.</remarks>
    public class PropertyCollection
    {
        private HybridDictionary properties;
        private readonly object syncLock = new object();

        /// <summary>
        /// Adds a new property to the collection.
        /// </summary>
        /// <param name="key">The key by which the property can be retrieved. Must be non-null.</param>
        /// <param name="property">The property to associate with the key.</param>
        /// <exception cref="ArgumentException">An element with the same key already exists in the PropertyCollection.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        public void AddProperty(object key, object property)
        {
            lock (this.syncLock)
            {
                if (this.properties == null)
                {
                    this.properties = new HybridDictionary();
                }
                this.properties.Add(key, property);
            }
        }

        /// <summary>
        /// Removes the property associated with the specified key.
        /// </summary>
        /// <param name="key">The key of the property to remove.</param>
        /// <returns><c>true</c> if the property was found and removed, <c>false</c> if the property was not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool RemoveProperty(object key)
        {
            lock (this.syncLock)
            {
                if (this.properties != null && this.properties.Contains(key))
                {
                    this.properties.Remove(key);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets or creates a property of type <typeparamref name="T"/> from the property collection. If
        /// there is already a property with the specified <paramref name="key"/>, returns the existing property. Otherwise,
        /// uses <paramref name="creator"/> to create an instance of that type and add it to the collection with the specified <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="key">The key of the property to get or create.</param>
        /// <param name="creator">The delegate used to create the property (if needed).</param>
        /// <returns>The property that was requested.</returns>
        public T GetOrCreateSingletonProperty<T>(object key, Func<T> creator) where T : class
        {
            if (creator == null)
                throw new ArgumentNullException(nameof(creator));

            lock (this.syncLock)
            {
                if (this.properties == null)
                {
                    this.properties = new HybridDictionary();
                }
                else
                {
                    if (this.properties.Contains(key))
                        return (T)this.properties[key];
                }

                T result = creator();

                //It is possible that executing the creator function had the side-effect of adding a property with this key to the property
                //bag (the locks only prevent access from other threads, not from re-entrant calls by this thread). This is bad since the
                //creator function is getting called twice but our best option is to discard the result created above and return the one
                //that is already in the property bag so, at least, we are being consistent.
                if (this.properties.Contains(key))
                {
                    result = (T)(this.properties[key]);
                }
                else
                {
                    this.properties.Add(key, result);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets or creates a property of type <typeparamref name="T"/> from the property collection. If
        /// there is already a property of that type, it returns the existing property. Otherwise, it
        /// uses <paramref name="creator"/> to create an instance of that type.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="creator">The delegate used to create the property (if needed).</param>
        /// <returns>An instance of the property.</returns>
        /// <remarks>The key used in the property collection will be typeof(T).</remarks>
        public T GetOrCreateSingletonProperty<T>(Func<T> creator) where T : class
        {
            return this.GetOrCreateSingletonProperty<T>(typeof(T), creator);
        }

        /// <summary>
        /// Gets the property associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The property value, or null if the property is not set.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="KeyNotFoundException"><paramref name="key"/> does not exist in the property collection.</exception>
        public TProperty GetProperty<TProperty>(object key)
        {
            return (TProperty)this.GetProperty(key);
        }

        /// <summary>
        /// Gets the property associated with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The property value, or null if the property is not set.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="KeyNotFoundException"><paramref name="key"/> does not exist in the property collection.</exception>
        public object GetProperty(object key)
        {
            lock (this.syncLock)
            {
                if (this.properties == null)
                {
                    throw new KeyNotFoundException("key");
                }

                object item = this.properties[key];

                //If item is null, it could mean the dictionary has the key but it is just null.
                //Check Contains() to figure out whether it is a missing key or a null value.
                if ((item == null) && !this.properties.Contains(key))
                    throw new KeyNotFoundException("key");

                return item;
            }
        }

        /// <summary>
        /// Gets the property associated with the specified key.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property associated with the specified key.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="property">The retrieved property, or default(TValue) if there is
        /// no property associated with the specified key.</param>
        /// <returns><c>true</c> if the property was found, otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        public bool TryGetProperty<TProperty>(object key, out TProperty property)
        {
            lock (this.syncLock)
            {
                if (this.properties != null)
                {
                    object item = this.properties[key];

                    //If item is null, it could mean the dictionary has the key but it is just null.
                    //Check Contains() to figure out whether it is a missing key or a null value.
                    if ((item != null) || this.properties.Contains(key))
                    {
                        property = (TProperty)item;
                        return true;
                    }
                }
            }

            property = default(TProperty);
            return false;
        }

        /// <summary>
        /// Determines whether the property collection contains a property for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns><c>true</c> if the property exists, otherwise <c>false</c>.</returns>
        public bool ContainsProperty(object key)
        {
            lock (this.syncLock)
            {
                return this.properties != null && this.properties.Contains(key);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Object"/> with the specified key.
        /// </summary>
        public object this[object key] 
        {
            get
            {
                return this.GetProperty(key);
            }
            set
            {
                this.SetProperty(key, value);
            }
        }

        /// <summary>
        /// Returns the property collection as a read-only collection.
        /// </summary>
        /// <value>The read-only collection.</value>
        public ReadOnlyCollection<KeyValuePair<object, object>> PropertyList
        {
            get
            {
                List<KeyValuePair<object, object>> propertyList = new List<KeyValuePair<object, object>>();

                lock (this.syncLock)
                {
                    if (this.properties != null)
                    {
                        foreach (DictionaryEntry property in this.properties)
                        {
                            propertyList.Add(new KeyValuePair<object, object>(property.Key, property.Value));
                        }
                    }
                }

                return propertyList.AsReadOnly();
            }
        }

        #region Private Helpers

        /// <summary>
        /// Sets the property value for a given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="property">The property to set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        private void SetProperty(object key, object property)
        {
            lock (this.syncLock)
            {
                if (this.properties == null)
                {
                    this.properties = new HybridDictionary();
                }
                this.properties[key] = property;
            }
        }

        #endregion Private Helpers
    }
}
