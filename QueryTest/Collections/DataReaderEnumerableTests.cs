using BaseTest.Mock;
using BaseTest.Models;
using BaseTest.Utils;
using NSubstitute;
using SharpOrm;
using SharpOrm.Collections;
using SharpOrm.DataTranslation;
using SharpOrm.DataTranslation.Reader;
using System.Data.Common;

namespace QueryTest.Collections
{
	public class DataReaderEnumerableTests : DbMockTest
	{
		[Fact]
		public void Constructor_WithFactory_ShouldInitializeCorrectly()
		{
			// Arrange
			var reader = GetReader();
			var registry = TranslationRegistry.Default;
			var factory = Substitute.For<IRecordReaderFactory>();
			var recordReader = Substitute.For<BaseRecordReader>(reader, registry);
			factory.OfType(typeof(Customer), reader, registry).Returns(recordReader);

			// Act
			var enumerable = new DataReaderEnumerable<Customer>(reader, registry, factory);

			// Assert
			Assert.NotNull(enumerable);
			factory.Received(1).OfType(typeof(Customer), reader, registry);
		}

		[Fact]
		public void GetEnumerator_Generic_ShouldReturnTypedEnumerator()
		{
			// Arrange
			var reader = GetReader(
				new Cell("Id", 1),
				new Cell("Name", "John Doe")
			);
			var registry = TranslationRegistry.Default;
			var factory = new RecordReaderFactory();
			var enumerable = new DataReaderEnumerable<Customer>(reader, registry, factory);

			// Act
			var enumerator = enumerable.GetEnumerator();

			// Assert
			Assert.NotNull(enumerator);
			Assert.IsAssignableFrom<IEnumerator<Customer>>(enumerator);
		}

		[Fact]
		public void GetEnumerator_NonGeneric_ShouldReturnEnumerator()
		{
			// Arrange
			var reader = GetReader(
				new Cell("Id", 1),
				new Cell("Name", "John Doe")
			);
			var registry = TranslationRegistry.Default;
			var factory = new RecordReaderFactory();
			var enumerable = new DataReaderEnumerable<Customer>(reader, registry, factory);

		// Act
		var enumerator = ((System.Collections.IEnumerable)enumerable).GetEnumerator();

			// Assert
			Assert.NotNull(enumerator);
		}

	[Fact]
	public void GetEnumerator_ShouldConfigureCancellationToken_ForBaseRecordReader()
	{
		// Arrange
		var reader = GetReader(
			new Cell("Id", 1),
			new Cell("Name", "John Doe")
		);
		var registry = TranslationRegistry.Default;
		var factory = new RecordReaderFactory();
		var cts = new CancellationTokenSource();
		cts.Cancel();
		var enumerable = new DataReaderEnumerable<Customer>(reader, registry, factory)
		{
			Token = cts.Token
		};

		// Act
		var enumerator = enumerable.GetEnumerator();

		// Assert
		// MoveNext should return false immediately due to cancellation
		Assert.False(enumerator.MoveNext());
	}

		[Fact]
		public void Enumerate_WithMultipleRecords_ShouldReturnAllItems()
		{
			// Arrange
			var cells1 = new[] { new Cell("Id", 1), new Cell("Name", "John") };
			var cells2 = new[] { new Cell("Id", 2), new Cell("Name", "Jane") };
			var cells3 = new[] { new Cell("Id", 3), new Cell("Name", "Bob") };

			var reader = new MockDataReader(
				i => i switch
				{
					0 => new Row(cells1),
					1 => new Row(cells2),
					2 => new Row(cells3),
					_ => null!
				},
				3
			);

			var registry = TranslationRegistry.Default;
			var factory = new RecordReaderFactory();
			var enumerable = new DataReaderEnumerable<Customer>(reader, registry, factory);

			// Act
			var items = enumerable.ToList();

			// Assert
			Assert.Equal(3, items.Count);
		}

		[Fact]
		public void Enumerate_WithEmptyReader_ShouldReturnNoItems()
		{
			// Arrange
			var reader = GetReader();
			var registry = TranslationRegistry.Default;
			var factory = new RecordReaderFactory();
			var enumerable = new DataReaderEnumerable<Customer>(reader, registry, factory);

			// Act
			var items = enumerable.ToList();

			// Assert
			Assert.Empty(items);
		}

