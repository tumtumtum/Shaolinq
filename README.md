# Shaolinq

Shaolinq is a powerful ORM and Linq provider for C# and .NET. It provides a  thoughtful, fast and powerful alternative to Linq to SQL and the Entity Framework.

Notable features:

 * Code first object model
 * Top class LINQ support with everything entity framework supports and beyond
 * First class composite primary key support
 * Fully configurable naming by convention using attributes or configuration
 * Full support for async/await that goes beyond what's possible with the Entity Framework
 * Full support for *recursively* including collection properties - handles all necessary and complex join(s) for you
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
	public DateTime? Birthdate {get; set; }
	
	[PersistedMember]
	public abstract int Age { get; set; }
	
	[PersistedMember]
	public abstract string Name { get; set; }
	
	[PersistedMember]
	public abstract Person BestFriend { get; set; }
	
	[BackReference]
	public abstract BorrowedBook { get; set; }
	
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
	person.Age = 18;
	
	scope.Complete();
}

```


Insert object asynchronously:

```csharp

using (var scope = new DataAccessScope())
{
	var person = model.People.Create();
	
	person.Name = "Steve";
	person.Age = 18;
	
	await scope.CompleteAsync();
}

```

Update object asynchronously and without needing to performa SELECT query:

```csharp

using (var scope = new DataAccessScope())
{
	// Gets a reference to an object with a composite primary key without hitting the database
	
	var book  =  model.Books.GetReference(new { Id = 100, PublisherName = "Penguin" });
	
	book.Title = "Expert Shaolinq";
	
	// Will throw if the book (above) does not exist on commit in a single trip to the database
	
	await scope.CompleteAsync();
}

```

Perform queries with implicit joins and explicit joins using Include. Query for all books and for each book all borrowers and for each borrower their best friend. Then print out the borrower and their best friends' name.

```csharp
var books = await model.Books.Include(c => c.Borrowers.IncludedItems().BestFriend).ToListAsync();

foreach (var value in books.Borrowers.Items().SelectMany(c => new { c.Name, BestFriendName = c.BestFriend.Name })))
{
	Console.WriteLine($"Borrower: {value.Name} BestFriend: {value.BestFriend.Name}")
}
```

Asynchronously find the age of all people in the database

```csharp
var averageAge = await model.People.AverageAsync(c => c.Age);

Console.WriteLine($"Average age is {averageAge}");
```

Delete all people named Steve from the database using LINQ syntax

```csharp

using (var scope = new DataAccessScope())
{
	await model.People.Where(c => c.Name == "Steve").DeleteAsync();
	
	// or
	
	await model.People.DeleteAsync(c => c.Name == "Steve");
	
	Console.WriteLine("Deleted all people named Steve");
	
	await scope.CompleteAsync();
}

```


Asynchronously enumerate all people whos name starts with Steve using fast server-side case-insensitive index

```csharp
using (var enumerator = model.People.Where(c => c.Description.ToLower().StartsWith("steve")).GetAsyncEnumerator())
{
	while (await enumerator.MoveNextAsync())
	{
		Console.WriteLine($"Name: {enumerator.Current.Name}");
	}
}
```

// Query using SELECT FOR UPDATE (lock individual row)

```csharp
using (var scope = new DataAccessScope())
{
	var person = await model.People.SelectForUpdateAsync(c => c.Name == "Steve");
	
	person.Age = 19;
	
	await scope.CompleteAsync();
}
```

Find all people whos name contains 's' server-side two different ways

```csharp
var people1 = await model.People.Where(c => c.Name.IsLike("%s%")).ToListAsync();
var people2 = await model.People.Where(c => c.Name.IndexOf("s") >= 0).ToListAsync();
```

Print the names all people who have a bestfriend who has a bestfriend whos name is "Steve"

```csharp
// Will perform automatic implicit left join on BestFriend
var people = await model
	.People
	.Where(c => c.BestFriend.BestFriend.Name == "Steve")
	.WithEachAsync(Console.WriteLine);
```

Assign a person's best friend without querying for the best friend if you know the object primary keys.

```csharp

using (var scope = new DataAccessScope())
{
	// No query performed
	var person1 = model.People.GeReference(personId);
	
	// No query performed
	person1.BestFriend = model.People.GetReference(bestFriendId);
	
	// A single UPDATE statement is performed
	await scope.CompleteAsync();
}

```

Find all people born in December using server-side date functions

```csharp
var people = await model.Where(c => c.Birthdate.Year == 12).ToListAsync();
```

// Find all people and books that are related using classic linq join syntax

```csharp
var result = await (from book in model.Books
	join person in model.People on book equals person.BorrowedBook
	select new { person }).ToListAsync();
```


// Asynchronously aggregate and enumerate (all people grouped by name with average age)

```csharp
var values = await (from person in model.People
	group person by person.Name
	select new { name = person.Name, AverageAge = person.Average(c => Age) }).GetAsyncEnumerator();
	
while (await values.MoveNextAsync())
{
	var value = values.Current;
	
	Console.WriteLine($"Average age of person with name of {value.Name} is {value.AverageAge}");
}
```




---
Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)
