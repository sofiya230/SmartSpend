using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using YoFi.Core.Models;

namespace YoFi.SampleGen.Tests.Unit
{
    [TestClass]
    public class GeneratorDefinitionTest
    {
        [TestInitialize]
        public void SetUp()
        {
            SampleDataPattern.Year = 2021;
        }

        [TestCleanup]
        public void Cleanup()
        {
            SampleDataPattern.Year = DateTime.Now.Year;
        }

        #region Helpers
        private int NumPeriodsFor(FrequencyEnum scheme) => SampleDataPattern.FrequencyPerYear[scheme];

        private IEnumerable<Transaction> SimpleTest(FrequencyEnum scheme, JitterEnum datejitter = JitterEnum.None, int numperperiod = 1)
        {
            var periods = NumPeriodsFor(scheme);
            var periodicamount = 100m;
            var amount = periodicamount * periods * numperperiod;
            var item = new SampleDataPattern() { DateFrequency = scheme, AmountYearly = amount, AmountJitter = JitterEnum.None, DateJitter = datejitter, DateRepeats = numperperiod, Category = "Category", Payee = "Payee" };

            var actual = item.GetTransactions();

            Assert.AreEqual(periods * numperperiod, actual.Count());

            foreach (var result in actual)
            {
                Assert.AreEqual(periodicamount, result.Amount);

                Assert.AreEqual(item.Payee, result.Payee);
                Assert.AreEqual(item.Category, result.Category);
            }

            return actual;
        }
        #endregion

        #region Invididual features

        [TestMethod]
        public void MultiplePayees()
        {
            var payees = new List<string>() { "First", "Second", "Third" };
            var item = new SampleDataPattern() { DateFrequency = FrequencyEnum.Weekly, AmountYearly = 5200m, AmountJitter = JitterEnum.None, DateJitter = JitterEnum.None, Category = "Category", Payee = string.Join(",",payees) };

            var actual = item.GetTransactions();

            Assert.IsTrue(actual.Any(x => x.Payee != actual.First().Payee));

            Assert.IsTrue(actual.All(x => payees.Contains(x.Payee)));
        }

        #endregion


        #region No Jitter

        [TestMethod]
        public void MonthlySimple()
        {
            var actual = SimpleTest(FrequencyEnum.Monthly);

            Assert.IsTrue(actual.All(x => x.Timestamp.Day == actual.First().Timestamp.Day));
        }

        [TestMethod]
        public void YearlySimple()
        {
            SimpleTest(FrequencyEnum.Yearly);
        }

        [TestMethod]
        public void SemiMonthlySimple()
        {
            var actual = SimpleTest(FrequencyEnum.SemiMonthly);

            Assert.IsTrue(actual.All(x => x.Timestamp.Day == 1 || x.Timestamp.Day == 15));
        }

        [TestMethod]
        public void QuarterlySimple()
        {
            var year = 2000;
            SampleDataPattern.Year = year;
            var actual = SimpleTest(FrequencyEnum.Quarterly);

            var firstdayofquarter = Enumerable.Range(1, 4).Select(x => new DateTime(year, x * 3 - 2, 1));
            var daysofquarter = actual.Select(x => x.Timestamp.DayOfYear - firstdayofquarter.Last(y => x.Timestamp >= y).DayOfYear);
            Assert.IsTrue(daysofquarter.All(x => x == daysofquarter.First()));
        }

        [TestMethod]
        public void WeeklySimple()
        {
            var actual = SimpleTest(FrequencyEnum.Weekly);

            Assert.IsTrue(actual.All(x => x.Timestamp.DayOfWeek == actual.First().Timestamp.DayOfWeek));
        }

        [TestMethod]
        public void ManyPerWeekSimple()
        {
            var actual = SimpleTest(FrequencyEnum.Weekly, datejitter:JitterEnum.High, numperperiod: 3);

            Assert.IsTrue(actual.Any(x => x.Timestamp.DayOfWeek != actual.First().Timestamp.DayOfWeek));
        }
        #endregion

        #region Amount Jitter

