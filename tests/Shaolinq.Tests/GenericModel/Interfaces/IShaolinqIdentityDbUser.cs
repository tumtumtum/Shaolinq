// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq.Tests.GenericModel.Interfaces
{
	public interface IShaolinqIdentityDbUser<TPrimaryKey>
	{
		TPrimaryKey Id { get; set; }
		string UserName { get; set; }
		string Name { get; set; }
		string Email { get; set; }
		bool EmailConfirmed { get; set; }
		string PasswordHash { get; set; }
		string SecurityStamp { get; set; }
		bool IsAnonymousUser { get; set; }
		DateTime ActivationDate { get; set; }
	}
}