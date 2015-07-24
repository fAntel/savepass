//
//  gui.cs
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
using Gtk;
using System.Globalization;
using System.Text;
using Mono.Unix;

namespace savepass
{
	public class gui
	{
		private passwds _p;
		private file _file;

		private static Window _window;
		private ListStore _model;
		private TreeView _treeview;
		private Box _hbox;
		private MenuItem _add_item;
		private MenuItem _save_item;
		private MenuItem _save_as_item;
		private MenuItem _close_item;
		private MenuItem _default_file_item;
		private MenuItem _unset_default_file_item;
		private TreeViewColumn _date_time_column;

		private static string default_file_string = Catalog.GetString("<default file>");
		private static string yes_string = Catalog.GetString("Yes");
		private static string no_string = Catalog.GetString("No");
		private static string ok_string = Catalog.GetString("OK");
		private static string cancel_string = Catalog.GetString("Cancel");
		private static string apply_string = Catalog.GetString("Apply");


		/* Function to create buttons */
		private Button create_button(string lable, string name, EventHandler handler, bool sensitive = true)
		{
			var button = new Button();
			button.Label = lable;
			if (name != null) {
				var icon = new Image();
				icon.Pixbuf = IconTheme.Default.LoadIcon(name, (int) IconSize.Button, 0);
				button.Image = icon;
			}
			button.Clicked += handler;
			button.Sensitive = sensitive;
			return button;
		}

		/* Create TreeView for notes */
		private void create_treeview(ref Box hbox)
		{
			/* Create model. First column for note, second for time of changes */
			_model = new ListStore(typeof(string), typeof(string));
			var sw = new ScrolledWindow();
			hbox.PackStart(sw, true, true, 0);

			_treeview = new TreeView(_model);
			sw.Add(_treeview);

			/* Making notes to wrap */
			var renderer = new CellRendererText();
			renderer.WrapWidth = 1;
			renderer.WrapMode = Pango.WrapMode.Word;
			var column = new TreeViewColumn(Catalog.GetString("Note"), renderer, "text", 0);
			column.Resizable = true;
			column.Expand = true;
			_treeview.AppendColumn(column);

			_date_time_column = new TreeViewColumn(Catalog.GetString("Date/time"), new CellRendererText(), "text", 1);
			_treeview.AppendColumn(_date_time_column);
		}

