//
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
		public void set_up()
		{
			p = new passwd("pass", "note");
			ps = new passwds(p.to_data());
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
		public void set_up()
		{
			p = new passwd("pass", "note");
			byte[] p_data = p.to_data();
			data = new byte[p_data.Length * 3];
			Array.Copy(p_data, 0, data, 0, p_data.Length);
			Array.Copy(p_data, 0, data, p_data.Length, p_data.Length);
			Array.Copy(p_data, 0, data, p_data.Length * 2, p_data.Length);
			ps = new passwds(data);
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
		public void set_up()
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
			bool result;
			string pass, note;

			result = ps.get_pass_note(1, out pass, out note);

			Assert.IsFalse(result);
		}

		[Test()]
		public void test_change_nothing()
		{
			string pass, note;

			ps.change(0, null, null);

			ps.get_pass_note(0, out pass, out note);
			Assert.AreEqual(pass, "pass");
			Assert.AreEqual(note, "note");
		}

		[Test()]
		public void test_change_pass()
		{
			string pass, note;

			ps.change(0, "pass0", null);

			ps.get_pass_note(0, out pass, out note);
			Assert.AreEqual(pass, "pass0");
			Assert.AreEqual(note, "note");
		}

		[Test()]
		public void test_change_note()
		{
			string pass, note;

			ps.change(0, null, "note0");

			ps.get_pass_note(0, out pass, out note);
			Assert.AreEqual(pass, "pass");
			Assert.AreEqual(note, "note0");
		}

		[Test()]
		public void test_change_both()
		{
			string pass, note;

			ps.change(0, "pass0", "note0");

			ps.get_pass_note(0, out pass, out note);
			Assert.AreEqual(pass, "pass0");
			Assert.AreEqual(note, "note0");
		}

		[Test()]
		public void test_del()
		{
			ps.del(0);

			Assert.IsTrue(ps.check_limits(0, false));
		}

		[Test()]
		public void test_list()
		{
			ps.add("pass1", "note1");
			ps.add("pass2", "note2");
			string[] result = { "note", "note1", "note2" };
			string[] notes;
			DateTime[] time;

			ps.list(out notes, out time);

			Assert.AreEqual(result, notes);
		}

		[Test()]
		public void test_list_for_one_element()
		{
			string[] result = { "note" };
			string[] notes;
			DateTime[] time;

			ps.list(out notes, out time);

			Assert.AreEqual(result, notes);
		}

		[Test()]
		public void test_list_when_it_empty()
		{
			ps.del(0);
			string[] result = {};
			string[] notes;
			DateTime[] time;

			ps.list(out notes, out time);

			Assert.AreEqual(result, notes);
		}

		[Test()]
		public void test_search()
		{
			ps.add("pass1", "note1");
			ps.add("pass2", "n");
			int[] indexes_result = { 1, 2 };
			string[] notes_result = { "note", "note1"};
			int[] indexes;
			string[] notes;
			DateTime[] times;

			ps.search("note", out indexes, out notes, out times);

			Assert.AreEqual(notes_result, notes);
			Assert.AreEqual(indexes_result, indexes);
		}

		[Test()]
		public void test_search_for_one_element()
		{
			int[] indexes_result = { 1 };
			string[] result = { "note" };
			int[] indexes;
			string[] notes;
			DateTime[] times;

			ps.search("note", out indexes, out notes, out times);

			Assert.AreEqual(result, notes);
			Assert.AreEqual(indexes_result, indexes);
		}

		[Test()]
		[ExpectedException(typeof(EmptyArrayException))]
		public void test_search_and_get_pass_with_wrong_note()
		{
			int[] indexes;
			string[] notes;
			DateTime[] times;

			ps.search("note0", out indexes, out notes, out times);

			Assert.IsNull(indexes);
			Assert.IsNull(notes);
			Assert.IsNull(times);
		}

		[Test()]
		public void test_search_and_get_pass()
		{
			string pass;

			ps.search_and_get_pass("note", out pass);

			Assert.AreEqual(pass, "pass");
		}

		[Test()]
		[ExpectedException(typeof(EmptyArrayException))]
		public void test_search_with_wrong_note()
		{
			string pass;

			ps.search_and_get_pass("note0", out pass);

			Assert.IsNull(pass);
		}

		[Test()]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void test_search_and_get_pass_with_several_same_notes()
		{
			string pass;
			ps.add("pass1", "note1");

			ps.search_and_get_pass("note", out pass);

			Assert.IsNull(pass);
		}
	}
}
