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

namespace savepass
{
	public class savepass
	{
		public const string program_name = "savepass";
		public const string version_number = "0.7.1";
		public static conf c;

		static int Main(string[] args)
		{
			int exit_value = 0;
			bool changed = false;
			Mono.Unix.Catalog.Init(program_name, "po");
			IUI ui;
			try {
				ui = new console(args);
				exit_value = ui.config(out c);
			} catch (Exception) {
				return Environment.ExitCode;
			}
			if (exit_value != 0)
				return exit_value;
#if GTK
			Gtk.Application.Init();
#endif
			changed = ui.run();
			if (changed)
				file.write_to_file(ui.filename, ui.p.to_data(), ui.master);
			return Environment.ExitCode;
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

	public class AllOKException: Exception
	{
		public AllOKException(): base(){}
		public AllOKException(string message): base(message) {}
		public AllOKException(int value, string message, Exception innerException): base(message, innerException) {}
	}

	public class EmptyArrayException: Exception
	{
		protected EmptyArrayException(): base(){}
		public EmptyArrayException(string message): base(message) {}
		public EmptyArrayException(int value, string message, Exception innerException): base(message, innerException) {}
	}
}
