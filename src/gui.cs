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

namespace savepass
{
	public class gui: IUI
	{
		private passwds _p;
		private string _filename;
		private string _master;
		private bool _changed = false;

		private static Window _window;
		private ListStore _model;
		private TreeView _treeview;
		private Box _hbox;
		private MenuItem _add_item;
		private MenuItem _save_item;
		private MenuItem _save_as_item;
		private MenuItem _close_item;

		private static Button create_button(string lable, string name)
		{
			var button = new Button();
			button.Label = lable;
			if (name != null) {
				var icon = new Image();
				icon.Pixbuf = IconTheme.Default.LoadIcon(name, (int) IconSize.Button, 0);
				button.Image = icon;
			}
			return button;
		}

		private void create_treeview(ref Box hbox)
		{
			_model = new ListStore(typeof(string), typeof(string));
			var sw = new ScrolledWindow();
			hbox.PackStart(sw, true, true, 0);
			_treeview = new TreeView(_model);
			sw.Add(_treeview);
			var renderer = new CellRendererText();
			renderer.WrapWidth = 1;
			renderer.WrapMode = Pango.WrapMode.Word;
			var column = new TreeViewColumn("Note", renderer, "text", 0);
			column.Resizable = true;
			column.Expand = true;
			_treeview.AppendColumn(column);
			column = new TreeViewColumn("Time", new CellRendererText(), "text", 1);
			_treeview.AppendColumn(column);
		}

		public gui(string[] args)
		{
			Application.Init(savepass.program_name, ref args);
			// Create window
			_window = new Window("savepass");
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
			var item = new MenuItem("File");
			menu_bar.Append(item);
			var menu = new Menu();
			item.Submenu = menu;

			var new_item = new MenuItem("New");
			menu.Append(new_item);
			new_item.Activated += new_activated;
			var open_item = new MenuItem("Open");
			menu.Append(open_item);
			open_item.Activated += open_activated;
			_save_item = new MenuItem("Save");
			menu.Append(_save_item);
			_save_item.Activated += save_activated;
			_save_as_item = new MenuItem("Save as");
			menu.Append(_save_as_item);
			_save_as_item.Activated += save_activated;
			var separator = new SeparatorMenuItem();
			menu.Append(separator);
			var preferences_item = new MenuItem("Preferences");
			menu.Append(preferences_item);
			//preferences_item.Activated += preferences_activated;
			separator = new SeparatorMenuItem();
			menu.Append(separator);
			_close_item = new MenuItem("Close");
			menu.Append(_close_item);
			_close_item.Activated += close_activated;
			var quit_item = new MenuItem("Quit");
			menu.Append(quit_item);
			quit_item.Activated += delegate {
				if (is_changed())
					return;
				Application.Quit();
			};

			// Edit
			item = new MenuItem("Edit");
			menu_bar.Append(item);
			menu = new Menu();
			item.Submenu = menu;

			_add_item = new MenuItem("Add");
			menu.Append(_add_item);
			_add_item.Activated += add_clicked;
			var edit_item = new MenuItem("Edit");
			menu.Append(edit_item);
			edit_item.Activated += edit_clicked;
			edit_item.Sensitive = false;
			var delete_item = new MenuItem("Delete");
			menu.Append(delete_item);
			delete_item.Activated += delete_clicked;
			delete_item.Sensitive = false;
			var copy_item = new MenuItem("Copy");
			menu.Append(copy_item);
			copy_item.Activated += copy_clicked;
			copy_item.Sensitive = false;
			var show_item = new MenuItem("Show");
			menu.Append(show_item);
			show_item.Activated += show_clicked;
			show_item.Sensitive = false;

			// Help
			item = new MenuItem("Help");
			menu_bar.Append(item);
			menu = new Menu();
			item.Submenu = menu;

			var help_item = new MenuItem("Help");
			menu.Append(help_item);
			//help_item.Activated += help_activated;
			var about_item = new MenuItem("About");
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
			var add_button = create_button("Add", "list-add");
			buttons_box.PackStart(add_button, false, true, 0);
			add_button.Clicked += add_clicked;
			var edit_button = create_button("Edit", null);
			buttons_box.PackStart(edit_button, false, true, 0);
			edit_button.Clicked += edit_clicked;
			edit_button.Sensitive = false;
			var delete_button = create_button("Delete", "edit-delete");
			buttons_box.PackStart(delete_button, false, true, 0);
			delete_button.Clicked += delete_clicked;
			delete_button.Sensitive = false;
			var copy_button = create_button("Copy", "edit-copy");
			buttons_box.PackStart(copy_button, false, true, 0);
			copy_button.Clicked += copy_clicked;
			copy_button.Sensitive = false;
			var show_button = create_button("Show", null);
			buttons_box.PackStart(show_button, false, true, 0);
			show_button.Clicked += show_clicked;
			show_button.Sensitive = false;

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

			turn_off_sensetivity();
			_window.ShowAll();
		}

