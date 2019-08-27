/*
** $Id: loadlib.c,v 1.52.1.3 2008/08/06 13:29:28 roberto Exp $
** Dynamic library loader for Linyee
** See Copyright Notice in Linyee.h
**
** This module contains an implementation of loadlib for Unix systems
** that have dlfcn, an implementation for Darwin (Mac OS X), an
** implementation for Windows, and a stub for other systems.
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Linyee
{
	public partial class Linyee
	{

		/* prefix for open functions in C libraries */
		public const string LUAPOF = "luaopen_";

		/* separator for open functions in C libraries */
		public const string LUAOFSEP = "_";


		public const string LIBPREFIX = "LOADLIB: ";

		public const string POF = LUAPOF;
		public const string LIBFAIL = "open";


		/* error codes for ll_loadfunc */
		public const int ERRLIB			= 1;
		public const int ERRFUNC		= 2;

		//public static void setprogdir(LinyeeState L) { }

		public static void SetProgDir(LinyeeState L)
		{
			#if WINDOWS_PHONE
			// On Windows Phone, the current directory is the root of the 
			// Isolated Storage directory, which is "/".

			CharPtr buff = "/";

			#elif SILVERLIGHT
			// Not all versions of Silverlight support this method.
			// So, if it is unsupported, rollback to the Isolated
			// Storage root (a.k.a. the leap of faith).

			CharPtr buff;
			try
			{
				buff = Directory.GetCurrentDirectory(); 
			}
			catch (MethodAccessException)
			{
				buff = "/";
			}
			#else
				CharPtr buff = Directory.GetCurrentDirectory(); 
			#endif

			LinyeeLGSub(L, LinyeeToString(L, -1), LINYEE_EXECDIR, buff);
			LinyeeRemove(L, -2);  /* remove original string */
		}


		#if LINYEE_DL_DLOPEN
		/*
		** {========================================================================
		** This is an implementation of loadlib based on the dlfcn interface.
		** The dlfcn interface is available in Linux, SunOS, Solaris, IRIX, FreeBSD,
		** NetBSD, AIX 4.2, HPUX 11, and  probably most other Unix flavors, at least
		** as an emulation layer on top of native functions.
		** =========================================================================
		*/

		//#include <dlfcn.h>

		static void ll_unloadlib (void *lib) {
		  dlclose(lib);
		}


		static void *ll_load (LinyeeState L, readonly CharPtr path) {
		  void *lib = dlopen(path, RTLD_NOW);
		  if (lib == null) ly_pushstring(L, dlerror());
		  return lib;
		}


		static ly_CFunction ll_sym (LinyeeState L, void *lib, readonly CharPtr sym) {
		  ly_CFunction f = (ly_CFunction)dlsym(lib, sym);
		  if (f == null) ly_pushstring(L, dlerror());
		  return f;
		}

		/* }====================================================== */



		//#elif defined(LINYEE_DL_DLL)
		/*
		** {======================================================================
		** This is an implementation of loadlib for Windows using native functions.
		** =======================================================================
		*/

		//#include <windows.h>


		//#undef setprogdir

		static void setprogdir (LinyeeState L) {
		  char buff[MAX_PATH + 1];
		  char *lb;
		  DWORD nsize = sizeof(buff)/GetUnmanagedSize(typeof(char));
		  DWORD n = GetModuleFileNameA(null, buff, nsize);
		  if (n == 0 || n == nsize || (lb = strrchr(buff, '\\')) == null)
			luaL_error(L, "unable to get ModuleFileName");
		  else {
			*lb = '\0';
			luaL_gsub(L, ly_tostring(L, -1), LINYEE_EXECDIR, buff);
			ly_remove(L, -2);  /* remove original string */
		  }
		}


		static void pusherror (LinyeeState L) {
		  int error = GetLastError();
		  char buffer[128];
		  if (FormatMessageA(FORMAT_MESSAGE_IGNORE_INSERTS | FORMAT_MESSAGE_FROM_SYSTEM,
			  null, error, 0, buffer, sizeof(buffer), null))
			ly_pushstring(L, buffer);
		  else
			ly_pushfstring(L, "system error %d\n", error);
		}

		static void ll_unloadlib (void *lib) {
		  FreeLibrary((HINSTANCE)lib);
		}


		static void *ll_load (LinyeeState L, readonly CharPtr path) {
		  HINSTANCE lib = LoadLibraryA(path);
		  if (lib == null) pusherror(L);
		  return lib;
		}


		static ly_CFunction ll_sym (LinyeeState L, void *lib, readonly CharPtr sym) {
		  ly_CFunction f = (ly_CFunction)GetProcAddress((HINSTANCE)lib, sym);
		  if (f == null) pusherror(L);
		  return f;
		}

		/* }====================================================== */



