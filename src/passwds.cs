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
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;

/* Возможно надо сделать чтение не строки а массива символов. В общем надо сейчас будет поработать над тем как хранить данные */
namespace passwdsaver
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

		/* Add new password to the list */
		public int add()
		{
			string password, note;

			try {
				Console.Write("Enter password: ");
				password = Console.ReadLine();
				Console.Write("Enter note: ");
				note = Console.ReadLine();
			} catch (IOException e) {
				passwdsaver.print(String.Format("some error with console: {0}", e.Message), true);
				return 2;
			} catch (OutOfMemoryException e) {
				passwdsaver.print(e.Message, true);
				return 2;
			} catch (ArgumentOutOfRangeException e) {
				passwdsaver.print(e.Message, true);
				return 2;
			}
			_passwds.Add(new passwd(password, note));
			return 0;
		}

		private static int are_you_sure()
		{
			string answer;

			try {
				Console.Write("Are you sure? (y/n) ");
				answer = Console.ReadLine();
				return (string.Compare(answer, "y", true) == 0) ? 0 : 1;
			} catch (IOException e) {
				passwdsaver.print(String.Format("some error with console: {0}", e.Message), true);
				return 2;
			} catch (Exception e) {
				passwdsaver.print(e.Message, true);
				return 2;
			}
		}

		/* Change password with number n */
		public int change(byte n)
		{
			string str;

			if (check_limits(n, false))
				return 1;
			passwd new_passwd = new passwd(_passwds[n - 1]);
			try {
				Console.Write("Enter new password (if you press ENTER password will stay the same): ");
				str = Console.ReadLine();
				if (str.Length != 0)
					new_passwd.password = str;
				Console.Write("Enter new note [{0}]: ", new_passwd.note);
				str = Console.ReadLine();
				if (str.Length != 0)
					new_passwd.note = str;
			} catch (IOException e) {
				passwdsaver.print(String.Format("some error with console: {0}", e.Message), true);
				return 2;
			} catch (OutOfMemoryException e) {
				passwdsaver.print(e.Message, true);
				return 2;
			} catch (ArgumentOutOfRangeException e) {
				passwdsaver.print(e.Message, true);
				return 2;
			}
			_passwds[n - 1] = new_passwd;
			return 0;
		}

		private bool check_limits(byte n, bool full)
		{
			if (n == 0) {
				if (_passwds.Count == 0) {
					passwdsaver.print("there are no passwords in this file", full);
					return true;
				}
			} else if (n > _passwds.Count) {
				passwdsaver.print(string.Format("there is no password with number {0} in this file", n), full);
				return true;
			}
			return false;
		}

		/* Delete password with number n from array */
		public  int del (byte n)
		{
			int return_value;

			if (check_limits(n, true))
				return 1;
			return_value = are_you_sure();
			if (return_value == 0)
				_passwds.RemoveAt(n - 1);
			return return_value == 2 ? 2 : 0;
		}
	
		/* Show password with number n
		 * if on_screen == true then password will be
		 * printed on the screen, otherwise it will be
		 * copied to clipboard */
		public int get(byte n, bool on_screen)
		{
			if (check_limits(n, true))
				return 1;
			if (on_screen || !passwdsaver.c.always_in_clipboard)
				Console.WriteLine(_passwds[(int) n - 1].password);
			else {
				#if WINDOWS
				Clipboard.SetText(_passwds[(int) n - 1].password, TextDataFormat.Text);
				#elif GTK
				Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
				clipboard.Text = _passwds[(int) n - 1].password;
				#endif
			}
			return 0;
		}

		/* Print note with given format */
		private int print_note(int i, passwd p)
		{
			if (!passwdsaver.c.show_date_time) {
				Console.WriteLine("{0,3}) {1}", i, p.note);
				return 0;
			}

			try {
				Console.WriteLine("{0,3}) {1} {2}", i,
					p.time.ToString(passwdsaver.c.format_date_time, CultureInfo.CurrentCulture),
					p.note);
			} catch (FormatException e) {
				passwdsaver.print(String.Format("date_time_format is invalid: {0}", e.Message), false);
				return 1;
			} catch (Exception e) {
				passwdsaver.print(String.Format(e.Message), true);
				return 2;
			}
			return 0;
		}

		/* Search password in array with given note */
		public int get_pass(string note, bool on_screen)
		{
			if (check_limits(0, true))
				return 1;
			if (!String.IsNullOrWhiteSpace(note)) {
				passwdsaver.print("string for search cannot be an empty string\n" +
					"or cosists exclusively of white-space characters.\n" +
					"If you want to see all passwords use --show", true);
				return 1;
			}
			List<passwd> result = _passwds.FindAll(
				delegate(passwd p) {
					return p.note.Contains(note);
				});
			if (result.Count == 0) {
				passwdsaver.print(String.Format("there is no notes containing \"{0}\" as a substring.",
					note), false);
				return 1;
			} else if (result.Count > 1) {
				passwdsaver.print(String.Format("Too much notes compare to \"{0}\". Try to refine your query.",
					note), false);
				return 1;
			}
			if (on_screen)
				Console.WriteLine(result[0].password);
			else {
				#if WINDOWS
				Clipboard.SetText(result[0].password, TextDataFormat.Text);
				#elif GTK
				Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
				clipboard.Text = result[0].password;
				#endif
			}
			return 0;
		}

		/* Show the list of notes */
		public int list()
		{
			int result;

			if (check_limits(0, true))
				return 1;
			try {
				Console.WriteLine("Passwords' notes:");
				for (int i = 0; i < _passwds.Count; ++i)
					if ((result = print_note(i + 1, _passwds[i])) != 0)
						return result;
			} catch (IOException e) {
				passwdsaver.print(e.Message, true);
				return 2;
			}
			return 0;
		}

		/* Find all notes in array with given note as a substring */
		public int search(string note)
		{
			int val;
			if (check_limits(0, true))
				return 1;
			if (note == "") {
				passwdsaver.print("string for search cannot be an empty string.\n" +
					"If you want to see all passwords use --show", false);
				return 1;
			}
			List<passwd> result = _passwds.FindAll(
				delegate(passwd p) {
					return p.note.Contains(note);
				});
			if (result.Count == 0) {
				passwdsaver.print(String.Format("there is no notes containing \"{0}\" as a substring.",
					note), false);
				return 1;
			}
			Console.WriteLine("Passwords' notes contains \"{0}\":", note);
			foreach (passwd p in result)
				if ((val = print_note(_passwds.IndexOf(p) + 1, p)) != 0)
					return val;
			return 0;
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
	}
}
