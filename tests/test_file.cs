//
//  test_file.cs
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
using NUnit.Framework;
using System;
using System.Text;

namespace savepass
{
	[TestFixture()]
	public class test_file
	{
		[Test()]
		public void test_blowfish()
		{
			blowfish b = new blowfish("key");
			Assert.AreEqual("test_blowfish\0\0\0", Encoding.UTF8.GetString(
				b.decrypt(b.encrypt(
					Encoding.UTF8.GetBytes("test_blowfish")))));
		}
	}
}

