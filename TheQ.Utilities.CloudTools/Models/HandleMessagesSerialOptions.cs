﻿// <copyright file="HandleSerialMessageOptions.cs" company="nett">
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using TheQ.Utilities.CloudTools.Storage.ExtendedQueue;
using TheQ.Utilities.CloudTools.Storage.Internal;



namespace TheQ.Utilities.CloudTools.Storage.Models
{
	/// <summary>
	///     <para>Input arguments for the <c>ExtendedQueue</c> framework.</para>
	/// </summary>
	public class HandleMessagesSerialOptions : HandleMessagesOptionsBase
	{
		/// <summary>
		///     <para>Initializes a new instance of the <see cref="HandleMessagesSerialOptions" /></para>
		///     <para>class.</para>
		/// </summary>
		/// <param name="timeWindow">
		///     <para>The time window within which a message is still valid for processing (older messages will be discarded). Use <see cref="System.TimeSpan.Zero" /></para>
		///     <para>to ignore this check.</para>
		/// </param>
		/// <param name="messageLeaseTime">The amount of time between periodic refreshes on the lease of a message.</param>
		/// <param name="pollFrequency">The frequency with which the queue is being polled for new messages.</param>
		/// <param name="poisonMessageThreshold">The amount of times a message can be enqueued.</param>
		/// <param name="cancelToken">A cancellation token to allow cancellation of this process.</param>
		/// <param name="messageHandler">An action that specifies how a message should be handled. Returns a value indicating whether the message has been handled successfully and should be removed.</param>
		/// <param name="poisonHandler">
		///     <para>An action that specifies how a poison message should be handled, which fires before <see cref="TheQ.Utilities.CloudTools.Storage.Models.HandleMessagesSerialOptions.MessageHandler" /></para>
		///     <para>. Returns a value indicating whether the message has been handled successfully and should be removed.</para>
		/// </param>
		/// <param name="exceptionHandler">An action that specifies how an exception should be handled.</param>
		/// <exception cref="ArgumentNullException">messageHandler;The Message Handler <see langword="delegate" /> is required</exception>
		/// <exception cref="ArgumentException">
		///     Message Lease Time cannot be lower than 30 seconds! or Poll Frequency cannot be lower than 1 second! or Poison Message Threshold cannot be lower than 1
		/// </exception>
		public HandleMessagesSerialOptions(
			TimeSpan timeWindow,
			TimeSpan messageLeaseTime,
			TimeSpan pollFrequency,
			int poisonMessageThreshold,
			CancellationToken cancelToken,
			[NotNull] Func<QueueMessageWrapper, Task<bool>> messageHandler,
			[CanBeNull] Func<QueueMessageWrapper, Task<bool>> poisonHandler = null,
			[CanBeNull] Action<Exception> exceptionHandler = null)
			: base(timeWindow, messageLeaseTime, pollFrequency, poisonMessageThreshold, cancelToken, exceptionHandler)

		{
			if (messageHandler == null) throw new ArgumentNullException("messageHandler", "The Message Handler delegate is required");

			this.MessageHandler = messageHandler;
			this.PoisonHandler = poisonHandler;
		}



		/// <summary>
		///     An action that specifies how a message should be handled. Returns a value indicating whether the message has been handled successfully and should be removed.
		/// </summary>
		[NotNull]
		public Func<QueueMessageWrapper, Task<bool>> MessageHandler { get; private set; }


		/// <summary>
		///     <para>An action that specifies how a poison message should be handled, which fires before <see cref="TheQ.Utilities.CloudTools.Storage.Models.HandleMessagesSerialOptions.MessageHandler" /></para>
		///     <para>. Returns a value indicating whether the message has been handled successfully and should be removed.</para>
		/// </summary>
		[CanBeNull]
		public Func<QueueMessageWrapper, Task<bool>> PoisonHandler { get; private set; }
	}
}