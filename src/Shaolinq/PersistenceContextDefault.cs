// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿namespace Shaolinq
{
	public enum PersistenceContextDefault
	{
		/// <summary>
		/// Uses explicit context (if declared), then assembly default (if declared)
		/// and then namespace tail.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Uses only assembly default context name
		/// as declared by the DefaultPersistenceContext attribute
		/// </summary>
		AssemblyDefault = 1,

		/// <summary>
		/// Uses only explicitly declared persistence context name
		/// </summary>
		Explicit = 2,

		/// <summary>
		/// Uses only namespace tail as the persistence context name
		/// </summary>
		NamespaceTail = 3,
	}
}
