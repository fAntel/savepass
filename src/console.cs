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
using System.Collections.Generic;
using Mono.Options;
using Mono.Unix;

namespace savepass
{
	public class console: IUI
	{
		private enum keys: byte { help = 0, version, add, change, del, get, on_screen,
			get_pass, list, search, file, conf_file, always_in_clipboard,
			always_save_time_of_change, show_date_time, format, save, system,
			config, filename };

		private passwds _p;
		private string _filename;
		private string _master;
		private Dictionary<keys, object> _dict;

		public console(string[] args)
		{
			_dict = new Dictionary<keys, object>();
			List<string> rest = null;
			var options = new OptionSet {
				Catalog.GetString("savepass is password saver"),
				"",
				Catalog.GetString("Usage: savepass [OPTIONS] [FILE]"),
				"",
				Catalog.GetString("Options:"),
				{ "a|add", Catalog.GetString("Add new password to the list"),
					v => { if (v != null) _dict.Add(keys.add, null); } },
				{ "c|change=", Catalog.GetString("Change password with number {N}"),
					v => _dict.Add(keys.change, (object) Convert.ToInt32(v)) },
				{ "d|delete=", Catalog.GetString("Delete password with number {N}"),
					v => _dict.Add(keys.del, (object) Convert.ToInt32(v)) },
				{ "g|get=", Catalog.GetString("Get password with number {N}"),
					v => _dict.Add(keys.get, (object) Convert.ToInt32(v)) },
#if WINDOWS || GTK
				{ "C|on_screen", Catalog.GetString("Get password on screen and not in the clipboard"),
					v => _dict.Add(keys.on_screen, (object) Convert.ToBoolean(v != null)) },
#endif
				{ "G|get_pass=", Catalog.GetString("Get password with note {NOTE}"),
					v => _dict.Add(keys.get_pass, (object) v) },
				{ "l|list", Catalog.GetString("Show list of passwords' notes"),
					v => { if (v != null) _dict.Add(keys.list, null); } },
				{ "s|search=", Catalog.GetString("Show list of passwords' notes like {NOTE}"),
					v => _dict.Add(keys.search, (object) v) },
				{ "f|file=", Catalog.GetString("Set the default {FILE}"),
					v => _dict.Add(keys.file, (object) v) },
				{ "version", Catalog.GetString("Show version"),
					v => { if (v != null) _dict.Add(keys.version, null); } },
				{ "h|help",  Catalog.GetString("Show this text"),
					v => { if (v != null) _dict.Add(keys.help, null); } },
				"",
				Catalog.GetString("Settings' options:"),
				{ "conf_file=", Catalog.GetString("{*.conf} file with settings"),
					v => _dict.Add(keys.conf_file, (object) v) },
				{"A|always_in_clipboard", Catalog.GetString("Set {mod} of --get option"),
					v => _dict.Add(keys.always_in_clipboard, (object) Convert.ToBoolean(v != null)) },
				{"t|always_save_time_of_change", Catalog.GetString("Set {mod} of --change option"),
					v => _dict.Add(keys.always_save_time_of_change, (object) Convert.ToBoolean(v != null)) },
				{"H|show_date_time", Catalog.GetString("Show date and time when used --list and --search options"),
					v => _dict.Add(keys.show_date_time, (object) Convert.ToBoolean(v != null)) },
				{"format=", Catalog.GetString("Set {format} for date and time output"),
					v => _dict.Add(keys.format, (object) v) },
				{"S|save", Catalog.GetString("Save new settings passed via the command line and exit"),
					v => { if (v != null) _dict.Add(keys.save, null); } },
				{"system", Catalog.GetString("Work with settings in system configuration file"),
					v => { if (v != null) _dict.Add(keys.system, null); } },
				{"config=", Catalog.GetString("Show values of all/setted/system/user settings"),
					v => _dict.Add(keys.config, (object) v) },
				"",
				Catalog.GetString("Report bugs to: keldzh@gmail.com")
			};
			if (args.Length == 0) {
				options.WriteOptionDescriptions(Console.Out);
				Environment.ExitCode = 1;
				throw new ArgumentException();
			}
			try {
				rest = options.Parse(args);
			} catch (OptionException e) {
				savepass.print(string.Format(Catalog.GetString(
					"option parsing failed: {0}\nTry `{1} --help' for more information."),
					e.Message, Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName)),
					true);
				Environment.ExitCode = 2;
				throw new OptionException();
			} catch (FormatException e) {
				savepass.print(string.Format(Catalog.GetString(
					"option parsing failed: {0}\nTry `{1} --help' for more information."), e.Message), true);
				Environment.ExitCode = 2;
				throw new FormatException();
			} catch (Exception e) {
				savepass.print(e.Message, true);
				Environment.ExitCode = 2;
				throw new Exception();
			}
			if (_dict.ContainsKey(keys.help)) {
				options.WriteOptionDescriptions(Console.Out);
				Environment.ExitCode = 0;
				throw new AllOKException();
			}
			if (_dict.ContainsKey(keys.version)) {
				Console.WriteLine(
					"{0} {1}\n" +
					"Copyright (C) 2015 Anton Kovalyov\n" +
					"License GPLv3: GNU GPL version 3 or later <http://www.gnu.org/licenses/gpl-3.0.html>\n" +
					"This program comes with ABSOLUTELY NO WARRANTY, to the extent permitted by law.\n" +
					"This is free software, and you are welcome to redistribute it\n" +
					"under certain conditions.",
					savepass.program_name, savepass.version_number);
				Environment.ExitCode = 0;
				throw new AllOKException();
			}
			if (rest.Count > 1) {
				savepass.print(Catalog.GetString("too much options"), false);
				Environment.ExitCode = 1;
				throw new ArgumentException();
			} else if (rest.Count == 1)
				_dict.Add(keys.filename, (object) rest[0]);

		}