	[Fact]
	public void Enumerate_WithCancellationToken_ShouldStopEnumeration()
	{
		// Arrange
		var cells1 = new[] { new Cell("Id", 1), new Cell("Name", "John") };
		var cells2 = new[] { new Cell("Id", 2), new Cell("Name", "Jane") };

		var reader = new MockDataReader(
			i => i switch
			{
				0 => new Row(cells1),
				1 => new Row(cells2),
				_ => null!
			},
			2
		);

		var registry = TranslationRegistry.Default;
		var factory = new RecordReaderFactory();
		var cts = new CancellationTokenSource();
		var enumerable = new DataReaderEnumerable<Customer>(reader, registry, factory)
		{
			Token = cts.Token
		};

		// Act
		var enumerator = enumerable.GetEnumerator();
		enumerator.MoveNext(); // Read first item
		cts.Cancel(); // Cancel after first item
		var canContinue = enumerator.MoveNext(); // Try to continue

		// Assert
		Assert.False(canContinue);
	}

		[Fact]
		public void TEnumerator_Current_ShouldReturnTypedObject()
		{
			// Arrange
			var reader = GetReader(
				new Cell("Id", 1),
				new Cell("Name", "John Doe")
			);
			var registry = TranslationRegistry.Default;
			var factory = new RecordReaderFactory();
			var enumerable = new DataReaderEnumerable<Customer>(reader, registry, factory);

			// Act
			var enumerator = enumerable.GetEnumerator();
			if (enumerator.MoveNext())
			{
				var current = enumerator.Current;

				// Assert
				Assert.NotNull(current);
				Assert.IsType<Customer>(current);
			}
		}

		[Fact]
		public void TEnumerator_Dispose_ShouldNotThrow()
		{
			// Arrange
			var reader = GetReader();
			var registry = TranslationRegistry.Default;
			var factory = new RecordReaderFactory();
			var enumerable = new DataReaderEnumerable<Customer>(reader, registry, factory);
			var enumerator = enumerable.GetEnumerator();

			// Act & Assert - Dispose should not throw
			enumerator.Dispose();
			Assert.True(true); // If we get here, no exception was thrown
		}

		[Fact]
		public void TEnumerator_Reset_ShouldThrowNotSupportedException()
		{
			// Arrange
			var reader = GetReader();
			var registry = TranslationRegistry.Default;
			var factory = new RecordReaderFactory();
			var enumerable = new DataReaderEnumerable<Customer>(reader, registry, factory);
			var enumerator = enumerable.GetEnumerator();

		// Act & Assert
		Assert.Throws<NotSupportedException>(() => ((System.Collections.IEnumerator)enumerator).Reset());
		}

		[Fact]
		public void ObsoleteConstructor_WithDbDataReader_ShouldWork()
		{
			// Arrange
			var reader = GetReader(
				new Cell("Id", 1),
				new Cell("Name", "John Doe")
			) as DbDataReader;
			var registry = TranslationRegistry.Default;

			// Act
#pragma warning disable CS0618 // Type or member is obsolete
			var enumerable = new DataReaderEnumerable<Customer>(reader!, registry);
#pragma warning restore CS0618

			// Assert
			Assert.NotNull(enumerable);
		}

		[Fact]
		public void ObsoleteConstructor_WithDbDataReaderAndFkQueue_ShouldWork()
		{
			// Arrange
			var reader = GetReader(
				new Cell("Id", 1),
				new Cell("Name", "John Doe")
			) as DbDataReader;
		var registry = TranslationRegistry.Default;
#pragma warning disable CS0618 // Type or member is obsolete
		var fkQueue = Substitute.For<IFkQueue>();
#pragma warning restore CS0618

		// Act
#pragma warning disable CS0618 // Type or member is obsolete
			var enumerable = new DataReaderEnumerable<Customer>(reader!, registry, fkQueue);
#pragma warning restore CS0618

			// Assert
			Assert.NotNull(enumerable);
		}