		/* Create main window */
		public gui(file f, passwds p)
		{
			// Create window
			_window = new Window(constants.program_name);
			_window.DeleteEvent += delegate(object o, DeleteEventArgs e) {
				if (is_changed()) {
					e.RetVal = true;
				} else {
					e.RetVal = false;
					Application.Quit();
				}
			};
			_window.Resize(600, 400);

			// Create menu
			var vbox = new Box(Orientation.Vertical, 2);
			_window.Add(vbox);
			var menu_bar = new MenuBar();
			vbox.PackStart(menu_bar, false, true, 0);
			// File
			var item = new MenuItem(Catalog.GetString("File"));
			menu_bar.Append(item);
			var menu = new Menu();
			item.Submenu = menu;

			var new_item = new MenuItem(Catalog.GetString("New..."));
			menu.Append(new_item);
			new_item.Activated += new_activated;
			var open_item = new MenuItem(Catalog.GetString("Open..."));
			menu.Append(open_item);
			open_item.Activated += open_activated;
			_save_item = new MenuItem(Catalog.GetString("Save"));
			menu.Append(_save_item);
			_save_item.Activated += save_activated;
			_save_as_item = new MenuItem(Catalog.GetString("Save as..."));
			menu.Append(_save_as_item);
			_save_as_item.Activated += save_activated;
			var separator = new SeparatorMenuItem();
			menu.Append(separator);
			var preferences_item = new MenuItem(Catalog.GetString("Preferences..."));
			menu.Append(preferences_item);
			preferences_item.Activated += preferences_activated;
			separator = new SeparatorMenuItem();
			menu.Append(separator);
			item = new MenuItem(Catalog.GetString("Default file"));
			menu.Append(item);
			var default_file_menu = new Menu();
			item.Submenu = default_file_menu;
			_default_file_item = new MenuItem(default_file_string);
			default_file_menu.Append(_default_file_item);
			_default_file_item.Activated += default_file_activated;
			separator = new SeparatorMenuItem();
			default_file_menu.Append(separator);
			_unset_default_file_item = new MenuItem(Catalog.GetString("Unset default file"));
			default_file_menu.Append(_unset_default_file_item);
			_unset_default_file_item.Sensitive = false;
			_unset_default_file_item.Activated += delegate {
				savepass.c.default_file = null;
				_unset_default_file_item.Sensitive = false;
				_default_file_item.Label = default_file_string;
				savepass.c.Save();
			};
			separator = new SeparatorMenuItem();
			menu.Append(separator);
			_close_item = new MenuItem(Catalog.GetString("Close"));
			menu.Append(_close_item);
			_close_item.Activated += close_activated;
			var quit_item = new MenuItem(Catalog.GetString("Quit"));
			menu.Append(quit_item);
			quit_item.Activated += delegate {
				if (is_changed())
					return;
				Application.Quit();
			};

			// Edit
			item = new MenuItem(Catalog.GetString("Edit"));
			menu_bar.Append(item);
			menu = new Menu();
			item.Submenu = menu;

			_add_item = new MenuItem(Catalog.GetString("Add..."));
			menu.Append(_add_item);
			_add_item.Activated += add_clicked;
			var edit_item = new MenuItem(Catalog.GetString("Edit..."));
			menu.Append(edit_item);
			edit_item.Activated += edit_clicked;
			edit_item.Sensitive = false;
			var delete_item = new MenuItem(Catalog.GetString("Delete"));
			menu.Append(delete_item);
			delete_item.Activated += delete_clicked;
			delete_item.Sensitive = false;
			var copy_item = new MenuItem(Catalog.GetString("Copy"));
			menu.Append(copy_item);
			copy_item.Activated += copy_clicked;
			copy_item.Sensitive = false;
			var show_item = new MenuItem(Catalog.GetString("Show..."));
			menu.Append(show_item);
			show_item.Activated += show_clicked;
			show_item.Sensitive = false;

			// Help
			item = new MenuItem(Catalog.GetString("Help"));
			menu_bar.Append(item);
			menu = new Menu();
			item.Submenu = menu;

			var help_item = new MenuItem(Catalog.GetString("Help"));
			menu.Append(help_item);
			//help_item.Activated += help_activated;
			var about_item = new MenuItem(Catalog.GetString("About..."));
			menu.Append(about_item);
			about_item.Activated += about_activated;

			// Create box for treeview and buttons
			_hbox = new Box(Orientation.Horizontal, 2);
			vbox.PackStart(_hbox, true, true, 0);
			// Create treeview
			create_treeview(ref _hbox);
			// Create buttons
			var buttons_box = new Box(Orientation.Vertical, 3);
			_hbox.PackStart(buttons_box, false, true, 3);
			var add_button = create_button(Catalog.GetString("Add"), "list-add", add_clicked);
			buttons_box.PackStart(add_button, false, true, 0);
			var edit_button = create_button(Catalog.GetString("Edit"), null, edit_clicked, false);
			buttons_box.PackStart(edit_button, false, true, 0);
			var delete_button = create_button(Catalog.GetString("Delete"), "edit-delete", delete_clicked, false);
			buttons_box.PackStart(delete_button, false, true, 0);
			var copy_button = create_button(Catalog.GetString("Copy"), "edit-copy", copy_clicked, false);
			buttons_box.PackStart(copy_button, false, true, 0);
			var show_button = create_button(Catalog.GetString("Show"), null, show_clicked, false);
			buttons_box.PackStart(show_button, false, true, 0);

			var selection = _treeview.Selection;
			selection.Changed += delegate(object sender, EventArgs e) {
				TreeIter iter;
				if (((TreeSelection) sender).GetSelected(out iter)) {
					edit_button.Sensitive = true;
					delete_button.Sensitive = true;
					copy_button.Sensitive = true;
					show_button.Sensitive = true;

					edit_item.Sensitive = true;
					delete_item.Sensitive = true;
					copy_item.Sensitive = true;
					show_item.Sensitive = true;
				} else {
					edit_button.Sensitive = false;
					delete_button.Sensitive = false;
					copy_button.Sensitive = false;
					show_button.Sensitive = false;

					edit_item.Sensitive = false;
					delete_item.Sensitive = false;
					copy_item.Sensitive = false;
					show_item.Sensitive = false;
				}

			};

			turn_on_off_sensitivity(false);
			_window.MapEvent += delegate {
				if (_file != null)
					open_file();
			};
			_window.ShowAll();

			_file = f;
			_p = p;
		}

