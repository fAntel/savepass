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

namespace savepass
{
	public class conf
	{
		/* Types for default values */
		private struct conf_settings
		{
			private readonly string _group;
			private readonly string _name;
			private readonly string _default_value;
			private readonly string _comment;

			public conf_settings(string g, string name, string default_value, string comment)
			{
				_group = g;
				_name = name;
				_default_value = default_value;
				_comment = comment;
			}

			public string group { get { return _group; } }
			public string name { get { return _name; } }
			public string default_value { get { return _default_value; } }
			public string comment { get { return _comment; } }

		}

		/* Default values for settings */
		private static readonly IList<conf_settings> default_settings = new ReadOnlyCollection<conf_settings>(new[] {
			new conf_settings("Passwords", "always_in_clipboard", "true",
				"Set this to \"false\" that program print password\n" +
				"# on the screen by default rather than to clipboard"),
			new conf_settings("Passwords", "always_save_time_of_change", "true",
				"By default program will save date/time of changing\n" +
				"# password or note. Set this to \"false\" that program\n" +
				"# will save date/time only when changing password"),
			new conf_settings("View", "show_date_time", "false",
				"Show date/time of adding or last changing password or comment"),
			new conf_settings("View", "format_date_time", "G",
				"For format of string with date/time program use\n" +
				"# C# format for date/time. You can read more about\n" +
				"# it in Microsoft Developer Network article\n" +
				"# \"Custom Date and Time Format Strings\"\n" +
				"# (http://msdn.microsoft.com/en-us/library/8kb3ddd4(v=vs.110).aspx)"),
			new conf_settings("File", "default_file", "", "Absolute path to default file with passwords")
		});

		private string default_settings_data =
			"[" + default_settings[(int) settings.always_in_clipboard].group + "]\n" +
			"# " + default_settings[(int) settings.always_in_clipboard].comment + "\n" +
			default_settings[(int) settings.always_in_clipboard].name + "=" +
			default_settings[(int) settings.always_in_clipboard].default_value + "\n" +
			"# " + default_settings[(int) settings.always_save_time_of_change].comment + "\n" +
			default_settings[(int) settings.always_save_time_of_change].name + "=" +
			default_settings[(int) settings.always_save_time_of_change].default_value + "\n" +
			"[" + default_settings[(int) settings.show_date_time].group + "]\n" +
			"# " + default_settings[(int) settings.show_date_time].comment + "\n" +
			default_settings[(int) settings.show_date_time].name + "=" +
			default_settings[(int) settings.show_date_time].default_value + "\n" +
			"# " + default_settings[(int) settings.format_date_time].comment + "\n" +
			default_settings[(int) settings.format_date_time].name + "=" + 
			default_settings[(int) settings.format_date_time].default_value;

		/* Key file */
		private enum settings: int { always_in_clipboard = 0, always_save_time_of_change, show_date_time, format_date_time,
			default_file};
		private GKeyFile _conf = null, _system_conf = null, _user_conf = null;
		private string _conf_file = null, _system_conf_file = null, _user_conf_file = null;
		private bool _sys;

