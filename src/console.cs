//
//  console.cs
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
#define GTK
using System;
using System.IO;
using System.Text;
using System.Globalization;

namespace savepass
{
	public class console: IUI
	{
		private passwds _p;

		public console(passwds p)
		{
			_p = p;
		}

		/* Add new password in array of passwords */
		public byte add()
		{
			string password0, password1, note;

			try {
				do {
					Console.Write("Enter password: ");
					password0 = read_password();
					Console.Write("Enter password again: ");
					password1 = read_password();
					if (String.Compare(password0, password1) != 0) {
						Console.WriteLine("Passwords doesn't match. Try again");
					}
				} while (String.Compare(password0, password1) != 0);
				Console.Write("Enter note: ");
				note = Console.ReadLine();
			} catch (IOException e) {
				savepass.print(String.Format("some error with console: {0}", e.Message), true);
				return 2;
			} catch (OutOfMemoryException e) {
				savepass.print(e.Message, true);
				return 2;
			} catch (ArgumentOutOfRangeException e) {
				savepass.print(e.Message, true);
				return 2;
			}
			_p.add(password0, note);
			return 0;
		}

		private static byte are_you_sure(string str)
		{
			string answer;

			try {
				Console.Write(String.Format("Are you sure{0}? (y/n) ", str));
				answer = Console.ReadLine();
				if (String.Compare(answer, "y", StringComparison.OrdinalIgnoreCase) == 0)
					return 0;
				else
					return 1;
			} catch (IOException e) {
				savepass.print(String.Format("some error with console: {0}", e.Message), true);
				return 2;
			} catch (Exception e) {
				savepass.print(e.Message, true);
				return 2;
			}
		}

		/* Change password and/or note in n element of array of passwords */
		public byte change(int n)
		{
			string pass, note, str0, str1;
			if (!_p.get_pass_note(n - 1, out pass, out note))
				return 1;
			try {
				do {
					Console.Write("Enter new password (if you press ENTER password will stay the same): ");
					str0 = read_password();
					if (str0.Length == 0)
						break;
					Console.Write("Enter new password again: ");
					str1 = read_password();
					if (String.Compare(str0, str1) != 0)
						Console.WriteLine("Passwords doesn't match. Try again");
				} while (String.Compare(str0, str1) != 0);
				pass = str0.Length > 0 ? str0 : null;
				Console.Write("Enter new note [{0}]: ", note);
				str0 = Console.ReadLine();
				note = str0.Length > 0 ? str0 : null;
			} catch (IOException e) {
				savepass.print(String.Format("some error with console: {0}", e.Message), true);
				return 2;
			} catch (OutOfMemoryException e) {
				savepass.print(e.Message, true);
				return 2;
			} catch (ArgumentOutOfRangeException e) {
				savepass.print(e.Message, true);
				return 2;
			}
			_p.change(n - 1, pass, note);
			return 0;
		}

		/* Remove password with number n from array of passwords */
		public byte del(int n)
		{
			string pass, note;
			byte return_value;

			--n;
			if (_p.check_limits(n, true))
				return 1;
			_p.get_pass_note(n, out pass, out note);
			return_value = are_you_sure(
				String.Format(" you want to delete password with note \"{0}\"", note));
			if (return_value == 0)
				_p.del(n);
			return return_value == 2 ? (byte) 2 : (byte) 0;
		}

		/* Show password with number n
		 * if on_screen == true then password will be
		 * printed on the screen, otherwise it will be
		 * copied to clipboard */
		public byte get_nth_pass(int n, bool on_screen)
		{
			string pass, note;

			--n;
			if (_p.check_limits(n, true))
				return 1;
			_p.get_pass_note(n, out pass, out note);
			put(on_screen, pass);
			return 0;
		}

		/* Print string or put it into clipboard */
		private void put(bool on_screen, string str)
		{
			if (on_screen  || !savepass.c.always_in_clipboard)
				Console.WriteLine(str);
			else {
				#if WINDOWS
				Clipboard.SetText(str, TextDataFormat.Text);
				#elif GTK
				Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
				clipboard.Text = str;
				clipboard.Store();
				#endif
			}
		}

		/* Print note with given format */
		private byte print_note(int i, string note, DateTime time)
		{
			if (!savepass.c.show_date_time) {
				Console.WriteLine("{0,3}) {1}", i, note);
				return 0;
			}

			try {
				Console.WriteLine("{0,3}) {1} {2}", i,
					time.ToString(savepass.c.format_date_time, CultureInfo.CurrentCulture),
					note);
			} catch (FormatException e) {
				savepass.print(String.Format("date_time_format is invalid: {0}", e.Message), false);
				return 1;
			} catch (Exception e) {
				savepass.print(e.Message, true);
				return 2;
			}
			return 0;
		}

		/* Show the list of notes */
		public byte list()
		{
			byte result;
			string[] notes;
			DateTime[] times;

			if (_p.check_limits(0, true))
				return 1;
			_p.list(out notes, out times);
			try {
				Console.WriteLine("Passwords' notes:");
				for (int i = 0; i < notes.Length; ++i)
					if ((result = print_note(i + 1, notes[i], times[i])) != 0)
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

		/* Search password in array with given note */
		public byte search_and_get_pass(string note, bool on_screen)
		{
			errors count;
			string pass;

			if (String.IsNullOrWhiteSpace(note)) {
				savepass.print("string for search cannot be an empty string\n" +
					"or cosists exclusively of white-space characters.\n" +
					"If you want to see all passwords use --show", true);
				return 1;
			}
			if (_p.check_limits(0, true))
				return 1;
			count = _p.search_and_get_pass(note, out pass);
			if (count == errors.empty_array) {
				savepass.print(String.Format("there is no notes containing \"{0}\" as a substring.",
					note), false);
				return 1;
			} else if (count == errors.too_much_elemets) {
				savepass.print(String.Format("Too much notes compare to \"{0}\". Try to refine your query.",
					note), false);
				return 1;
			}
			put(on_screen, pass);
			return 0;
		}
	}
}
