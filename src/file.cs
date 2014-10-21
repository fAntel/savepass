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

namespace passwdsaver
{
	public static class file
	{
		// переписать функцию так, чтобы проверялось существует ли файл.
		// Если нет, то надо предупредить пользователя, что файла такого нет
		public static string read_from_file(string path)
		{
			string data = null;

			try {
				using (StreamReader f = new StreamReader(path)) {
					data = f.ReadToEnd();
				}
			} catch (FileNotFoundException e) {
				return "";
			} catch (Exception e) {
				passwdsaver.print(String.Format("reading file {0} failed: {1}", path, e.Message), true);
				return null;
			}
			return data;
		}

		public static void write_to_file(string path, string data)
		{
			try {
				using (FileStream f = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)) {
					using (BinaryWriter w = new BinaryWriter(f)) {
						w.Write(data.ToCharArray());
					}
				}
			} catch (Exception e) {
				passwdsaver.print(e.Message, true);
			}
		}
	}
}