		public static Window window
		{
			get { return _window; }
		}
			
		public void run()
		{
			if (_file == null) {
				string file = savepass.c.default_file;
				if (!String.IsNullOrWhiteSpace(file)) {
					_default_file_item.Label = file;
					_unset_default_file_item.Sensitive = true;
					_file = new file(file);
				}
			} else
				open_file();
			_date_time_column.Visible = savepass.c.show_date_time;

			Application.Run();
		}

		/************************
		 * Additional functions *
		 ************************/

		/* return true if user sure */
		private bool are_you_sure(string str)
		{
			bool answer;

			var dialog = new Dialog(constants.program_name, _window,
				DialogFlags.DestroyWithParent | DialogFlags.Modal,
				yes_string, ResponseType.Yes,
				no_string, ResponseType.Yes, null);
			dialog.Resizable = false;
			dialog.DefaultResponse = ResponseType.Ok;
			var content_area = dialog.ContentArea;
			var label = new Label(String.Format(Catalog.GetString(
				"Are you sure you want to delete password with note \"{0}\"?"), str));
			content_area.PackStart(label, true, true, 3);
			dialog.ShowAll();

			answer = dialog.Run() == (int) ResponseType.Yes;
			dialog.Destroy();
			return answer;
		}

		/* return true if user doesn't want continue current action */
		private bool is_changed()
		{
			if (_p.changed) {
				int response = save_changes();
				switch (response) {
					case (int) ResponseType.Yes:
						save_activated(null, null);
						break;
					case (int) ResponseType.Cancel:
						return true;
				}
			}
			return false;
		}

		/* Ask master password and open file*/
		private void open_file()
		{
			if (_file.master == null) {
				string master;
				master = get_master_password(true);
				if (master == null) {
					_file = null;
					return;
				}
				_file.master = master;

				byte [] data = _file.read();
				if (data == null) {
					_file = null;
					return;
				}
				_p = new passwds(data);
			}

			_model.Clear();
			foreach (passwd i in _p)
				_model.AppendValues(i.note,
					i.time.ToString(savepass.c.format_date_time,
						CultureInfo.CurrentCulture));
			turn_on_off_sensitivity(true);
		}
			
		/* Turn on/off sensitivity of actions with passwords.
		 * Sensitivity is on when file is creating or opening.
		 * Sensitivity is off when there is no open or create file */
		private void turn_on_off_sensitivity(bool on)
		{
			_hbox.Sensitive = on;
			_add_item.Sensitive = on;
			_save_item.Sensitive = on;
			_save_as_item.Sensitive = on;
			_close_item.Sensitive = on;
		}

		/* Set ComboBoxText with acceptable formats of date/time */
		private void set_format_date_time_combo(ComboBoxText list)
		{
			var example = DateTime.Now;
			string[] date_time_formats = { "d", "D", "f", "F", "g", "G", "m", "y"};
			foreach (string str in date_time_formats)
				list.Append(str, example.ToString(str, CultureInfo.CurrentCulture));
		}

		/* Convert values in second column (date/time) to new format of date/time */
		private void change_date_time_column_values(string format)
		{
			TreeIter iter;
			_model.GetIterFirst(out iter);
			foreach (passwd p in _p) {
				_model.SetValue(iter, 1,
					p.time.ToString(format, CultureInfo.CurrentCulture));
				_model.IterNext(ref iter);
			}
		}

		/***********
		 * Dialogs *
		 ***********/

		/* Dialog for adding or editing password */
		private class add_edit_dialog: Dialog
		{
			private readonly Dialog dialog;
			//private Label pass_again_label;
			private Entry pass_entry;
			private Entry pass_again_entry;
			private Entry note_entry;
			private CheckButton visibility_checkbox;
			private Label error_label;

