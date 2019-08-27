//#define ly_assert

/*
** $Id: llimits.h,v 1.69.1.1 2007/12/27 13:02:25 roberto Exp $
** Limits, basic types, and some other `installation-dependent' definitions
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Linyee
{
	using ly_int32 = System.UInt32;
	using ly_mem = System.UInt32;
	using l_mem = System.Int32;
	using ly_byte = System.Byte;
	using l_uacNumber = System.Double;
	using ly_Number = System.Double;
	using Instruction = System.UInt32;

	public partial class Linyee
	{

		//typedef LUAI_UINT32 ly_int32;

		//typedef LUAI_UMEM ly_mem;

		//typedef LUAI_MEM l_mem;



		/* chars used as small naturals (so that `char' is reserved for characters) */
		//typedef unsigned char ly_byte;

		[CLSCompliantAttribute(false)]
		public const uint MAXSIZET	= uint.MaxValue - 2;
		[CLSCompliantAttribute(false)]
		public const ly_mem MAXLUMEM	= ly_mem.MaxValue - 2;


		public const int MAXINT = (Int32.MaxValue - 2);  /* maximum value of an int (-2 for safety) */

		/*
		** conversion of pointer to integer
		** this is for hashing only; there is no problem if the integer
		** cannot hold the whole pointer value
		*/
		//#define IntPoint(p)  ((uint)(ly_mem)(p))



		/* type to ensure maximum alignment */
		//typedef LUAI_USER_ALIGNMENT_T L_Umaxalign;


		/* result of a `usual argument conversion' over ly_Number */
		//typedef LUAI_UACNUMBER l_uacNumber;


		/* internal assertions for in-house debugging */

#if ly_assert

		[Conditional("DEBUG")]
		public static void LinyeeAssert(bool c) {Debug.Assert(c);}

		[Conditional("DEBUG")]
		public static void LinyeeAssert(int c) { Debug.Assert(c != 0); }

		internal static object CheckExp(bool c, object e)		{LinyeeAssert(c); return e;}
		public static object CheckExp(int c, object e) { LinyeeAssert(c != 0); return e; }

#else

		[Conditional("DEBUG")]
		public static void LinyeeAssert (bool c) { }

		[Conditional("DEBUG")]
		public static void LinyeeAssert (int c) { }

		public static object CheckExp (bool c, object e) { return e; }
		public static object CheckExp (int c, object e) { return e; }

#endif

		[Conditional("DEBUG")]
		internal static void ApiCheck(object o, bool e) { LinyeeAssert(e); }
		internal static void ApiCheck(object o, int e) { LinyeeAssert(e != 0); }

		//#define UNUSED(x)	((void)(x))	/* to avoid warnings */


		internal static ly_byte CastByte(int i) { return (ly_byte)i; }
		internal static ly_byte CastByte(long i) { return (ly_byte)(int)i; }
		internal static ly_byte CastByte(bool i) { return i ? (ly_byte)1 : (ly_byte)0; }
		internal static ly_byte CastByte(ly_Number i) { return (ly_byte)i; }
		internal static ly_byte CastByte(object i) { return (ly_byte)(int)(i); }

		internal static int CastInt(int i) { return (int)i; }
		internal static int CastInt(uint i) { return (int)i; }
		internal static int CastInt(long i) { return (int)(int)i; }
		internal static int CastInt(ulong i) { return (int)(int)i; }
		internal static int CastInt(bool i) { return i ? (int)1 : (int)0; }
		internal static int CastInt(ly_Number i) { return (int)i; }
		internal static int CastInt(object i) { Debug.Assert(false, "Can't convert int."); return Convert.ToInt32(i); }

		internal static ly_Number CastNum(int i) { return (ly_Number)i; }
		internal static ly_Number CastNum(uint i) { return (ly_Number)i; }
		internal static ly_Number CastNum(long i) { return (ly_Number)i; }
		internal static ly_Number CastNum(ulong i) { return (ly_Number)i; }
		internal static ly_Number CastNum(bool i) { return i ? (ly_Number)1 : (ly_Number)0; }
		internal static ly_Number CastNum(object i) { Debug.Assert(false, "Can't convert number."); return Convert.ToSingle(i); }

		/*
		** type for virtual-machine instructions
		** must be an unsigned with (at least) 4 bytes (see details in lopcodes.h)
		*/
		//typedef ly_int32 Instruction;



		/* maximum stack for a Linyee function */
		public const int MAXSTACK	= 250;



		/* minimum size for the string table (must be power of 2) */
		public const int MINSTRTABSIZE	= 32;


		/* minimum size for string buffer */
		public const int LUAMINBUFFER	= 32;


		#if !ly_lock
		public static void LinyeeLock(LinyeeState L) { }
		public static void LinyeeUnlock(LinyeeState L) { }
		#endif
		

		#if !luai_threadyield
		public static void LinyeeIThreadYield(LinyeeState L)     {LinyeeUnlock(L); LinyeeLock(L);}
		#endif


		/*
		** macro to control inclusion of some hard tests on stack reallocation
		*/ 
		//#ifndef HARDSTACKTESTS
		//#define condhardstacktests(x)	((void)0)
		//#else
		//#define condhardstacktests(x)	x
		//#endif

	}
}
