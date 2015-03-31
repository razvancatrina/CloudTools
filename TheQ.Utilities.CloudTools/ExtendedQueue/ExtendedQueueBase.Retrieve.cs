﻿// <copyright file="ExtendedQueueBase.Retrieve.cs" company="nett">
//      Copyright (c) 2015 All Right Reserved, http://q.nett.gr
//      Please see the License.txt file for more information. All other rights reserved.
// </copyright>
// <author>James Kavakopoulos</author>
// <email>ofthetimelords@gmail.com</email>
// <date>2015/03/30</date>
// <summary>
// 
// </summary>

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using TheQ.Utilities.CloudTools.Storage.Models.ObjectModel;



namespace TheQ.Utilities.CloudTools.Storage.ExtendedQueue
{
	public abstract partial class ExtendedQueueBase
	{
		/// <summary>
		///     This member is intended for internal usage only. Converts an incoming message to an entity.
		/// </summary>
		/// <typeparam name="T">The type of the object to attempt to deserialise to.</typeparam>
		/// <param name="message">The original message.</param>
		/// <param name="token">An optional cancellation token.</param>
		/// <returns>The contents of the message as an instance of type <typeparamref name="T" />.</returns>
		public virtual Task<T> DecodeMessageAsync<T>(QueueMessageWrapper message, CancellationToken token) { return this.DecodeMessageAsync<T>(message, token, this); }


		/// <summary>
		///     This member is intended for internal usage only. Converts an incoming message to an entity.
		/// </summary>
		/// <typeparam name="T">The type of the object to attempt to deserialise to.</typeparam>
		/// <param name="message">The original message.</param>
		/// <param name="token">An optional cancellation token.</param>
		/// <param name="invoker">The (optional) decorator that called this method.</param>
		/// <returns>The contents of the message as an instance of type <typeparamref name="T" />.</returns>
		internal virtual async Task<T> DecodeMessageAsync<T>(QueueMessageWrapper message, CancellationToken token, ExtendedQueueBase invoker)
		{
			var msgBytes = message.ActualMessage.AsBytes;
			var overflownId = (message.OverflowId = this.Get(invoker).GetOverflownMessageId(message.ActualMessage));
			var wasOverflown = (message.WasOverflown = !string.IsNullOrWhiteSpace(overflownId));

			msgBytes = await (wasOverflown
				? this.Get(invoker).GetOverflownMessageContentsAsync(message.ActualMessage, overflownId, token)
				: this.Get(invoker).GetNonOverflownMessageContentsAsync(message.ActualMessage, token)).ConfigureAwait(false);

			var serialized = await this.Get(invoker).ByteArrayToSerializedMessageContents(msgBytes, this.Get(invoker)).ConfigureAwait(false);

			return typeof(T) == typeof(string) ? (T)(object)serialized : this.DeserializeToObject<T>(serialized);
		}



		/// <summary>
		/// Converts a raw message content to a string representing a serialised entity.
		/// </summary>
		/// <param name="messageBytes">The message as a byte array.</param>
		/// <param name="invoker">The (optional) decorator that called this method.</param>
		/// <returns>The original serialised entity as a <see cref="string"/>.</returns>
		protected internal virtual async Task<string> ByteArrayToSerializedMessageContents(byte[] messageBytes, ExtendedQueueBase invoker)
		{
			using (var converter = new MemoryStream(messageBytes))
			{
				converter.Seek(0, SeekOrigin.Begin);

				using (var decoratedConverter = this.Get(invoker).GetByteDecoder(converter)) 
				using (var reader = new StreamReader(decoratedConverter)) 
					return await reader.ReadToEndAsync().ConfigureAwait(false);
			}
		}



		protected internal abstract Stream GetByteDecoder(Stream originalConverter);


		protected internal abstract string GetOverflownMessageId(IQueueMessage message);


		protected internal abstract Task<byte[]> GetOverflownMessageContentsAsync(IQueueMessage message, string id, CancellationToken token);


		protected internal abstract Task<byte[]> GetNonOverflownMessageContentsAsync(IQueueMessage message, CancellationToken token);


		protected internal abstract T DeserializeToObject<T>(string serializedContents);


		protected internal abstract Task RemoveOverflownContentsAsync(QueueMessageWrapper message, CancellationToken token);
	}
}