			/* Create dialog
			 * caption - title of dialog
			 * if it is dialog for edit password then
			 * pass and note are password and note */
			public add_edit_dialog(string caption, string pass = null, string note = null)
			{
				dialog = new Dialog(caption, _window, DialogFlags.DestroyWithParent,
					ok_string, ResponseType.Ok,
					cancel_string, ResponseType.Cancel, null);
				dialog.Resizable = false;
				dialog.TransientFor = _window;
				dialog.DefaultResponse = ResponseType.Ok;
				dialog.SkipTaskbarHint = true;
				var content_area = dialog.ContentArea;
				content_area.BorderWidth = 4;
				var grid = new Grid();
				content_area.PackStart(grid, false, false, 2);
				grid.RowSpacing = 3;
				grid.ColumnSpacing = 3;

				var label = new Label(Catalog.GetString("Password:"));
				grid.Attach(label, 0, 0, 1, 1);
				label.Halign = Align.Start;
				var pass_again_label = new Label(Catalog.GetString("Password again:"));
				grid.Attach(pass_again_label, 0, 1, 1, 1);
				label = new Label(Catalog.GetString("Note:"));
				grid.Attach(label, 0, 2, 1, 1);
				label.Halign = Align.Start;

				pass_entry = new Entry();
				grid.Attach(pass_entry, 1, 0, 1, 1);
				pass_entry.Visibility = false;
				pass_again_entry = new Entry();
				grid.Attach(pass_again_entry, 1, 1, 1, 1);
				pass_again_entry.Visibility = false;
				if (pass != null) {
					pass_entry.Text = pass;
					pass_again_entry.Text = pass;
				}
				note_entry = new Entry();
				grid.Attach(note_entry, 1, 2, 1, 1);
				if (note != null)
					note_entry.Text = note;

				visibility_checkbox = new CheckButton(Catalog.GetString("Show password"));
				grid.Attach(visibility_checkbox, 0, 3, 2, 1);
				visibility_checkbox.Toggled += delegate {
					pass_entry.Visibility = !pass_entry.Visibility;
					pass_again_label.Visible = !pass_again_label.Visible;
					pass_again_entry.Visible = !pass_again_entry.Visible;
					error_label.Visible = false;
				};

				/* Label for error messages */
				error_label = new Label();
				grid.Attach(error_label, 0, 4, 2, 1);

				/* Hide error label when user change text in entries */
				InsertedTextHandler hide_label_insert = delegate {
					error_label.Visible = false;
				};
				pass_entry.Buffer.InsertedText += hide_label_insert;
				pass_again_entry.Buffer.InsertedText += hide_label_insert;
				note_entry.Buffer.InsertedText += hide_label_insert;
				DeletedTextHandler hide_label_delete = delegate {
					error_label.Visible = false;
				};
				pass_entry.Buffer.DeletedText += hide_label_delete;
				pass_again_entry.Buffer.DeletedText += hide_label_delete;
				note_entry.Buffer.DeletedText += hide_label_delete;

				dialog.ShowAll();
				error_label.Visible = false;
			}

			public string pass
			{
				get { return pass_entry.Text; }
			}

			public string pass_again
			{
				get { return pass_again_entry.Text; }
			}

			public string note
			{
				get { return note_entry.Text; }
			}

			public bool show_pass
			{
				get { return visibility_checkbox.Active; }
			}

			public int run() { return dialog.Run();	}

			public void passs_doesnt_match()
			{
				error_label.Markup = String.Format("<span foreground=\"red\">{0}</span>",
					Catalog.GetString("Passwords doesn't match"));
				error_label.Visible = true;
			}

			public void entry_is_empty()
			{
				string msg = "<span foreground=\"red\">"
					+ Catalog.GetString("{0} is empty")
					+ "</span>";
				string name;
				if (String.IsNullOrEmpty(pass))
					name = Catalog.GetString("Password");
				else if (String.IsNullOrEmpty(pass_again) && !show_pass)
					name = Catalog.GetString("Password again");
				else
					name = Catalog.GetString("Note");
				error_label.Markup = String.Format(msg, name);
				error_label.Visible = true;
			}

			public void destroy() {	dialog.Destroy(); }
		}

