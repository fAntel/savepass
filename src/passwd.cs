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

namespace passwdsaver
{
	public class passwd: IFormattable
	{
		private string _passwd;
		private string _note;
		private DateTime _added;

		/* Constructor for adding new passwords */
		public passwd(string passwd, string note)
		{
			_passwd = passwd;
			_note = note;
			_added = DateTime.UtcNow;
		}

		/* Constructor for creating password from file */
		public passwd(string data)
		{
			string[] a = data.Split(new Char[] {'\t'}, 3);
			_passwd = a[0];
			_note = a[1];
			_added = new DateTime(Convert.ToInt64(a[2]), DateTimeKind.Utc);
		}

		/* Constructor for copying password from another object */
		public passwd(passwd p)
		{
			_passwd = p.password;
			_note = p.note;
			_added = p.added;
		}

		public string password
		{
			get { return _passwd; }
			set { _passwd = value; }
		}

		public string note
		{
			get { return _note; }
			set { _note = value; }
		}

		public DateTime added
		{
			get { return _added; }
		}

		/* Convert fields to string for writing to file */
		public override string ToString()
		{
			return String.Format("{0}\t{1}\t{2}", _passwd, _note, _added.Ticks);
		}

		public string ToString(string format)
		{
			return this.ToString();
		}

		public string ToString(string format, IFormatProvider provider)
		{
			return this.ToString();
		}
	}
}
