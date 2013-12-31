// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System.IO;
using System.Xml;
using Platform.Xml.Serialization;

namespace Shaolinq.Xml
{
	public static class XmlSerializationExtensions
	{
		public static T Create<T>(this DataAccessObjectsQueryable<T> querable, TextReader reader, bool transient, SerializationParameters parameters)
			where T : class, IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();
			var value = querable.Create(transient);

			serializer.Deserialize(value, reader, parameters);

			return value;
		}

		public static T Create<T>(this DataAccessObjectsQueryable<T> querable, TextReader reader, bool transient)
			where T : class, IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();
			var value = querable.Create(transient);

			serializer.Deserialize(value, reader, SerializationParameters.Empty);

			return value;
		}

		public static T Create<T>(this DataAccessObjectsQueryable<T> querable, XmlReader reader, bool transient, SerializationParameters parameters)
			where T : class, IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();
			var value = querable.Create(transient);

			serializer.Deserialize(value, reader, parameters);

			return value;
		}

		public static T Create<T>(this DataAccessObjectsQueryable<T> querable, XmlReader reader, bool transient)
			where T : class, IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();
			var value = querable.Create(transient);

			serializer.Deserialize(value, reader, SerializationParameters.Empty);

			return value;
		}

		public static T Create<T>(this DataAccessObjectsQueryable<T> querable, string xml, bool transient)
			where T : class, IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();
			var value = querable.Create(transient);
			
			serializer.Deserialize(value, xml);

			return value;
		}

		public static T Create<T>(this DataAccessObjectsQueryable<T> querable, string xml, bool transient, SerializationParameters parameters)
			where T : class, IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();
			var value = querable.Create(transient);

			serializer.Deserialize(value, xml, parameters);

			return value;
		}

		public static T PopulateFromXml<T>(this T value, string xml)
			where T : class, IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();

			serializer.Deserialize(value, xml);

			return value;
		}

		public static T PopulateFromXml<T>(this T value, string xml, SerializationParameters parameters)
			where T : IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();

			serializer.Deserialize(value, xml);

			return value;
		}

		public static T PopulateFromXml<T>(this T value, TextReader reader, SerializationParameters parameters)
			where T : IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();

			serializer.Deserialize(value, reader, parameters);

			return value;
		}

		public static T PopulateFromXml<T>(this T value, XmlReader reader, SerializationParameters parameters)
			where T : IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();

			serializer.Deserialize(value, reader, parameters);

			return value;
		}

		public static T PopulateFromXml<T>(this T value, TextReader reader)
			where T : IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();

			serializer.Deserialize(value, reader);

			return value;
		}

		public static T PopulateFromXml<T>(this T value, XmlReader reader)
			where T : IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();

			serializer.Deserialize(value, reader, SerializationParameters.Empty);

			return value;
		}

		public static void WriteXml<T>(this T value, XmlWriter writer)
			where T : IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();

			serializer.Serialize(value, writer);
		}

		public static void WriteXml<T>(this T value, TextWriter writer)
			where T : IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();

			serializer.Serialize(value, writer);
		}

		public static string ToXml<T>(this T value)
			where T : IDataAccessObject
		{
			var serializer = XmlSerializer<T>.New();

			return serializer.SerializeToString(value);
		}
	}
}
