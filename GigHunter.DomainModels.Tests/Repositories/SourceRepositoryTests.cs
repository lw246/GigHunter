﻿using GigHunter.TestUtilities.Assertors;
using GigHunter.TestUtilities.Database;
using GigHunter.DomainModels.Models;
using GigHunter.DomainModels.Repositories;
using MongoDB.Bson;
using NUnit.Framework;

namespace GigHunter.DomainModels.Tests.Repositories
{
	[TestFixture]
	public class SourceRepositoryTest
	{
		private IRepository<Source> _sourceRepository;
		private MongoDatabaseUtilities<Source> _mongoDatabaseUtilities;
		private Source _testSiteOne;
		private Source _testSiteTwo;
		private Source _testSiteThree;

		[SetUp]
		public void ResetTestSources()
		{
			_sourceRepository = new SourceRepository();
			_mongoDatabaseUtilities = new MongoDatabaseUtilities<Source>("sources");

			_testSiteOne = TestSourceOne();
			_testSiteTwo = TestSourceTwo();
			_testSiteThree = TestSourceThree();
		}

		[Test]
		public void Add_SingleValidSource_ShouldBeInserted()
		{
			_mongoDatabaseUtilities.AddItem(_testSiteOne);

			var retrievedSource = _mongoDatabaseUtilities.FindRecordById(_testSiteOne.Id);

			SiteAssertor.New()
				.Expected(_testSiteOne)
				.Actual(retrievedSource[0])
				.DoAssert();
		}

		[Test]
		public void GetAll_ThreeItemsInCollection_ShouldReturnAllThree()
		{
			_mongoDatabaseUtilities.AddItem(_testSiteOne);
			_mongoDatabaseUtilities.AddItem(_testSiteTwo);
			_mongoDatabaseUtilities.AddItem(_testSiteThree);

			var resultFromDatabase = _sourceRepository.GetAll();

			Assert.AreEqual(3, resultFromDatabase.Count);

			SiteAssertor.New()
				.Expected(_testSiteOne)
				.Actual(resultFromDatabase.Find(a => a.Id == _testSiteOne.Id))
				.DoAssert();

			SiteAssertor.New()
				.Expected(_testSiteTwo)
				.Actual(resultFromDatabase.Find(a => a.Id == _testSiteTwo.Id))
				.DoAssert();

			SiteAssertor.New()
				.Expected(_testSiteThree)
				.Actual(resultFromDatabase.Find(a => a.Id == _testSiteThree.Id))
				.DoAssert();
		}

		[Test]
		public void GetAll_EmptyDatabase_ShouldReturnEmptyList()
		{
			var resultFromDatabase = _sourceRepository.GetAll();
			CollectionAssert.IsEmpty(resultFromDatabase);
		}

		[Test]
		public void GetById_ValidObjectId_ShouldReturnSingleSource()
		{
			_mongoDatabaseUtilities.AddItem(_testSiteOne);
			// This has same details, but different Id to the above
			_mongoDatabaseUtilities.AddItem(TestSourceOne());

			var result = _sourceRepository.GetById(_testSiteOne.Id);

			SiteAssertor.New()
				.Expected(_testSiteOne)
				.Actual(result)
				.DoAssert();
		}

		[Test]
		public void GetById_InvalidObjectId_ShouldReturnEmptyList()
		{
			_mongoDatabaseUtilities.AddItem(_testSiteOne);

			var idToLookFor = "5c72a6f52f44614ba8c56071";
			var result = _sourceRepository.GetById(idToLookFor);

			Assert.IsNull(result);
		}

		[Test]
		public void GetByName_ValidName_ShouldReturnSingleSource()
		{
			_mongoDatabaseUtilities.AddItem(_testSiteOne);
			// This has same details, but different Id to the above
			_mongoDatabaseUtilities.AddItem(_testSiteTwo);

			var result = _sourceRepository.GetByName(_testSiteOne.Name);

			Assert.AreEqual(1, result.Count);
			SiteAssertor.New()
				.Expected(_testSiteOne)
				.Actual(result[0])
				.DoAssert();
		}

		[Test]
		public void GetByName_ValidNameMultiplePresent_ShouldReturnMultipleSources()
		{
			_mongoDatabaseUtilities.AddItem(_testSiteOne);
			// This has same details, but different Id to the above
			_mongoDatabaseUtilities.AddItem(TestSourceOne());

			var result = _sourceRepository.GetByName(_testSiteOne.Name);

			Assert.AreEqual(2, result.Count);
			
			foreach(var source in result)
			{
				Assert.AreEqual(_testSiteOne.Name, source.Name);
			}

			Assert.AreNotEqual(result[0].Id, result[1].Id);
		}