		public static Window window
		{
			get { return _window; }
		}

		/* return 0 if user sure */
		private int are_you_sure(string str)
		{
			int answer;

			var dialog = new Dialog("savepass", _window,
				DialogFlags.DestroyWithParent | DialogFlags.Modal,
				"OK", ResponseType.Ok, "Cancel", ResponseType.Cancel, null);
			dialog.Resizable = false;
			dialog.DefaultResponse = ResponseType.Ok;
			var content_area = dialog.ContentArea;
			var label = new Label(String.Format(
				"Are you sure you want to delete password with note \"{0}\"?", str));
			content_area.PackStart(label, true, true, 3);
			dialog.ShowAll();

			answer = dialog.Run() == (int) ResponseType.Ok ? 0 : 1;

			dialog.Destroy();

			return answer;
		}
			
		public int config(out conf c)
		{
			c = null;
			try {
				c = new conf();
			} catch (Exception) {
				return 2;
			}
			return 0;
		}

		public void run()
		{
			Environment.ExitCode = 0;

			Application.Run();
		}

		private void turn_on_sensetivity()
		{
			_hbox.Sensitive = true;
			_add_item.Sensitive = true;
			_save_item.Sensitive = true;
			_save_as_item.Sensitive = true;
			_close_item.Sensitive = true;
		}

		private void turn_off_sensetivity()
		{
			_hbox.Sensitive = false;
			_add_item.Sensitive = false;
			_save_item.Sensitive = false;
			_save_as_item.Sensitive = false;
			_close_item.Sensitive = false;
		}

		/***********
		 * Dialogs *
		 ***********/

		private class add_edit_dialog: Dialog {
			private readonly Dialog dialog;
			private Label pass_again_label;
			private Entry pass_entry;
			private Entry pass_again_entry;
			private Entry note_entry;
			private CheckButton visibility_checkbox;
			private Label error_label;