		/* Add new password in array of passwords */
		public int add(ref bool changed)
		{
			string password0, password1, note;

			try {
				do {
					Console.Write(Catalog.GetString("Enter password: "));
					password0 = read_password();
					Console.Write(Catalog.GetString("Enter password again: "));
					password1 = read_password();
					if (!password0.Equals(password1, StringComparison.Ordinal)) {
						Console.WriteLine(Catalog.GetString("Passwords doesn't match. Try again"));
					}
				} while (!password0.Equals(password1, StringComparison.Ordinal));
				Console.Write(Catalog.GetString("Enter note: "));
				note = Console.ReadLine();
			} catch (IOException e) {
				savepass.print(e.Message, true);
				return 2;
			} catch (OutOfMemoryException e) {
				savepass.print(e.Message, true);
				return 2;
			} catch (ArgumentOutOfRangeException e) {
				savepass.print(e.Message, true);
				return 2;
			}
			_p.add(password0, note);
			changed = true;
			return 0;
		}

		/* return 0 if user sure */
		private static int are_you_sure(string str)
		{
			string answer;

			try {
				Console.Write(String.Format(Catalog.GetString(
					"Are you sure you want to delete password with note \"{0}\"? (y/n) "), str));
				answer = Console.ReadLine();
				if (answer == null)
					return 1;
				return answer.Equals("y", StringComparison.CurrentCultureIgnoreCase) ? 0 : 1;
			} catch (IOException e) {
				savepass.print(e.Message, true);
				return 2;
			} catch (Exception e) {
				savepass.print(e.Message, true);
				return 2;
			}
		}

		/* Change password and/or note in n element of array of passwords */
		public int change(int n, ref bool changed)
		{
			string pass, note, str0, str1;
			if (!_p.get_pass_note(n - 1, out pass, out note))
				return 1;
			try {
				do {
					Console.Write(Catalog.GetString(
						"Enter new password (if you press ENTER password will stay the same): "));
					str0 = read_password();
					if (str0.Length == 0)
						break;
					Console.Write(Catalog.GetString("Enter new password again: "));
					str1 = read_password();
					if (!str0.Equals(str1, StringComparison.Ordinal))
						Console.WriteLine(Catalog.GetString(
							"Passwords doesn't match. Try again"));
				} while (!str0.Equals(str1, StringComparison.Ordinal));
				pass = str0.Length > 0 ? str0 : null;
				Console.Write(Catalog.GetString("Enter new note [{0}]: "), note);
				str0 = Console.ReadLine();
				note = str0.Length > 0 ? str0 : null;
			} catch (IOException e) {
				savepass.print(e.Message, true);
				return 2;
			} catch (OutOfMemoryException e) {
				savepass.print(e.Message, true);
				return 2;
			} catch (ArgumentOutOfRangeException e) {
				savepass.print(e.Message, true);
				return 2;
			}
			_p.change(n - 1, pass, note);
			changed |= pass != null || note != null;
			return 0;
		}

