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
		private Dictionary<keys, object> dict;

		public console(string[] args, string version_number)
		{
			dict = new Dictionary<keys, object>();
			List<string> rest = null;
			OptionSet options = new OptionSet() {
				"Usage: savepass [OPTIONS] FILE - password saver",
				"",
				"Options:",
				{ "a|add", "Add new password to the list",
					v => { if (v != null) dict.Add(keys.add, null); } },
				{ "c|change=", "Change password with number {N}",
					v => { dict.Add(keys.change, (object) Convert.ToInt32(v)); } },
				{ "d|delete=", "Delete password with number {N}",
					v => { dict.Add(keys.del, (object) Convert.ToInt32(v)); } },
				{ "g|get=", "Get password with number {N}",
					v => { dict.Add(keys.get, (object) Convert.ToInt32(v)); } },
#if WINDOWS || GTK
				{ "C|on_screen", "Get password on screen and not in the clipboard",
					v => { dict.Add(keys.on_screen, (object) Convert.ToBoolean(v != null)); } },
#endif
				{ "G|get_pass=", "Get password with note {NOTE}",
					v => { dict.Add(keys.get_pass, (object) v); } },
				{ "l|list", "Show list of passwords' notes",
					v => { if (v != null) dict.Add(keys.list, null); } },
				{ "s|search=", "Show list of passwords' notes like {NOTE}",
					v => { dict.Add(keys.search, (object) v); } },
				{ "f|file=", "Set the default {FILE}",
					v => { dict.Add(keys.file, (object) v); } },
				{ "version", "Show version",
					v => { if (v != null) dict.Add(keys.version, null); } },
				{ "h|help",  "Show this text",
					v => { if (v != null) dict.Add(keys.help, null); } },
				"Settings options:",
				{ "conf_file=", "{*.conf} file with settings",
					v => { dict.Add(keys.conf_file, (object) v); } },
				{"A|always_in_clipboard", "Set {mod} of --get option",
					v => { dict.Add(keys.always_in_clipboard, (object) Convert.ToBoolean(v != null)); } },
				{"t|always_save_time_of_change=", "Set {mod} of --change option",
					v => { dict.Add(keys.always_save_time_of_change, (object) Convert.ToBoolean(v != null)); } },
				{"H|show_date_time", "Show date and time when used --list and --search options",
					v => { dict.Add(keys.show_date_time, (object) Convert.ToBoolean(v != null)); } },
				{"format=", "Set {format} for date and time output",
					v => { dict.Add(keys.format, (object) v); } },
				{"S|save", "Save new settings passed via the command line", 
					v => { if (v != null) dict.Add(keys.save, null); } },
				{"system", "Work with settings in system configuration file",
					v => { if (v != null) dict.Add(keys.system, null); } },
				{"config=", "Show values of all/setted/system/user settings",
					v => { dict.Add(keys.config, (object) v); } }
			};
			if (args.Length == 0) {
				options.WriteOptionDescriptions(Console.Out);
				Environment.ExitCode = 1;
				throw new ArgumentException();
			}
			try {
				rest = options.Parse(args);
			} catch (OptionException e) {
				savepass.print(string.Format("option parsing failed: {0}\nTry `{1} --help' for more information.",
					e.Message, Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName)),
					true);
				Environment.ExitCode = 2;
				throw new OptionException();
			} catch (FormatException e) {
				savepass.print(string.Format("number was written wrong: {0}", e.Message), true);
				Environment.ExitCode = 2;
				throw new FormatException();
			} catch (Exception e) {
				savepass.print(e.Message, true);
				Environment.ExitCode = 2;
				throw new Exception();
			}
			if (dict.ContainsKey(keys.help)) {
				options.WriteOptionDescriptions(Console.Out);
				Environment.ExitCode = 0;
				throw new AllOKException();
			}
			if (dict.ContainsKey(keys.version)) {
				Console.WriteLine(
					"{0} {1}\n" +
					"Copyright (C) 2014 Anton Kovalyov\n" +
					"License GPLv3: GNU GPL version 3 or later <http://www.gnu.org/licenses/gpl-3.0.html>\n" +
					"This program comes with ABSOLUTELY NO WARRANTY, to the extent permitted by law.\n" +
					"This is free software, and you are welcome to redistribute it\n" +
					"under certain conditions.",
					Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName), version_number);
				Environment.ExitCode = 0;
				throw new AllOKException();
			}
			if (rest.Count > 1) {
				savepass.print("too much options", false);
				Environment.ExitCode = 1;
				throw new ArgumentException();
			} else if (rest.Count == 1)
				dict.Add(keys.filename, (object) rest[0]);

		}

		public passwds p
		{
			get { return _p; }
		}

		public string filename
		{
			get { return _filename; }
		}

		public string master
		{
			get { return _master; }
		}

		/* Add new password in array of passwords */
		public int add()
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

		private static int are_you_sure(string str)
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
		public int change(int n)
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

		/* Parse options for configuration file and set _filename */
		public int config(out conf c)
		{
			string conf_file = dict.ContainsKey(keys.conf_file) ? (string) dict[keys.conf_file] : null;
			string config = dict.ContainsKey(keys.config) ? (string) dict[keys.config] : null;
			c = new conf(conf_file,
				dict.ContainsKey(keys.system) ||
				String.Compare(config, "system") == 0);
			if (dict.ContainsKey(keys.file) && !String.IsNullOrWhiteSpace((string) dict[keys.file])) {
				c.default_file = (string) dict[keys.file];
				c.Save();
				_filename = (string) dict[keys.file];
			} 
			if (dict.ContainsKey(keys.config)) {
				if (String.Compare(config, "all") == 0)
					c.list_all();
				else if (String.Compare(config, "setted") == 0)
					c.list_setted(null);
				else if (String.Compare(config, "system") == 0)
					c.list(true);
				else if (String.Compare(config, "user") == 0)
					c.list(false);
				else {
					savepass.print("wrong argument for config option", false);
					return 1;
				}
				Environment.ExitCode = 0;
				throw new AllOKException();
			} else if (dict.ContainsKey(keys.filename))
				_filename = (string) dict[keys.filename];
			if (dict.ContainsKey(keys.always_in_clipboard))
				c.always_in_clipboard = (bool) dict[keys.always_in_clipboard];
			if (dict.ContainsKey(keys.always_save_time_of_change))
				c.always_save_time_of_change = (bool) dict[keys.always_save_time_of_change];
			if (dict.ContainsKey(keys.show_date_time))
				c.show_date_time = (bool) dict[keys.show_date_time];
			if (dict.ContainsKey(keys.format) &&
				!String.IsNullOrWhiteSpace((string) dict[keys.format]))
				c.format_date_time = (string) dict[keys.format];
			if (dict.ContainsKey(keys.save))
				c.Save();
			if (_filename == null) {
				if (c.default_file != null) {
					_filename = c.default_file;
				} else if (dict.ContainsKey(keys.save)) {
					Environment.ExitCode = 0;
					throw new AllOKException();
				} else {
					savepass.print(string.Format(
						"File name must be specified\nTry run {0} --help for more information",
						Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName)), false);
					return 1;
				}
			}
			return 0;
		}

		/* Remove password with number n from array of passwords */
		public int del(int n)
		{
			string pass, note;
			int return_value;

			--n;
			if (_p.check_limits(n, true))
				return 1;
			_p.get_pass_note(n, out pass, out note);
			return_value = are_you_sure(
				String.Format(" you want to delete password with note \"{0}\"", note));
			if (return_value == 0) {
				_p.del(n);
				savepass.print("password was deleted", false);
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
			string[] notes;
			DateTime[] times;

			if (_p.check_limits(0, true))
				return 1;
			_p.list(out notes, out times);
			Console.WriteLine("Passwords' notes:");
			for (int i = 0; i < notes.Length; ++i)
				if ((result = print_note(i + 1, notes[i], times[i])) != 0)
					return result;
			return 0;
		}

		/* Print note with given format */
		private int print_note(int i, string note, DateTime time)
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

		/* Process the other command line parameters */
		public int run()
		{
			int exit_value = 0;
			byte[] data;
			bool on_screen = dict.ContainsKey(keys.on_screen) ? (bool) dict[keys.on_screen] :
				#if WINDOWS || GTK
					false;
				#else
					true;
				#endif
				
			try {
				Console.Write("Enter master password: ");
				_master = read_password();
			} catch (IOException e) {
				savepass.print(String.Format("some error with console: {0}", e.Message), true);
				return 2;
			} catch (InvalidOperationException e) {
				savepass.print(e.Message, true);
				return 2;
			}

			data = file.read_from_file(_filename, _master);
			if (data == null)
				return 2;
			_p = new passwds(data);

			if (dict.ContainsKey(keys.list))
				exit_value = list();
			else if (dict.ContainsKey(keys.search))
				exit_value = search((string) dict[keys.search]);
			else if (dict.ContainsKey(keys.get))
				exit_value = get_nth_pass((int) dict[keys.get], on_screen);
			else if (dict.ContainsKey(keys.get_pass))
				exit_value = search_and_get_pass((string) dict[keys.get_pass], on_screen);
			else if (dict.ContainsKey(keys.add))
				exit_value = add();
			else if (dict.ContainsKey(keys.change))
				exit_value = change((int) dict[keys.change]);
			else if (dict.ContainsKey(keys.del))
				exit_value = del((int) dict[keys.del]);
			return exit_value;
		}

		/* Find and print all notes in array with given note as a substring */
		public int search(string note)
		{
			int result;
			int[] indexes;
			string[] notes;
			DateTime[] times;

			if (note == "") {
				savepass.print("string for search cannot be an empty string.\n" +
					"If you want to see all passwords use --show", false);
				return 1;
			}
			if (_p.check_limits(0, true))
				return 1;
			try {
				p.search(note, out indexes, out notes, out times);
			} catch (EmptyArrayException e) {
				savepass.print(e.Message, false);
				return 1;
			}
			Console.WriteLine("Passwords' notes contains \"{0}\":", note);
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
				savepass.print("string for search cannot be an empty string\n" +
					"or cosists exclusively of white-space characters.\n" +
					"If you want to see all passwords use --show", true);
				return 1;
			}
			if (_p.check_limits(0, true))
				return 1;
			try {
				p.search_and_get_pass(note, out pass);
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
