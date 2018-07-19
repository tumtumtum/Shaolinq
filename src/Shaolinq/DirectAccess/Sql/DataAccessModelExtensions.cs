using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Platform;
using Shaolinq.Persistence;
using Shaolinq.Persistence.Linq;
using Platform.Reflection;

namespace Shaolinq.DirectAccess.Sql
{
	public static partial class DataAccessModelExtensions
	{
		public static SqlDialect GetCurrentSqlDialect<TDataAccessModel>(this TDataAccessModel model)
			where TDataAccessModel : DataAccessModel
		{
			return model.GetCurrentSqlDatabaseContext().SqlDialect;
		}

		/// <summary>
		/// Opens and returns a new connection to the database for the given <see cref="model"/>.
		/// </summary>
		/// <typeparam name="TDataAccessModel">The type of <see cref="DataAccessModel"/></typeparam>
		/// <param name="model">The <see cref="DataAccessModel"/></param>
		/// <remarks>This method does not require an existing <see cref="DataAccessScope"/>. The new connection will be
		/// unrelated to any existing scope and it is up to the caller to dispose of the connection.</remarks>
		/// <returns>The <see cref="IDbConnection"/></returns>
		[RewriteAsync]
		public static IDbConnection OpenConnection<TDataAccessModel>(this TDataAccessModel model)
			where TDataAccessModel : DataAccessModel
		{
			return model.GetCurrentSqlDatabaseContext().OpenConnection();
		}

		/// <summary>
		/// Creates a <see cref="IDbCommand"/> object for the current <see cref="DataAccessScope"/> and <see cref="model"/>.
		/// </summary>
		/// <typeparam name="TDataAccessModel">The type of <see cref="DataAccessModel"/></typeparam>
		/// <param name="model">The <see cref="DataAccessModel"/></param>
		/// <remarks>This method can only be called from within a <see cref="DataAccessScope"/></remarks>
		/// <returns>The <see cref="IDbCommand"/></returns>
		public static IDbCommand CreateCommand<TDataAccessModel>(this TDataAccessModel model)
			where TDataAccessModel : DataAccessModel
		{
			return model.GetCurrentCommandsContext().CreateCommand();
		}

		/// <summary>
		/// Executes a given SQL query and returns the number of rows affected.
		/// </summary>
		/// <typeparam name="TDataAccessModel"></typeparam>
		/// <param name="model">The <see cref="DataAccessModel"/></param>
		/// <param name="sql">The SQL query as a string</param>
		/// <param name="arguments">Arguments for the SQL query</param>
		/// <remarks>
		/// This method can only be called from within a <see cref="DataAccessScope"/>.
		/// </remarks>
		/// <returns>The number of rows affected by the query.</returns>
		[RewriteAsync]
		public static int ExecuteNonQuery<TDataAccessModel>(this TDataAccessModel model, string sql, params object[] arguments)
			where TDataAccessModel : DataAccessModel
		{
			var args = new List<TypedValue>(arguments.Select(c => new TypedValue(c?.GetType() ?? typeof(object), c)));

			return model.GetCurrentCommandsContext().ExecuteNonQuery(sql, args);
		}

		/// <summary>
		/// Returns a list of results for a given SQL query.
		/// </summary>
		/// <typeparam name="TDataAccessModel"></typeparam>
		/// <typeparam name="T">The type of object to return for each value in the result set. Also see <see cref="readObject"/></typeparam>
		/// <param name="model">The <see cref="DataAccessModel"/></param>
		/// <param name="readObject">A function that converts an <see cref="IDataReader"/> into an object of type <see cref="T"/></param>
		/// <param name="sql">The SQL query as a string</param>
		/// <remarks>
		/// This method can only be called from within a <see cref="DataAccessScope"/>.
		/// </remarks>
		/// <returns>An <see cref="IAsyncEnumerable{T}"/> that presents an <see cref="IDataReader"/> for each row in the result set.</returns>
		[RewriteAsync]
		public static List<T> ExecuteReadAll<TDataAccessModel, T>(this TDataAccessModel model, Func<IDataReader, T> readObject, string sql)
			where TDataAccessModel : DataAccessModel
		{
			var emptyArgs = new { };

			return model.ExecuteReadAll(readObject, sql, emptyArgs);
		}

