//
//  conf.cs
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
using System.Collections.ObjectModel;
using System.IO;
using KeyFile;

namespace passwdsaver
{
	public class conf
	{
		/* Types for default values */
		private struct conf_settings
		{
			private readonly string _default_value;
			private readonly string _comment;

			public conf_settings(string default_value, string comment)
			{
				_default_value = default_value;
				_comment = comment;
			}

			public string default_value { get { return _default_value; } }
			public string comment { get { return _comment; } }

		}

		/* Default values for settings */
		private static readonly IList<conf_settings> default_settings = new ReadOnlyCollection<conf_settings>(new[] {
			/* always_in_clipboard */ new conf_settings("true",
				"Set this to \"false\" to option --get print password\n" +
				"# on the screen by default"),
			/* show_date_time */ new conf_settings("false",
				"Show date/time of adding or last changing password or comment"),
			/* format_date_time */ new conf_settings("", "")
		});

		/* Key file */
		private enum settings { always_in_clipboard = 0, show_date_time, format_date_time };
		private GKeyFile _conf;
		private bool settings_changed = false;
		private string _conf_file;

		public conf(string conf_file)
		{
			bool default_file = false;

			if (conf_file == null) {
				default_file = true;
				_conf_file = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName) + ".conf";
			} else
				_conf_file = conf_file;
			if (File.Exists(conf_file)) {
				if (!default_file)
					Console.WriteLine("File {0} doesn't exist. It will be created if any parametrs to be used", conf_file);
				settings_changed = true;
				try {
					_conf = new GKeyFile();
					_conf.LoadFromData(
						"[Passwords]\n" +
						"# " + default_settings[(int) settings.always_in_clipboard].comment + "\n" +
						"always_in_clipboard=" +
							default_settings[(int) settings.always_in_clipboard].default_value + "\n" +
						"[View]\n" +
						"# " + default_settings[(int) settings.show_date_time].comment + "\n" +
						"show_date_time=" +
							default_settings[(int) settings.show_date_time].default_value + "\n" +
						"# " + default_settings[(int) settings.format_date_time].comment + "\n" +
						"format_date_time=" + 
							default_settings[(int) settings.format_date_time].default_value,
						Flags.KeepComments | Flags.KeepTranslations);
				} catch (Exception e) {
					passwdsaver.print(String.Format("creating object for settings failed: {0}", e.Message),
						true);
				}
			} else {
				try {
					_conf = new GKeyFile(_conf_file, Flags.KeepComments | Flags.KeepTranslations);
				} catch (Exception e) {
					passwdsaver.print(String.Format("openging configuration file {0} failed: {1}",
						conf_file, e.Message), true);
				}
			}
		}

		~conf()
		{
			if (settings_changed)
				_conf.Save(_conf_file);
		}

		public bool always_in_clipboard
		{
			get { 
				bool v;
				try {
					v = _conf.GetBoolean("Passwords", "always_in_clipboard");
				} catch (GLib.GException e) {
					_conf.SetBoolean("Passwords", "always_in_clipboard",
						v = Convert.ToBoolean(default_settings[(int) settings.always_in_clipboard].default_value));
					_conf.SetComment("Passwords", "always_in_clipboard",
						default_settings[(int) settings.always_in_clipboard].comment);
					settings_changed = true;
				}
				return v;
			}
			set {
				_conf.SetBoolean("Passwords", "always_in_clipboard", value);
				settings_changed = true;
				try {
					_conf.GetComment("Passwords", "always_in_clipboard");
				} catch (GLib.GException e) {
					_conf.SetComment("Passwords", "always_in_clipboard",
						default_settings[(int) settings.always_in_clipboard].comment);
				}
			}
		}

		public bool show_date_time
		{
			get {
				bool v;
				try {
					v = _conf.GetBoolean("View", "show_date_time");
				} catch (GLib.GException e) {
					_conf.SetBoolean("View", "show_date_time",
						v = Convert.ToBoolean(default_settings[(int) settings.show_date_time].default_value));
					_conf.SetComment("View", "show_date_time",
						default_settings[(int) settings.show_date_time].comment);
					settings_changed = true;
				}
				return v;
			}
			set {
				_conf.SetBoolean("View", "show_date_time", value);
				settings_changed = true;
				try {
					_conf.GetComment("View", "show_date_time");
				} catch (GLib.GException e) {
					_conf.SetComment("View", "show_date_time",
						default_settings[(int) settings.show_date_time].comment);
				}
			}
		}

		public string format_date_time
		{
			get {
				string v;
				try {
					v = _conf.GetString("View", "format_date_time");
				} catch (GLib.GException e) {
					_conf.SetString("View", "format_date_time",
						v = default_settings[(int) settings.format_date_time].default_value);
					_conf.SetComment("View", "format_date_time",
						default_settings[(int) settings.format_date_time].comment);
					settings_changed = true;
				}
				return v;
			}
			set {
				_conf.SetString("View", "format_date_time", value);
				settings_changed = true;
				try {
					_conf.GetComment("View", "format_date_time");
				} catch (GLib.GException e) {
					_conf.SetComment("View", "format_date_time",
						default_settings[(int) settings.format_date_time].comment);
				}
			}
		}
	}
}