			public add_edit_dialog(string caption, string pass, string note)
			{
				dialog = new Dialog(caption, _window, DialogFlags.DestroyWithParent,
					"OK", ResponseType.Ok, "Cancel", ResponseType.Cancel, null);
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

				var label = new Label("Password:");
				grid.Attach(label, 0, 0, 1, 1);
				label.Halign = Align.Start;
				this.pass_again_label = new Label("Password again:");
				grid.Attach(this.pass_again_label, 0, 1, 1, 1);
				label = new Label("Note:");
				grid.Attach(label, 0, 2, 1, 1);
				label.Halign = Align.Start;

				this.pass_entry = new Entry();
				grid.Attach(this.pass_entry, 1, 0, 1, 1);
				this.pass_entry.Visibility = false;
				this.pass_again_entry = new Entry();
				grid.Attach(this.pass_again_entry, 1, 1, 1, 1);
				this.pass_again_entry.Visibility = false;
				if (pass != null) {
					pass_entry.Text = pass;
					pass_again_entry.Text = pass;
				}
				this.note_entry = new Entry();
				grid.Attach(this.note_entry, 1, 2, 1, 1);
				if (note != null)
					note_entry.Text = note;

				this.visibility_checkbox = new CheckButton("Show password");
				grid.Attach(this.visibility_checkbox, 0, 3, 2, 1);
				this.visibility_checkbox.Toggled += delegate {
					this.pass_entry.Visibility = !this.pass_entry.Visibility;
					this.pass_again_label.Visible = !this.pass_again_label.Visible;
					this.pass_again_entry.Visible = !this.pass_again_entry.Visible;
					this.error_label.Visible = false;
				};

				this.error_label = new Label();
				grid.Attach(this.error_label, 0, 4, 2, 1);

				dialog.ShowAll();
				this.error_label.Visible = false;
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

			public int run()
			{
				return dialog.Run();
			}

			public void passs_doesnt_match()
			{
				error_label.Markup = "<span foreground=\"red\">Passwords doesn't match</span>";
				error_label.Visible = true;
			}

			public void entry_is_empty()
			{
				const string msg = "<span foreground=\"red\">{0} is empty</span>";
				string name;
				if (String.IsNullOrEmpty(pass))
					name = "Password";
				else if (String.IsNullOrEmpty(pass_again) && !show_pass)
					name = "Password again";
				else
					name = "Note";
				error_label.Markup = String.Format(msg, name);
				error_label.Visible = true;
			}

			public void destroy()
			{
				dialog.Destroy();
			}
		}