		/* Dialog for requesting master password
		 * Open mean that we are opening file rather than creating a new one.
		 * In that case we show path to the file */
		private string get_master_password(bool open = false)
		{
			string  str = null;
			var dialog = new Dialog(Catalog.GetString("Master password"), _window,
				DialogFlags.DestroyWithParent | DialogFlags.Modal,
				ok_string, ResponseType.Ok,
				cancel_string, ResponseType.Cancel);
			dialog.Resizable = false;
			dialog.TransientFor = _window;
			dialog.DefaultResponse = ResponseType.Ok;
			dialog.SkipTaskbarHint = true;
			var content_area = dialog.ContentArea;
			content_area.BorderWidth = 4;
			var grid = new Grid();
			content_area.PackStart(grid, false, false, 2);
			grid.RowSpacing = 3;
			grid.ColumnSpacing = 3;

			Label label;
			if (open) {
				label = new Label(String.Format(Catalog.GetString("Opening file {0}"),
					_file.path));
				grid.Attach(label, 0, 0, 2, 1);

			}
			label = new Label(Catalog.GetString("Master password:"));
			grid.Attach(label, 0, 1, 1, 1);
			label.Halign = Align.End;
			var entry = new Entry();
			grid.Attach(entry, 1, 1, 1, 1);
			entry.Visibility = false;
			label = new Label();
			grid.Attach(label, 0, 2, 2, 1);

			entry.Buffer.DeletedText += delegate {
				label.Visible = false;
			};
			entry.Buffer.InsertedText += delegate {
				label.Visible = false;
			};

			dialog.ShowAll();
			label.Visible = false;
			while (dialog.Run() == (int) ResponseType.Ok) {
				if (entry.Text.Length < 4) {
					label.Markup = "<span foreground=\"red\">" +
						Catalog.GetString(
							"The length of the master password must be 4 or more characters") +
						"</span>";
					label.Visible = true;
					continue;
				}
				if (entry.Text.Length > 56) {
					label.Markup = "<span foreground=\"red\">" +
						Catalog.GetString(
							"The length of the master password must be no more than 56 characters") +
						"</span>";
					label.Visible = true;
					continue;
				}
				str = entry.Text;
				break;
			}

			dialog.Destroy();
			return str;
		}

		/* Dialog for choosing file to save or open */
		private string open_save_dialog(bool open)
		{
			string result = null;
			var dialog = new FileChooserDialog(
				open ? Catalog.GetString("Open File") : Catalog.GetString("Save"),
				_window, 
				open ? FileChooserAction.Open : FileChooserAction.Save,
				cancel_string, ResponseType.Cancel,
				open ? Catalog.GetString("Open") : Catalog.GetString("Save"), ResponseType.Accept);
			dialog.DoOverwriteConfirmation = true;
			dialog.SkipTaskbarHint = true;
			var filter = new FileFilter();
			filter.Name = Catalog.GetString("*.pds files");
			filter.AddPattern("*.pds");
			dialog.AddFilter(filter);
			filter = new FileFilter();
			filter.Name = Catalog.GetString("All files");
			filter.AddPattern("*");
			dialog.AddFilter(filter);

			dialog.ShowAll();
			if (dialog.Run() == (int) ResponseType.Accept)
				result = dialog.Filename;

			dialog.Destroy();
			return result;
		}

		/* Dialog to ask the user what to do with changed file */
		private int save_changes()
		{
			var dialog = new Dialog(Catalog.GetString("Save changes"), _window,
				DialogFlags.DestroyWithParent | DialogFlags.Modal,
				no_string, ResponseType.No,
				cancel_string, ResponseType.Cancel,
				yes_string, ResponseType.Yes);
			dialog.Resizable = false;
			dialog.TransientFor = _window;
			dialog.DefaultResponse = ResponseType.Cancel;
			dialog.SkipTaskbarHint = true;
			var content_area = dialog.ContentArea;
			content_area.BorderWidth = 4;
			var box = new Box(Orientation.Horizontal, 3);
			content_area.PackStart(box, false, false, 2);

			var image = new Image();
			image.SetFromIconName("dialog-information", IconSize.Dialog);
			box.PackStart(image, false, false, 0);
			var label = new Label(Catalog.GetString("Save changes in the file?"));
			box.PackStart(label, false, false, 0);

			dialog.ShowAll();
			int response = dialog.Run();
			dialog.Destroy();

			return response;
		}

