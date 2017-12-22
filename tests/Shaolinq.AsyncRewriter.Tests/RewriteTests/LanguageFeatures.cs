using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests.RewriteTests
{
	public partial class LanguageFeatures
	{
		[RewriteAsync]
		public void NumericLiterals()
		{
			const int One = 0b0001;
			const int Two = 0b0010;
			const int Four = 0b0100;
			const int Eight = 0b1000;
			const int Sixteen = 0b0001_0000;
			const int ThirtyTwo = 0b0010_0000;
			const int SixtyFour = 0b0100_0000;
			const int OneHundredTwentyEight = 0b1000_0000;

			const long BillionsAndBillions = 100_000_000_000;

			const double AvogadroConstant = 6.022_140_857_747_474e23;
			const decimal GoldenRatio = 1.618_033_988_749_894_848_204_586_834_365_638_117_720_309_179M;
		}

		[RewriteAsync]
		public int? NullConditional(string input)
		{
			return input?.Length;
		}

		[RewriteAsync]
		public void ThrowExpression(string input)
		{
			var x = input ?? throw new ApplicationException();
		}

		[RewriteAsync]
		public int OutVariable()
		{
			int.TryParse("123", out var parsed);

			return parsed;
		}

		[RewriteAsync]
		public void IndexInitializer()
		{
			var webErrors = new Dictionary<int, string>
			{
				[404] = "Page not Found",
				[302] = "Page moved, but left a forwarding address.",
				[500] = "The web server can't come out to play today."
			};
		}

		[RewriteAsync]
		public static int PatternMatchIf(IEnumerable<object> values)
		{
			var sum = 0;
			foreach (var item in values)
			{
				if (item is int val)
					sum += val;
				else if (item is IEnumerable<object> subList)
					sum += PatternMatchIf(subList);
			}
			return sum;
		}

		[RewriteAsync]
		public static int PatternMatchSwitch(IEnumerable<object> values)
		{
			var sum = 0;
			foreach (var item in values)
			{
				switch (item)
				{
					case int val:
						sum += val;
						break;
					case IEnumerable<object> subList:
						sum += PatternMatchSwitch(subList);
						break;
				}
			}
			return sum;
		}

		[RewriteAsync]
		public static int PatternMatchSwitchWithConstants(IEnumerable<object> values)
		{
			var sum = 0;
			foreach (var item in values)
			{
				switch (item)
				{
					case 0:
						break;
					case int val:
						sum += val;
						break;
					case IEnumerable<object> subList when subList.Any():
						sum += PatternMatchSwitchWithConstants(subList);
						break;
					case IEnumerable<object> subList:
						break;
					case null:
						break;
					default:
						throw new InvalidOperationException("unknown item type");
				}
			}
			return sum;
		}

		[RewriteAsync]
		public static ref int RefReturn(int[,] matrix, Func<int, bool> predicate)
		{
			for (var i = 0; i < matrix.GetLength(0); i++)
			for (var j = 0; j < matrix.GetLength(1); j++)
				if (predicate(matrix[i, j]))
					return ref matrix[i, j];
			throw new InvalidOperationException("Not found");
		}

		[RewriteAsync]
		public static IEnumerable<char> LocalFunction(char start, char end)
		{
			if (start < 'a' || start > 'z')
				throw new ArgumentOutOfRangeException(paramName: nameof(start), message: "start must be a letter");
			if (end < 'a' || end > 'z')
				throw new ArgumentOutOfRangeException(paramName: nameof(end), message: "end must be a letter");

			if (end <= start)
				throw new ArgumentException($"{nameof(end)} must be greater than {nameof(start)}");

			return LocalFunctionInternal();

			//[RewriteAsync] // can't do this
			IEnumerable<char> LocalFunctionInternal()
			{
				for (var c = start; c < end; c++)
					yield return c;
			}
		}

		//[RewriteAsync] // this really breaks everything following it in the file
		//private static void TupleInput((int, string) input)
		//{
		//	Console.WriteLine(input);
		//}

		//[RewriteAsync]
		//private static (int, string) TupleOutput()
		//{
		//	return (1, "foo");
		//}

		//[RewriteAsync]
		//private static (int, string) GetTuple((int, string) input)
		//{
		//	return (input.Item1 + 1, input.Item2 + "!");
		//}

		[RewriteAsync]
		private static int GetInt(int input)
		{
			return input + 1;
		}
	}
}
