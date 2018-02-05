// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using Shaolinq.Persistence;

namespace Shaolinq
{
	public partial interface IDataAccessModelHook
	{
		/// <summary>
		/// Called when the model needs to create a Guid
		/// </summary>
		/// <returns>A Guid or null if the hook wants to defer creation to another hook or default</returns>
		Guid? CreateGuid();

		/// <summary>
		/// Called when the model needs to create a Guid
		/// </summary>
		/// <param name="propertyDescriptor">The property descriptor related to the GUID if applicable</param>
		/// <returns>A Guid or null if the hook wants to defer creation to another hook or default</returns>
		Guid? CreateGuid(PropertyDescriptor propertyDescriptor);

		/// <summary>
		/// Called after a new object has been created
		/// </summary>
		[RewriteAsync]
		void Create(DataAccessObject dataAccessObject);

		/// <summary>
		/// Called just after an object has been read from the database
		/// </summary>
		[RewriteAsync]
		void Read(DataAccessObject dataAccessObject);
		
		/// <summary>
		/// Called just before changes/updates are written to the database
		/// </summary>
		[RewriteAsync]
		void BeforeSubmit(DataAccessModelHookSubmitContext context);

		/// <summary>
		/// Called just after changes have been written to thea database
		/// </summary>
		/// <remarks>
		/// A transactiojn is usually committed after this call unless the call is due
		/// to a <see cref="DataAccessModel.Flush()"/> call
		/// </remarks>
		[RewriteAsync]
		void AfterSubmit(DataAccessModelHookSubmitContext context);
	}
}
