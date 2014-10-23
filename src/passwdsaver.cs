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
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Mono.Options;

namespace passwdsaver
{
	public class passwdsaver
	{
		static int Main(string [] args)
		{
			bool add = false, help = false, show = false, version = false;
#if WINDOWS || GTK
			bool on_screen = false;
#else
			bool on_screen = true;
#endif
			byte del = 0, get = 0;
			int exit_value = 0;
			string f = null;
			string name = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);
			OptionSet options = new OptionSet() {
				"Usage: passwdsaver [OPTIONS] -f FILE - password saver",
				"",
				"Options:",
				{ "a|add", "Add new password to the list", v => add = v != null },
				{ "d|delete=", "Delete password with number {N}", v => del = Convert.ToByte(v) },
				{ "g|get=", "Get password with number {N}", v => get = Convert.ToByte(v) },
#if WINDOWS || GTK
				{ "C|on_screen", "Get password on screen. Must be used with --get", v => on_screen = (v == null ? false : true)},
#endif
				{ "s|show", "Show list of passwords' notes", v => show = v != null },
				{ "f|file=", "{FILE} with the list of passwords", v => f = v },
				{ "version", "Show version", v => version = v != null },
				{ "h|help",  "Show this text", v => help = v != null }
			};
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
					"{0}\n" +
					"Copyright (C) 2014 Anton Kovalyov\n" +
					"License GPLv3: GNU GPL version 3 or later <http://www.gnu.org/licenses/gpl-3.0.html>\n" +
					"This program comes with ABSOLUTELY NO WARRANTY, to the extent permitted by law.\n" +
					"This is free software, and you are welcome to redistribute it\n" +
					"under certain conditions.",
					Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName));
				return 0;
			}
			if (f == null) {
				print(string.Format("File name must be specified\nTry run {0} --help for more information", name), false);
				return 1;
			}
			passwds p = new passwds(file.read_from_file(f));
			if (show)
				exit_value = p.show();
			else if (get > 0)
				exit_value = p.get(get, on_screen);
			else if (add)
				exit_value = p.add();
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
