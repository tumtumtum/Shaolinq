// Copyright (c) 2007-2015 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.GenericModel.Interfaces
{
	public interface IShaolinqIdentityDbUserClaim<TPrimaryKey, TDbUser>
	{
		TPrimaryKey Id { get; set; }
		TDbUser User { get; set; }
		string ClaimType { get; set; }
		string ClaimValue { get; set; }
	}
}