		private string get_master_password()
		{
			string  str = null;
			var dialog = new Dialog("Master password", _window,
				DialogFlags.DestroyWithParent | DialogFlags.Modal,
				"OK", ResponseType.Ok, "Cancel", ResponseType.Cancel);
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

			var label = new Label("Master password:");
			grid.Attach(label, 0, 0, 1, 1);
			label.Halign = Align.End;
			var entry = new Entry();
			grid.Attach(entry, 1, 0, 1, 1);
			entry.Visibility = false;
			label = new Label();
			grid.Attach(label, 0, 1, 2, 1);

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
						"The length of the master password must be 4 or more characters" +
						"</span>";
					label.Visible = true;
					continue;
				}
				if (entry.Text.Length > 56) {
					label.Markup = "<span foreground=\"red\">" +
						"The length of the master password must be no more than 56 characters" +
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

		private bool open_save_dialog(bool open)
		{
			bool result = true;

			var dialog = new FileChooserDialog(open ? "Open File" : "Save",
				_window, 
				open ? FileChooserAction.Open : FileChooserAction.Save,
				"Cancel", ResponseType.Cancel,
				open ? "Open" : "Save",	ResponseType.Accept);
			dialog.DoOverwriteConfirmation = true;
			dialog.SkipTaskbarHint = true;
			var filter = new FileFilter();
			filter.Name = "*.pds files";
			filter.AddPattern("*.pds");
			dialog.AddFilter(filter);
			filter = new FileFilter();
			filter.Name = "All files";
			filter.AddPattern("*");
			dialog.AddFilter(filter);

			dialog.ShowAll();
			result &= dialog.Run() == (int) ResponseType.Accept;

			_filename = dialog.Filename;
			dialog.Destroy();
			return result;
		}

		private int save_changes()
		{
			var dialog = new Dialog("Save changes", _window,
				DialogFlags.DestroyWithParent | DialogFlags.Modal,
				"No", ResponseType.No, "Cancel", ResponseType.Cancel,
				"Yes", ResponseType.Yes);
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
			var label = new Label("Save changes in the file?");
			box.PackStart(label, false, false, 0);

			dialog.ShowAll();
			int response = dialog.Run();
			dialog.Destroy();

			return response;
		}

		/******************
		 * Event Handlers *
		 ******************/

		private bool is_changed()
		{
			if (_changed) {
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

		private void add_clicked(object sender, EventArgs e)
		{
			var dialog = new add_edit_dialog("Add new password", null, null);

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
				_changed = true;
				break;
			}

			dialog.destroy();
		}

		private void edit_clicked(object sender, EventArgs e)
		{
			var selection = _treeview.Selection;
			TreeIter iter;
			if (!selection.GetSelected(out iter))
				return;

			string note = (string) _model.GetValue(iter, 0);
			string pass;
			_p.search_and_get_pass(note, out pass);

			var dialog = new add_edit_dialog("Edit password", pass, note);

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
				_changed |= !pass.Equals(dialog.pass, StringComparison.Ordinal) ||
					!note.Equals(dialog.note, StringComparison.Ordinal);
				if (_changed) {
					var new_p = _p.change(int.Parse(_model.GetStringFromIter(iter)),
						dialog.pass, dialog.note);
					_model.SetValues(iter, new_p.note,
								new_p.time.ToString(
									savepass.c.format_date_time,
									CultureInfo.CurrentCulture));
				}
				break;
			}

			dialog.destroy();
		}

		private void delete_clicked(object sender, EventArgs e)
		{
			var selection = _treeview.Selection;
			TreeIter iter;
			if (!selection.GetSelected(out iter))
				return;

			if (are_you_sure((string) _model.GetValue(iter, 0)) != 0)
				return;

			_p.del(int.Parse(_model.GetStringFromIter(iter)));
			_model.Remove(ref iter);
			_changed = true;
		}

		private void copy_clicked(object sender, EventArgs e)
		{
			var selection = _treeview.Selection;
			TreeIter iter;
			if (!selection.GetSelected(out iter))
				return;

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

		private void show_clicked(object sender, EventArgs e)
		{
			var selection = _treeview.Selection;
			TreeIter iter;
			if (!selection.GetSelected(out iter))
				return;

			string note, pass;
			_p.get_pass_note(int.Parse(_model.GetStringFromIter(iter)), out pass, out note);
			var dialog = new MessageDialog(_window, DialogFlags.DestroyWithParent,
				MessageType.Info, ButtonsType.Close, pass);
			dialog.TransientFor = _window;
			dialog.Run();
			dialog.Destroy();
		}

		private void new_activated(object sender, EventArgs args)
		{
			if (is_changed())
				return;

			_master = get_master_password();
			if (_master == null)
				return;

			_filename = null;
			var data = new byte[0];
			_p = new passwds(data);

			_model.Clear();
			turn_on_sensetivity();

			_changed = false;
		}

		private void open_activated(object sender, EventArgs args)
		{
			if (is_changed())
				return;

			if (!open_save_dialog(true))
				return;
			_master = get_master_password();
			if (_master == null) {
				_filename = null;
				return;
			}

			var data = file.read_from_file(_filename, _master);
			if (data == null)
				return;
			_p = new passwds(data);

			_model.Clear();
			foreach (passwd i in _p)
				_model.AppendValues(i.note,
					i.time.ToString(savepass.c.format_date_time,
						CultureInfo.CurrentCulture));
			turn_on_sensetivity();
			_changed = false;
		}

		private void save_activated(object sender, EventArgs args)
		{
			if ((_filename == null || sender.Equals(_save_as_item))
				&& !open_save_dialog(false))
					return;
		
			file.write_to_file(_filename, _p.to_data(), _master);
			_changed = false;
		}

		private void close_activated(object sender, EventArgs args)
		{
			if (is_changed())
				return;

			_model.Clear();
			_filename = null;
			_changed = false;
			turn_off_sensetivity();
		}
			
		private void about_activated(object sender, EventArgs args)
		{
			var dialog = new AboutDialog();
			dialog.TransientFor = _window;
			dialog.Authors = savepass.authors;
			dialog.Comments = "savepass is a password saver";
			dialog.Copyright = "Copyright (C) Kovalyov Anton 2015";
			dialog.Documenters = savepass.documenters;
			dialog.LicenseType = License.Gpl30;
			dialog.ProgramName = savepass.program_name;
			dialog.TranslatorCredits = savepass.translator_credits;
			dialog.Version = savepass.version_number;

			dialog.Run();
			dialog.Destroy();
		}
	}
}
