using Platform;

namespace Shaolinq
{
	public static class ObjectExtensions
	{
		public static void PopulateFrom<T, U>(this T value, U source)
		{
			ProjectionContext projectionContext;
			var valueDataAccessObject = value as IDataAccessObject;
			
			if (valueDataAccessObject != null)
			{
				projectionContext = valueDataAccessObject.DataAccessModel.DataAccessObjectProjectionContext;
			}
			else
			{
				projectionContext = ProjectionContext.Default;
			}

			ProjectionContext.ProjectInto(projectionContext, value, source);
		}
	}
}