		/* Dialog to work with settings */
		private void run_preferences_dialog()
		{
			int response;
			var dialog = new Dialog(Catalog.GetString("Preferences"), _window,
				DialogFlags.DestroyWithParent,
				apply_string, ResponseType.Apply,
				ok_string, ResponseType.Ok,
				cancel_string, ResponseType.Cancel);
			dialog.TransientFor = _window;
			dialog.Resizable = false;
			dialog.DefaultResponse = ResponseType.Cancel;
			dialog.SkipTaskbarHint = true;
			var content_area = dialog.ContentArea;
			content_area.BorderWidth = 4;
			var hbox = new Box(Orientation.Vertical, 3);
			content_area.PackStart(hbox, true, true, 2);

			var always_save_time_of_change = new CheckButton(Catalog.GetString("Always save time of change"));
			hbox.PackStart(always_save_time_of_change, false, false, 0);
			always_save_time_of_change.Active = savepass.c.always_save_time_of_change;
			always_save_time_of_change.TooltipText =
				Catalog.GetString("Check it if you want the program to save date/time of changes always, " +
					"not just only when changing password");
			var show_date_time = new CheckButton("Show date/time");
			hbox.PackStart(show_date_time, false, false, 0);
			show_date_time.Active = savepass.c.show_date_time;
			show_date_time.TooltipText = Catalog.GetString(
				"Show another column with date/time of creation/changing password and/or note");

			var format_date_time_box = new Box(Orientation.Horizontal, 3);
			hbox.PackStart(format_date_time_box, false, false, 2);
			format_date_time_box.MarginLeft = 20;
			format_date_time_box.Sensitive = show_date_time.Active;
			var format_date_time_label = new Label(Catalog.GetString("Format of date/time column: "));
			format_date_time_box.PackStart(format_date_time_label, false, false, 0);
			var format_date_time_combo = new ComboBoxText();
			format_date_time_box.PackStart(format_date_time_combo, false, false, 0);
			set_format_date_time_combo(format_date_time_combo);
			format_date_time_combo.ActiveId = savepass.c.format_date_time;

			show_date_time.Toggled += delegate {
				format_date_time_box.Sensitive = show_date_time.Active;
			};
			dialog.ShowAll();
			for (;;) {
				response = dialog.Run();
				if (response == (int) ResponseType.Cancel)
					break;

				savepass.c.always_save_time_of_change = always_save_time_of_change.Active;
				savepass.c.show_date_time = show_date_time.Active;
				_date_time_column.Visible = show_date_time.Active;
				change_date_time_column_values(format_date_time_combo.ActiveId);
				savepass.c.Save();

				if (response == (int) ResponseType.Ok)
					break;
			}
			dialog.Destroy();
		}

		/******************
		 * Event Handlers *
		 ******************/

		/* Add new password */
		private void add_clicked(object sender, EventArgs e)
		{
			var dialog = new add_edit_dialog(Catalog.GetString("Add new password"));

			while (dialog.run() == (int) ResponseType.Ok) {
				if (String.IsNullOrEmpty(dialog.pass) ||
				    (String.IsNullOrEmpty(dialog.pass_again) && !dialog.show_pass) ||
				    String.IsNullOrWhiteSpace(dialog.note)) {
					dialog.entry_is_empty();
					continue;
				}
				if (!dialog.show_pass && dialog.pass != dialog.pass_again) {
					dialog.passs_doesnt_match();
					continue;
				}
				var new_p = _p.add(dialog.pass, dialog.note);
				_model.AppendValues(new_p.note,
					new_p.time.ToString(savepass.c.format_date_time,
						CultureInfo.CurrentCulture));
				break;
			}

			dialog.destroy();
		}

		/* Edit selected in tree view password */
		private void edit_clicked(object sender, EventArgs e)
		{
			TreeIter iter;
			_treeview.Selection.GetSelected(out iter);
			string note, pass;
			_p.get_pass_note(int.Parse(_model.GetStringFromIter(iter)),
				out pass, out note);

			var dialog = new add_edit_dialog(Catalog.GetString("Edit password"), pass, note);

			while (dialog.run() == (int) ResponseType.Ok) {
				if (String.IsNullOrEmpty(dialog.pass) ||
					(String.IsNullOrEmpty(dialog.pass_again) && !dialog.show_pass) ||
					String.IsNullOrWhiteSpace(dialog.note)) {
					dialog.entry_is_empty();
					continue;
				}
				if (!dialog.show_pass && dialog.pass != dialog.pass_again) {
					dialog.passs_doesnt_match();
					continue;
				}
				if (!pass.Equals(dialog.pass, StringComparison.Ordinal) ||
					!note.Equals(dialog.note, StringComparison.Ordinal)) {
					var new_p = _p.change(int.Parse(_model.GetStringFromIter(iter)),
						dialog.pass == pass ? pass : null,
						dialog.note == note ? note : null);
					_model.SetValues(iter, new_p.note,
								new_p.time.ToString(
									savepass.c.format_date_time,
									CultureInfo.CurrentCulture));
				}
				break;
			}
			dialog.destroy();
		}

