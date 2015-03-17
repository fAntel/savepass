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

		private static int are_you_sure(string str)
		{
			string answer;

			try {
				Console.Write(String.Format("Are you sure{0}? (y/n) ", str));
				answer = Console.ReadLine();
				return String.Compare(answer, "y", StringComparison.OrdinalIgnoreCase) == 0 ? 0 : 1;
			} catch (IOException e) {
				savepass.print(String.Format("some error with console: {0}", e.Message), true);
				return 2;
			} catch (Exception e) {
				savepass.print(e.Message, true);
				return 2;
			}
		}

		/* Change password with number n */
		public void change(int n, string pass, string note)
		{
			if (pass != null)
				_passwds[n].password = pass;
			if (note != null)
				_passwds[n].note = note;
		}

		/* Check is n within _passwds */
		private bool check_limits(int n, bool full)
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
		public  int del(byte n)
		{
			int return_value;

			if (check_limits(n - 1, true))
				return 1;
			return_value = are_you_sure(
				String.Format(" you want to delete password with note \"{0}\"",
					_passwds[n - 1].note));
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
			if (check_limits(n - 1, true))
				return 1;
			if (on_screen || !savepass.c.always_in_clipboard)
				Console.WriteLine(_passwds[(int) n - 1].password);
			else {
				#if WINDOWS
				Clipboard.SetText(_passwds[(int) n - 1].password, TextDataFormat.Text);
				#elif GTK
				Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
				clipboard.Text = _passwds[(int) n - 1].password;
				clipboard.Store();
				#endif
			}
			return 0;
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

		/* Search password in array with given note */
		public int get_pass(string note, bool on_screen)
		{
			if (check_limits(0, true))
				return 1;
			if (String.IsNullOrWhiteSpace(note)) {
				savepass.print("string for search cannot be an empty string\n" +
					"or cosists exclusively of white-space characters.\n" +
					"If you want to see all passwords use --show", true);
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
			} else if (result.Count > 1) {
				savepass.print(String.Format("Too much notes compare to \"{0}\". Try to refine your query.",
					note), false);
				return 1;
			}
			if (on_screen  || !savepass.c.always_in_clipboard)
				Console.WriteLine(result[0].password);
			else {
				#if WINDOWS
				Clipboard.SetText(result[0].password, TextDataFormat.Text);
				#elif GTK
				Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
				clipboard.Text = result[0].password;
				clipboard.Store();
				#endif
			}
			return 0;
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
				savepass.print(e.Message, true);
				return 2;
			}
			return 0;
		}

		/* Read characters from stdin but do not echo them */
		private static String read_password()
		{
			StringBuilder password = new StringBuilder();
			ConsoleKeyInfo key;

			do {
				key = Console.ReadKey(true);
				if (((key.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt) ||
				    ((key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control) ||
				    (key.Key == ConsoleKey.Tab) ||
				    (key.Key == ConsoleKey.Enter))
					continue;
				if (key.Key == ConsoleKey.Backspace) {
					if (!String.IsNullOrEmpty(password.ToString()))
						password.Remove(password.Length - 1, 1);
				} else if ((key.KeyChar == '\u0000'))
					continue;
				else
					password.Append(key.KeyChar);
			} while (key.Key != ConsoleKey.Enter);
			Console.WriteLine();
			return password.ToString();
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
