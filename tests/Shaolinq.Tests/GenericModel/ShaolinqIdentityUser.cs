using System;
using Shaolinq.Tests.GenericModel.Interfaces;

namespace Shaolinq.Tests.GenericModel
{
	public class ShaolinqIdentityUser<TKey>
		where TKey : IEquatable<TKey>
	{
		public TKey Id { get; internal set; }
		public string UserName { get; set; }
		public string Email { get; set; }
		public bool EmailConfirmed { get; set; }
		public string PasswordHash { get; set; }
		public string SecurityStamp { get; set; }
		public bool IsAnonymousUser { get; set; }

		public virtual void PopulateFromDbUser(IShaolinqIdentityDbUser<TKey> dbUser)
		{
			if (dbUser == null)
			{
				return;
			}

			Id = dbUser.Id;
			UserName = dbUser.UserName;
			Email = dbUser.Email;
			EmailConfirmed = dbUser.EmailConfirmed;
			PasswordHash = dbUser.PasswordHash;
			SecurityStamp = dbUser.SecurityStamp;
			IsAnonymousUser = dbUser.IsAnonymousUser;
		}

		public virtual void PopulateDbUser(IShaolinqIdentityDbUser<TKey> toUser)
		{
			toUser.UserName = UserName;
			toUser.Email = Email;
			toUser.EmailConfirmed = EmailConfirmed;
			toUser.PasswordHash = PasswordHash;
			toUser.SecurityStamp = SecurityStamp;
			toUser.IsAnonymousUser = IsAnonymousUser;
		}
	}
}