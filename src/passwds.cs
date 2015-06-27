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
using Mono.Unix;

namespace savepass
{
	public class passwds
	{
		private readonly List<passwd> _passwds;

		/* Create _passwds array from byte array from file */
		public passwds(byte[] data)
		{
			_passwds = new List<passwd>();
			int i = 0;
			while (i < data.Length)
				_passwds.Add(new passwd(ref data, ref i));
		}

		/* Add new password to the list */
		public passwd add(string pass, string note)
		{
			var p = new passwd(pass, note);
			_passwds.Add(p);
			return p;
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
					savepass.print(Catalog.GetString(
						"there are no passwords in this file"), full);
					return true;
				}
			} else if (n >= _passwds.Count) {
				savepass.print(string.Format(Catalog.GetString(
					"there is no password with number {0} in this file"), n), full);
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

		/* Return list of notes and list of times */
		public void list(out string[] notes, out DateTime[] times)
		{
			var n = new List<string>();
			var t = new List<DateTime>();

			for (int i = 0; i < _passwds.Count; ++i) {
				n.Add(_passwds[i].note);
				t.Add(_passwds[i].time);
			}
			notes = n.ToArray();
			times = t.ToArray();
		}

		/* Find all notes in array with given note as a substring */
		public void search(string note, out int[] indexes, out string[] notes, out DateTime[] times)
		{
			indexes = null;
			notes = null;
			times = null;
			var i = new List<int>();
			var n = new List<string>();
			var t = new List<DateTime>();

			List<passwd> result = _passwds.FindAll(
				p => p.note.Contains(note));
			if (result.Count == 0) {
				throw new EmptyArrayException(
					String.Format(Catalog.GetString(
						"there is no notes containing \"{0}\" as a substring."),
						note));
			}

			for (int j = 0; j < result.Count; ++j) {
				i.Add(_passwds.IndexOf(result[j]) + 1);
				n.Add(result[j].note);
				t.Add(result[j].time);
			}
			indexes = i.ToArray();
			notes = n.ToArray();
			times = t.ToArray();
		}

		/* Search password in array with given note */
		public void search_and_get_pass(string note, out string pass)
		{
			pass = null;
			List<passwd> result = _passwds.FindAll(
				p => p.note.Contains(note));
			if (result.Count == 0)
				throw new EmptyArrayException(
					String.Format(Catalog.GetString(
						"there is no notes containing \"{0}\" as a substring."),
						note));
			else if (result.Count > 1)
				throw new ArgumentOutOfRangeException(note,
					String.Format(Catalog.GetString(
						"too much notes compare to \"{0}\". Try to refine your query."),
						note));
			pass = result[0].password;
		}

		/* Convert array to byte array for writing to the file */
		public byte[] to_data()
		{
			var data = new List<byte>();
			foreach (passwd p in _passwds) {
				if (p != null)
					data.AddRange(p.to_data());
			}
			return data.ToArray();
		}
	}
}