		/* Parse options for configuration file and set _filename */
		public int config(out conf c)
		{
			c = null;
			string conf_file = _dict.ContainsKey(keys.conf_file) ? (string) _dict[keys.conf_file] : null;
			string config_option = _dict.ContainsKey(keys.config) ? (string) _dict[keys.config] : null;
			try {
				c = new conf(conf_file,
					_dict.ContainsKey(keys.system) ||
					String.Equals(config_option, "system", StringComparison.OrdinalIgnoreCase));
			} catch (Exception) {
				return 2;
			}
			if (_dict.ContainsKey(keys.file) && !String.IsNullOrWhiteSpace((string) _dict[keys.file])) {
				c.default_file = (string) _dict[keys.file];
				c.Save();
				_filename = (string) _dict[keys.file];
			} 
			if (_dict.ContainsKey(keys.config)) {
				if (String.Equals(config_option, "all", StringComparison.OrdinalIgnoreCase))
					c.list_all();
				else if (String.Equals(config_option, "setted", StringComparison.OrdinalIgnoreCase))
					c.list_setted(null);
				else if (String.Equals(config_option, "system", StringComparison.OrdinalIgnoreCase))
					c.list(true);
				else if (String.Equals(config_option, "user", StringComparison.OrdinalIgnoreCase))
					c.list(false);
				else {
					savepass.print(Catalog.GetString(
						"wrong argument for config option"), false);
					return 1;
				}
				Environment.ExitCode = 0;
				throw new AllOKException();
			} else if (_dict.ContainsKey(keys.filename))
				_filename = (string) _dict[keys.filename];
			if (_dict.ContainsKey(keys.always_in_clipboard))
				c.always_in_clipboard = (bool) _dict[keys.always_in_clipboard];
			if (_dict.ContainsKey(keys.always_save_time_of_change))
				c.always_save_time_of_change = (bool) _dict[keys.always_save_time_of_change];
			if (_dict.ContainsKey(keys.show_date_time))
				c.show_date_time = (bool) _dict[keys.show_date_time];
			if (_dict.ContainsKey(keys.format) &&
				!String.IsNullOrWhiteSpace((string) _dict[keys.format]))
				c.format_date_time = (string) _dict[keys.format];
			if (_dict.ContainsKey(keys.save))
				c.Save();
			if (_filename == null) {
				if (_dict.ContainsKey(keys.save)) {
					Environment.ExitCode = 0;
					throw new AllOKException();
				}
				if (c.default_file != null) {
					_filename = c.default_file;
				} else {
					savepass.print(string.Format(Catalog.GetString(
						"File name must be specified\nTry run {0} --help for more information"),
						Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName)), false);
					return 1;
				}
			}
			return 0;
		}

		/* Remove password with number n from array of passwords */
		public int del(int n, ref bool changed)
		{
			string pass, note;
			int return_value;

			--n;
			if (_p.check_limits(n, true))
				return 1;
			_p.get_pass_note(n, out pass, out note);
			return_value = are_you_sure(note);
			if (return_value == 0) {
				_p.del(n);
				changed = true;
				savepass.print(Catalog.GetString("password was deleted"), false);
			}
			return return_value == 2 ? 2 : 0;
		}

		/* Show password with number n
		 * if on_screen == true then password will be
		 * printed on the screen, otherwise it will be
		 * copied to clipboard */
		public int get_nth_pass(int n, bool on_screen)
		{
			string pass, note;

			--n;
			if (_p.check_limits(n, true))
				return 1;
			_p.get_pass_note(n, out pass, out note);
			put(on_screen, pass);
			return 0;
		}

		/* Show the list of notes */
		public int list()
		{
			int result;

			if (_p.check_limits(0, true))
				return 1;
			Console.WriteLine(Catalog.GetString("Passwords' notes:"));
			int i = 0;
			foreach (passwd p in _p)
				if ((result = print_note(++i, p.note, p.time)) != 0)
					return result;
			return 0;
		}

		/* Print note with given format */
		private static int print_note(int i, string note, DateTime time)
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
				savepass.print(String.Format(Catalog.GetString(
					"date_time_format is invalid: {0}"), e.Message), false);
				return 1;
			} catch (Exception e) {
				savepass.print(e.Message, true);
				return 2;
			}
			return 0;
		}

		/* Print string or put it into clipboard */
		private static void put(bool on_screen, string str)
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

		/* Read characters from stdin but do not echo them */
		private static String read_password()
		{
			var password = new StringBuilder();
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

		/* Process the other command line parameters */
		public void run()
		{
			int exit_value = 0;
			bool changed = false;
			byte[] data;
			bool on_screen = _dict.ContainsKey(keys.on_screen) ? (bool) _dict[keys.on_screen] :
				#if WINDOWS || GTK
					false;
				#else
					true;
				#endif
				
			try {
				while (true) {
					Console.Write(Catalog.GetString("Enter master password: "));
					_master = read_password();
					if (_master.Length < 4)
						Console.WriteLine(Catalog.GetString(
							"The length of the master password must be 4 or " +
							"more characters. Try again"));
					else if (_master.Length > 56)
						Console.WriteLine(Catalog.GetString(
							"The length of the master password must be no more " +
							"than 56 characters. Try again"));
					else
						break;
				}
			} catch (IOException e) {
				savepass.print(e.Message, true);
				Environment.ExitCode = 2;
				return;
			} catch (InvalidOperationException e) {
				savepass.print(e.Message, true);
				Environment.ExitCode = 2;
				return;
			}

			data = file.read_from_file(_filename, _master);
			if (data == null) {
				Environment.ExitCode = 2;
				return;
			}
			_p = new passwds(data);

#if GTK
			Gtk.Application.Init();
#endif

			if (_dict.ContainsKey(keys.list))
				exit_value = list();
			else if (_dict.ContainsKey(keys.search))
				exit_value = search((string) _dict[keys.search]);
			else if (_dict.ContainsKey(keys.get))
				exit_value = get_nth_pass((int) _dict[keys.get], on_screen);
			else if (_dict.ContainsKey(keys.get_pass))
				exit_value = search_and_get_pass((string) _dict[keys.get_pass], on_screen);
			else if (_dict.ContainsKey(keys.add))
				exit_value = add(ref changed);
			else if (_dict.ContainsKey(keys.change))
				exit_value = change((int) _dict[keys.change], ref changed);
			else if (_dict.ContainsKey(keys.del))
				exit_value = del((int) _dict[keys.del], ref changed);
			Environment.ExitCode = exit_value;
			if (changed)
				file.write_to_file(_filename, _p.to_data(), _master);
		}

		/* Find and print all notes in array with given note as a substring */
		public int search(string note)
		{
			int result;
			int[] indexes;
			string[] notes;
			DateTime[] times;

			if (note == "") {
				savepass.print(Catalog.GetString(
					"string for search cannot be an empty string.\n" +
					"If you want to see all passwords use --list"), false);
				return 1;
			}
			if (_p.check_limits(0, true))
				return 1;
			try {
				_p.search(note, out indexes, out notes, out times);
			} catch (EmptyArrayException e) {
				savepass.print(e.Message, false);
				return 1;
			}
			Console.WriteLine(Catalog.GetString(
				"Passwords' notes contains \"{0}\":"), note);
			for (int i = 0; i < notes.Length; ++i)
				if ((result = print_note(indexes[i], notes[i], times[i])) != 0)
					return result;
			return 0;
		}

		/* Search password in array with given note */
		public int search_and_get_pass(string note, bool on_screen)
		{
			string pass;

			if (String.IsNullOrWhiteSpace(note)) {
				savepass.print(Catalog.GetString(
					"string for search cannot be an empty string\n" +
					"or cosists exclusively of white-space characters.\n" +
					"If you want to see all passwords use --list"), true);
				return 1;
			}
			if (_p.check_limits(0, true))
				return 1;
			try {
				_p.search_and_get_pass(note, out pass);
			} catch (EmptyArrayException e) {
				savepass.print(e.Message, false);
				return 1;
			} catch (ArgumentOutOfRangeException e) {
				savepass.print(e.Message, false);
				return 1;
			}
			put(on_screen, pass);
			return 0;
		}
	}
}
