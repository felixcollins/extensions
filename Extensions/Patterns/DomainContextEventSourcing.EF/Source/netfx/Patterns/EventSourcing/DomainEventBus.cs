﻿#region BSD License
/* 
Copyright (c) 2010, NETFx
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

* Neither the name of Clarius Consulting nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

/// <summary>
/// Default implementation of an <see cref="IDomainEventBus{TId}"/> that 
/// invokes handlers as events are published, and where handlers are 
/// run in-process.
/// <para>
/// Handlers with <see cref="DomainEventHandler.IsAsync"/> set to 
/// <see langword="true"/> are invoked through the optional 
/// async runner delegate passed to the constructor.
/// </para>
/// </summary>
/// <typeparam name="TId">The type of identifier used by aggregate roots in the domain.</typeparam>
/// <nuget id="netfx-Patterns.EventSourcing.Core" />
partial class DomainEventBus<TId> : IDomainEventBus<TId>
	where TId : IComparable
{
	private Action<Action> asyncActionRunner;
	private IEnumerable<DomainEventHandler> eventHandlers;

	/// <summary>
	/// Initializes the <see cref="None"/> null object 
	/// pattern property.
	/// </summary>u
	static DomainEventBus()
	{
		None = new NullBus();
	}

	/// <summary>
	/// Gets a default domain event bus implementation that 
	/// does nothing (a.k.a. Null Object Pattern).
	/// </summary>
	public static IDomainEventBus<TId> None { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="DomainEventBus{TId}"/> class with 
	/// the default async runner that enqueues work in the <see cref="ThreadPool"/>.
	/// </summary>
	/// <param name="eventHandlers">The event handlers.</param>
	public DomainEventBus(IEnumerable<DomainEventHandler> eventHandlers)
		: this(eventHandlers, action => ThreadPool.QueueUserWorkItem(state => action()))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DomainEventBus{TId}"/> class with 
	/// the given async runner.
	/// </summary>
	/// <param name="eventHandlers">The event handlers.</param>
	/// <param name="asyncActionRunner">The async action runner to use to invoke event handlers 
	/// that have <see cref="DomainEventHandler.IsAsync"/> set to <see langword="true"/>.</param>
	public DomainEventBus(IEnumerable<DomainEventHandler> eventHandlers, Action<Action> asyncActionRunner)
	{
		Guard.NotNull(() => eventHandlers, eventHandlers);
		Guard.NotNull(() => asyncActionRunner, asyncActionRunner);

		if (eventHandlers.Any(eh =>
			eh == null ||
			eh.EventType == null ||
			!InheritsFromGenericHandler(eh.GetType())))
			throw new ArgumentException("eventHandlers");

		this.eventHandlers = eventHandlers.Where(eh => eh != null && eh.EventType != null).ToList();
		this.asyncActionRunner = asyncActionRunner;
	}

	/// <summary>
	/// Publishes the specified event to the bus so that all subscribers are notified.
	/// </summary>
	/// <param name="sender">The sender of the event.</param>
	/// <param name="args">The event payload.</param>
	public virtual void Publish(AggregateRoot<TId> sender, TimestampedEventArgs args)
	{
		Guard.NotNull(() => sender, sender);
		Guard.NotNull(() => args, args);

		var compatibleHandlers = this.eventHandlers.Where(h => h.EventType.IsAssignableFrom(args.GetType())).ToList();
		dynamic dynamicEvent = args;

		// By making this dynamic, we allow event handlers to subscribe to base classes
		foreach (dynamic handler in compatibleHandlers.Where(h => !h.IsAsync).AsParallel())
		{
			handler.Handle(sender.Id, dynamicEvent);
		}

		// Run background handlers through the async runner.
		foreach (dynamic handler in compatibleHandlers.Where(h => h.IsAsync).AsParallel())
		{
			asyncActionRunner(() => handler.Handle(sender.Id, args));
		}
	}

	private bool InheritsFromGenericHandler(Type type)
	{
		var baseType = type.BaseType;
		while (baseType != typeof(object))
		{
			if (baseType.IsGenericType &&
				baseType.GetGenericTypeDefinition() == typeof(DomainEventHandler<,>))
				return true;

			baseType = baseType.BaseType;
		}

		return false;
	}

	/// <summary>
	/// Provides a null <see cref="IDomainEventBus{TId}"/> implementation 
	/// for use when no events have been configured.
	/// </summary>
	private class NullBus : IDomainEventBus<TId>
	{
		/// <summary>
		/// Does nothing.
		/// </summary>
		public void Publish(AggregateRoot<TId> sender, TimestampedEventArgs args)
		{
		}
	}
}
