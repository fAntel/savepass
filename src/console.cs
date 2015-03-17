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
using System;
using System.IO;
using System.Text;

namespace savepass
{
	public class console: IUI
	{
		private passwds _p;

		public console(passwds p)
		{
			_p = p;
		}
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
	}
}
