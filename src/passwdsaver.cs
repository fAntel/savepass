//  
//  passwdsaver.cs
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

namespace passwdsaver
{
	public class passwdsaver
	{
		private const string version_number = "0.3";
		public static conf c;

		static int Main(string [] args)
		{
			bool add = false, help = false, list = false, version = false;
			bool A = false, A_seted = false, h = false, H = false, S = false;
#if WINDOWS || GTK
			bool on_screen = false;
#else
			bool on_screen = true;
#endif
			byte change = 0, del = 0, get = 0;
			int exit_value = 0;
			string conf_file = null, f = null, format = null, get_pass = null, search = null;
			string name = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
			OptionSet options = new OptionSet() {
				"Usage: passwdsaver [OPTIONS] -f FILE - password saver",
				"",
				"Options:",
				{ "a|add", "Add new password to the list", v => add = v != null },
				{ "c|change=", "Change password with number {N}", v => change = Convert.ToByte(v) },
				{ "d|delete=", "Delete password with number {N}", v => del = Convert.ToByte(v) },
				{ "g|get=", "Get password with number {N}", v => get = Convert.ToByte(v) },
#if WINDOWS || GTK
				{ "C|on_screen", "Get password on screen. Must be used with --get", v => on_screen = (v == null ? false : true)},
#endif
				{ "G|get_pass=", "Get password with note {NOTE}", v => get_pass = v},
				{ "l|list", "Show list of passwords' notes", v => list = v != null },
				{ "s|search=", "Show list of passwords' notes like {NOTE}", v => search = v},
				{ "f|file=", "{FILE} with the list of passwords", v => f = v },
				{ "version", "Show version", v => version = v != null },
				{ "help",  "Show this text", v => help = v != null },
				"Settings options:",
				{ "conf_file=", "{.conf} file with settings", v => conf_file = v },
				{"A|always_in_clipboard=", "Set {mod} (true or false) of --get option", v => { A = Convert.ToBoolean(v); A_seted = true;} },
				{"h|show_date_time", "Show date and time when used --show and --search options", v => h = v != null},
				{"H|no_date_time", "Don't show date and time when use --show and --search options", v => H = v != null},
				{"format=", "Set {format} for date and time output", v => format = v},
				{"S|save", "Save new settings passed via the command line", v => S = v != null},
			};
#if GTK
			Gtk.Application.Init();
#endif
			if (args.Length == 0) {
				options.WriteOptionDescriptions(Console.Out);
				return 1;
			}
			try {
				options.Parse(args);
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
			c = new conf(conf_file);
			/* Process the command line parameters related with the settings */
			if (A_seted)
				c.always_in_clipboard = A;
			if (h || H)
				c.show_date_time = H ? false : true;
			if (!String.IsNullOrWhiteSpace(format))
				c.format_date_time = format;
			if (S)
				c.Save();
			if (f == null) {
				if (S)
					return 0;
				print(string.Format("File name must be specified\nTry run {0} --help for more information", name), false);
				return 1;
			}
			passwds p = new passwds(file.read_from_file(f));
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
				exit_value = p.add();
			else if (change > 0)
				exit_value = p.change(change);
			else if (del > 0)
				exit_value = p.del(del);
			if (exit_value == 0)
				file.write_to_file(f, p.ToString());
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
