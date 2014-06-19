// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shaolinq
{
	public class MissingOrInvalidPrimaryKeyException
		: DataAccessException
	{
		public MissingOrInvalidPrimaryKeyException(string message)
			: base(message, null)
		{
		}
	}
}
