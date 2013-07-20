Shaolinq
========

Shaolinq is a powerful ORM and Linq provider for C# and .NET. It provides a  thoughtful, fast and powerful alternative to Linq to SQL and the Entity Framework.

Notable features:

 * Code first object model
 * First class LINQ support
 * Automatic schema creation and migration
 * Natural object model architecture with both high-level and super-fine-grained access to SQL via LINQ
 * Insanely fast performance by using dynamically generated code (System.Reflection.Emit) to avoid super slow dynamic reflection calls (dynamic calls is used by most other ORMs)
 * Automatic similar LINQ query match & cache to avoid slow labmda expression compile times
 * Truely abstract object and query model: single code based, multiple backends
 * One-Many and Many-Many relations supported within the object model and LINQ
 * Advanced LINQ support including natural support of server side functions and operations including SQL LIKE and date time conversions.
 * Support for not just selecting but deleting sets of objects using LINQ based syntax
 * First class support for Sqlite, MySql and Postgres
 * Support for client-side computed property values: Computed properties are properties that are based on a combination of one or more other properties but are stored denormalised in their own column for faster (indexed) lookups.
 * Easily support new providers with full LINQ support (less than 100 lines of code)
 * Natural transaction model. Object graphs are saved together and saving objects individually is a thing of the past
 * Nested transactions support
 * Partial object updates designed into the framework from day one: Only the columns that have changed will be updated on the server
 
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
 	* LINQ providers that only provide basic SELECT/WHERE support are pointless and lame
 * Make it super easy to support new databases
 	* Use ADO.NET as the underlying data provider
 	* Adding new providers to the ORM should not require understanding LINQ, expression trees, etc.
 * Support Sqlite, MySql and Postgres out of the box
 * Require only a configuration change to switch underlying database providers
 

---
Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)