#elif LINYEE_DL_DYLD
		/*
		** {======================================================================
		** Native Mac OS X / Darwin Implementation
		** =======================================================================
		*/

		//#include <mach-o/dyld.h>


		/* Mac appends a `_' before C function names */
		//#undef POF
		//#define POF	"_" LINYEE_POF


		static void pusherror (LinyeeState L) {
		  CharPtr err_str;
		  CharPtr err_file;
		  NSLinkEditErrors err;
		  int err_num;
		  NSLinkEditError(err, err_num, err_file, err_str);
		  ly_pushstring(L, err_str);
		}


		static CharPtr errorfromcode (NSObjectFileImageReturnCode ret) {
		  switch (ret) {
			case NSObjectFileImageInappropriateFile:
			  return "file is not a bundle";
			case NSObjectFileImageArch:
			  return "library is for wrong CPU type";
			case NSObjectFileImageFormat:
			  return "bad format";
			case NSObjectFileImageAccess:
			  return "cannot access file";
			case NSObjectFileImageFailure:
			default:
			  return "unable to load library";
		  }
		}


		static void ll_unloadlib (void *lib) {
		  NSUnLinkModule((NSModule)lib, NSUNLINKMODULE_OPTION_RESET_LAZY_REFERENCES);
		}


		static void *ll_load (LinyeeState L, readonly CharPtr path) {
		  NSObjectFileImage img;
		  NSObjectFileImageReturnCode ret;
		  /* this would be a rare case, but prevents crashing if it happens */
		  if(!_dyld_present()) {
			ly_pushliteral(L, "dyld not present");
			return null;
		  }
		  ret = NSCreateObjectFileImageFromFile(path, img);
		  if (ret == NSObjectFileImageSuccess) {
			NSModule mod = NSLinkModule(img, path, NSLINKMODULE_OPTION_PRIVATE |
							   NSLINKMODULE_OPTION_RETURN_ON_ERROR);
			NSDestroyObjectFileImage(img);
			if (mod == null) pusherror(L);
			return mod;
		  }
		  ly_pushstring(L, errorfromcode(ret));
		  return null;
		}


		static ly_CFunction ll_sym (LinyeeState L, void *lib, readonly CharPtr sym) {
		  NSSymbol nss = NSLookupSymbolInModule((NSModule)lib, sym);
		  if (nss == null) {
			ly_pushfstring(L, "symbol " + LINYEE_QS + " not found", sym);
			return null;
		  }
		  return (ly_CFunction)NSAddressOfSymbol(nss);
		}

		/* }====================================================== */



