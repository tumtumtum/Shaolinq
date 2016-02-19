# Shaolinq

===

Shaolinq is a powerful ORM and Linq provider for C# and .NET. It provides a  thoughtful, fast and powerful alternative to Linq to SQL and the Entity Framework.

Notable features:

 * Code first object model
 * First class LINQ support
 * Full support for async/await that goes beyond what's possible with the Entity Framework
 * Automatic schema creation and migration
 * Natural object model architecture with both high-level and super-fine-grained access to SQL via LINQ
 * Insanely fast performance by using dynamically generated code (System.Reflection.Emit) to avoid slow dynamic reflection calls (dynamic calls is used by most other ORMs)
 * Automatic LINQ query caching to avoid slow labmda expression compile times
 * Truely abstract object and query model: single code based, multiple backends
 * One-Many and Many-Many relations supported within the object model and LINQ
 * Advanced LINQ support including natural support of server side functions and operations including SQL LIKE, date time conversions, and C# operation converstions such as ToLower() and ToUpper() that work as naturally as native C#
 * Support for not just selecting but deleting sets of objects using LINQ based syntax
 * First class support for Sqlite, MySql and Postgres and SQL server
 * Support for client-side computed property values for automatic performance critical denormalization where necessary
 * Easily support new providers with full LINQ support (less than 100 lines of code)
 * Natural transaction model. Object graphs are saved together and saving objects individually is a thing of the past
 * Unique deflated reference architecture allowing partial object updates and references without pre-requisite reads required by most other ORMs
 
Motivation:
 
The lack of a suitable free code-first ORM with support for major open-source databases in 2007-2008 for .NET motivated me to design a write a new one with a query system based on C# operator overloading. I added LINQ support in mid 2008 after Microsoft released LINQ in .NET 3.5. The primary goals of Shaolinq was:

 * A code-first object model
   * Why write XML when you can write C#?
   * Why write SQL when you can write C#?
* Performance close to or faster than using SQL directly
* Avoiding reflection: Pushing the boundaries of bare-to-the-metal code in .NET
* Tracking and commiting changes at the property/column level rather than the object     level. Changing a single property on an object should only generate an UPDATE statement for the respective column.
* Automatic cached pre-compiled LINQ query support
* Support for executing and projecting stored procedures into objects
 * WYSIWYG schema
 	* The schema should not look like it was designed for an ORM
  * Never generate *special columns* or *special tables*
 	* The *object* nature of the object-model should not undermine the *relational* nature of the database-schema
 	* It should be easy to consume any existing database schema without any *special mapping*
 * Comprehensive LINQ support
 * Fully support LINQ as a first class primary query language unlike other ORMs that support only base SELECT and WHERE clauses
 * Make it super easy to support new databases
 * Adding new providers to the ORM should not require understanding LINQ, expression trees, etc.
 * Support Sqlite, MySql and Postgres and SQL servver out of the box
 * Require only a configuration (web.config) change to switch underlying database providers
 

## Code

===

Define data model:

```csharp

// Object inheriting from generic DataAccessObject to get "Id" primary key property

[DataAccessObject]
public abstract class Person : DataAccessObject<Guid>
{
	[AutoIncrement]
	[PersistedMember]
	public abstract Guid Id { get; set; }
	
    [PersistedMember]
    public abstract int Age { get; set; }
	
    [PersistedMember]
    public abstract string Name { get; set; }
    
    [PersistedMember]
    public abstract Person BestFriend { get; set; }
    
    [Description]
    [Index(LowercaseIndex = true)]
    [ComputedTextMember("{Name} of age {Age}")]
    public abstract string Description { get; set; }
}


// Object inheriting from non generic DataAccessObject to manually define its own primary keys

[DataAccessObject]
public abstract Book : DataAccessObject
{
	[PrimaryKey("$(TYPE_NAME)$(PROPERTY_NAME)")]
	[PersistedMember]
	public abstract long SerialNumber { get; set; }
		
	[PrimaryKey]
	[PersistedMember]
	public abstract string PublisherName { get; set; }
	
	[PersistedMember]
	public abstract string Title { get; set; }
	
	[RelatedDataAccessObjects]
	public abstract RelatedDataAccessObjects<Person> Borrowers { get; }
}

// The data access model - defines all types/tables

[DataAccessModel]
public abstract class ExampleModel : DataAccessModel
{
    [DataAccessObjects]
    public abstract DataAccessObjects<Book> Books { get; }
    
    [DataAccessObjects]
    public abstract DataAccessObjects<Person> People { get; }
}
```

Create SQLite database:

```csharp
using Shaolinq;
using Shaolinq.Sqlite;

static void Main()
{
	var configuration = SqliteConfiguration.Create(":memory:");
	var model = DataAccessModel.BuildDataAccessModel<ExampleModel>(configuration);

	model.Create(DatabaseCreationOptions.DeleteExistingDatabase);
}
```

Create MySQL database:

```csharp
using Shaolinq;
using Shaolinq.MySql;

static void Main()
{
	var configuration = = MySqlConfiguration.Create("ExampleDatabase", "localhost", "root", "root");
	var model = DataAccessModel.BuildDataAccessModel<ExampleModel>(configuration);

	model.Create(DatabaseCreationOptions.DeleteExistingDatabase);
}
```


Insert objects:

```csharp

using (var scope = new DataAccessScope())
{
	var person = model.people.Create();
	
	person.Name = "Steve";
	
	scope.Complete();
}

```

// Insert object using distributed transaction

```csharp

using (var scope = new TransactionScope())
{
	var person = model.people.Create();
	
	person.Name = "Steve";
	
	scope.Complete();
}

```


Insert object asynchronously:

```csharp

using (var scope = new DataAccessScope())
{
	var person = model.People.Create();
	
	person.Name = "Steve";
	
	await scope.CompleteAsync();
}

```

Update object asynchronously and without needing to performa SELECT query:

```csharp

using (var scope = new DataAccessScope())
{
	var book  =  model.Books.GetReference(new { Id = 100, PublisherName = "Penguin" });
	
	book.Title = "Expert Shaolinq";
	
	await scope.CompleteAsync();
}

```

Perform queries with implicit joins and explicit joins using Include.
(Query for all books and for each book all borrowers and for each borrow also include the borrow's best friend then print out all the borrowers name and their best friends' name)

```csharp

using (var scope = new DataAccessScope())
{
	var books = await model.Books.Include(c => c.Borrowers.IncludedItems().BestFriend).ToListAsync();
	
	foreach (var value in books.Borrowers.Items().SelectMany(c => new { c.Name, BestFriendName = c.BestFriend.Name })))
	{
		Console.WriteLine($"Borrower: {value.Name} BestFriend: {vlaue.BestFriend.Name}")
	}
}

```

Asynchronously find the age of all people in the database

```csharp

using (var scope = new DataAccessScope())
{
	var averageAge = await model.People.AverageAsync(c => c.Age);
	
	Console.WriteLine($"Average age is {averageAge}");
}

```


Delete a person from the database using LINQ

```csharp

using (var scope = new DataAccessScope())
{
	await model.People.DeleteWhereAsync(c => c.Name == "Steve");
	
	Console.WriteLine("Deleted all people named Steve");
	
	await scope.CompleteAsync();
}

```


---
Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)
