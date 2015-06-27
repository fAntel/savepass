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
		private readonly ListStore _model;
		private readonly TreeView _treeview;

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

		public gui(string[] args)
		{
			Application.Init(savepass.program_name, ref args);
			// Create window
			_window = new Window("savepass");
			_window.DeleteEvent += on_delete;
			_window.Resize(600, 400);
			// Create menu
			// Create box for treeview and buttons
			var hbox = new Box(Orientation.Horizontal, 2);
			_window.Add(hbox);
			// Create treeview
			_model = new ListStore(typeof(string), typeof(string));
			var sw = new ScrolledWindow();
			hbox.PackStart(sw, true, true, 0);
			_treeview = new TreeView(_model);
			sw.Add(_treeview);
			var column = new TreeViewColumn("Note", new CellRendererText(), "text", 0);
			column.Resizable = true;
			_treeview.AppendColumn(column);
			column = new TreeViewColumn("Time", new CellRendererText(), "text", 1);
			column.Resizable = true;
			_treeview.AppendColumn(column);
			// Create buttons
			var buttons_box = new Box(Orientation.Vertical, 3);
			hbox.PackStart(buttons_box, false, true, 3);
			var add_button = create_button("Add", "list-add");
			buttons_box.PackStart(add_button, false, true, 0);
			add_button.Clicked += add_clicked;
			var edit_button = create_button("Edit", null);
			buttons_box.PackStart(edit_button, false, true, 0);
			edit_button.Clicked += edit_clicked;
			var delete_button = create_button("Delete", "edit-delete");
			buttons_box.PackStart(delete_button, false, true, 0);
			delete_button.Clicked += delete_clicked;
			var copy_button = create_button("Copy", "edit-copy");
			buttons_box.PackStart(copy_button, false, true, 0);
			copy_button.Clicked += copy_clicked;
			var show_button = create_button("Show", null);
			buttons_box.PackStart(show_button, false, true, 0);
			show_button.Clicked += show_clicked;

			_window.ShowAll();
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

		/* return 0 if user sure */
		private static int are_you_sure(string str)
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
			return 0;
		}

		public bool run()
		{
			Environment.ExitCode = 0;

			// this fragment will be deleted when add New|Open file
			byte[] data = new byte[0];
			_p = new passwds(data);

			Application.Run();
			//return _changed;
			return false;
		}

		/******************
		 * Event Handlers *
		 ******************/

		private static void on_delete(object sender, DeleteEventArgs data)
		{
			Application.Quit();
		}

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
				dialog.DefaultResponse = ResponseType.Ok;
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
				           new_p.added.ToString("g", CultureInfo.CurrentCulture),
				           "");
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
								new_p.time.ToString("g", CultureInfo.CurrentCulture));
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
			dialog.Run();
			dialog.Destroy();
		}
	}
}
