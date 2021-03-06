﻿// <copyright file="AzureGlobalMutex.cs" company="nett">
//      Copyright (c) 2015 All Right Reserved, http://q.nett.gr
//      Please see the License.txt file for more information. All other rights reserved.
// </copyright>
// <author>James Kavakopoulos</author>
// <email>ofthetimelords@gmail.com</email>
// <date>2015/03/31</date>
// <summary>
// 
// </summary>

using System.Linq;
using System.Threading;

using TheQ.Utilities.CloudTools.Storage.GlobalMutexFramework;
using TheQ.Utilities.CloudTools.Storage.Infrastructure;
using TheQ.Utilities.CloudTools.Storage.Internal;



namespace TheQ.Utilities.CloudTools.Azure.GlobalMutexFramework
{
	/// <summary>
	///     An implementation of <see cref="GlobalMutexBase{TLockState}" /> for Windows Azure using BLOBs for the underlying lock operations.
	/// </summary>
	public class AzureGlobalMutex : GlobalMutexBase<AzureLockState>
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="AzureGlobalMutex" /> class.
		/// </summary>
		/// <param name="lockStateProvider">The lock state provider using BLOBs for locking.</param>
		/// <param name="initialState">The initial state of the lock.</param>
		public AzureGlobalMutex(AzureLockStateProvider lockStateProvider, AzureLockState initialState) : base(lockStateProvider, initialState) { }



		/// <summary>
		///     Initializes a new instance of the <see cref="AzureGlobalMutex" /> class.
		/// </summary>
		/// <param name="cancelToken">The cancellation token.</param>
		/// <param name="lockStateProvider">The lock state provider using BLOBs for locking.</param>
		/// <param name="initialState">The initial state of the lock.</param>
		public AzureGlobalMutex(CancellationToken cancelToken, AzureLockStateProvider lockStateProvider, AzureLockState initialState)
			: base(cancelToken, lockStateProvider, initialState) { }



		/// <summary>
		///     Initializes a new instance of the <see cref="AzureGlobalMutex" /> class.
		/// </summary>
		/// <param name="lockStateProvider">The lock state provider using BLOBs for locking.</param>
		/// <param name="initialState">The initial state of the lock.</param>
		/// <param name="logService">The logging service to use.</param>
		public AzureGlobalMutex(AzureLockStateProvider lockStateProvider, AzureLockState initialState, [CanBeNull] ILogService logService)
			: base(lockStateProvider, initialState, logService) { }



		/// <summary>
		///     Initializes a new instance of the <see cref="AzureGlobalMutex" /> class.
		/// </summary>
		/// <param name="cancelToken">The cancellation token.</param>
		/// <param name="lockStateProvider">The lock state provider using BLOBs for locking.</param>
		/// <param name="initialState">The initial state of the lock.</param>
		/// <param name="logService">The logging service to use.</param>
		public AzureGlobalMutex(CancellationToken cancelToken, AzureLockStateProvider lockStateProvider, AzureLockState initialState, [CanBeNull] ILogService logService)
			: base(cancelToken, lockStateProvider, initialState, logService) { }
	}
}