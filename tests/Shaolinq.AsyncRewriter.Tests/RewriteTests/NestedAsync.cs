using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaolinq.AsyncRewriter.Tests.RewriteTests
{
	public interface IThing : IThingAsync
	{
		void DoSomethingNested();

		void DoSomethingNotNested();
		Task DoSomethingNotNestedAsync();
	}

	public interface IThingAsync
	{
		Task DoSomethingNestedAsync();
	}

	public class Thing : IThing
	{
		public void DoSomethingNested()
		{
		}

		public async Task DoSomethingNestedAsync()
		{
		}

		public void DoSomethingNotNested()
		{
		}

		public async Task DoSomethingNotNestedAsync()
		{
		}
	}

	public class WidgetAsync
	{
		public async Task DoSomethingNestedAsync()
		{
		}
	}

	public class Widget : WidgetAsync
	{
		public void DoSomethingNested()
		{
		}

		public void DoSomethingNotNested()
		{
		}

		public async Task DoSomethingNotNestedAsync()
		{
		}
	}

	public partial class NestedAsync
	{
		private IThing iThing;
		private Thing thing;
		private Widget widget;

		[RewriteAsync]
		public void Foo()
		{
			iThing.DoSomethingNested();
			iThing.DoSomethingNotNested();

			thing.DoSomethingNested();
			thing.DoSomethingNotNested();

			widget.DoSomethingNested();
			widget.DoSomethingNotNested();
		}
	}
}
