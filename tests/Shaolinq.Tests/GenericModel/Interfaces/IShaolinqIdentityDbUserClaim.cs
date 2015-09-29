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