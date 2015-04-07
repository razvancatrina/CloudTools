﻿// <copyright file="HandleMessagesTests.cs" company="nett">
//      Copyright (c) 2015 All Right Reserved, http://q.nett.gr
//      Please see the License.txt file for more information. All other rights reserved.
// </copyright>
// <author>James Kavakopoulos</author>
// <email>ofthetimelords@gmail.com</email>
// <date>2015/02/06</date>
// <summary>
// 
// </summary>


using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TheQ.Utilities.CloudTools.Azure;
using TheQ.Utilities.CloudTools.Azure.ExtendedQueue;
using TheQ.Utilities.CloudTools.Storage.ExtendedQueue;
using TheQ.Utilities.CloudTools.Storage.ExtendedQueue.Decorators;
using TheQ.Utilities.CloudTools.Storage.Models;
using TheQ.Utilities.CloudTools.Tests.Integration.Azure.Mocks;
using TheQ.Utilities.CloudTools.Tests.Integration.Azure.Models;

namespace TheQ.Utilities.CloudTools.Tests.Integration.Azure.ConcurrencyTests
{
	[TestClass]
	public class ExtendedQueueTests
	{
		[TestCategory("Integration - ExtendedQueue")]
		[TestMethod]
		public void TestSerial_NormalProcessing()
		{
			// Arrange
			const int runCount = 100;	
			var client = new CloudEnvironment();
			var overflow = client.BlobClient.GetContainerReference("overflownqueues-1");
			var queue = client.QueueClient.GetQueueReference("test1");
			var result = string.Empty;
			var expected = string.Empty;
			var sw = new Stopwatch();
			var factory = new AzureExtendedQueueFactory(new AzureBlobContainer(overflow), new ConsoleLogService());
			var equeue = factory.Create(new AzureQueue(queue));

			for (var i = 0; i < runCount; i++) expected += i.ToString(CultureInfo.InvariantCulture);

			using (var mre = new ManualResetEvent(false))
			{
				var options = new HandleSerialMessageOptions(
					TimeSpan.FromSeconds(0),
					TimeSpan.FromMinutes(2),
					TimeSpan.FromSeconds(30),
					5,
					new CancellationToken(),
					message =>
					{
						if (message.GetMessageContents<string>() == "END")
						{
							mre.Set();
							return true;
						}

						result += message.GetMessageContents<string>();
						return true;
					},
					null,
					ex => { throw ex; });

				// Act
				sw.Start();
				queue.CreateIfNotExists();
				overflow.CreateIfNotExists();
				queue.Clear();
				for (var i = 0; i < runCount; i++) equeue.AddMessageEntity(i.ToString(CultureInfo.InvariantCulture));
				equeue.AddMessageEntity("END");
				equeue.HandleMessagesAsync(options);

				// Assert
				mre.WaitOne();
				sw.Stop();
				Trace.WriteLine("Total execution time (in seconds): " + sw.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
				Assert.AreEqual(expected, result);
			}
		}



		[TestCategory("Integration - ExtendedQueue")]
		[TestMethod]
		public void TestSerial_ParallelProcessing()
		{
			// Arrange
			const int runCount = 100;
			var client = new CloudEnvironment();
			var queue = client.QueueClient.GetQueueReference("test2");
			var overflow = client.BlobClient.GetContainerReference("overflownqueues-2");
			var locking = new object();
			var result = string.Empty;
			var expected = string.Empty;
			var sw = new Stopwatch();
			long actuallyRun = 0;
			var factory = new AzureExtendedQueueFactory(new AzureBlobContainer(overflow), new ConsoleLogService());
			var equeue = factory.Create(new AzureQueue(queue));


			for (var i = 1; i < runCount + 1; i++) expected += ((char)(i)).ToString(CultureInfo.InvariantCulture);

			using (var mre = new ManualResetEvent(false))
			{
				var options = new HandleParallelMessageOptions(
					TimeSpan.FromSeconds(0),
					TimeSpan.FromMinutes(2),
					TimeSpan.FromSeconds(30),
					5,
					50,
					new CancellationToken(),
					message =>
					{
						lock (locking)
						{
							var character = message.GetMessageContents<string>();
							result += character;
						}

						if (Interlocked.Increment(ref actuallyRun) == runCount) mre.Set();

						return true;
					},
					null,
					ex => { throw ex; });

				// Act
				sw.Start();
				queue.CreateIfNotExists();
				overflow.CreateIfNotExists();
				queue.Clear();
				for (var i = 1; i < runCount + 1; i++) equeue.AddMessageEntity(((char)(i)).ToString(CultureInfo.InvariantCulture));
				equeue.HandleMessagesInParallelAsync(options);

				// Assert
				mre.WaitOne();
				sw.Stop();
				Trace.WriteLine("Total execution time (in seconds): " + sw.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
				Assert.IsTrue(expected.All(c => result.Contains(c)));
			}
		}



		[TestCategory("Integration - ExtendedQueue")]
		[TestMethod]
		public void TestSerial_BatchProcessing()
		{
			// Arrange
			const int runCount = 100;
			var client = new CloudEnvironment();
			var queue = client.QueueClient.GetQueueReference("test3");
			var overflow = client.BlobClient.GetContainerReference("overflownqueues-3");
			var locking = new object();
			var result = string.Empty;
			var expected = string.Empty;
			var sw = new Stopwatch();
			long actuallyRun = 0;
			var factory = new AzureExtendedQueueFactory(new AzureBlobContainer(overflow), new ConsoleLogService());
			var equeue = factory.Create(new AzureQueue(queue));

			for (var i = 1; i < runCount + 1; i++) expected += ((char)(i)).ToString(CultureInfo.InvariantCulture);

			using (var mre = new ManualResetEvent(false))
			{
				var options = new HandleBatchMessageOptions(
					TimeSpan.FromSeconds(0),
					TimeSpan.FromMinutes(2),
					TimeSpan.FromSeconds(30),
					5,
					50,
					new CancellationToken(),
					messages =>
					{
						lock (locking)
						{
							foreach (var message in messages)
							{
								var character = message.GetMessageContents<string>();
								result += character;

								if (Interlocked.Increment(ref actuallyRun) == runCount) mre.Set();
							}
						}

						return messages;
					},
					null,
					ex => { throw ex; });

				// Act
				sw.Start();
				queue.CreateIfNotExists();
				overflow.CreateIfNotExists();
				queue.Clear();
				for (var i = 1; i < runCount + 1; i++) equeue.AddMessageEntity(((char)(i)).ToString(CultureInfo.InvariantCulture));
				equeue.HandleMessagesInBatchAsync(options);

				// Assert
				mre.WaitOne();
				sw.Stop();
				Trace.WriteLine("Total execution time (in seconds): " + sw.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
				Assert.IsTrue(expected.All(c => result.Contains(c)));
			}
		}



		[TestCategory("Integration - ExtendedQueue")]
		[TestMethod]
		public void TestOverflownMessages()
		{
			// Arrange
			var client = new CloudEnvironment();
			var queue = client.QueueClient.GetQueueReference("test4");
			var overflow = client.BlobClient.GetContainerReference("overflownqueues-4");
			var result = string.Empty;
			var rnd = new Random();
			var expected = new string(Enumerable.Range(1, 128 * 1024).Select(r => (char)rnd.Next(1024, 4096)).ToArray());
			var sw = new Stopwatch();
			var factory = new AzureExtendedQueueFactory(new AzureBlobContainer(overflow), new ConsoleLogService());
			var equeue = factory.Create(new AzureQueue(queue));

			using (var mre = new ManualResetEvent(false))
			{
				var options = new HandleSerialMessageOptions(
					TimeSpan.FromSeconds(0),
					TimeSpan.FromMinutes(2),
					TimeSpan.FromSeconds(30),
					5,
					new CancellationToken(),
					message =>
					{
						result = message.GetMessageContents<string>();
						mre.Set();
						return true;
					},
					null,
					ex => { throw ex; });

				// Act
				sw.Start();
				queue.CreateIfNotExists();
				overflow.CreateIfNotExists();
				queue.Clear();
				equeue.AddMessageEntity(expected);
				equeue.HandleMessagesAsync(options);

				// Assert
				mre.WaitOne();
				sw.Stop();
				Trace.WriteLine("Total execution time (in seconds): " + sw.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
				Assert.AreEqual(expected, result);
			}
		}



		[TestCategory("Integration - ExtendedQueue")]
		[TestMethod]
		public void TestSerializedMessages()
		{
			// Arrange
			var client = new CloudEnvironment();
			var queue = client.QueueClient.GetQueueReference("test5");
			var overflow = client.BlobClient.GetContainerReference("overflownqueues-5");
			ComplexModel result = null;
			var expected = new ComplexModel { Name = "Test" };
			var sw = new Stopwatch();
			var factory = new AzureExtendedQueueFactory(new AzureBlobContainer(overflow), new ConsoleLogService());
			var equeue = factory.Create(new AzureQueue(queue));

			using (var mre = new ManualResetEvent(false))
			{
				var options = new HandleSerialMessageOptions(
					TimeSpan.FromSeconds(0),
					TimeSpan.FromMinutes(2),
					TimeSpan.FromSeconds(30),
					5,
					new CancellationToken(),
					message =>
					{
						result = message.GetMessageContents<ComplexModel>();
						mre.Set();
						return true;
					},
					null,
					ex => { throw ex; });

				// Act
				sw.Start();
				queue.CreateIfNotExists();
				overflow.CreateIfNotExists();
				queue.Clear();
				equeue.AddMessageEntity(expected);
				equeue.HandleMessagesAsync(options);

				// Assert
				mre.WaitOne();
				sw.Stop();
				Trace.WriteLine("Total execution time (in seconds): " + sw.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
				Assert.AreEqual(expected.Name, result.Name);
				Assert.AreEqual(expected.ADictionary.First().Key, result.ADictionary.First().Key);
				Assert.AreEqual(expected.ADictionary.First().Value, result.ADictionary.First().Value);
				Assert.AreEqual(expected.AList.First(), result.AList.First());
			}
		}



		[TestCategory("Integration - ExtendedQueue")]
		[TestMethod]
		public void TestErrorDuringRetrieval_GetIdFromMessagePointer()
		{
			// Arrange
			var client = new CloudEnvironment();
			var queue = client.QueueClient.GetQueueReference("test6");
			var overflow = client.BlobClient.GetContainerReference("overflownqueues-6");
			var rnd = new Random();
			ComplexModel result = null;
			var expected = new ComplexModel { Name = new string(Enumerable.Range(1, 128 * 1024).Select(r => (char)rnd.Next(1024, 4096)).ToArray()) };
			var sw = new Stopwatch();
			var succeeded = false;
			var factoryOne = new DefaultExtendedQueueFactory(
				new AzureQueueMessageProvider(),
				new AzureMaximumMessageSizeProvider(),
				new AzureMaximumMessagesPerRequestProvider(),
				new ChaosMonkeyOverflownMessageHandler(new AzureBlobContainer(overflow), ChaosMonkeyOverflownMessageHandler.FailureMode.GetIdFromMessagePointer),
				new ConsoleLogService()
				);

			new AzureExtendedQueueFactory(new AzureBlobContainer(overflow), new ConsoleLogService());
			var equeue = factoryOne.Create(new AzureQueue(queue));

			using (var mre = new ManualResetEvent(false))
			{
				var options = new HandleSerialMessageOptions(
					TimeSpan.FromSeconds(0),
					TimeSpan.FromMinutes(2),
					TimeSpan.FromSeconds(30),
					1,
					new CancellationToken(),
					message =>
					{
						result = message.GetMessageContents<ComplexModel>();
						mre.Set();
						return true;
					},
					null,
					ex =>
					{
						succeeded = true;
						mre.Set();
					});

				// Act
				sw.Start();
				queue.CreateIfNotExists();
				overflow.CreateIfNotExists();
				queue.Clear();
				equeue.AddMessageEntity(expected);
				equeue.HandleMessagesAsync(options);

				// Assert
				mre.WaitOne();
				sw.Stop();
				Trace.WriteLine("Total execution time (in seconds): " + sw.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
				Assert.IsTrue(succeeded);
			}
		}



		[TestCategory("Integration - ExtendedQueue")]
		[TestMethod]
		public void TestErrorDuringRetrieval_GetOverflownContents()
		{
			// Arrange
			var client = new CloudEnvironment();
			var queue = client.QueueClient.GetQueueReference("test7");
			var overflow = client.BlobClient.GetContainerReference("overflownqueues-7");
			var rnd = new Random();
			ComplexModel result = null;
			var expected = new ComplexModel { Name = new string(Enumerable.Range(1, 128 * 1024).Select(r => (char)rnd.Next(1024, 4096)).ToArray()) };
			var sw = new Stopwatch();
			var succeeded = false;
			var factoryOne = new DefaultExtendedQueueFactory(
				new AzureQueueMessageProvider(),
				new AzureMaximumMessageSizeProvider(),
				new AzureMaximumMessagesPerRequestProvider(),
				new ChaosMonkeyOverflownMessageHandler(new AzureBlobContainer(overflow), ChaosMonkeyOverflownMessageHandler.FailureMode.GetOverflownContents),
				new ConsoleLogService()
				);

			new AzureExtendedQueueFactory(new AzureBlobContainer(overflow), new ConsoleLogService());
			var equeue = factoryOne.Create(new AzureQueue(queue));

			using (var mre = new ManualResetEvent(false))
			{
				var options = new HandleSerialMessageOptions(
					TimeSpan.FromSeconds(0),
					TimeSpan.FromMinutes(2),
					TimeSpan.FromSeconds(30),
					1,
					new CancellationToken(),
					message =>
					{
						result = message.GetMessageContents<ComplexModel>();
						mre.Set();
						return true;
					},
					null,
					ex =>
					{
						succeeded = true;
						mre.Set();
					});

				// Act
				sw.Start();
				queue.CreateIfNotExists();
				overflow.CreateIfNotExists();
				queue.Clear();
				equeue.AddMessageEntity(expected);
				equeue.HandleMessagesAsync(options);

				// Assert
				mre.WaitOne();
				sw.Stop();
				Trace.WriteLine("Total execution time (in seconds): " + sw.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
				Assert.IsTrue(succeeded);
			}
		}



		[TestCategory("Integration - ExtendedQueue")]
		[TestMethod]
		public void TestErrorDuringRetrieval_RemoveOverflownContents()
		{
			// Arrange
			var client = new CloudEnvironment();
			var queue = client.QueueClient.GetQueueReference("test8");
			var overflow = client.BlobClient.GetContainerReference("overflownqueues-8");
			var rnd = new Random();
			ComplexModel result = null;
			var expected = new ComplexModel { Name = new string(Enumerable.Range(1, 128 * 1024).Select(r => (char)rnd.Next(1024, 4096)).ToArray()) };
			var sw = new Stopwatch();
			var succeeded = false;
			var factoryOne = new DefaultExtendedQueueFactory(
				new AzureQueueMessageProvider(),
				new AzureMaximumMessageSizeProvider(),
				new AzureMaximumMessagesPerRequestProvider(),
				new ChaosMonkeyOverflownMessageHandler(new AzureBlobContainer(overflow), ChaosMonkeyOverflownMessageHandler.FailureMode.RemoveOverflownContents),
				new ConsoleLogService()
				);

			new AzureExtendedQueueFactory(new AzureBlobContainer(overflow), new ConsoleLogService());
			var equeue = factoryOne.Create(new AzureQueue(queue));

			using (var mre = new ManualResetEvent(false))
			{
				var options = new HandleSerialMessageOptions(
					TimeSpan.FromSeconds(0),
					TimeSpan.FromMinutes(2),
					TimeSpan.FromSeconds(30),
					5,
					new CancellationToken(),
					message =>
					{
						result = message.GetMessageContents<ComplexModel>();
						return true;
					},
					null,
					ex =>
					{
						succeeded = true;
						mre.Set();
					});

				// Act
				sw.Start();
				queue.CreateIfNotExists();
				overflow.CreateIfNotExists();
				queue.Clear();
				equeue.AddMessageEntity(expected);
				equeue.HandleMessagesAsync(options);

				// Assert
				mre.WaitOne();
				sw.Stop();
				Trace.WriteLine("Total execution time (in seconds): " + sw.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
				Assert.IsTrue(succeeded);
			}
		}



		[TestCategory("Integration - ExtendedQueue")]
		[TestMethod]
		public void TestErrorDuringRetrieval_Serialize()
		{
			// Arrange
			var client = new CloudEnvironment();
			var queue = client.QueueClient.GetQueueReference("test9");
			var overflow = client.BlobClient.GetContainerReference("overflownqueues-9");
			var rnd = new Random();
			ComplexModel result = null;
			var expected = new ComplexModel { Name = new string(Enumerable.Range(1, 128 * 1024).Select(r => (char)rnd.Next(1024, 4096)).ToArray()) };
			var sw = new Stopwatch();
			var succeeded = false;
			var factoryOne = new DefaultExtendedQueueFactory(
				new AzureQueueMessageProvider(),
				new AzureMaximumMessageSizeProvider(),
				new AzureMaximumMessagesPerRequestProvider(),
				new ChaosMonkeyOverflownMessageHandler(new AzureBlobContainer(overflow), ChaosMonkeyOverflownMessageHandler.FailureMode.Serialize),
				new ConsoleLogService()
				);

			new AzureExtendedQueueFactory(new AzureBlobContainer(overflow), new ConsoleLogService());
			var equeue = factoryOne.Create(new AzureQueue(queue));

			using (var mre = new ManualResetEvent(false))
			{
				var options = new HandleSerialMessageOptions(
					TimeSpan.FromSeconds(0),
					TimeSpan.FromMinutes(2),
					TimeSpan.FromSeconds(30),
					5,
					new CancellationToken(),
					message =>
					{
						result = message.GetMessageContents<ComplexModel>();
						mre.Set();
						return true;
					},
					null,
					null);

				// Act
				sw.Start();
				queue.CreateIfNotExists();
				overflow.CreateIfNotExists();
				queue.Clear();
				try
				{
					equeue.AddMessageEntity(expected);
				}
				catch (Exception)
				{
					succeeded = true;
				}

				equeue.HandleMessagesAsync(options);

				// Assert
				sw.Stop();
				Trace.WriteLine("Total execution time (in seconds): " + sw.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture));
				Assert.IsTrue(succeeded);
			}
		}
	}
}