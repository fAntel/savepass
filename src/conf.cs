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
using GLib;
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
				"Set this to \"false\" that program print password\n" +
				"# on the screen by default rather than to clipboard"),
			/* show_date_time */ new conf_settings("false",
				"Show date/time of adding or last changing password or comment"),
			/* format_date_time */ new conf_settings("",
				"For format of string with date/time program use\n" +
				"# C# format for date/time. You can read more about\n" +
				"# it in Microsoft Developer Network article\n" +
				"# \"Custom Date and Time Format Strings\"\n" +
				"# (http://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx)")
		});

		private string default_settings_data =
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
			default_settings[(int) settings.format_date_time].default_value;

		/* Key file */
		private enum settings { always_in_clipboard = 0, show_date_time, format_date_time };
		private GKeyFile _conf = null, _system_conf = null, _user_conf = null;
		private string _conf_file = null, _system_conf_file = null, _user_conf_file = null;
		private bool _sys;

		public conf(string conf_file, bool sys)
		{
			_sys = sys;
			if (conf_file != null) {
				_conf_file = conf_file;
				if (!File.Exists(conf_file)) {
					passwdsaver.print(String.Format("configuration file {0} doesn't exists. " +
						"If -S option used it will be created", conf_file), false);
					_conf = new GKeyFile();
					return;
				}
				try {
					_conf = new GKeyFile(conf_file, Flags.KeepComments | Flags.KeepTranslations);
				} catch (Exception e) {
					passwdsaver.print(String.Format("openging configuration file {0} failed: {1}",
						conf_file, e.Message), true);
				}
				return;
			}
			_system_conf = load_system_settings(sys);
			_user_conf = load_user_settings();
			if (_system_conf == null && _user_conf == null) {
				try {
					_conf = new GKeyFile();
					_conf.LoadFromData(default_settings_data,
						Flags.KeepComments | Flags.KeepTranslations);
				} catch (Exception e) {
					passwdsaver.print(String.Format("creating object for default settings failed: {0}", e.Message),
						true);
				}
				return;
			}
			if (_system_conf != null && _user_conf != null) {
				_conf = merge_settings();
				return;
			}
			_conf = new GKeyFile();
			_conf.LoadFromData(_system_conf == null ? _user_conf.ToData() : _system_conf.ToData(),
				Flags.KeepComments | Flags.KeepTranslations);
		}

		/* Load settings from system file if there is one */
		private GKeyFile load_system_settings(bool sys)
		{
			GKeyFile file;
			// Looking for system config file
			_system_conf_file = Path.Combine(
				#if WINDOWS
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName),
				#else
				"/etc/",
				#endif
				Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName));
			_system_conf_file = Path.ChangeExtension(_system_conf_file, "conf");
			if (!File.Exists(_system_conf_file)) {
				if (sys)
					file = new GKeyFile();
				else
					file = null;
			} else {
				try {
					file = new GKeyFile(_system_conf_file, Flags.KeepComments | Flags.KeepTranslations);
				} catch (Exception e) {
					passwdsaver.print(String.Format("openging system configuration file {0} failed: {1}",
						_system_conf_file, e.Message), true);
					return null;
				}
			}
			return file;
		}

		/* Load settings from user file if there is one */
		private GKeyFile load_user_settings()
		{
			GKeyFile file;
			// Looking for user config file
			_user_conf_file = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName),
				Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName));
			_user_conf_file = Path.ChangeExtension(_user_conf_file, "conf");
			if (!File.Exists(_user_conf_file))
				return null;
			try {
				file = new GKeyFile(_user_conf_file, Flags.KeepComments | Flags.KeepTranslations);
			} catch (Exception e) {
				passwdsaver.print(String.Format("openging user configuration file {0} failed: {1}",
					_user_conf_file, e.Message), true);
				return null;
			}
			return file;
		}

		/* Merge settings from system config file with settings from conf_file */
		private GKeyFile merge_settings()
		{
			GKeyFile conf = new GKeyFile();
			conf.LoadFromData(_system_conf.ToData(), Flags.KeepTranslations | Flags.KeepTranslations);
			string[] groups = _user_conf.GetGroups();
			string[] keys;
			foreach (string g in groups) {
				keys = _user_conf.GetKeys(g);
				foreach (string key in keys) {
					conf.SetValue(g, key, _user_conf.GetValue(g, key));
				}
			}
			return conf;
		}

		public bool always_in_clipboard
		{
			get { 
				bool v;
				try {
					v = _conf.GetBoolean("Passwords", "always_in_clipboard");
				} catch (GException) {
					_conf.SetBoolean("Passwords", "always_in_clipboard",
						v = Convert.ToBoolean(default_settings[(int) settings.always_in_clipboard].default_value));
				}
				return v;
			}
			set {
				_conf.SetBoolean("Passwords", "always_in_clipboard", value);
				if (_sys)
					_system_conf.SetBoolean("Passwords", "always_in_clipboard", value);
				else if (_user_conf != null)
					_user_conf.SetBoolean("Passwords", "always_in_clipboard", value);
				try {
					if (String.IsNullOrWhiteSpace(_conf.GetComment("Passwords", "always_in_clipboard")))
						_conf.SetComment("Passwords", "always_in_clipboard",
							default_settings[(int) settings.always_in_clipboard].comment);
				} catch (GException e) {
					passwdsaver.print(String.Format("{0}", e.Message), true);

				}
				if (_sys)
					try {
						if (String.IsNullOrWhiteSpace(_system_conf.GetComment("Passwords", "always_in_clipboard")))
							_system_conf.SetComment("Passwords", "always_in_clipboard",
								default_settings[(int) settings.always_in_clipboard].comment);
					} catch (GException e) {
						passwdsaver.print(String.Format("{0}", e.Message), true);
					}
				else if (_user_conf != null)
					try {
						if (String.IsNullOrWhiteSpace(_user_conf.GetComment("Passwords", "always_in_clipboard")))
							_user_conf.SetComment("Passwords", "always_in_clipboard",
								default_settings[(int) settings.always_in_clipboard].comment);
					} catch (GException e) {
						passwdsaver.print(String.Format("{0}", e.Message), true);
					}
			}
		}

		public bool show_date_time
		{
			get {
				bool v;
				try {
					v = _conf.GetBoolean("View", "show_date_time");
				} catch (GException) {
					_conf.SetBoolean("View", "show_date_time",
						v = Convert.ToBoolean(default_settings[(int) settings.show_date_time].default_value));
				}
				return v;
			}
			set {
				_conf.SetBoolean("View", "show_date_time", value);
				if (_sys)
					_system_conf.SetBoolean("View", "show_date_time", value);
				else if (_user_conf != null)
					_user_conf.SetBoolean("View", "show_date_time", value);
				try {
					if (String.IsNullOrWhiteSpace(_conf.GetComment("View", "show_date_time")))
						_conf.SetComment("View", "show_date_time",
							default_settings[(int) settings.show_date_time].comment);
				} catch (GException e) {
					passwdsaver.print(String.Format("{0}", e.Message), true);
				}
				if (_sys)
					try {
						if (String.IsNullOrWhiteSpace(_system_conf.GetComment("View", "show_date_time")))
							_system_conf.SetComment("View", "show_date_time",
								default_settings[(int) settings.show_date_time].comment);
					} catch (GException e) {
						passwdsaver.print(String.Format("{0}", e.Message), true);
					}
				else if (_user_conf != null)
					try {
						if (String.IsNullOrWhiteSpace(_user_conf.GetComment("View", "show_date_time")))
							_user_conf.SetComment("View", "show_date_time",
								default_settings[(int) settings.show_date_time].comment);
					} catch (GException e) {
						passwdsaver.print(String.Format("{0}", e.Message), true);
					}
			}
		}

		public string format_date_time
		{
			get {
				string v;
				try {
					v = _conf.GetString("View", "format_date_time");
				} catch (GException) {
					_conf.SetString("View", "format_date_time",
						v = default_settings[(int) settings.format_date_time].default_value);
				}
				return v;
			}
			set {
				_conf.SetString("View", "format_date_time", value);
				if (_sys)
					_system_conf.SetString("View", "format_date_time", value);
				else if (_user_conf != null)
					_user_conf.SetString("View", "format_date_time", value);
				try {
					if (String.IsNullOrWhiteSpace(_conf.GetComment("View", "format_date_time")))
						_conf.SetComment("View", "format_date_time",
							default_settings[(int) settings.format_date_time].comment);
				} catch (GException e) {
					passwdsaver.print(String.Format("{0}", e.Message), true);
				}
				if (_sys)
					try {
						if (String.IsNullOrWhiteSpace(_system_conf.GetComment("View", "format_date_time")))
							_system_conf.SetComment("View", "format_date_time",
								default_settings[(int) settings.format_date_time].comment);
					} catch (GException e) {
						passwdsaver.print(String.Format("{0}", e.Message), true);
					}
				else if (_user_conf != null)
					try {
						if (String.IsNullOrWhiteSpace(_user_conf.GetComment("View", "format_date_time")))
							_user_conf.SetComment("View", "format_date_time",
								default_settings[(int) settings.format_date_time].comment);
					} catch (GException e) {
						passwdsaver.print(String.Format("{0}", e.Message), true);
					}
			}
		}

		public void Save()
		{
			if (_conf_file != null)
				_conf.Save(_conf_file);
			else if (_sys)
				_system_conf.Save(_system_conf_file);
			else if (_user_conf != null) {
				Directory.CreateDirectory(Path.GetDirectoryName(_user_conf_file));
				_user_conf.Save(_user_conf_file);
			} else {
				Directory.CreateDirectory(Path.GetDirectoryName(_user_conf_file));
				_conf.Save(_user_conf_file);
			}
		}
	}
}