#else
		/*
		** {======================================================
		** Fallback for other systems
		** =======================================================
		*/

		//#undef LIB_FAIL
		//#define LIB_FAIL	"absent"


		public const string DLMSG = "dynamic libraries not enabled; check your Linyee installation";


		public static void LLUnloadLib (object lib) {
		  //(void)lib;  /* to avoid warnings */
		}


		public static object LLLoad (LinyeeState L, CharPtr path) {
		  //(void)path;  /* to avoid warnings */
		  LinyeePushLiteral(L, DLMSG);
		  return null;
		}


		public static LinyeeNativeFunction LLSym (LinyeeState L, object lib, CharPtr sym) {
		  //(void)lib; (void)sym;  /* to avoid warnings */
		  LinyeePushLiteral(L, DLMSG);
		  return null;
		}

		/* }====================================================== */
		#endif



		private static object LLRegister (LinyeeState L, CharPtr path) {
			// todo: the whole usage of plib here is wrong, fix it - mjf
		  //void **plib;
		  object plib = null;
		  LinyeePushFString(L, "%s%s", LIBPREFIX, path);
		  LinyeeGetTable(L, LINYEE_REGISTRYINDEX);  /* check library in registry? */
		  if (!LinyeeIsNil(L, -1))  /* is there an entry? */
			plib = LinyeeToUserData(L, -1);
		  else {  /* no entry yet; create one */
			LinyeePop(L, 1);
			//plib = ly_newuserdata(L, (uint)Marshal.SizeOf(plib));
			//plib[0] = null;
			LinyeeLGetMetatable(L, "_LOADLIB");
			LinyeeSetMetatable(L, -2);
			LinyeePushFString(L, "%s%s", LIBPREFIX, path);
			LinyeePushValue(L, -2);
			LinyeeSetTable(L, LINYEE_REGISTRYINDEX);
		  }
		  return plib;
		}


		/*
		** __gc tag method: calls library's `ll_unloadlib' function with the lib
		** handle
		*/
		private static int Gctm (LinyeeState L) {
		  object lib = LinyeeLCheckUData(L, 1, "_LOADLIB");
		  if (lib != null) LLUnloadLib(lib);
		  lib = null;  /* mark library as closed */
		  return 0;
		}


		private static int LLLoadFunc (LinyeeState L, CharPtr path, CharPtr sym) {
		  object reg = LLRegister(L, path);
		  if (reg == null) reg = LLLoad(L, path);
		  if (reg == null)
			return ERRLIB;  /* unable to load library */
		  else {
			LinyeeNativeFunction f = LLSym(L, reg, sym);
			if (f == null)
			  return ERRFUNC;  /* unable to find function */
			LinyeePushCFunction(L, f);
			return 0;  /* return function */
		  }
		}


		private static int LLLoadLib (LinyeeState L) {
		  CharPtr path = LinyeeLCheckString(L, 1);
		  CharPtr init = LinyeeLCheckString(L, 2);
		  int stat = LLLoadFunc(L, path, init);
		  if (stat == 0)  /* no errors? */
			return 1;  /* return the loaded function */
		  else {  /* error; error message is on stack top */
			LinyeePushNil(L);
			LinyeeInsert(L, -2);
			LinyeePushString(L, (stat == ERRLIB) ?  LIBFAIL : "init");
			return 3;  /* return nil, error message, and where */
		  }
		}



		/*
		** {======================================================
		** 'require' function
		** =======================================================
		*/


		private static int Readable (CharPtr filename) {
		  Stream f = fopen(filename, "r");  /* try to open file */
		  if (f == null) return 0;  /* open failed */
		  fclose(f);
		  return 1;
		}


		private static CharPtr PushNextTemplate (LinyeeState L, CharPtr path) {
		  CharPtr l;
		  while (path[0] == LINYEE_PATHSEP[0]) path = path.next();  /* skip separators */
		  if (path[0] == '\0') return null;  /* no more templates */
		  l = strchr(path, LINYEE_PATHSEP[0]);  /* find next separator */
		  if (l == null) l = path + strlen(path);
		  LinyeePushLString(L, path, (uint)(l - path));  /* template */
		  return l;
		}


		private static CharPtr FindFile (LinyeeState L, CharPtr name,
												   CharPtr pname) {
		  CharPtr path;
		  name = LinyeeLGSub(L, name, ".", LINYEE_DIRSEP);
		  LinyeeGetField(L, LINYEE_ENVIRONINDEX, pname);
		  path = LinyeeToString(L, -1);
		  if (path == null)
			LinyeeLError(L, LINYEE_QL("package.%s") + " must be a string", pname);
		  LinyeePushLiteral(L, "");  /* error accumulator */
		  while ((path = PushNextTemplate(L, path)) != null) {
			CharPtr filename;
			filename = LinyeeLGSub(L, LinyeeToString(L, -1), LINYEE_PATH_MARK, name);
			LinyeeRemove(L, -2);  /* remove path template */
			if (Readable(filename) != 0)  /* does file exist and is readable? */
			  return filename;  /* return that file name */
			LinyeePushFString(L, "\n\tno file " + LINYEE_QS, filename);
			LinyeeRemove(L, -2);  /* remove file name */
			LinyeeConcat(L, 2);  /* add entry to possible error message */
		  }
		  return null;  /* not found */
		}


		private static void LoadError (LinyeeState L, CharPtr filename) {
		  LinyeeLError(L, "error loading module " + LINYEE_QS + " from file " + LINYEE_QS + ":\n\t%s",
						LinyeeToString(L, 1), filename, LinyeeToString(L, -1));
		}


		private static int LoaderLinyee (LinyeeState L) {
		  CharPtr filename;
		  CharPtr name = LinyeeLCheckString(L, 1);
		  filename = FindFile(L, name, "path");
		  if (filename == null) return 1;  /* library not found in this path */
		  if (LinyeeLLoadFile(L, filename) != 0)
			LoadError(L, filename);
		  return 1;  /* library loaded successfully */
		}


		private static CharPtr MakeFuncName (LinyeeState L, CharPtr modname) {
		  CharPtr funcname;
		  CharPtr mark = strchr(modname, LINYEE_IGMARK[0]);
		  if (mark!=null) modname = mark + 1;
		  funcname = LinyeeLGSub(L, modname, ".", LUAOFSEP);
		  funcname = LinyeePushFString(L, POF + "%s", funcname);
		  LinyeeRemove(L, -2);  /* remove 'gsub' result */
		  return funcname;
		}


		private static int LoaderC (LinyeeState L) {
		  CharPtr funcname;
		  CharPtr name = LinyeeLCheckString(L, 1);
		  CharPtr filename = FindFile(L, name, "cpath");
		  if (filename == null) return 1;  /* library not found in this path */
		  funcname = MakeFuncName(L, name);
		  if (LLLoadFunc(L, filename, funcname) != 0)
			LoadError(L, filename);
		  return 1;  /* library loaded successfully */
		}


		private static int LoaderCRoot (LinyeeState L) {
		  CharPtr funcname;
		  CharPtr filename;
		  CharPtr name = LinyeeLCheckString(L, 1);
		  CharPtr p = strchr(name, '.');
		  int stat;
		  if (p == null) return 0;  /* is root */
		  LinyeePushLString(L, name, (uint)(p - name));
		  filename = FindFile(L, LinyeeToString(L, -1), "cpath");
		  if (filename == null) return 1;  /* root not found */
		  funcname = MakeFuncName(L, name);
		  if ((stat = LLLoadFunc(L, filename, funcname)) != 0) {
			if (stat != ERRFUNC) LoadError(L, filename);  /* real error */
			LinyeePushFString(L, "\n\tno module " + LINYEE_QS + " in file " + LINYEE_QS,
							   name, filename);
			return 1;  /* function not found */
		  }
		  return 1;
		}


		private static int LoaderPreLoad (LinyeeState L) {
		  CharPtr name = LinyeeLCheckString(L, 1);
		  LinyeeGetField(L, LINYEE_ENVIRONINDEX, "preload");
		  if (!LinyeeIsTable(L, -1))
			LinyeeLError(L, LINYEE_QL("package.preload") + " must be a table");
		  LinyeeGetField(L, -1, name);
		  if (LinyeeIsNil(L, -1))  /* not found? */
			LinyeePushFString(L, "\n\tno field package.preload['%s']", name);
		  return 1;
		}


		public static object sentinel = new object();


		public static int LLRequire (LinyeeState L) {
		  CharPtr name = LinyeeLCheckString(L, 1);
		  int i;
		  LinyeeSetTop(L, 1);  /* _LOADED table will be at index 2 */
		  LinyeeGetField(L, LINYEE_REGISTRYINDEX, "_LOADED");
		  LinyeeGetField(L, 2, name);
		  if (LinyeeToBoolean(L, -1) != 0) {  /* is it there? */
			if (LinyeeToUserData(L, -1) == sentinel)  /* check loops */
			  LinyeeLError(L, "loop or previous error loading module " + LINYEE_QS, name);
			return 1;  /* package is already loaded */
		  }
		  /* else must load it; iterate over available loaders */
		  LinyeeGetField(L, LINYEE_ENVIRONINDEX, "loaders");
		  if (!LinyeeIsTable(L, -1))
			LinyeeLError(L, LINYEE_QL("package.loaders") + " must be a table");
		  LinyeePushLiteral(L, "");  /* error message accumulator */
		  for (i=1; ; i++) {
			LinyeeRawGetI(L, -2, i);  /* get a loader */
			if (LinyeeIsNil(L, -1))
			  LinyeeLError(L, "module " + LINYEE_QS + " not found:%s",
							name, LinyeeToString(L, -2));
			LinyeePushString(L, name);
			LinyeeCall(L, 1, 1);  /* call it */
			if (LinyeeIsFunction(L, -1))  /* did it find module? */
			  break;  /* module loaded successfully */
			else if (LinyeeIsString(L, -1) != 0)  /* loader returned error message? */
			  LinyeeConcat(L, 2);  /* accumulate it */
			else
			  LinyeePop(L, 1);
		  }
		  LinyeePushLightUserData(L, sentinel);
		  LinyeeSetField(L, 2, name);  /* _LOADED[name] = sentinel */
		  LinyeePushString(L, name);  /* pass name as argument to module */
		  LinyeeCall(L, 1, 1);  /* run loaded module */
		  if (!LinyeeIsNil(L, -1))  /* non-nil return? */
			LinyeeSetField(L, 2, name);  /* _LOADED[name] = returned value */
		  LinyeeGetField(L, 2, name);
		  if (LinyeeToUserData(L, -1) == sentinel) {   /* module did not set a value? */
			LinyeePushBoolean(L, 1);  /* use true as result */
			LinyeePushValue(L, -1);  /* extra copy to be returned */
			LinyeeSetField(L, 2, name);  /* _LOADED[name] = true */
		  }
		  return 1;
		}

		/* }====================================================== */



		/*
		** {======================================================
		** 'module' function
		** =======================================================
		*/
		  

		private static void SetFEnv (LinyeeState L) {
		  LinyeeDebug ar = new LinyeeDebug();
		  if (LinyeeGetStack(L, 1, ref ar) == 0 ||
			  LinyeeGetInfo(L, "f", ref ar) == 0 ||  /* get calling function */
			  LinyeeIsCFunction(L, -1))
			LinyeeLError(L, LINYEE_QL("module") + " not called from a Linyee function");
		  LinyeePushValue(L, -2);
		  LinyeeSetFEnv(L, -2);
		  LinyeePop(L, 1);
		}


		private static void DoOptions (LinyeeState L, int n) {
		  int i;
		  for (i = 2; i <= n; i++) {
			LinyeePushValue(L, i);  /* get option (a function) */
			LinyeePushValue(L, -2);  /* module */
			LinyeeCall(L, 1, 0);
		  }
		}


		private static void ModInit (LinyeeState L, CharPtr modname) {
		  CharPtr dot;
		  LinyeePushValue(L, -1);
		  LinyeeSetField(L, -2, "_M");  /* module._M = module */
		  LinyeePushString(L, modname);
		  LinyeeSetField(L, -2, "_NAME");
		  dot = strrchr(modname, '.');  /* look for last dot in module name */
		  if (dot == null) dot = modname;
		  else dot = dot.next();
		  /* set _PACKAGE as package name (full module name minus last part) */
		  LinyeePushLString(L, modname, (uint)(dot - modname));
		  LinyeeSetField(L, -2, "_PACKAGE");
		}


		private static int LLModule (LinyeeState L) {
		  CharPtr modname = LinyeeLCheckString(L, 1);
		  int loaded = LinyeeGetTop(L) + 1;  /* index of _LOADED table */
		  LinyeeGetField(L, LINYEE_REGISTRYINDEX, "_LOADED");
		  LinyeeGetField(L, loaded, modname);  /* get _LOADED[modname] */
		  if (!LinyeeIsTable(L, -1)) {  /* not found? */
			LinyeePop(L, 1);  /* remove previous result */
			/* try global variable (and create one if it does not exist) */
			if (LinyeeLFindTable(L, LINYEE_GLOBALSINDEX, modname, 1) != null)
			  return LinyeeLError(L, "name conflict for module " + LINYEE_QS, modname);
			LinyeePushValue(L, -1);
			LinyeeSetField(L, loaded, modname);  /* _LOADED[modname] = new table */
		  }
		  /* check whether table already has a _NAME field */
		  LinyeeGetField(L, -1, "_NAME");
		  if (!LinyeeIsNil(L, -1))  /* is table an initialized module? */
			LinyeePop(L, 1);
		  else {  /* no; initialize it */
			LinyeePop(L, 1);
			ModInit(L, modname);
		  }
		  LinyeePushValue(L, -1);
		  SetFEnv(L);
		  DoOptions(L, loaded - 1);
		  return 0;
		}


		private static int LLSeeAll (LinyeeState L) {
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		  if (LinyeeGetMetatable(L, 1)==0) {
			LinyeeCreateTable(L, 0, 1); /* create new metatable */
			LinyeePushValue(L, -1);
			LinyeeSetMetatable(L, 1);
		  }
		  LinyeePushValue(L, LINYEE_GLOBALSINDEX);
		  LinyeeSetField(L, -2, "__index");  /* mt.__index = _G */
		  return 0;
		}


		/* }====================================================== */



		/* auxiliary mark (for internal use) */
		public readonly static string AUXMARK		= String.Format("{0}", (char)1);

		private static void SetPath (LinyeeState L, CharPtr fieldname, CharPtr envname,
										   CharPtr def) {
		  CharPtr path = getenv(envname);
		  if (path == null)  /* no environment variable? */
			LinyeePushString(L, def);  /* use default */
		  else {
			/* replace ";;" by ";AUXMARK;" and then AUXMARK by default path */
			path = LinyeeLGSub(L, path, LINYEE_PATHSEP + LINYEE_PATHSEP,
									  LINYEE_PATHSEP + AUXMARK + LINYEE_PATHSEP);
			LinyeeLGSub(L, path, AUXMARK, def);
			LinyeeRemove(L, -2);
		  }
		  SetProgDir(L);
		  LinyeeSetField(L, -2, fieldname);
		}


		private readonly static LinyeeLReg[] PKFuncs = {
		  new LinyeeLReg("loadlib", LLLoadLib),
		  new LinyeeLReg("seeall", LLSeeAll),
		  new LinyeeLReg(null, null)
		};


		private readonly static LinyeeLReg[] LLFuncs = {
		  new LinyeeLReg("module", LLModule),
		  new LinyeeLReg("require", LLRequire),
		  new LinyeeLReg(null, null)
		};


		public readonly static LinyeeNativeFunction[] loaders =
		  {LoaderPreLoad, LoaderLinyee, LoaderC, LoaderCRoot, null};


		public static int LinyeeOpenPackage (LinyeeState L) {
		  int i;
		  /* create new type _LOADLIB */
		  LinyeeLNewMetatable(L, "_LOADLIB");
		  LinyeePushCFunction(L, Gctm);
		  LinyeeSetField(L, -2, "__gc");
		  /* create `package' table */
		  LinyeeLRegister(L, LINYEE_LOADLIBNAME, PKFuncs);
		#if LINYEE_COMPAT_LOADLIB
		  ly_getfield(L, -1, "loadlib");
		  ly_setfield(L, LINYEE_GLOBALSINDEX, "loadlib");
		#endif
		  LinyeePushValue(L, -1);
		  LinyeeReplace(L, LINYEE_ENVIRONINDEX);
		  /* create `loaders' table */
		  LinyeeCreateTable(L, loaders.Length - 1, 0);
		  /* fill it with pre-defined loaders */
		  for (i=0; loaders[i] != null; i++) {
			LinyeePushCFunction(L, loaders[i]);
			LinyeeRawSetI(L, -2, i+1);
		  }
		  LinyeeSetField(L, -2, "loaders");  /* put it in field `loaders' */
		  SetPath(L, "path", LINYEE_PATH, LINYEE_PATH_DEFAULT);  /* set field `path' */
		  SetPath(L, "cpath", LINYEE_CPATH, LINYEE_CPATH_DEFAULT); /* set field `cpath' */
		  /* store config information */
		  LinyeePushLiteral(L, LINYEE_DIRSEP + "\n" + LINYEE_PATHSEP + "\n" + LINYEE_PATH_MARK + "\n" +
							 LINYEE_EXECDIR + "\n" + LINYEE_IGMARK);
		  LinyeeSetField(L, -2, "config");
		  /* set field `loaded' */
		  LinyeeLFindTable(L, LINYEE_REGISTRYINDEX, "_LOADED", 2);
		  LinyeeSetField(L, -2, "loaded");
		  /* set field `preload' */
		  LinyeeNewTable(L);
		  LinyeeSetField(L, -2, "preload");
		  LinyeePushValue(L, LINYEE_GLOBALSINDEX);
		  LinyeeLRegister(L, null, LLFuncs);  /* open lib into global table */
		  LinyeePop(L, 1);
		  return 1;  /* return 'package' table */
		}

	}
}
