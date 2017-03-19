// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

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

			this.Id = dbUser.Id;
			this.UserName = dbUser.UserName;
			this.Email = dbUser.Email;
			this.EmailConfirmed = dbUser.EmailConfirmed;
			this.PasswordHash = dbUser.PasswordHash;
			this.SecurityStamp = dbUser.SecurityStamp;
			this.IsAnonymousUser = dbUser.IsAnonymousUser;
		}

		public virtual void PopulateDbUser(IShaolinqIdentityDbUser<TKey> toUser)
		{
			toUser.UserName = this.UserName;
			toUser.Email = this.Email;
			toUser.EmailConfirmed = this.EmailConfirmed;
			toUser.PasswordHash = this.PasswordHash;
			toUser.SecurityStamp = this.SecurityStamp;
			toUser.IsAnonymousUser = this.IsAnonymousUser;
		}
	}
}