		/// <summary>
		/// Returns a list of results for a given SQL query.
		/// </summary>
		/// <typeparam name="TDataAccessModel"></typeparam>
		/// <typeparam name="T">The type of object to return for each value in the result set. Also see <see cref="readObject"/></typeparam>
		/// <typeparam name="TArgs">The anonymous type containing the parameters referenced by <see cref="sql"/></typeparam>
		/// <param name="model">The <see cref="DataAccessModel"/></param>
		/// <param name="readObject">A function that converts an <see cref="IDataReader"/> into an object of type <see cref="T"/></param>
		/// <param name="sql">The SQL query as a string</param>
		/// <param name="args">An anonymous type containing the parameters referenced by <see cref="sql"/></param>
		/// <remarks>
		/// This method can only be called from within a <see cref="DataAccessScope"/>.
		/// </remarks>
		/// <returns>An <see cref="IAsyncEnumerable{T}"/> that presents an <see cref="IDataReader"/> for each row in the result set.</returns>
		[RewriteAsync]
		public static List<T> ExecuteReadAll<TDataAccessModel, TArgs, T>(this TDataAccessModel model, Func<IDataReader, T> readObject, string sql, TArgs args)
			where TDataAccessModel : DataAccessModel
		{
			var retval = new List<T>();

			model.ExecuteReader(readObject, sql, args).WithEach(c => retval.Add(c));

			return retval;
		}

		/// <summary>
		/// Gets an <see cref="IAsyncEnumerable{T}"/> for a given SQL query.
		/// </summary>
		/// <typeparam name="TDataAccessModel"></typeparam>
		/// <param name="model">The <see cref="DataAccessModel"/></param>
		/// <param name="sql">The SQL query as a string</param>
		/// <remarks>
		/// This method does not block. The query is executed on the first call to <see cref="IEnumerator{T}.MoveNext()"/>.
		/// or <see cref="IAsyncEnumerator{T}.MoveNextAsync()"/>.
		/// <para>This method can only be called from within a <see cref="DataAccessScope"/>.</para>
		/// </remarks>
		/// <returns>An <see cref="IAsyncEnumerable{T}"/> that presents an <see cref="IDataReader"/> for each row in the result set.</returns>
		public static IAsyncEnumerable<IDataReader> ExecuteReader<TDataAccessModel>(this TDataAccessModel model, string sql)
			where TDataAccessModel : DataAccessModel
		{
			return ExecuteReader(model, c => c, sql, new {});
		}

		/// <summary>
		/// Gets an <see cref="IAsyncEnumerable{T}"/> for a given SQL query.
		/// </summary>
		/// <typeparam name="TDataAccessModel"></typeparam>
		/// <typeparam name="TArgs">The anonymous type containing the parameters referenced by <see cref="sql"/></typeparam>
		/// <param name="model">The <see cref="DataAccessModel"/></param>
		/// <param name="sql">The SQL query as a string</param>
		/// <param name="args">An anonymous type containing the parameters referenced by <see cref="sql"/></param>
		/// <remarks>
		/// This method does not block. The query is executed on the first call to <see cref="IEnumerator{T}.MoveNext()"/>.
		/// or <see cref="IAsyncEnumerator{T}.MoveNextAsync()"/>.
		/// <para>This method can only be called from within a <see cref="DataAccessScope"/>.</para>
		/// </remarks>
		/// <returns>An <see cref="IAsyncEnumerable{T}"/> that presents an <see cref="IDataReader"/> for each row in the result set.</returns>
		public static IAsyncEnumerable<IDataReader> ExecuteReader<TDataAccessModel, TArgs>(this TDataAccessModel model, string sql, TArgs args)
			where TDataAccessModel : DataAccessModel
		{
			return ExecuteReader(model, c => c, sql, args);
		}

		/// <summary>
		/// Gets an <see cref="IAsyncEnumerable{T}"/> for a given SQL query.
		/// </summary>
		/// <typeparam name="TDataAccessModel"></typeparam>
		/// <typeparam name="T">The type of object to return for each value in the result set. Also see <see cref="readObject"/></typeparam>
		/// <param name="model">The <see cref="DataAccessModel"/></param>
		/// <param name="readObject">A function that converts an <see cref="IDataReader"/> into an object of type <see cref="T"/></param>
		/// <param name="sql">The SQL query as a string</param>
		/// <remarks>
		/// This method does not block. The query is executed on the first call to <see cref="IEnumerator{T}.MoveNext()"/>.
		/// or <see cref="IAsyncEnumerator{T}.MoveNextAsync()"/>.
		/// <para>This method can only be called from within a <see cref="DataAccessScope"/>.</para>
		/// </remarks>
		/// <returns>An <see cref="IAsyncEnumerable{T}"/> that presents an object of type <see cref="T"/> for each row in the result set.</returns>
		public static IAsyncEnumerable<T> ExecuteReader<TDataAccessModel, T>(this TDataAccessModel model, Func<IDataReader, T> readObject, string sql)
			where TDataAccessModel : DataAccessModel
		{
			return ExecuteReader(model, readObject, sql, new { });
		}

