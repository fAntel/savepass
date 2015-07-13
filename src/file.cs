//  
//  file.cs
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
using Mono.Unix;

namespace savepass
{
	public class file
	{
		private blowfish _b;

		public file(string filename = null, string master = null)
		{
			if (filename != null)
				path = filename;
			if (master != null)
				_b = new blowfish(master);
		}

		public string path {
			get;
			set;
		}

		public string master
		{
			set { _b = new blowfish(value); }
		}

		public byte[] read()
		{
			byte[] data = null;

			try {
				using (var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)) {
					using (var r = new BinaryReader(f)) {
						data = r.ReadBytes((int) f.Length);
					}
				}
			} catch (FileNotFoundException) {
				savepass.print(String.Format(Catalog.GetString(
					"file {0} doesn't exists"), path), false);
			} catch (Exception e) {
				savepass.print(String.Format(Catalog.GetString(
					"reading file {0} failed: {1}"), path, e.Message), true);
			}
			if (data != null) {
				try {
					data = _b.decrypt(data);
				} catch (Exception) {
					savepass.print(Catalog.GetString(
						"wrong master password"), false);
					data = null;
				}
			}
			return data;
		}

		public bool write(passwds p)
		{
			byte[] data = _b.encrypt(p.to_data());
			try {
				using (var f = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
					using (var w = new BinaryWriter(f)) {
						w.Write(data);
					}
				}
			} catch (Exception e) {
				savepass.print(String.Format(Catalog.GetString(
					"saving file {0} failed: {1}"), path, e.Message), true);
				return false;
			}
			p.changed = false;
			return true;
		}
	}
}
