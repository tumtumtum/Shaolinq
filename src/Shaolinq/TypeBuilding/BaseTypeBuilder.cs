using System.Reflection.Emit;

namespace Shaolinq.TypeBuilding
{
	public abstract class BaseTypeBuilder
	{
		public ModuleBuilder ModuleBuilder { get; protected set; }
		public AssemblyBuildContext AssemblyBuildContext { get; protected set; }

		protected BaseTypeBuilder(AssemblyBuildContext assemblyBuildContext, ModuleBuilder moduleBuilder)
		{
			this.ModuleBuilder = moduleBuilder;
			this.AssemblyBuildContext = assemblyBuildContext;
		}
	}
}
