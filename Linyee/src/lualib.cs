/*
** $Id: lualib.h,v 1.36.1.1 2007/12/27 13:02:25 roberto Exp $
** Linyee standard libraries
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Linyee
{
	public partial class Linyee
	{
		/* Key to file-handle type */
		public const string LINYEE_FILEHANDLE = "FILE*";

		public const string LINYEE_COLIBNAME = "coroutine";
		public const string LINYEE_TABLIBNAME = "table";
		public const string LINYEE_IOLIBNAME = "io";
		public const string LINYEE_OSLIBNAME = "os";
		public const string LINYEE_STRLIBNAME = "string";
		public const string LINYEE_MATHLIBNAME = "math";
		public const string LINYEE_DBLIBNAME = "debug";
		public const string LINYEE_LOADLIBNAME = "package";

	}
}
