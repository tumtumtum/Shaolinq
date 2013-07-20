namespace Shaolinq.Tests.DataAccessModel.KungFuSchool
{
	[DataAccessModel]
	public abstract class KungFuSchoolDataAccessModel
		: BaseDataAccessModel
	{
		[DataAccessObjects]
		public abstract DataAccessObjects<School> Schools { get; }
			
		[DataAccessObjects]
		public abstract DataAccessObjects<Product> Products { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Student> Students { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<Instructor> Instructors { get; }
	}
}
