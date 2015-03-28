//  
//  passwds.cs
//  
//  Author:
//       keldzh <keldzh@gmail.com>
// 
//  Copyright (c) 2014 Anton Kovalyov
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
#define GTK
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;

/* Возможно надо сделать чтение не строки а массива символов. В общем надо сейчас будет поработать над тем как хранить данные */
namespace savepass
{
	public class passwds
	{
		private List<passwd> _passwds;

		/* Create _passwds array from string from file */
		public passwds(string data)
		{
			_passwds = new List<passwd>();
			string[] a = data.Split(new char[] {'\n'});
			foreach (string str in a)
				if (!String.IsNullOrWhiteSpace(str))
					_passwds.Add(new passwd(str));
		}
		/* Create _passwds array from byte array from file */
		public passwds(byte[] data)
		{
			_passwds = new List<passwd>();
			int i = 0;
			while (i < data.Length)
				_passwds.Add(new passwd(ref data, ref i));
		}

		/* Add new password to the list */
		public void add(string pass, string note)
		{
			_passwds.Add(new passwd(pass, note));
		}

		/* Change password with number n */
		public void change(int n, string pass, string note)
		{
			if (pass != null)
				_passwds[n].password = pass;
			if (note != null)
				_passwds[n].note = note;
		}

		/* Check is n within _passwds
		 * return false if n within _passwds
		 * and true if not
		 */
		public bool check_limits(int n, bool full)
		{
			if (n == 0) {
				if (_passwds.Count == 0) {
					savepass.print("there are no passwords in this file", full);
					return true;
				}
			} else if (n >= _passwds.Count) {
				savepass.print(string.Format("there is no password with number {0} in this file", n), full);
				return true;
			}
			return false;
		}

		/* Delete password with number n from array */
		public  void del(int n)
		{
			_passwds.RemoveAt(n);
		}

		/* Return password and note for n element of _passwds for change function */
		public bool get_pass_note(int n, out string pass, out string note)
		{
			if (check_limits(n, false)) {
				pass = null;
				note = null;
				return false;
			}
			pass = _passwds[n].password;
			note = _passwds[n].note;
			return true;
		}

		/* Print note with given format */
		private int print_note(int i, passwd p)
		{
			if (!savepass.c.show_date_time) {
				Console.WriteLine("{0,3}) {1}", i, p.note);
				return 0;
			}

			try {
				Console.WriteLine("{0,3}) {1} {2}", i,
					p.time.ToString(savepass.c.format_date_time, CultureInfo.CurrentCulture),
					p.note);
			} catch (FormatException e) {
				savepass.print(String.Format("date_time_format is invalid: {0}", e.Message), false);
				return 1;
			} catch (Exception e) {
				savepass.print(e.Message, true);
				return 2;
			}
			return 0;
		}

		/* Return list of notes and list of times */
		public void list(out string[] notes, out DateTime[] times)
		{
			List<string> n = new List<string>();
			List<DateTime> t = new List<DateTime>();

			for (int i = 0; i < _passwds.Count; ++i) {
				n.Add(_passwds[i].note);
				t.Add(_passwds[i].time);
			}
			notes = n.ToArray();
			times = t.ToArray();
		}

		/* Find all notes in array with given note as a substring */
		public int search(string note)
		{
			int val;
			if (check_limits(0, true))
				return 1;
			if (note == "") {
				savepass.print("string for search cannot be an empty string.\n" +
					"If you want to see all passwords use --show", false);
				return 1;
			}
			List<passwd> result = _passwds.FindAll(
				delegate(passwd p) {
					return p.note.Contains(note);
				});
			if (result.Count == 0) {
				savepass.print(String.Format("there is no notes containing \"{0}\" as a substring.",
					note), false);
				return 1;
			}
			Console.WriteLine("Passwords' notes contains \"{0}\":", note);
			foreach (passwd p in result)
				if ((val = print_note(_passwds.IndexOf(p) + 1, p)) != 0)
					return val;
			return 0;
		}

		/* Search password in array with given note */
		public errors search_and_get_pass(string note, out string pass)
		{
			pass = null;
			List<passwd> result = _passwds.FindAll(
				delegate(passwd p) {
					return p.note.Contains(note);
				});
			if (result.Count == 0)
				return errors.empty_array;
			else if (result.Count > 1)
				return errors.too_much_elemets;
			pass = result[0].password;
			return errors.all_ok;
		}

		/* Convert array to string for writing to the file */
		public override string ToString()
		{
			StringBuilder str = new StringBuilder();

			foreach (passwd p in _passwds) {
				if (p != null)
					str.AppendLine(p.ToString());
			}
			return str.ToString();
		}

		public byte[] to_data()
		{
			List<byte> data = new List<byte>();
			foreach (passwd p in _passwds) {
				if (p != null)
					data.AddRange(p.to_data());
			}
			return data.ToArray();
		}
	}
}