        [DataRow(JitterEnum.Low)]
        [DataRow(JitterEnum.Moderate)]
        [DataRow(JitterEnum.High)]
        [DataTestMethod]
        public void YearlyAmountJitter(JitterEnum jitter)
        {
            var amount = 1234.56m;
            var item = new SampleDataPattern() { DateFrequency = FrequencyEnum.Yearly, AmountYearly = amount, AmountJitter = jitter, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            var numtries = 100;
            var actual = Enumerable.Repeat(1, numtries).SelectMany(x => item.GetTransactions());

            Assert.AreEqual(numtries, actual.Count());

            Assert.IsTrue(actual.Any(x => x.Amount != actual.First().Amount));

            var jittervalue = SampleDataPattern.AmountJitterValues[jitter];
            var min = actual.Min(x => x.Amount);
            var max = actual.Max(x => x.Amount);
            Assert.AreEqual(((double)amount * (1 - jittervalue)), (double)min, (double)amount * jittervalue / 5.0);
            Assert.AreEqual(((double)amount * (1 + jittervalue)), (double)max, (double)amount * jittervalue / 5.0);
        }

        [DataRow(JitterEnum.Low)]
        [DataRow(JitterEnum.Moderate)]
        [DataRow(JitterEnum.High)]
        [DataTestMethod]
        public void MonthlyAmountJitterOnce(JitterEnum jitter)
        {
            var amount = 100.00m;
            var item = new SampleDataPattern() { DateFrequency = FrequencyEnum.Monthly, AmountYearly = 12 * amount, AmountJitter = jitter, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            var actual = item.GetTransactions();

            Assert.AreEqual(12, actual.Count());

            Assert.IsTrue(actual.Any(x => x.Amount != actual.First().Amount));

            var jittervalue = SampleDataPattern.AmountJitterValues[jitter];
            var min = actual.Min(x => x.Amount);
            var max = actual.Max(x => x.Amount);
            Assert.IsTrue(min >= amount * (1 - (decimal)jittervalue));
            Assert.IsTrue(max <= amount * (1 + (decimal)jittervalue));
        }

        [DataRow(FrequencyEnum.Monthly, JitterEnum.Low)]
        [DataRow(FrequencyEnum.Monthly, JitterEnum.Moderate)]
        [DataRow(FrequencyEnum.Monthly, JitterEnum.High)]
        [DataRow(FrequencyEnum.Quarterly, JitterEnum.Low)]
        [DataRow(FrequencyEnum.Quarterly, JitterEnum.Moderate)]
        [DataRow(FrequencyEnum.Quarterly, JitterEnum.High)]
        [DataRow(FrequencyEnum.Weekly, JitterEnum.Low)]
        [DataRow(FrequencyEnum.Weekly, JitterEnum.Moderate)]
        [DataRow(FrequencyEnum.Weekly, JitterEnum.High)]
        [DataTestMethod]
        public void AmountJitterMany(FrequencyEnum scheme, JitterEnum jitter)
        {
            var periods = NumPeriodsFor(scheme);
            var amount = 100.00m;
            var item = new SampleDataPattern() { DateFrequency = scheme, AmountYearly = periods * amount, AmountJitter = jitter, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            var numtries = 100;
            var actual = Enumerable.Repeat(1, numtries).SelectMany(x => item.GetTransactions());

            Assert.IsTrue(actual.Any(x => x.Amount != actual.First().Amount));

            var jittervalue = SampleDataPattern.AmountJitterValues[jitter];
            var min = actual.Min(x => x.Amount);
            var max = actual.Max(x => x.Amount);
            Assert.AreEqual(((double)amount * (1 - jittervalue)), (double)min, (double)amount * (double)jittervalue / 5.0);
            Assert.AreEqual(((double)amount * (1 + jittervalue)), (double)max, (double)amount * (double)jittervalue / 5.0);
        }

        #endregion 

        #region Date Jitter

        [DataRow(JitterEnum.Low)]
        [DataRow(JitterEnum.Moderate)]
        [DataRow(JitterEnum.High)]
        [DataTestMethod]
        public void MonthlyDateJitterOnce(JitterEnum jitter)
        {
            var scheme = FrequencyEnum.Monthly;
            var actual = SimpleTest(scheme, datejitter:jitter);

            Assert.IsTrue(actual.Any(x => x.Timestamp.Day != actual.First().Timestamp.Day));

            var jittervalue = SampleDataPattern.DateJitterValues[jitter];
            var min = actual.Min(x => x.Timestamp.Day);
            var max = actual.Max(x => x.Timestamp.Day);
            var actualrange = max - min;
            var expectedrange = SampleDataPattern.SchemeTimespans[scheme].Days * (double)jittervalue;
            Assert.IsTrue(actualrange <= expectedrange);
        }

        [DataRow(JitterEnum.Low)]
        [DataRow(JitterEnum.Moderate)]
        [DataRow(JitterEnum.High)]
        [DataTestMethod]
        public void QuarterlyDateJitterOnce(JitterEnum jitter)
        {
            var year = 2000;
            SampleDataPattern.Year = year;
            var scheme = FrequencyEnum.Quarterly;
            var actual = SimpleTest(scheme, datejitter: jitter);

            var firstdayofquarter = Enumerable.Range(1, 4).Select(x => new DateTime(year, x * 3 - 2, 1));
            var daysofquarter = actual.Select(x => x.Timestamp.DayOfYear - firstdayofquarter.Last(y => x.Timestamp >= y).DayOfYear);
            Assert.IsTrue(daysofquarter.Any(x => x != daysofquarter.First()));

            var jittervalue = SampleDataPattern.DateJitterValues[jitter];
            var min = daysofquarter.Min();
            var max = daysofquarter.Max();
            var actualrange = max - min;
            var expectedrange = SampleDataPattern.SchemeTimespans[scheme].Days * (double)jittervalue;
            Assert.IsTrue(actualrange <= expectedrange);

        }

        [DataRow(JitterEnum.Moderate)]
        [DataRow(JitterEnum.High)]
        [DataTestMethod]
        public void WeeklyDateJitterOnce(JitterEnum jitter)
        {
            var year = 2000;
            SampleDataPattern.Year = year;
            var scheme = FrequencyEnum.Weekly;
            var actual = SimpleTest(scheme, datejitter: jitter);

            Assert.IsTrue(actual.Any(x => x.Timestamp.DayOfWeek != actual.First().Timestamp.DayOfWeek));


            var firstdayofweek = Enumerable.Range(0, 52).Select(x => new DateTime(year, 1, 1) + TimeSpan.FromDays(7*x));
            var daysofweek = actual.Select(x => x.Timestamp.DayOfYear - firstdayofweek.Last(y => x.Timestamp >= y).DayOfYear);
            var min = daysofweek.Min();
            var max = daysofweek.Max();
            var actualrange = max - min;

            var jittervalue = SampleDataPattern.DateJitterValues[jitter];
            var expectedrange = SampleDataPattern.SchemeTimespans[scheme].Days * (double)jittervalue;

            Assert.IsTrue(actualrange <= expectedrange);

        }
        #endregion

        #region Splits

        [DataRow(FrequencyEnum.SemiMonthly)]
        [DataRow(FrequencyEnum.Monthly)]
        [DataRow(FrequencyEnum.Quarterly)]
        [DataRow(FrequencyEnum.Weekly)]
        [DataRow(FrequencyEnum.Yearly)]
        [DataTestMethod]
        public void Splits(FrequencyEnum scheme)
        {
            var periods = NumPeriodsFor(scheme);
            var periodicamount = 100m;
            var item = new SampleDataPattern() { DateFrequency = scheme, DateJitter = JitterEnum.None, Payee = "Payee" };

            var splits = new List<SampleDataPattern>()
            {
                new SampleDataPattern() { Category = "1", AmountYearly = periodicamount * periods * -1, DateFrequency = scheme, AmountJitter = JitterEnum.None },
                new SampleDataPattern() { Category = "2", AmountYearly = periodicamount * periods * -2, DateFrequency = scheme, AmountJitter = JitterEnum.None },
                new SampleDataPattern() { Category = "3", AmountYearly = periodicamount * periods * -3, DateFrequency = scheme, AmountJitter = JitterEnum.None },
                new SampleDataPattern() { Category = "4", AmountYearly = periodicamount * periods * 10, DateFrequency = scheme, AmountJitter = JitterEnum.None },
            };

            var actual = item.GetTransactions(splits);

            Assert.AreEqual(periods, actual.Count());

            foreach (var result in actual)
            {
                Assert.AreEqual(periodicamount * (10 - 3 - 2 - 1), result.Amount);

                Assert.AreEqual(item.Payee, result.Payee);

                int i = splits.Count();
                while(i-- > 0)
                {
                    Assert.AreEqual(splits[i].Category, result.Splits.Skip(i).First().Category);

                    Assert.AreEqual(splits[i].AmountYearly / periods, result.Splits.Skip(i).First().Amount);
                }
            }
        }

        [TestMethod]
        public void Loan()
        {
            var payment = -1581.02m;
            var item = new SampleDataPattern()
            {
                DateFrequency = FrequencyEnum.Monthly,
                DateJitter = JitterEnum.None,
                AmountJitter = JitterEnum.None,
                Category = "Principal",
                AmountYearly = payment * 12,
                Payee = "The Bank",
                Loan = "{ \"interest\": \"Interest\", \"amount\": 375000, \"rate\": 3, \"term\": 360, \"origination\": \"1/1/2010\" }"
            };

            SampleDataPattern.Year = 2021;

            var actual = item.GetTransactions();

            Assert.AreEqual(12, actual.Count());

            foreach (var result in actual)
            {
                Assert.AreEqual(payment, result.Amount);

                Assert.AreEqual(item.Payee, result.Payee);

                Assert.IsNotNull(result.Splits);

                Assert.AreEqual(2, result.Splits.Count);

                Assert.AreEqual(-670.0, (double)(result.Splits.Where(x => x.Category == "Interest").Single().Amount),20.0);

                Assert.AreEqual(-900.0, (double)(result.Splits.Where(x => x.Category == "Principal").Single().Amount),20.0);
            }
        }
        #endregion

        #region Others

        [TestMethod]
        public void Rounding()
        {
            var amount = 100m;
            var item = new SampleDataPattern() { DateFrequency = FrequencyEnum.Monthly, AmountYearly = amount, AmountJitter = JitterEnum.None, DateJitter = JitterEnum.None, Category = "Category", Payee = "Payee" };

            var actual = item.GetTransactions();

            Assert.AreEqual(8.33m, actual.First().Amount);
        }

        #endregion
    }
}
