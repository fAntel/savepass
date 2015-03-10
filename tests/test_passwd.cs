﻿//
//  test_passwd.cs
//
//  Author:
//       keldzh <keldzh@gmail.com>
//
//  Copyright (c) 2015 Anton Kovalyov
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using NUnit.Framework;

namespace savepass
{
	[TestFixture()]
	public class test_passwd
	{
		private DateTime now;

		[SetUp]
		public void Init()
		{
			now = DateTime.Now;
		}

		[Test()]
		public void test_constructor_for_new_password()
		{
			passwd p = new passwd("pass", "note", now);

			Assert.AreEqual(p.password, "pass");
			Assert.AreEqual(p.note, "note");
			Assert.AreEqual(p.added, now);
			Assert.AreEqual(p.changed, DateTime.MinValue);
		}

		[Test()]
		public void test_constructor_for_copying()
		{
			passwd p0 = new passwd("pass", "note");
			passwd p1 = new passwd(p0);
			Assert.AreEqual(p0.password, p1.password);
			Assert.AreEqual(p0.note, p1.note);
			Assert.AreEqual(p0.added, p1.added);
			Assert.AreEqual(p0.changed, p1.changed);
		}

		[Test()]
		public void test_changed_with_password()
		{
			passwd p = new passwd("pass", "note");
			Assert.AreEqual(p.changed, DateTime.MinValue);
			p.password = "pass0";
			Assert.AreNotEqual(p.time, now);
		}

		[Test()]
		public void test_changed_with_note()
		{
			passwd p = new passwd("pass", "note");
			Assert.AreEqual(p.changed, DateTime.MinValue);
			p.note = "note0";
			Assert.AreNotEqual(p.time, now);
		}

		[Test()]
		public void test_time_without_changed()
		{
			passwd p = new passwd("pass", "note", now);
			Assert.AreEqual(p.time, now);
		}

		[Test()]
		public void test_time_with_changed()
		{
			passwd p = new passwd("pass", "note", DateTime.MaxValue, now);
			Assert.AreEqual(p.time, now);
		}
	}
}

