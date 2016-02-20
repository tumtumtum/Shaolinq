// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Tests.GenericModel.Interfaces
{
	public interface IShaolinqIdentityDbUserRole<TPrimaryKey, TDbUser>
	{
		TPrimaryKey Id { get; set; }
		TDbUser User { get; set; }
		string Role { get; set; }
	}
}