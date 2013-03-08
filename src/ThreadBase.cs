/*
 * Copyright (C) 2005 Jorn Baayen <jorn.baayen@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA 02110-1301, USA.
 */

using System;
using System.Collections;
using System.Threading;

namespace Muine
{
	public abstract class ThreadBase
	{
		// Objects
		protected Thread thread;
		protected Queue queue;

		// Variables				
		protected bool thread_done = false;

		// Constructor
		public ThreadBase ()
		{
			queue = Queue.Synchronized (new Queue ());

			GLib.IdleHandler idle = new GLib.IdleHandler (MainLoopIdle);
			GLib.Idle.Add (idle);

			thread = new Thread (new ThreadStart (ThreadFunc));
			thread.IsBackground = true;
			thread.Priority = ThreadPriority.BelowNormal;
		}

		// Methods
		// Methods :: Abstract
		// Methods :: Abstract :: ThreadFunc
		protected abstract void ThreadFunc ();

		// Methods :: Abstract :: MainLoopIdle
		protected abstract bool MainLoopIdle ();
	}
}
