// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	public interface IDataAccessModelHook
	{
		/// <summary>
		/// Called when the model needs to create a Guid
		/// </summary>
		/// <returns>A Guid or null if the hook wants to defer creation to another hook or default</returns>
		Guid? CreateGuid();

		/// <summary>
		/// Called after a new object has been created
		/// </summary>
		void Create(DataAccessObject dataAccessObject);

		/// <summary>
		/// Called just after an object has been read from the database
		/// </summary>
		void Read(DataAccessObject dataAccessObject);
		
		/// <summary>
		/// Called just before changes/updates are written to the database
		/// </summary>
		void BeforeSubmit(DataAccessModelHookSubmitContext context);

		/// <summary>
		/// Called just after changes have been written to thea database
		/// </summary>
		/// <remarks>
		/// A transactiojn is usually committed after this call unless the call is due
		/// to a <see cref="DataAccessModel.Flush()"/> call
		/// </remarks>
		void AfterSubmit(DataAccessModelHookSubmitContext context);
	}
}
