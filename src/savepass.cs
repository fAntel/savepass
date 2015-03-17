//  
//  savepass.cs
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
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Mono.Options;

namespace savepass
{
	public class savepass
	{
		private const string version_number = "0.7";
		public static conf c;

		static int Main(string [] args)
		{
			bool add = false, A = false, A_setted = false, H = false, h = false;
			bool help = false, list = false, S = false, sys = false;
			bool t = false, t_setted = false, version = false;
#if WINDOWS || GTK
			bool on_screen = false;
#else
			bool on_screen = true;
#endif
			byte change = 0, del = 0, get = 0;
			int exit_value = 0;
			string conf_file = null, config = null, f = null, format = null;
			string get_pass = null, search = null;
			string name = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
			string filename = null;
			System.Collections.Generic.List<string> rest;
			OptionSet options = new OptionSet() {
				"Usage: savepass [OPTIONS] FILE - password saver",
				"",
				"Options:",
				{ "a|add", "Add new password to the list", v => add = v != null },
				{ "c|change=", "Change password with number {N}", v => change = Convert.ToByte(v) },
				{ "d|delete=", "Delete password with number {N}", v => del = Convert.ToByte(v) },
				{ "g|get=", "Get password with number {N}", v => get = Convert.ToByte(v) },
#if WINDOWS || GTK
				{ "C|on_screen", "Get password on screen. Must be used with --get", v => on_screen = (v != null)},
#endif
				{ "G|get_pass=", "Get password with note {NOTE}", v => get_pass = v},
				{ "l|list", "Show list of passwords' notes", v => list = v != null },
				{ "s|search=", "Show list of passwords' notes like {NOTE}", v => search = v},
				{ "f|file=", "Set the default {FILE}", v => f = v },
				{ "version", "Show version", v => version = v != null },
				{ "help",  "Show this text", v => help = v != null },
				"Settings options:",
				{ "conf_file=", "{.conf} file with settings", v => conf_file = v },
				{"A|always_in_clipboard=", "Set {mod} (true or false) of --get option", v => { A = Convert.ToBoolean(v); A_setted = true;} },
				{"t|always_save_time_of_change=", "Set {mod} (true or false) of --change option", v => { t = Convert.ToBoolean(v); t_setted = true;} },
				{"h|show_date_time", "Show date and time when used --show and --search options", v => h = v != null},
				{"H|no_date_time", "Don't show date and time when use --show and --search options", v => H = v != null},
				{"format=", "Set {format} for date and time output", v => format = v},
				{"S|save", "Save new settings passed via the command line", v => S = v != null},
				{"system", "Work with settings in system configuration file", v => sys = v != null},
				{"config=", "Show values of all/setted/system/user settings", v => config = v }
			};
#if GTK
			Gtk.Application.Init();
#endif
			if (args.Length == 0) {
				options.WriteOptionDescriptions(Console.Out);
				return 1;
			}
			try {
				rest = options.Parse(args);
			} catch (OptionException e) {
				print(string.Format("option parsing failed: {0}\nTry `{1} --help' for more information.", e.Message, name), true);
				return 2;
			} catch (FormatException e) {
				print(string.Format("number was written wrong: {0}", e.Message), true);
				return 2;
			} catch (Exception e) {
				print(e.Message, true);
				return 2;
			}
			if (help == true) {
				options.WriteOptionDescriptions(Console.Out);
				return 0;
			}
			if (version == true) {
				Console.WriteLine(
					"{0} {1}\n" +
					"Copyright (C) 2014 Anton Kovalyov\n" +
					"License GPLv3: GNU GPL version 3 or later <http://www.gnu.org/licenses/gpl-3.0.html>\n" +
					"This program comes with ABSOLUTELY NO WARRANTY, to the extent permitted by law.\n" +
					"This is free software, and you are welcome to redistribute it\n" +
					"under certain conditions.",
					Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName), version_number);
				return 0;
			}
			c = new conf(conf_file, sys || String.Compare(config, "system") == 0);
			if (!String.IsNullOrWhiteSpace(f)) {
				c.default_file = f;
				c.Save();
				filename = f;
			} 
			if (config != null) {
				if (String.Compare(config, "all") == 0)
					c.list_all();
				else if (String.Compare(config, "setted") == 0)
					c.list_setted(null);
				else if (String.Compare(config, "system") == 0)
					c.list(true);
				else if (String.Compare(config, "user") == 0)
					c.list(false);
				else {
					print("wrong argument for config option", false);
					return 1;
				}
				return 0;
			} else if (rest.Count > 1) {
				print("too much options", false);
				return 1;
			} else if (rest.Count == 1)
				filename = rest[0];
			if (A_setted)
				c.always_in_clipboard = A;
			if (t_setted)
				c.always_save_time_of_change = t;
			if (h || H)
				c.show_date_time = !H;
			if (!String.IsNullOrWhiteSpace(format))
				c.format_date_time = format;
			if (S)
				c.Save();
			if (filename == null) {
				if (c.default_file != null) {
					filename = c.default_file;
				} else if (S)
					return 0;
				else {
					print(string.Format("File name must be specified\nTry run {0} --help for more information", name), false);
					return 1;
				}
			}
			passwds p = new passwds(file.read_from_file(filename));
			IUI console = new console(p);
			/* Process the other command line parameters */
			if (list)
				exit_value = p.list();
			else if (search != null)
				exit_value = p.search(search);
			else if (get > 0)
				exit_value = p.get(get, on_screen);
			else if (get_pass != null)
				exit_value = p.get_pass(get_pass, on_screen);
			else if (add)
				exit_value = console.add();
			else if (change > 0)
				exit_value = p.change(change);
			else if (del > 0)
				exit_value = p.del(del);
			if (exit_value == 0)
				file.write_to_file(filename, p.ToString());
			return exit_value;
		}

		/* Print errors to the screen in certain format */
		public static void print(string msg, bool full, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
		{
			if (full)
				Console.WriteLine("{0}:{1}:{2}: {3}",
					Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName),
					Path.GetFileNameWithoutExtension(file), line, msg);
			else
				Console.WriteLine("{0}: {1}",
			        	          Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName),
			                	  msg);
		}
	}
}
