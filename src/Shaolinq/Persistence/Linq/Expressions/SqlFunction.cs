// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)
namespace Shaolinq.Persistence.Linq.Expressions
{
	public enum SqlFunction
	{
		/// <summary>
		/// SQL IS NULL
		/// </summary>
		IsNull,

		/// <summary>
		/// SQL IS NOT NULL
		/// </summary>
		IsNotNull,

		/// <summary>
		/// Returns null if both arguments are equal
		/// </summary>
		NullIf,

		/// <summary>
		/// Gets the Date's date part only
		/// </summary>
		Date,

		/// <summary>
		/// Gets a Date's day of the week
		/// </summary>
		DayOfWeek,

		/// <summary>
		/// Gets a Date's day of the month
		/// </summary>
		DayOfMonth,

		/// <summary>
		/// Gets a Date's day of the year
		/// </summary>
		DayOfYear,

		/// <summary>
		/// Gets a Date's week of the year
		/// </summary>
		Week,

		/// <summary>
		/// Gets a Date's month of the year
		/// </summary>
		Month,

		/// <summary>
		/// Gets a Date's year
		/// </summary>
		Year,

		/// <summary>
		/// Gets a Date's hour
		/// </summary>
		Hour,

		/// <summary>
		/// Gets a Date's minute
		/// </summary>
		Minute,

		/// <summary>
		/// Gets a Date's seconds
		/// </summary>
		Second,

		NumberBasedDatePartStart = DayOfWeek,
		NumberBasedDatePartEnd = Second,
		
		/// <summary>
		/// Compares a string using LIKE
		/// </summary>
		Like,

		/// <summary>
		/// Compares a string using NOT LIKE
		/// </summary>
		NotLike,

		/// <summary>
		/// Gets the DateTime on the server
		/// </summary>
		ServerNow,

		/// <summary>
		/// Gets the UTC DateTime on the server
		/// </summary>
		ServerUtcNow,

		/// <summary>
		/// Gets part of a string
		/// </summary>
		Substring,
		
		StartsWith,

		EndsWith,

		ContainsString,

		StringLength,

		/// <summary>
		/// Removes spaces from the start and end of a string
		/// </summary>
		Trim,

		/// <summary>
		/// Removes spaces from the start and end of a string
		/// </summary>
		TrimLeft,

		/// <summary>
		/// Removes spaces from the start and end of a string
		/// </summary>
		TrimRight,

		/// <summary>
		/// Converts a string to upper case
		/// </summary>
		Upper,

		/// <summary>
		/// Converts a string to lower case
		/// </summary>
		Lower,

		/// <summary>
		/// Concats two or more strings
		/// </summary>
		Concat,

		/// <summary>
		/// "In" function
		/// </summary>
		In,

		/// <summary>
		/// "In" function
		/// </summary>
		Exists,

		/// <summary>
		/// Round function for floating point numbers. 
		/// </summary>
		Round,
		
		/// <summary>
		/// Count of blobbed lists or dictionaries
		/// </summary>
		CollectionCount,

		Coalesce,
		CompareObject,
		DateTimeAddTimeSpan,
		DateTimeAddYears,
		DateTimeAddMonths,
		DateTimeAddDays,
		TimeSpanFromMinutes,
		TimeSpanFromSeconds,
		TimeSpanFromDays,
		TimeSpanFromHours,
		RecordsAffected,
		UserDefined
	}
}
