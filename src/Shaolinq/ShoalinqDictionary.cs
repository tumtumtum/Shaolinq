// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Platform.Collections;

namespace Shaolinq
{
	public class ShoalinqDictionary
	{
		public static PropertyInfo GetChangedProperty(Type keyType, Type valueType)
		{
			return typeof(ShoalinqDictionary<,>).MakeGenericType(keyType, valueType).GetProperty("Changed");
		}

		public static ConstructorInfo GetCopyConstructor(Type keyType, Type valueType)
		{
			var type = typeof(ShoalinqDictionary<,>).MakeGenericType(keyType, valueType);

			return type.GetConstructors().First(c => c.GetParameters().Length == 1);
		}
	}

	public class ShoalinqDictionary<K, V>
		: AbstractDictionary<K, V>, IDictionary<K, V>
	{
		public bool Changed { get; internal set; }

		private readonly IDictionary<K, V> dictionary;

		public ShoalinqDictionary()
			: this(new Dictionary<K, V>())
		{
		}

		public ShoalinqDictionary(IDictionary<K, V> dictionary)
		{
			this.dictionary = dictionary;

			this.AfterCleared += HandleChanged;
			this.AfterItemAdded += HandleChanged;
			this.AfterItemChanged += HandleChanged;
			this.AfterItemRemoved += HandleChanged;

			if (dictionary != null)
			{
				foreach (var keyValuePair in dictionary)
				{
					this.Add(keyValuePair);
				}
			}
		}

		private void HandleChanged(object sender, EventArgs eventArgs)
		{
			this.Changed = true;
		}

		public override void Clear()
		{
			this.dictionary.Clear();

			this.OnAfterCleared(new CollectionEventArgs<KeyValuePair<K, V>>());
		}

		public override bool Remove(KeyValuePair<K, V> item)
		{
			var retval = this.dictionary.Remove(item);

			this.OnAfterItemRemoved(new CollectionEventArgs<KeyValuePair<K, V>>(item));

			return retval;
		}

		public override int Count
		{
			get
			{
				return this.dictionary.Count;
			}
		}

		public override void Add(K key, V value)
		{
			this.dictionary.Add(key, value);
		}

		public override bool Remove(K key)
		{
			V value;

			if (this.dictionary.TryGetValue(key, out value))
			{
				this.dictionary.Remove(key);

				var keyValuePair = new KeyValuePair<K, V>(key, value);
                
				this.OnAfterItemRemoved(new CollectionEventArgs<KeyValuePair<K, V>>(keyValuePair));

				return true;
			}
			
			return false;
		}

		public override bool TryGetValue(K key, out V value)
		{
			return this.dictionary.TryGetValue(key, out value);
		}

		public override V this[K key]
		{
			get
			{
				return this.dictionary[key];
			}
			set
			{
				this.dictionary[key] = value;

				this.OnAfterItemChanged(new DictionaryEventArgs<K, V>(key, value));
			}
		}

		public override IEnumerator<KeyValuePair<K, V>> GetEnumerator()
		{
			return this.dictionary.GetEnumerator();
		}
	}
}
