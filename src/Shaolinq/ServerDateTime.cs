// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;

namespace Shaolinq
{
	public static class ServerDateTime
	{
		public static DateTime Now
		{
			get 
			{
				return DateTime.Now; 
			}
		}

		public static DateTime UtcNow
		{
			get
			{
				return DateTime.UtcNow;
			}
		}
	}
}