		/* Delete selected in tree view password */
		private void delete_clicked(object sender, EventArgs e)
		{
			var selection = _treeview.Selection;
			TreeIter iter;
			if (!selection.GetSelected(out iter))
				return;
			if (!are_you_sure((string) _model.GetValue(iter, 0)))
				return;
			_p.del(int.Parse(_model.GetStringFromIter(iter)));
			_model.Remove(ref iter);
		}

		/* Copy selected in tree view password to clipboard */
		private void copy_clicked(object sender, EventArgs e)
		{
			var selection = _treeview.Selection;
			TreeIter iter;
			selection.GetSelected(out iter);
			string note, pass;
			_p.get_pass_note(int.Parse(_model.GetStringFromIter(iter)), out pass, out note);
		#if WINDOWS
			Clipboard.SetText(pass, TextDataFormat.Text);
		#elif GTK
			Gtk.Clipboard clipboard = Gtk.Clipboard.Get(Gdk.Atom.Intern("CLIPBOARD", false));
			clipboard.Text = pass;
			clipboard.Store();
		#endif
		}

		/* Show selected in tree view password on the screen */
		private void show_clicked(object sender, EventArgs e)
		{
			var selection = _treeview.Selection;
			TreeIter iter;
			if (!selection.GetSelected(out iter))
				return;

			string note, pass;
			_p.get_pass_note(int.Parse(_model.GetStringFromIter(iter)), out pass, out note);
			var dialog = new MessageDialog(_window,
				DialogFlags.DestroyWithParent | DialogFlags.Modal,
				MessageType.Info, ButtonsType.Close, pass);
			dialog.TransientFor = _window;
			dialog.Run();
			dialog.Destroy();
		}

		/* Create a new file */
		private void new_activated(object sender, EventArgs args)
		{
			if (is_changed())
				return;

			string master = get_master_password();
			if (master == null)
				return;

			_file = new file(null, master);
			var data = new byte[0];
			_p = new passwds(data);

			_model.Clear();
			turn_on_off_sensitivity(true);
		}

		/* Open file */
		private void open_activated(object sender, EventArgs args)
		{
			if (is_changed())
				return;
			string file = open_save_dialog(true);
			if (file == null)
				return;
			_file = new file(file);
			open_file();
		}

		/* Save or save as file */
		private void save_activated(object sender, EventArgs args)
		{
			if (_file.path == null || sender.Equals(_save_as_item)){
				string f = open_save_dialog(false);
				if (f == null)
					return;
				_file.path = f;
			}
			_file.write(_p);
		}

		/* Work with settings */
		private void preferences_activated(object sender, EventArgs args)
		{
			run_preferences_dialog();
		}

		/* Change value of default file */
		private void default_file_activated(object sender, EventArgs args)
		{
			string f = open_save_dialog(true);
			if (f != null) {
				savepass.c.default_file = f;
				savepass.c.Save();
				_default_file_item.Label = f;
				_unset_default_file_item.Sensitive = true;
			}
		}

		/* Close file */
		private void close_activated(object sender, EventArgs args)
		{
			if (is_changed())
				return;

			_model.Clear();
			_file = null;
			_p.changed = false;
			turn_on_off_sensitivity(false);
		}
			
		/* Show about dialog */
		private void about_activated(object sender, EventArgs args)
		{
			var dialog = new AboutDialog();
			dialog.TransientFor = _window;
			dialog.Authors = savepass.authors;
			dialog.Comments = String.Format(Catalog.GetString("{0} is a password saver"),
				constants.program_name);
			dialog.Copyright = Catalog.GetString("Copyright (C) Kovalyov Anton 2015");
			dialog.Documenters = savepass.documenters;
			dialog.LicenseType = License.Gpl30;
			dialog.ProgramName = constants.program_name;
			dialog.TranslatorCredits = savepass.translator_credits;
			dialog.Version = constants.version_number;

			dialog.Run();
			dialog.Destroy();
		}
	}
}
