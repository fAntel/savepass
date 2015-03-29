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

namespace savepass
{
	public static class file
	{
		public static byte[] read_from_file(string path, string master)
		{
			blowfish b;
			byte[] data = null;

			try {
				using (FileStream f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)) {
					using (BinaryReader r = new BinaryReader(f)) {
						data = r.ReadBytes((int) f.Length);
					}
				}
			} catch (FileNotFoundException) {
				savepass.print(String.Format("file {0} doesn't exists", path), false);
			} catch (Exception e) {
				savepass.print(String.Format("reading file {0} failed: {1}", path, e.Message), true);
			}
			if (data != null) {
				b = new blowfish(master);
				try {
					data = b.decrypt(data);
				} catch (Exception) {
					savepass.print("wrong master password", false);
					data = null;
				}
			}
			return data;
		}

		public static void write_to_file(string path, byte[] data, string master)
		{
			blowfish b = new blowfish(master);
			data = b.encrypt(data);
			try {
				using (FileStream f = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
					using (BinaryWriter w = new BinaryWriter(f)) {
						w.Write(data);
					}
				}
			} catch (Exception e) {
				savepass.print(e.Message, true);
			}
		}
	}
}