		[Test]
		public void GetByName_InvalidName_ShouldReturnSingleSource()
		{
			_mongoDatabaseUtilities.AddItem(_testSiteOne);
			// This has same details, but different Id to the above
			_mongoDatabaseUtilities.AddItem(TestSourceOne());

			var result = _sourceRepository.GetByName("invalidName");

			Assert.AreEqual(0, result.Count);
		}

		[Test]
		public void UpdateById_ValidObjectId_ShouldUpdateAndReturnTrue()
		{
			_mongoDatabaseUtilities.AddItem(_testSiteOne);
			_mongoDatabaseUtilities.AddItem(_testSiteTwo);

			_testSiteOne.Name = "Altered Name One";
			_testSiteOne.BaseUrl = "https://www.changedurlone.com";

			var result = _sourceRepository.UpdateById(_testSiteOne.Id, _testSiteOne);
			Assert.IsTrue(result);

			var recordFromDatabase = _mongoDatabaseUtilities.FindRecordById(_testSiteOne.Id);
			SiteAssertor.New()
				.Expected(_testSiteOne)
				.Actual(recordFromDatabase[0])
				.DoAssert();
		}

		[Test]
		public void UpdateById_InvalidObjectId_ShouldReturnFalseAndNotUpdate()
		{
			// Setup
			_mongoDatabaseUtilities.AddItem(_testSiteOne);

			var updatedDetails = new Source
			{
				Name = "Altered Name One",
				BaseUrl = "https://www.changedurlone.com"
			};

			// Perform
			var invalidObjectId = "5c72a6f52f44614ba8c56071";
			var result = _sourceRepository.UpdateById(invalidObjectId, _testSiteOne);

			// Verify
			Assert.IsFalse(result);

			var recordFromDatabase = _mongoDatabaseUtilities.FindRecordById(_testSiteOne.Id);
			SiteAssertor.New()
				.Expected(_testSiteOne)
				.Actual(recordFromDatabase[0])
				.DoAssert();
		}

		[Test]
		public void DeleteById_ValidObjectId_ShouldBeDeletedAndReturnNumberOfRecordsDeleted()
		{
			_mongoDatabaseUtilities.AddItem(_testSiteOne);
			_mongoDatabaseUtilities.AddItem(_testSiteTwo);

			var countBefore = _mongoDatabaseUtilities.CountRecordsInCollection();

			// Perform
			var result = _sourceRepository.DeleteById(_testSiteOne.Id);

			// Verify
			Assert.IsTrue(result);

			var countAfter = _mongoDatabaseUtilities.CountRecordsInCollection();
			Assert.AreEqual(countBefore - 1, countAfter);

			var findResult = _mongoDatabaseUtilities.FindRecordById(_testSiteOne.Id);
			CollectionAssert.IsEmpty(findResult);
		}

		[Test]
		public void DeleteById_InvalidObjectId_ShouldReturnZeroAndNotDeleteAnything()
		{
			_mongoDatabaseUtilities.AddItem(_testSiteOne);
			_mongoDatabaseUtilities.AddItem(_testSiteTwo);

			var countBefore = _mongoDatabaseUtilities.CountRecordsInCollection();

			// Perform
			var invalidId = "5c72a6f52f44614ba8c56071";
			var result = _sourceRepository.DeleteById(invalidId);

			// Verify
			Assert.IsFalse(result);

			var countAfter = _mongoDatabaseUtilities.CountRecordsInCollection();
			Assert.AreEqual(countBefore, countAfter);
		}

		[TearDown]
		public void DropSourceCollection()
		{
			_mongoDatabaseUtilities.RemoveCollection();
		}

		#region Test Data
		private static Source TestSourceOne()
		{
			return new Source
			{
				Name = "SeeTickets",
				BaseUrl = "https://www.seetickets.com"
			};
		}

		private static Source TestSourceTwo()
		{
			return new Source
			{
				Name = "TicketMaster",
				BaseUrl = "https://www.ticketmaster.com"
			};
		}

		private static Source TestSourceThree()
		{
			return new Source
			{
				Name = "Ents24",
				BaseUrl = "https://www.ents24.com"
			};
		}
		#endregion
	}
}
