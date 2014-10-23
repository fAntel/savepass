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
				if (str != "")
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
				passwdsaver.print(String.Format("some error with console: {0}", e.Message), false);
				return 2;
			} catch (OutOfMemoryException e) {
				passwdsaver.print(e.Message, false);
				return 2;
			} catch (ArgumentOutOfRangeException e) {
				passwdsaver.print(e.Message, false);
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
				passwdsaver.print(String.Format("some error with console: {0}", e.Message), false);
				return 2;
			} catch (Exception e) {
				passwdsaver.print(e.Message, false);
				return 2;
			}
		}

		/* Change password with number n */
		public int change(byte n)
		{
			string password, note;
			if (check_limits(n, false))
				return 1;
			passwd new_passwd = new passwd(_passwds[n - 1]);
			try {
				Console.Write("Enter new password (if you press ENTER password will stay the same): ");
				password = Console.ReadLine();
				if (password.Length != 0)
					new_passwd.password = password;
				Console.Write("Enter new note [{0}]: ", new_passwd.note);
				note = Console.ReadLine();
				if (note.Length != 0)
					new_passwd.note = note;
			} catch (IOException e) {
				passwdsaver.print(String.Format("some error with console: {0}", e.Message), false);
				return 2;
			} catch (OutOfMemoryException e) {
				passwdsaver.print(e.Message, false);
				return 2;
			} catch (ArgumentOutOfRangeException e) {
				passwdsaver.print(e.Message, false);
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
		public int get(byte n, Boolean on_screen)
		{
			if (check_limits(n, true))
				return 1;
			if (on_screen)
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

		/* Search password in array with given note */
		public int search(string note)
		{
			if (check_limits(0, true))
				return 1;
			if (note == "") {
				passwdsaver.print("string for search cannot be an empty string.\n" +
					"If you want to see all passwords use --show", true);
				return 1;
			}
			List<passwd> result = _passwds.FindAll(
				delegate(passwd p) {
					return p.note.Contains(note);
				});
			Console.WriteLine("Passwords' notes contains \"{0}\":", note);
			foreach (passwd p in result)
				Console.WriteLine("{0,3}) {1}", _passwds.IndexOf(p) + 1, p.note);
			return 0;
		}

		/* Show the list of notes */
		public int show()
		{
			if (check_limits(0, true))
				return 1;
			try {
				Console.WriteLine("Passwords' notes:");
				for (int i = 0; i < _passwds.Count; ++i)
					Console.WriteLine("{0,3}) {1}", i+1, _passwds[i].note);
			} catch (IOException e) {
				passwdsaver.print(e.Message, true);
				return 2;
			}
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