		/// <summary>
		/// Gets an <see cref="IAsyncEnumerable{T}"/> for a given SQL query.
		/// </summary>
		/// <typeparam name="TDataAccessModel"></typeparam>
		/// <typeparam name="T">The type of object to return for each value in the result set. Also see <see cref="readObject"/></typeparam>
		/// <typeparam name="TArgs">The anonymous type containing the parameters referenced by <see cref="sql"/></typeparam>
		/// <param name="model">The <see cref="DataAccessModel"/></param>
		/// <param name="readObject">A function that converts an <see cref="IDataReader"/> into an object of type <see cref="T"/></param>
		/// <param name="sql">The SQL query as a string</param>
		/// <param name="args">An anonymous type containing the parameters referenced by <see cref="sql"/></param>
		/// <remarks>
		/// This method does not block. The query is executed on the first call to <see cref="IEnumerator{T}.MoveNext()"/>.
		/// or <see cref="IAsyncEnumerator{T}.MoveNextAsync()"/>.
		/// <para>This method can only be called from within a <see cref="DataAccessScope"/>.</para>
		/// </remarks>
		/// <returns>An <see cref="IAsyncEnumerable{T}"/> that presents an object of type <see cref="T"/> for each row in the result set.</returns>
		public static IAsyncEnumerable<T> ExecuteReader<TDataAccessModel, TArgs, T>(this TDataAccessModel model, Func<IDataReader, T> readObject, string sql, TArgs args)
			where TDataAccessModel : DataAccessModel
		{
			var sqlDatabaseContext = model.GetCurrentSqlDatabaseContext();

			var sqlQueryProvider = new SqlQueryProvider(model, sqlDatabaseContext);

			T ObjectReader(ObjectProjector objectProjector, IDataReader dataReader, int version, object[] dynamicParameters) => readObject(dataReader);

			var prefix = sqlDatabaseContext.SqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);
			var key = new Pair<Type, string>(typeof(TArgs), prefix);

			if (!typeToTypedValuesFunc.TryGetValue(key, out var del))
			{
				del = CreateToTypedValuesFunc<TArgs>(prefix);

				typeToTypedValuesFunc = new Dictionary<Pair<Type, string>, Delegate>(typeToTypedValuesFunc) {{ key, del }};
			}

			var typedValuesFunc = (Func<TArgs, List<TypedValue>>)del;
			var projector = new ObjectProjector<T>(sqlQueryProvider, model, model.GetCurrentSqlDatabaseContext(), sql, typedValuesFunc(args).ToReadOnlyCollection(), null, ObjectReader);

			return projector;
		}

		private static Func<TArgs, List<TypedValue>> CreateToTypedValuesFunc<TArgs>(string prefix)
		{
			var parameter = Expression.Parameter(typeof(TArgs));
			var initializers = new List<Expression>();

			foreach (var fieldOrProperty in typeof(TArgs).GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(c => c.MemberType == MemberTypes.Field || c.MemberType == MemberTypes.Property))
			{
				var type = Expression.Constant(fieldOrProperty.GetMemberReturnType());
				var value = Expression.PropertyOrField(parameter, fieldOrProperty.Name);

				var constructor = TypeUtils.GetConstructor(() => new TypedValue(default(Type), default(string), default(object)));

				var newTypedValue = Expression.New(constructor, type, Expression.Constant(prefix + fieldOrProperty.Name), Expression.Convert(value, typeof(object)));

				initializers.Add(newTypedValue);
			}

			var newList = Expression.New(typeof(List<TypedValue>));
			var body = (Expression)newList;
			
			if (initializers.Count > 0)
			{
				body = Expression.ListInit(newList, initializers);
			} 

			return Expression.Lambda<Func<TArgs, List<TypedValue>>>(body, parameter).Compile();
		}

		private static Dictionary<Pair<Type, string>, Delegate> typeToTypedValuesFunc = new Dictionary<Pair<Type, string>, Delegate>();
	}
}
