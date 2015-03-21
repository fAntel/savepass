﻿//
//  test_passwds.cs
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
	public class test_passwds_for_one_element
	{
		private passwd p;
		private passwds ps;

		[SetUp()]
		public void Init()
		{
			p = new passwd("pass", "note");
			ps = new passwds(p.to_data());
		}

		[Test()]
		public void test_constructor_for_one_element()
		{
			Assert.AreEqual(String.Format("{0}\n", p), ps.ToString());
		}

		[Test()]
		public void test_to_data_for_one_element()
		{
			Assert.AreEqual(p.to_data(), ps.to_data());
		}

		[Test()]
		public void test_check_limits_within_range()
		{
			Assert.IsFalse(ps.check_limits(0, false));
		}

		[Test()]
		public void test_check_limits_without_range()
		{
			Assert.IsTrue(ps.check_limits(1, false));
		}
	}

	[TestFixture()]
	public class test_passwds_for_several_element
	{
		private passwd p;
		private passwds ps;
		private byte[] data;

		[SetUp()]
		public void Init()
		{
			p = new passwd("pass", "note");
			byte [] p_data = p.to_data();
			data = new byte[p_data.Length * 3];
			Array.Copy(p_data, 0, data, 0, p_data.Length);
			Array.Copy(p_data, 0, data, p_data.Length, p_data.Length);
			Array.Copy(p_data, 0, data, p_data.Length * 2, p_data.Length);
			ps = new passwds(data);
		}

		[Test()]
		public void test_constructor_for_several_elements()
		{
			Assert.AreEqual(String.Format("{0}\n{0}\n{0}\n", p), ps.ToString());
		}

		[Test()]
		public void test_to_data_for_several_element()
		{
			Assert.AreEqual(data, ps.to_data());
		}

		[Test()]
		public void test_check_limits_within_range()
		{
			Assert.IsFalse(ps.check_limits(2, false));
		}

		[Test()]
		public void test_check_limits_without_range()
		{
			Assert.IsTrue(ps.check_limits(3, false));
		}
	}

	[TestFixture()]
	public class test_passwds_functions
	{
		private passwd p;
		private passwds ps;

		[SetUp()]
		public void Init()
		{
			p = new passwd("pass", "note");
			ps = new passwds(new byte[0]);
			ps.add(p.password, p.note);
		}

		[Test()]
		public void test_add()
		{
			byte[] data = ps.to_data();
			int i = 0;
			passwd result = new passwd(ref data, ref i);
			Assert.AreEqual(p.password, result.password);
			Assert.AreEqual(p.note, result.note);
		}

		[Test()]
		public void test_get_pass_note()
		{
			string pass, note;
			ps.get_pass_note(0, out pass, out note);
			Assert.AreEqual(p.password, pass);
			Assert.AreEqual(p.note, note);
		}

		[Test()]
		public void test_get_pass_note_without_range()
		{
			string pass, note;
			Assert.IsFalse(ps.get_pass_note(1, out pass, out note));
		}
	}
}