		public conf(string conf_file, bool sys)
		{
			_sys = sys;
			if (conf_file != null) {
				_conf_file = conf_file;
				if (!File.Exists(conf_file)) {
					savepass.print(String.Format("configuration file {0} doesn't exists. " +
						"If -S option used it will be created", conf_file), false);
					_conf = new GKeyFile();
					return;
				}
				try {
					_conf = new GKeyFile(conf_file, Flags.KeepComments | Flags.KeepTranslations);
				} catch (Exception e) {
					savepass.print(String.Format("openging configuration file {0} failed: {1}",
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
					savepass.print(String.Format("creating object for default settings failed: {0}", e.Message),
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
					savepass.print(String.Format("openging system configuration file {0} failed: {1}",
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
				savepass.print(String.Format("openging user configuration file {0} failed: {1}",
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

		private bool get_boolean(int code)
		{
			bool v;
			try {
				v = _conf.GetBoolean(default_settings[code].group, default_settings[code].name);
			} catch (Exception) {
				_conf.SetBoolean(default_settings[code].group, default_settings[code].name,
					v = Convert.ToBoolean(default_settings[code].default_value));
			}
			return v;
		}

		private void set_boolean(bool v, int code)
		{
			_conf.SetBoolean(default_settings[code].group, default_settings[code].name, v);
			if (_sys)
				_system_conf.SetBoolean(default_settings[code].group, default_settings[code].name, v);
			else if (_user_conf != null)
				_user_conf.SetBoolean(default_settings[code].group, default_settings[code].name, v);
			try {
				if (String.IsNullOrWhiteSpace(_conf.GetComment(default_settings[code].group,
					default_settings[code].name)))
					_conf.SetComment(default_settings[code].group,
						default_settings[code].name, default_settings[code].comment);
			} catch (Exception e) {
				savepass.print(String.Format("{0}", e.Message), true);

			}
			if (_sys)
				try {
				if (String.IsNullOrWhiteSpace(_system_conf.GetComment(default_settings[code].group,
					default_settings[code].name)))
					_system_conf.SetComment(default_settings[code].group,
						default_settings[code].name, default_settings[code].comment);
			} catch (Exception e) {
				savepass.print(String.Format("{0}", e.Message), true);
			}
			else if (_user_conf != null)
				try {
				if (String.IsNullOrWhiteSpace(_user_conf.GetComment(default_settings[code].group,
					default_settings[code].name)))
					_user_conf.SetComment(default_settings[code].group,
						default_settings[code].name, default_settings[code].comment);
			} catch (Exception e) {
				savepass.print(String.Format("{0}", e.Message), true);
			}
		}

		public bool always_in_clipboard
		{
			get {
				return get_boolean((int) settings.always_in_clipboard);
			}
			set {
				set_boolean(value, (int) settings.always_in_clipboard);
			}
		}

		public bool always_save_time_of_change
		{
			get {
				return get_boolean((int) settings.always_save_time_of_change);
			}
			set {
				set_boolean(value, (int) settings.always_save_time_of_change);
			}
		}

		public bool show_date_time
		{
			get {
				return get_boolean((int) settings.show_date_time);
			}
			set {
				set_boolean(value, (int) settings.show_date_time);
			}
		}

		public string format_date_time
		{
			get {
				string v;
				try {
					v = _conf.GetString(default_settings[(int) settings.format_date_time].group,
						default_settings[(int) settings.format_date_time].name);
				} catch (Exception) {
					_conf.SetString(default_settings[(int) settings.format_date_time].group,
						default_settings[(int) settings.format_date_time].name,
						v = default_settings[(int) settings.format_date_time].default_value);
				}
				return v;
			}
			set {
				_conf.SetString(default_settings[(int) settings.format_date_time].group,
					default_settings[(int) settings.format_date_time].name, value);
				if (_sys)
					_system_conf.SetString(default_settings[(int) settings.format_date_time].group,
						default_settings[(int) settings.format_date_time].name, value);
				else if (_user_conf != null)
					_user_conf.SetString(default_settings[(int) settings.format_date_time].group,
						default_settings[(int) settings.format_date_time].name, value);
				try {
					if (String.IsNullOrWhiteSpace(_conf.GetComment(default_settings[(int) settings.format_date_time].group,
						default_settings[(int) settings.format_date_time].name)))
						_conf.SetComment(default_settings[(int) settings.format_date_time].group,
							default_settings[(int) settings.format_date_time].name,
							default_settings[(int) settings.format_date_time].comment);
				} catch (Exception e) {
					savepass.print(e.Message, true);
				}
				if (_sys)
					try {
					if (String.IsNullOrWhiteSpace(_system_conf.GetComment(
							default_settings[(int) settings.format_date_time].group,
							default_settings[(int) settings.format_date_time].name)))
						_system_conf.SetComment(default_settings[(int) settings.format_date_time].group,
								default_settings[(int) settings.format_date_time].name,
								default_settings[(int) settings.format_date_time].comment);
					} catch (Exception e) {
						savepass.print(String.Format("{0}", e.Message), true);
					}
				else if (_user_conf != null)
					try {
					if (String.IsNullOrWhiteSpace(_user_conf.GetComment(
							default_settings[(int) settings.format_date_time].group,
							default_settings[(int) settings.format_date_time].name)))
						_user_conf.SetComment(default_settings[(int) settings.format_date_time].group,
								default_settings[(int) settings.format_date_time].name,
								default_settings[(int) settings.format_date_time].comment);
					} catch (Exception e) {
						savepass.print(String.Format("{0}", e.Message), true);
					}
			}
		}

		public string default_file
		{
			get {
				if (_conf.HasGroup(default_settings[(int) settings.default_file].group) &&
					_conf.HasKey(default_settings[(int) settings.default_file].group,
						default_settings[(int) settings.default_file].name))
					return _conf.GetString(default_settings[(int) settings.default_file].group,
						default_settings[(int) settings.default_file].name);
				return null;
			}
			set {
				string v;
				try {
					v = Path.GetFullPath(value);
				} catch (Exception e){
					savepass.print(String.Format("saving default file failed: {0}",
						e.Message), false);
					return;
				}
				_conf.SetString(default_settings[(int) settings.default_file].group,
					default_settings[(int) settings.default_file].name, v);
				if (_sys)
					_system_conf.SetString(default_settings[(int) settings.default_file].group,
						default_settings[(int) settings.default_file].name, v);
				else if (_user_conf != null)
					_user_conf.SetString(default_settings[(int) settings.default_file].group,
						default_settings[(int) settings.default_file].name, v);
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

		public void list(bool sys)
		{
			GKeyFile conf = sys ? _system_conf : _user_conf;
			list_setted(conf);
		}
				
		public void list_all()
		{
			string g = null;
			foreach (conf_settings s in default_settings) {
				if (String.Compare(g, s.group) != 0) {
					g = s.group;
					Console.WriteLine('[' + g + ']');
				}
				Console.Write(String.Format("{0} = ", s.name));
				if (_conf.HasGroup(s.group) && _conf.HasKey(s.group, s.name))
					Console.WriteLine(_conf.GetValue(s.group, s.name));
				else
					Console.WriteLine(String.Format("{0} (default)", s.default_value));
			}
		}

		public void list_setted(GKeyFile conf)
		{
			if (conf == null)
				conf = _conf;
			string[] keys;
			string[] groups = conf.GetGroups();
			foreach (string g in groups) {
				keys = conf.GetKeys(g);
				if (keys.Length > 0) {
					Console.WriteLine('[' + g + ']');
					foreach (string key in keys) {
						Console.WriteLine(String.Format(
							"{0} = {1}",
							key, conf.GetValue(g, key)));
					}
				}
			}
		}
	}
}
