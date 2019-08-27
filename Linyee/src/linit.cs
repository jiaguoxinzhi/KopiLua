/*
** $Id: linit.c,v 1.14.1.1 2007/12/27 13:02:25 roberto Exp $
** Initialization of libraries for Linyee.c
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Linyee
{
	public partial class Linyee
	{
		private readonly static LinyeeLReg[] lualibs = {
		  new LinyeeLReg("", LinyeeOpenBase),
		  new LinyeeLReg(LINYEE_LOADLIBNAME, LinyeeOpenPackage),
		  new LinyeeLReg(LINYEE_TABLIBNAME, luaopen_table),
		  new LinyeeLReg(LINYEE_IOLIBNAME, LinyeeOpenIo),
		  new LinyeeLReg(LINYEE_OSLIBNAME, LinyeeOpenOS),
		  new LinyeeLReg(LINYEE_STRLIBNAME, luaopen_string),
		  new LinyeeLReg(LINYEE_MATHLIBNAME, LinyeeOpenMath),
		  new LinyeeLReg(LINYEE_DBLIBNAME, LinyeeOpenDebug),
		  new LinyeeLReg(null, null)
		};


		public static void LinyeeLOpenLibs (LinyeeState L) {
		  for (int i=0; i<lualibs.Length-1; i++)
		  {
			LinyeeLReg lib = lualibs[i];
			LinyeePushCFunction(L, lib.func);
			LinyeePushString(L, lib.name);
			LinyeeCall(L, 1, 0);
		  }
		}

	}
}
