/*
** Linyee - An Extensible Extension Language
** Linyee.Net, Linyee, China (https://www.Linyee.net)
** See Copyright Notice at the end of this file
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Linyee.Lib.Resource;

[assembly: CLSCompliant(true)]
namespace Linyee
{
	using LinyeeNumberType = Double;
	using LinyeeIntegerType = System.Int32;

	/* Functions to be called by the debuger in specific events */
	public delegate void LinyeeHook(LinyeeState L, LinyeeDebug ar);

	public delegate int LinyeeNativeFunction(LinyeeState L);

	[CLSCompliantAttribute(true)]//主要用于不区分大小写
	public partial class Linyee
	{
		private static bool RunningOnUnix
		{
			get {
				var platform = (int)Environment.OSVersion.Platform;

				return (platform == 4) || (platform == 6) || (platform == 128);
			}
		}

		static Linyee()
		{
			if (RunningOnUnix) {
				LINYEE_ROOT = UNIX_LINYEE_ROOT;
				LINYEE_LDIR = UNIX_LINYEE_LDIR;
				LINYEE_CDIR = UNIX_LINYEE_CDIR;
				LINYEE_PATH_DEFAULT = UNIX_LINYEE_PATH_DEFAULT;
				LINYEE_CPATH_DEFAULT = UNIX_LINYEE_CPATH_DEFAULT;
			} else {
				LINYEE_ROOT = null;
				LINYEE_LDIR = WIN32_LINYEE_LDIR;
				LINYEE_CDIR = WIN32_LINYEE_CDIR;
				LINYEE_PATH_DEFAULT = WIN32_LINYEE_PATH_DEFAULT;
				LINYEE_CPATH_DEFAULT = WIN32_LINYEE_CPATH_DEFAULT;
			}
		}

		public const string LINYEE_VERSION = "Linyee 0.1";
		public const string LINYEE_RELEASE = "Linyee 0.1.0";
		public const int LINYEE_VERSION_NUM	= 00010000;
        /// <summary>
        /// 版权
        /// </summary>
		public static string LINYEE_COPYRIGHT = $"{Resource.Copyright} (C) 2019-2019 Linyee.Net, Linyee";
        /// <summary>
        /// 作者
        /// </summary>
		public const string LINYEE_AUTHORS = "R. Linyee, L. H. de Linyee & W. Linyee";


		/* mark for precompiled code (`<esc>Linyee') */
		public const string LINYEE_SIGNATURE = "\x01bLinyee";

		/* Byte order mark of UTF-8 Encoding */
		public const string UTF8_SIGNATURE = "\xEF\xBB\xBF";

		/* option for multiple returns in `ly_pcall' and `ly_call' */
		public const int LINYEE_MULTRET	= (-1);


		/*
		** pseudo-indices
		*/
		public const int LINYEE_REGISTRYINDEX	= (-10000);
		public const int LINYEE_ENVIRONINDEX	= (-10001);
		public const int LINYEE_GLOBALSINDEX	= (-10002);
		public static int LinyeeUpValueIndex(int i)	{return LINYEE_GLOBALSINDEX-i;}


		/* thread status; 0 is OK */
		public const int LINYEE_YIELD	= 1;
		public const int LINYEE_ERRRUN = 2;
		public const int LINYEE_ERRSYNTAX	= 3;
		public const int LINYEE_ERRMEM	= 4;
		public const int LINYEE_ERRERR	= 5;





		/*
		** functions that read/write blocks when loading/dumping Linyee chunks
		*/
		[CLSCompliantAttribute(false)]
        public delegate CharPtr ly_Reader(LinyeeState L, object ud, out uint sz);
		[CLSCompliantAttribute(false)]
		public delegate int ly_Writer(LinyeeState L, CharPtr p, uint sz, object ud);


		/*
		** prototype for memory-allocation functions
		*/
        //public delegate object ly_Alloc(object ud, object ptr, uint osize, uint nsize);

            /// <summary>
            /// 分配对象
            /// </summary>
            /// <param name="t"></param>
            /// <returns></returns>
		public delegate object ly_Alloc(Type t);


		/*
		** basic types
		*/
		public const int LINYEE_TNONE = -1;

        public const int LINYEE_TNIL = 0;
        public const int LINYEE_TBOOLEAN = 1;
        public const int LINYEE_TLIGHTUSERDATA = 2;
        public const int LINYEE_TNUMBER = 3;
        public const int LINYEE_TSTRING = 4;
        public const int LINYEE_TTABLE = 5;
        public const int LINYEE_TFUNCTION = 6;
        public const int LINYEE_TUSERDATA = 7;
        /// <summary>
        /// 
        /// </summary>
        public const int LINYEE_TTHREAD = 8;



		/* minimum Linyee stack available to a C function */
		public const int LINYEE_MINSTACK = 20;


		/* type of numbers in Linyee */
		//typedef LINYEE_NUMBER LinyeeNumberType;


		/* type for integer functions */
		//typedef LINYEE_INTEGER LinyeeIntegerType;

		/*
		** garbage-collection function and options
		*/

		public const int LINYEE_GCSTOP			= 0;
		public const int LINYEE_GCRESTART		= 1;
		public const int LINYEE_GCCOLLECT		= 2;
		public const int LINYEE_GCCOUNT		= 3;
		public const int LINYEE_GCCOUNTB		= 4;
		public const int LINYEE_GCSTEP			= 5;
		public const int LINYEE_GCSETPAUSE		= 6;
		public const int LINYEE_GCSETSTEPMUL	= 7;

		/* 
		** ===============================================================
		** some useful macros
		** ===============================================================
		*/

        public static void LinyeePop(LinyeeState L, int n)
        {
            LinyeeSetTop(L, -(n) - 1);
        }

        public static void LinyeeNewTable(LinyeeState L)
        {
            LinyeeCreateTable(L, 0, 0);
        }

        public static void LinyeeRegister(LinyeeState L, CharPtr n, LinyeeNativeFunction f)
        {
            LinyeePushCFunction(L, f);
            LinyeeSetGlobal(L, n);
        }

        public static void LinyeePushCFunction(LinyeeState L, LinyeeNativeFunction f)
        {
            LinyeePushCClosure(L, f, 0);
        }

		[CLSCompliantAttribute(false)]
        public static uint LinyeeStrLen(LinyeeState L, int i)
        {
            return LinyeeObjectLen(L, i);
        }

        public static bool LinyeeIsFunction(LinyeeState L, int n)
        {
            return LinyeeType(L, n) == LINYEE_TFUNCTION;
        }

        public static bool LinyeeIsTable(LinyeeState L, int n)
        {
			return LinyeeType(L, n) == LINYEE_TTABLE;
        }

        public static bool LinyeeIsLightUserData(LinyeeState L, int n)
        {
            return LinyeeType(L, n) == LINYEE_TLIGHTUSERDATA;
        }

        public static bool LinyeeIsNil(LinyeeState L, int n)
        {
            return LinyeeType(L, n) == LINYEE_TNIL;
        }

        public static bool LinyeeIsBoolean(LinyeeState L, int n)
        {
            return LinyeeType(L, n) == LINYEE_TBOOLEAN;
        }

        public static bool LinyeeIsThread(LinyeeState L, int n)
        {
            return LinyeeType(L, n) == LINYEE_TTHREAD;
        }

        public static bool LinyeeIsNone(LinyeeState L, int n)
        {
            return LinyeeType(L, n) == LINYEE_TNONE;
        }

        public static bool LinyeeIsNoneOrNil(LinyeeState L, LinyeeNumberType n)
        {
            return LinyeeType(L, (int)n) <= 0;
        }

        public static void LinyeePushLiteral(LinyeeState L, CharPtr s)
        {
            //TODO: Implement use using ly_pushlstring instead of ly_pushstring
			//ly_pushlstring(L, "" s, (sizeof(s)/GetUnmanagedSize(typeof(char)))-1)
            LinyeePushString(L, s);
        }

        public static void LinyeeSetGlobal(LinyeeState L, CharPtr s)
        {
            LinyeeSetField(L, LINYEE_GLOBALSINDEX, s);
        }

        public static void LinyeeGetGlobal(LinyeeState L, CharPtr s)
        {
            LinyeeGetField(L, LINYEE_GLOBALSINDEX, s);
        }

        public static CharPtr LinyeeToString(LinyeeState L, int i)
        {
            uint blah;
            return LinyeeToLString(L, i, out blah);
        }

		////#define ly_open()	luaL_newstate()
        /// <summary>
        /// 打开一个新线程
        /// </summary>
        /// <returns></returns>
		public static LinyeeState LinyeeOpen()
        {
            return LinyeeLNewState();
        }

        ////#define ly_getregistry(L)	ly_pushvalue(L, LINYEE_REGISTRYINDEX)
        public static void LinyeeGetRegistry(LinyeeState L)
        {
            LinyeePushValue(L, LINYEE_REGISTRYINDEX);
        }

        ////#define ly_getgccount(L)	ly_gc(L, LINYEE_GCCOUNT, 0)
        public static int LinyeeGetGCCount(LinyeeState L)
        {
            return LinyeeGC(L, LINYEE_GCCOUNT, 0);
        }

		//#define ly_Chunkreader		ly_Reader
		//#define ly_Chunkwriter		ly_Writer


		/*
		** {======================================================================
		** Debug API
		** =======================================================================
		*/


		/*
		** Event codes
		*/
		public const int LINYEE_HOOKCALL = 0;
        public const int LINYEE_HOOKRET = 1;
        public const int LINYEE_HOOKLINE = 2;
        public const int LINYEE_HOOKCOUNT = 3;
        public const int LINYEE_HOOKTAILRET = 4;


		/*
		** Event masks
		*/
		public const int LINYEE_MASKCALL = (1 << LINYEE_HOOKCALL);
        public const int LINYEE_MASKRET = (1 << LINYEE_HOOKRET);
        public const int LINYEE_MASKLINE = (1 << LINYEE_HOOKLINE);
        public const int LINYEE_MASKCOUNT = (1 << LINYEE_HOOKCOUNT);




        /* }====================================================================== */


        /******************************************************************************
		* Copyright (C) 2019-2019 Linyee.Net, Linyee.  All rights reserved.
		*
		* 
		* 本软件使用 WTFPL 许可
        * 任何人都可以任意使用本软件
        * 任何人都有复制与发布本协议的原始或修改过的版本的权利
		******************************************************************************/

    }
}
