//  
//  passwd.cs
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
using System.Text;

namespace savepass
{
	public class passwd
	{
		private string _passwd;
		private string _note;
		private DateTime _added;
		private DateTime _changed;

		/* Constructor for adding new passwords and testing */
		public passwd(string passwd, string note, DateTime added = default(DateTime), DateTime changed = default(DateTime))
		{
			_passwd = passwd;
			_note = note;
			if (added == DateTime.MinValue)
				_added = DateTime.Now;
			else
				_added = added;
			_changed = changed;
		}

		/* Constructor for creating password from file */
		public passwd(ref byte[] data, ref int i)
		{
			int len;
			len = Array.IndexOf<byte>(data, 0, i) - i;
			byte[] str = new byte[len];
			Array.Copy(data, i, str, 0, len);
			_passwd = Encoding.UTF8.GetString(str);
			i += len + 1;
			len = Array.IndexOf<byte>(data, 0, i) - i;
			str = new byte[len];
			Array.Copy(data, i, str, 0, len);
			_note = Encoding.UTF8.GetString(str);
			i += len + 1;
			str = new byte[sizeof(long)];
			Array.Copy(data, i, str, 0, sizeof(long));
			_added = new DateTime(BitConverter.ToInt64(str, 0), DateTimeKind.Local);
			i += sizeof(long);
			Array.Copy(data, i, str, 0, sizeof(long));
			i += sizeof(long);
			_changed = new DateTime(BitConverter.ToInt64(str, 0), DateTimeKind.Local);
		}

		public string password
		{
			get { return _passwd; }
			set { _passwd = value; _changed = DateTime.Now; }
		}

		public string note
		{
			get { return _note; }
			set {
				_note = value;
				/* Check for null used when testing only */
				if (savepass.c == null || savepass.c.always_save_time_of_change)
					_changed = DateTime.Now;
			}
		}

		public DateTime added
		{
			get { return _added; }
		}

		public DateTime changed
		{
			get { return _changed; }
		}

		public DateTime time
		{
			get { return (_changed == DateTime.MinValue) ? _added : _changed; }
		}

		/* Convert fields to byte array for writing to file */
		public byte[] to_data()
		{
			byte[] passwd = Encoding.UTF8.GetBytes(_passwd);
			byte[] note = Encoding.UTF8.GetBytes(_note);
			byte[] data = new byte[passwd.Length + 1 + note.Length + 1 + sizeof(long) * 2];
			Array.Copy(passwd, data, passwd.Length);
			int i = passwd.Length;
			data[i++] = 0;
			Array.Copy(note, 0, data, i, note.Length);
			i += note.Length;
			data[i++] = 0;
			Array.Copy(BitConverter.GetBytes(_added.Ticks), 0, data, i, sizeof(long));
			i += sizeof(long);
			Array.Copy(BitConverter.GetBytes(_changed.Ticks), 0, data, i, sizeof(long));
			return data;
		}
	}
}
