using Common.DotNet.Test;
using jcoliz.OfficeOpenXml.Serializer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.SampleData;
using CNTSampleData = Common.DotNet.Test.SampleData;

namespace YoFi.SampleGen.Tests.Unit
{
    [TestClass]
    public class GeneratorActionTest
    {
        public TestContext TestContext { get; set; }

        SampleDataGenerator generator;

        [TestInitialize]
        public void SetUp()
        {
            generator = new SampleDataGenerator();
            SampleDataPattern.Year = 2022;
        }

        [TestMethod]
        public void Loader()
        {
            var stream = SampleData.Open("TestData1.xlsx");

            generator.LoadDefinitions(stream);

            Assert.AreEqual(31, generator.Definitions.Count);

            Assert.AreEqual(6, generator.Definitions.Count(x => x.DateFrequency == FrequencyEnum.Quarterly));
            Assert.AreEqual(2, generator.Definitions.Count(x => x.AmountJitter == JitterEnum.Moderate));
        }

        [TestMethod]
        public void Generator()
        {
            Loader();

            generator.GenerateTransactions();

            Assert.AreEqual(435, generator.Transactions.Count);
            Assert.AreEqual(24, generator.Transactions.Count(x => x.Payee == "Big Megacorp"));
        }

        [TestMethod]
        public void PayeeGenerator()
        {
            Loader();

            generator.GeneratePayees();

            Assert.AreEqual(21, generator.Payees.Count);
        }

        [TestMethod]
        public void Writer()
        {
            Generator();

            using var stream = new MemoryStream();
            generator.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-Generator-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);

            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            var actual = reader.Deserialize<Transaction>();

            Assert.AreEqual(435, actual.Count());
            Assert.AreEqual(24, actual.Count(x => x.Payee == "Big Megacorp"));
        }

        [TestMethod]
        public void Splits()
        {
            Generator();

            using var stream = new MemoryStream();
            generator.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-Generator-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);

            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            var actual = reader.Deserialize<Split>("Split");

            Assert.AreEqual(312, actual.Count());
        }

        [TestMethod]
        public void SplitsMatch()
        {
            Generator();

            using var stream = new MemoryStream();
            generator.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-Generator-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);

            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            var transactions = reader.Deserialize<Transaction>().ToList();
            var splits = reader.Deserialize<Split>("Split");

            var lookup = splits.Where(x => x.TransactionID != 0).ToLookup(x => x.TransactionID);
            foreach(var group in lookup)
            {
                transactions.Where(x => x.ID == group.Key).Single().Splits = group.ToList();
            }

            Assert.AreEqual(36, transactions.Count(x => x.Splits?.Count > 1));
        }

        [TestMethod]
        public void PayeeWriter()
        {
            Generator();

            generator.GeneratePayees();

            using var stream = new MemoryStream();
            generator.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-Generator-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);

            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            var actual = reader.Deserialize<Payee>();

            Assert.AreEqual(21, actual.Count());
        }

        [TestMethod]
        public void BudgetWriter()
        {
            Generator();

            generator.GenerateBudget();

            using var stream = new MemoryStream();
            generator.Save(stream);

            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-Generator-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);

            stream.Seek(0, SeekOrigin.Begin);
            using var reader = new SpreadsheetReader();
            reader.Open(stream);
            var actual = reader.Deserialize<BudgetTx>();

            Assert.AreEqual(32, actual.Count());
        }

        [TestMethod]
        public void GenerateFullSampleData()
        {
            var instream = CNTSampleData.Open("FullSampleDataDefinition.xlsx");
            generator.LoadDefinitions(instream);
            generator.GenerateTransactions();
            generator.GeneratePayees();
            generator.GenerateBudget();

            using var stream = new MemoryStream();
            generator.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var year = SampleDataPattern.Year;
            Directory.CreateDirectory(year.ToString());
            var filename = $"{year}/SampleData-Full.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);
        }

        [TestMethod]
        public void GenerateUploadSampleData()
        {

            var instream = SampleData.Open("UploadSampleDataDefinition.xlsx");
            generator.LoadDefinitions(instream);
            generator.GenerateTransactions();
            generator.GeneratePayees();
            generator.GenerateBudget();

            using var stream = new MemoryStream();
            generator.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var filename = $"Test-Generator-{TestContext.TestName}.xlsx";
            File.Delete(filename);
            using var outstream = File.OpenWrite(filename);
            stream.CopyTo(outstream);
            TestContext.AddResultFile(filename);
        }

#if false        
        [TestMethod]
        public async Task GenerateJson()
        {
            var instream = CNDSampleData.Open("FullSampleDataDefinition.xlsx");
            generator.LoadDefinitions(instream);
            generator.GenerateTransactions(addids:false);
            generator.GeneratePayees();
            generator.GenerateBudget();

            var store = new SampleDataStore()
            {
                Transactions = generator.Transactions,
                Payees = generator.Payees,
                BudgetTxs = generator.BudgetTxs
            };

            var filename = "FullSampleData.json";
            File.Delete(filename);
            {
                using var jsonstream = File.OpenWrite(filename);
                await store.SerializeAsync(jsonstream);
            }
            TestContext.AddResultFile(filename);

            var newstore = new SampleDataStore();
            using var newstream = File.OpenRead(filename);
            await newstore.DeSerializeAsync(newstream);

            CollectionAssert.AreEqual(store.Transactions, newstore.Transactions);
            CollectionAssert.AreEqual(store.Payees, newstore.Payees);
            CollectionAssert.AreEqual(store.BudgetTxs, newstore.BudgetTxs);
        }
#endif

        [TestMethod]
        public async Task GenerateOfx()
        {
            Generator();
            var items = generator.Transactions.Where(x => x.Timestamp.Month == 12).ToList();

            var filename = "Generated.ofx";
            File.Delete(filename);
            {
                using var writestream = File.OpenWrite(filename);
                SampleDataOfx.WriteToOfx(items, writestream);
            }
            TestContext.AddResultFile(filename);

            var instream = File.OpenRead(filename);
            var Document = await OfxSharp.OfxDocumentReader.FromSgmlFileAsync(instream);
            var imported = Document.Statements.SelectMany(x => x.Transactions).Select(
                tx => new Transaction()
                {
                    Amount = tx.Amount,
                    Payee = tx.Memo?.Trim(),
                    BankReference = tx.ReferenceNumber?.Trim(),
                    Timestamp = tx.Date.Value.DateTime
                }
            ).ToList();

            CollectionAssert.AreEqual(items, imported);
        }

        [TestMethod]
        public void GenerateFullSampleOfx()
        {
            var instream = CNTSampleData.Open("FullSampleDataDefinition.xlsx");
            generator.LoadDefinitions(instream);
            generator.GenerateTransactions();

            foreach(var month in Enumerable.Range(1,12))
            {
                var items = generator.Transactions.Where(x => x.Timestamp.Month == month);
                var filename = $"FullSampleData-Month{month:D2}.ofx";
                File.Delete(filename);
                using var writestream = File.OpenWrite(filename);
                SampleDataOfx.WriteToOfx(items, writestream);
                TestContext.AddResultFile(filename);
            }
        }
    }
}
