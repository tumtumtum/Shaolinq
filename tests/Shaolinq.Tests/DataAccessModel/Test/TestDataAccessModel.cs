namespace Shaolinq.Tests.DataAccessModel.Test
{
	[DataAccessModel]
	public abstract class TestDataAccessModel
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

		[DataAccessObjects]
		public abstract DataAccessObjects<Fraternity> Fraternity { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<ObjectWithGuidAutoIncrementPrimaryKey> ObjectWithGuidAutoIncrementPrimaryKeys { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<ObjectWithGuidNonAutoIncrementPrimaryKey> ObjectWithGuidNonAutoIncrementPrimaryKeys { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<ObjectWithLongAutoIncrementPrimaryKey> ObjectWithLongAutoIncrementPrimaryKeys { get; }

		[DataAccessObjects]
		public abstract DataAccessObjects<ObjectWithLongNonAutoIncrementPrimaryKey> ObjectWithLongNonAutoIncrementPrimaryKeys { get; }
	}
}