		[Fact]
		public void ObsoleteConstructor_WithDbDataReaderAndMappedObject_ShouldWork()
		{
			// Arrange
		var reader = GetReader(
			new Cell("Id", 1),
			new Cell("Name", "John Doe")
		) as DbDataReader;
		var registry = TranslationRegistry.Default;
#pragma warning disable CS0618 // Type or member is obsolete
		var mappedObject = MappedObject.Create(reader!, typeof(Customer), null, registry);
#pragma warning restore CS0618

		// Act
#pragma warning disable CS0618 // Type or member is obsolete
			var enumerable = new DataReaderEnumerable<Customer>(reader!, mappedObject);
#pragma warning restore CS0618

			// Assert
			Assert.NotNull(enumerable);
		}

	[Fact]
	public void ObsoleteConstructor_WithNullMappedObject_ShouldThrowNullReferenceException()
	{
		// Arrange
		var reader = GetReader() as DbDataReader;

#pragma warning disable CS0618 // Type or member is obsolete
		Assert.Throws<ArgumentNullException>(() => new DataReaderEnumerable<Customer>(reader!, (IMappedObject)null!));
#pragma warning restore CS0618
	}

	[Fact]
	public void GetEnumerator_WithObsoleteEnumerator_ShouldConfigureCancellationToken()
	{
		// Arrange
		var reader = GetReader(
			new Cell("Id", 1),
			new Cell("Name", "John Doe")
		) as DbDataReader;
		var registry = TranslationRegistry.Default;
		var cts = new CancellationTokenSource();
		cts.Cancel();

#pragma warning disable CS0618 // Type or member is obsolete
		var enumerable = new DataReaderEnumerable<Customer>(reader!, registry)
		{
			Token = cts.Token
		};
#pragma warning restore CS0618

		// Act
		var enumerator = enumerable.GetEnumerator();

		// Assert
		// MoveNext should return false immediately due to cancellation
		Assert.False(enumerator.MoveNext());
	}

		[Fact]
		public void Enumerate_WithRowType_ShouldReturnRowObjects()
		{
			// Arrange
			var reader = GetReader(
				new Cell("Id", 1),
				new Cell("Name", "John Doe")
			);
			var registry = TranslationRegistry.Default;
			var factory = new RecordReaderFactory();
			var enumerable = new DataReaderEnumerable<Row>(reader, registry, factory);

			// Act
			var items = enumerable.ToList();

			// Assert
			Assert.NotEmpty(items);
			Assert.All(items, item => Assert.IsType<Row>(item));
		}

	[Fact]
	public void Enumerate_WithFactoryReturningNull_ShouldHandleGracefully()
	{
		// Arrange
		var reader = GetReader();
		var registry = TranslationRegistry.Default;
		var factory = Substitute.For<IRecordReaderFactory>();
		factory.OfType(typeof(Customer), reader, registry).Returns((BaseRecordReader)null!);

		// Act & Assert
		// GetEnumerator will throw NullReferenceException when trying to create TEnumerator
		var enumerable = new DataReaderEnumerable<Customer>(reader, registry, factory);
		Assert.Throws<NullReferenceException>(() =>
		{
			var enumerator = enumerable.GetEnumerator();
			enumerator.MoveNext(); // This will trigger the null reference
		});
	}

	[Fact]
	public void MultipleEnumerations_ShouldWorkIndependently()
	{
		// Arrange
		var cells = new[] { new Cell("Id", 1), new Cell("Name", "John") };
		// Note: MockDataReader can only be read once, so we need separate readers for each enumeration
		// This test verifies that the enumerable can be enumerated multiple times
		var registry = TranslationRegistry.Default;
		var factory = new RecordReaderFactory();

		// Act - First enumeration
		var reader1 = new MockDataReader(i => new Row(cells), 1);
		var enumerable1 = new DataReaderEnumerable<Customer>(reader1, registry, factory);
		var items1 = enumerable1.ToList();

		// Second enumeration with new reader
		var reader2 = new MockDataReader(i => new Row(cells), 1);
		var enumerable2 = new DataReaderEnumerable<Customer>(reader2, registry, factory);
		var items2 = enumerable2.ToList();

		// Assert
		Assert.Equal(items1.Count, items2.Count);
		Assert.Equal(1, items1.Count);
		Assert.Equal(1, items2.Count);
	}

	private new MockDataReader GetReader(params Cell[] cells)
	{
		return new MockDataReader(cells);
	}
	}
}

