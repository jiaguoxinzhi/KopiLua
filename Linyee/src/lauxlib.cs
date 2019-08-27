/*
** $Id: lauxlib.c,v 1.159.1.3 2008/01/21 13:20:51 roberto Exp $
** Auxiliary functions for building Linyee libraries
** See Copyright Notice in Linyee.h
*/

#define lauxlib_c
#define LINYEE_LIB

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Linyee
{
	using LinyeeNumberType = System.Double;
	using LinyeeIntegerType = System.Int32;

	public partial class Linyee
	{
		#if LINYEE_COMPAT_GETN
		public static int LinyeeLGetN(LinyeeState L, int t);
		public static void LinyeeLSetN(LinyeeState L, int t, int n);
		#else
		public static int LinyeeLGetN(LinyeeState L, int i) {return (int)LinyeeObjectLen(L, i);}
		public static void LinyeeLSetN(LinyeeState L, int i, int j) {} /* no op! */
		#endif

		#if LINYEE_COMPAT_OPENLIB
		//#define luaI_openlib	luaL_openlib
		#endif


		/* extra error code for `luaL_load' */
		public const int LINYEE_ERRFILE     = (LINYEE_ERRERR+1);


		public class LinyeeLReg {
		  public LinyeeLReg(CharPtr name, LinyeeNativeFunction func) {
			  this.name = name;
			  this.func = func;
		  }

		  public CharPtr name;
		  public LinyeeNativeFunction func;
		};


		/*
		** ===============================================================
		** some useful macros
		** ===============================================================
		*/

		public static void LinyeeLArgCheck(LinyeeState L, bool cond, int numarg, string extramsg) {
			if (!cond)
				LinyeeLArgError(L, numarg, extramsg);
		}
		public static CharPtr LinyeeLCheckString(LinyeeState L, int n) { return LinyeeLCheckLString(L, n); }
		public static CharPtr LinyeeLOptString(LinyeeState L, int n, CharPtr d) { uint len; return LinyeeLOptLString(L, n, d, out len); }
		public static int LinyeeLCheckInt(LinyeeState L, int n)	{return (int)LinyeeLCheckInteger(L, n);}
		public static int LinyeeLOptInt(LinyeeState L, int n, LinyeeIntegerType d)	{return (int)LinyeeLOptInteger(L, n, d);}
		public static long LinyeeLCheckLong(LinyeeState L, int n)	{return LinyeeLCheckInteger(L, n);}
		public static long LinyeeLOptLong(LinyeeState L, int n, LinyeeIntegerType d)	{return LinyeeLOptInteger(L, n, d);}

		public static CharPtr LinyeeLTypeName(LinyeeState L, int i)	{return LinyeeTypeName(L, LinyeeType(L,i));}

		//#define luaL_dofile(L, fn) \
		//    (luaL_loadfile(L, fn) || ly_pcall(L, 0, LINYEE_MULTRET, 0))

		//#define luaL_dostring(L, s) \
		//    (luaL_loadstring(L, s) || ly_pcall(L, 0, LINYEE_MULTRET, 0))

		public static void LinyeeLGetMetatable(LinyeeState L, CharPtr n) { LinyeeGetField(L, LINYEE_REGISTRYINDEX, n); }

		public delegate LinyeeNumberType LinyeeLOptDelegate (LinyeeState L, int narg);		
		public static LinyeeNumberType LinyeeLOpt(LinyeeState L, LinyeeLOptDelegate f, int n, LinyeeNumberType d) {
			return LinyeeIsNoneOrNil(L, n) ? d : f(L, (n));
		}

		public delegate LinyeeIntegerType LinyeeLOptDelegateInteger(LinyeeState L, int narg);
		public static LinyeeIntegerType LinyeeLOptInteger(LinyeeState L, LinyeeLOptDelegateInteger f, int n, LinyeeNumberType d) {
			return (LinyeeIntegerType)(LinyeeIsNoneOrNil(L, n) ? d : f(L, (n)));
		}

		/*
		** {======================================================
		** Generic Buffer manipulation
		** =======================================================
		*/



		public class LinyeeLBuffer {
		  public int p;			/* current position in buffer */
		  public int lvl;  /* number of strings in the stack (level) */
		  public LinyeeState L;
		  public CharPtr buffer = new char[LUAL_BUFFERSIZE];
		};

		public static void LinyeeLAddChar(LinyeeLBuffer B, char c) {
			if (B.p >= LUAL_BUFFERSIZE)
				LinyeeLPrepBuffer(B);
			B.buffer[B.p++] = c;
		}

		///* compatibility only */
		public static void LinyeeLPutChar(LinyeeLBuffer B, char c)	{LinyeeLAddChar(B,c);}

		public static void LinyeeLAddSize(LinyeeLBuffer B, int n)	{B.p += n;}

		/* }====================================================== */


		/* compatibility with ref system */

		/* pre-defined references */
		public const int LINYEE_NOREF       = (-2);
		public const int LINYEE_REFNIL      = (-1);

		//#define ly_ref(L,lock) ((lock) ? luaL_ref(L, LINYEE_REGISTRYINDEX) : \
		//      (ly_pushstring(L, "unlocked references are obsolete"), ly_error(L), 0))

		//#define ly_unref(L,ref)        luaL_unref(L, LINYEE_REGISTRYINDEX, (ref))

		//#define ly_getref(L,ref)       ly_rawgeti(L, LINYEE_REGISTRYINDEX, (ref))


		//#define luaL_reg	luaL_Reg


		/* This file uses only the official API of Linyee.
		** Any function declared here could be written as an application function.
		*/

		//#define lauxlib_c
		//#define LINYEE_LIB

		public const int FREELIST_REF	= 0;	/* free list of references */


		/* convert a stack index to positive */
		public static int AbsIndex(LinyeeState L, int i)
		{
			return ((i) > 0 || (i) <= LINYEE_REGISTRYINDEX ? (i) : LinyeeGetTop(L) + (i) + 1);
		}


		/*
		** {======================================================
		** Error-report functions
		** =======================================================
		*/


		public static int LinyeeLArgError (LinyeeState L, int narg, CharPtr extramsg) {
		  LinyeeDebug ar = new LinyeeDebug();
		  if (LinyeeGetStack(L, 0, ref ar)==0)  /* no stack frame? */
			  return LinyeeLError(L, "bad argument #%d (%s)", narg, extramsg);
		  LinyeeGetInfo(L, "n", ref ar);
		  if (strcmp(ar.namewhat, "method") == 0) {
			narg--;  /* do not count `self' */
			if (narg == 0)  /* error is in the self argument itself? */
			  return LinyeeLError(L, "calling " + LINYEE_QS + " on bad self ({1})",
								   ar.name, extramsg);
		  }
		  if (ar.name == null)
			ar.name = "?";
		  return LinyeeLError(L, "bad argument #%d to " + LINYEE_QS + " (%s)",
								narg, ar.name, extramsg);
		}


		public static int LinyeeLTypeError (LinyeeState L, int narg, CharPtr tname) {
		  CharPtr msg = LinyeePushFString(L, "%s expected, got %s",
											tname, LinyeeLTypeName(L, narg));
		  return LinyeeLArgError(L, narg, msg);
		}


		private static void TagError (LinyeeState L, int narg, int tag) {
		  LinyeeLTypeError(L, narg, LinyeeTypeName(L, tag));
		}


		public static void LinyeeLWhere (LinyeeState L, int level) {
		  LinyeeDebug ar = new LinyeeDebug();
		  if (LinyeeGetStack(L, level, ref ar) != 0) {  /* check function at level */
			LinyeeGetInfo(L, "Sl", ref ar);  /* get info about it */
			if (ar.currentline > 0) {  /* is there info? */
			  LinyeePushFString(L, "%s:%d: ", ar.short_src, ar.currentline);
			  return;
			}
		  }
		  LinyeePushLiteral(L, "");  /* else, no information available... */
		}

		public static int LinyeeLError(LinyeeState L, CharPtr fmt, params object[] p)
		{
		  LinyeeLWhere(L, 1);
		  LinyeePushVFString(L, fmt, p);
		  LinyeeConcat(L, 2);
		  return LinyeeError(L);
		}


		/* }====================================================== */


		public static int LinyeeLCheckOption (LinyeeState L, int narg, CharPtr def,
										 CharPtr [] lst) {
		  CharPtr name = (def != null) ? LinyeeLOptString(L, narg, def) :
									 LinyeeLCheckString(L, narg);
		  int i;
		  for (i=0; i<lst.Length; i++)
			if (strcmp(lst[i], name)==0)
			  return i;
		  return LinyeeLArgError(L, narg,
							   LinyeePushFString(L, "invalid option " + LINYEE_QS, name));
		}


		public static int LinyeeLNewMetatable (LinyeeState L, CharPtr tname) {
		  LinyeeGetField(L, LINYEE_REGISTRYINDEX, tname);  /* get registry.name */
		  if (!LinyeeIsNil(L, -1))  /* name already in use? */
			return 0;  /* leave previous value on top, but return 0 */
		  LinyeePop(L, 1);
		  LinyeeNewTable(L);  /* create metatable */
		  LinyeePushValue(L, -1);
		  LinyeeSetField(L, LINYEE_REGISTRYINDEX, tname);  /* registry.name = metatable */
		  return 1;
		}


		public static object LinyeeLCheckUData (LinyeeState L, int ud, CharPtr tname) {
		  object p = LinyeeToUserData(L, ud);
		  if (p != null) {  /* value is a userdata? */
			if (LinyeeGetMetatable(L, ud) != 0) {  /* does it have a metatable? */
			  LinyeeGetField(L, LINYEE_REGISTRYINDEX, tname);  /* get correct metatable */
			  if (LinyeeRawEqual(L, -1, -2) != 0) {  /* does it have the correct mt? */
				LinyeePop(L, 2);  /* remove both metatables */
				return p;
			  }
			}
		  }
		  LinyeeLTypeError(L, ud, tname);  /* else error */
		  return null;  /* to avoid warnings */
		}


		public static void LinyeeLCheckStack (LinyeeState L, int space, CharPtr mes) {
		  if (LinyeeCheckStack(L, space) == 0)
			LinyeeLError(L, "stack overflow (%s)", mes);
		}


		public static void LinyeeLCheckType (LinyeeState L, int narg, int t) {
		  if (LinyeeType(L, narg) != t)
			TagError(L, narg, t);
		}


		public static void LinyeeLCheckAny (LinyeeState L, int narg) {
		  if (LinyeeType(L, narg) == LINYEE_TNONE)
			LinyeeLArgError(L, narg, "value expected");
		}


		public static CharPtr LinyeeLCheckLString(LinyeeState L, int narg) {uint len; return LinyeeLCheckLString(L, narg, out len);}

		[CLSCompliantAttribute(false)]
		public static CharPtr LinyeeLCheckLString (LinyeeState L, int narg, out uint len) {
		  CharPtr s = LinyeeToLString(L, narg, out len);
		  if (s==null) TagError(L, narg, LINYEE_TSTRING);
		  return s;
		}


		public static CharPtr LinyeeLOptLString (LinyeeState L, int narg, CharPtr def) {
			uint len; return LinyeeLOptLString (L, narg, def, out len); }

		[CLSCompliantAttribute(false)]
		public static CharPtr LinyeeLOptLString (LinyeeState L, int narg, CharPtr def, out uint len) {
		  if (LinyeeIsNoneOrNil(L, narg)) {
			len = (uint)((def != null) ? strlen(def) : 0);
			return def;
		  }
		  else return LinyeeLCheckLString(L, narg, out len);
		}


		public static LinyeeNumberType LinyeeLCheckNumber (LinyeeState L, int narg) {
			LinyeeNumberType d = LinyeeToNumber(L, narg);
		  if ((d == 0) && (LinyeeIsNumber(L, narg)==0))  /* avoid extra test when d is not 0 */
			TagError(L, narg, LINYEE_TNUMBER);
		  return d;
		}


		public static LinyeeNumberType LinyeeLOptNumber (LinyeeState L, int narg, LinyeeNumberType def) {
		  return LinyeeLOpt(L, LinyeeLCheckNumber, narg, def);
		}


		public static LinyeeIntegerType LinyeeLCheckInteger (LinyeeState L, int narg) {
			LinyeeIntegerType d = LinyeeToInteger(L, narg);
		  if (d == 0 && LinyeeIsNumber(L, narg)==0)  /* avoid extra test when d is not 0 */
			TagError(L, narg, LINYEE_TNUMBER);
		  return d;
		}


		public static LinyeeIntegerType LinyeeLOptInteger (LinyeeState L, int narg, LinyeeIntegerType def) {
		  return LinyeeLOptInteger(L, LinyeeLCheckInteger, narg, def);
		}


		public static int LinyeeLGetMetafield (LinyeeState L, int obj, CharPtr event_) {
		  if (LinyeeGetMetatable(L, obj)==0)  /* no metatable? */
			return 0;
		  LinyeePushString(L, event_);
		  LinyeeRawGet(L, -2);
		  if (LinyeeIsNil(L, -1)) {
			LinyeePop(L, 2);  /* remove metatable and metafield */
			return 0;
		  }
		  else {
			LinyeeRemove(L, -2);  /* remove only metatable */
			return 1;
		  }
		}


		public static int LinyeeLCallMeta (LinyeeState L, int obj, CharPtr event_) {
		  obj = AbsIndex(L, obj);
		  if (LinyeeLGetMetafield(L, obj, event_)==0)  /* no metafield? */
			return 0;
		  LinyeePushValue(L, obj);
		  LinyeeCall(L, 1, 1);
		  return 1;
		}


		public static void LinyeeLRegister(LinyeeState L, CharPtr libname,
										LinyeeLReg[] l) {
		  LinyeeIOpenLib(L, libname, l, 0);
		}

		// we could just take the .Length member here, but let's try
		// to keep it as close to the C implementation as possible.
		private static int LibSize (LinyeeLReg[] l) {
		  int size = 0;
		  for (; l[size].name!=null; size++);
		  return size;
		}

		public static void LinyeeIOpenLib (LinyeeState L, CharPtr libname,
									  LinyeeLReg[] l, int nup) {		  
		  if (libname!=null) {
			int size = LibSize(l);
			/* check whether lib already exists */
			LinyeeLFindTable(L, LINYEE_REGISTRYINDEX, "_LOADED", 1);
			LinyeeGetField(L, -1, libname);  /* get _LOADED[libname] */
			if (!LinyeeIsTable(L, -1)) {  /* not found? */
			  LinyeePop(L, 1);  /* remove previous result */
			  /* try global variable (and create one if it does not exist) */
			  if (LinyeeLFindTable(L, LINYEE_GLOBALSINDEX, libname, size) != null)
				LinyeeLError(L, "name conflict for module " + LINYEE_QS, libname);
			  LinyeePushValue(L, -1);
			  LinyeeSetField(L, -3, libname);  /* _LOADED[libname] = new table */
			}
			LinyeeRemove(L, -2);  /* remove _LOADED table */
			LinyeeInsert(L, -(nup+1));  /* move library table to below upvalues */
		  }
		  int reg_num = 0;
		  for (; l[reg_num].name!=null; reg_num++) {
			int i;
			for (i=0; i<nup; i++)  /* copy upvalues to the top */
			  LinyeePushValue(L, -nup);
			LinyeePushCClosure(L, l[reg_num].func, nup);
			LinyeeSetField(L, -(nup+2), l[reg_num].name);
		  }
		  LinyeePop(L, nup);  /* remove upvalues */
		}



		/*
		** {======================================================
		** getn-setn: size for arrays
		** =======================================================
		*/

		#if LINYEE_COMPAT_GETN

		static int checkint (LinyeeState L, int topop) {
		  int n = (ly_type(L, -1) == LINYEE_TNUMBER) ? ly_tointeger(L, -1) : -1;
		  ly_pop(L, topop);
		  return n;
		}


		static void getsizes (LinyeeState L) {
		  ly_getfield(L, LINYEE_REGISTRYINDEX, "LINYEE_SIZES");
		  if (ly_isnil(L, -1)) {  /* no `size' table? */
			ly_pop(L, 1);  /* remove nil */
			ly_newtable(L);  /* create it */
			ly_pushvalue(L, -1);  /* `size' will be its own metatable */
			ly_setmetatable(L, -2);
			ly_pushliteral(L, "kv");
			ly_setfield(L, -2, "__mode");  /* metatable(N).__mode = "kv" */
			ly_pushvalue(L, -1);
			ly_setfield(L, LINYEE_REGISTRYINDEX, "LINYEE_SIZES");  /* store in register */
		  }
		}


		public static void luaL_setn (LinyeeState L, int t, int n) {
		  t = abs_index(L, t);
		  ly_pushliteral(L, "n");
		  ly_rawget(L, t);
		  if (checkint(L, 1) >= 0) {  /* is there a numeric field `n'? */
			ly_pushliteral(L, "n");  /* use it */
			ly_pushinteger(L, n);
			ly_rawset(L, t);
		  }
		  else {  /* use `sizes' */
			getsizes(L);
			ly_pushvalue(L, t);
			ly_pushinteger(L, n);
			ly_rawset(L, -3);  /* sizes[t] = n */
			ly_pop(L, 1);  /* remove `sizes' */
		  }
		}


		public static int luaL_getn (LinyeeState L, int t) {
		  int n;
		  t = abs_index(L, t);
		  ly_pushliteral(L, "n");  /* try t.n */
		  ly_rawget(L, t);
		  if ((n = checkint(L, 1)) >= 0) return n;
		  getsizes(L);  /* else try sizes[t] */
		  ly_pushvalue(L, t);
		  ly_rawget(L, -2);
		  if ((n = checkint(L, 2)) >= 0) return n;
		  return (int)ly_objlen(L, t);
		}

		#endif

		/* }====================================================== */



		public static CharPtr LinyeeLGSub (LinyeeState L, CharPtr s, CharPtr p,
																	   CharPtr r) {
		  CharPtr wild;
		  uint l = (uint)strlen(p);
		  LinyeeLBuffer b = new LinyeeLBuffer();
		  LinyeeLBuffInit(L, b);
		  while ((wild = strstr(s, p)) != null) {
			LinyeeLAddLString(b, s, (uint)(wild - s));  /* push prefix */
			LinyeeLAddString(b, r);  /* push replacement in place of pattern */
			s = wild + l;  /* continue after `p' */
		  }
		  LinyeeLAddString(b, s);  /* push last suffix */
		  LinyeeLPushResult(b);
		  return LinyeeToString(L, -1);
		}


		public static CharPtr LinyeeLFindTable (LinyeeState L, int idx,
											   CharPtr fname, int szhint) {
		  CharPtr e;
		  LinyeePushValue(L, idx);
		  do {
			e = strchr(fname, '.');
			if (e == null) e = fname + strlen(fname);
			LinyeePushLString(L, fname, (uint)(e - fname));
			LinyeeRawGet(L, -2);
			if (LinyeeIsNil(L, -1)) {  /* no such field? */
			  LinyeePop(L, 1);  /* remove this nil */
			  LinyeeCreateTable(L, 0, (e == '.' ? 1 : szhint)); /* new table for field */
			  LinyeePushLString(L, fname, (uint)(e - fname));
			  LinyeePushValue(L, -2);
			  LinyeeSetTable(L, -4);  /* set new table into field */
			}
			else if (!LinyeeIsTable(L, -1)) {  /* field has a non-table value? */
			  LinyeePop(L, 2);  /* remove table and value */
			  return fname;  /* return problematic part of the name */
			}
			LinyeeRemove(L, -2);  /* remove previous table */
			fname = e + 1;
		  } while (e == '.');
		  return null;
		}



		/*
		** {======================================================
		** Generic Buffer manipulation
		** =======================================================
		*/


		private static int BufferLen(LinyeeLBuffer B)	{return B.p;}
		private static int BufferFree(LinyeeLBuffer B)	{return LUAL_BUFFERSIZE - BufferLen(B);}

		public const int LIMIT = LINYEE_MINSTACK / 2;


		private static int EmptyBuffer (LinyeeLBuffer B) {
		  uint l = (uint)BufferLen(B);
		  if (l == 0) return 0;  /* put nothing on stack */
		  else {
			LinyeePushLString(B.L, B.buffer, l);
			B.p = 0;
			B.lvl++;
			return 1;
		  }
		}


		private static void AdjustStack (LinyeeLBuffer B) {
		  if (B.lvl > 1) {
			LinyeeState L = B.L;
			int toget = 1;  /* number of levels to concat */
			uint toplen = LinyeeStrLen(L, -1);
			do {
			  uint l = LinyeeStrLen(L, -(toget+1));
			  if (B.lvl - toget + 1 >= LIMIT || toplen > l) {
				toplen += l;
				toget++;
			  }
			  else break;
			} while (toget < B.lvl);
			LinyeeConcat(L, toget);
			B.lvl = B.lvl - toget + 1;
		  }
		}


		public static CharPtr LinyeeLPrepBuffer (LinyeeLBuffer B) {
		  if (EmptyBuffer(B) != 0)
			AdjustStack(B);
			return new CharPtr(B.buffer, B.p);
		}

		[CLSCompliantAttribute(false)]
		public static void LinyeeLAddLString (LinyeeLBuffer B, CharPtr s, uint l) {
			while (l-- != 0)
			{
				char c = s[0];
				s = s.next();
				LinyeeLAddChar(B, c);
			}
		}


		public static void LinyeeLAddString (LinyeeLBuffer B, CharPtr s) {
		  LinyeeLAddLString(B, s, (uint)strlen(s));
		}


		public static void LinyeeLPushResult (LinyeeLBuffer B) {
		  EmptyBuffer(B);
		  LinyeeConcat(B.L, B.lvl);
		  B.lvl = 1;
		}


		public static void LinyeeLAddValue (LinyeeLBuffer B) {
		  LinyeeState L = B.L;
		  uint vl;
		  CharPtr s = LinyeeToLString(L, -1, out vl);
		  if (vl <= BufferFree(B)) {  /* fit into buffer? */
			CharPtr dst = new CharPtr(B.buffer.chars, B.buffer.index + B.p);
			CharPtr src = new CharPtr(s.chars, s.index);
			for (uint i = 0; i < vl; i++)
				dst[i] = src[i];
			B.p += (int)vl;
			LinyeePop(L, 1);  /* remove from stack */
		  }
		  else {
			if (EmptyBuffer(B) != 0)
			  LinyeeInsert(L, -2);  /* put buffer before new value */
			B.lvl++;  /* add new value into B stack */
			AdjustStack(B);
		  }
		}


		public static void LinyeeLBuffInit (LinyeeState L, LinyeeLBuffer B) {
		  B.L = L;
		  B.p = /*B.buffer*/ 0;
		  B.lvl = 0;
		}

		/* }====================================================== */


		public static int LinyeeLRef (LinyeeState L, int t) {
		  int ref_;
		  t = AbsIndex(L, t);
		  if (LinyeeIsNil(L, -1)) {
			LinyeePop(L, 1);  /* remove from stack */
			return LINYEE_REFNIL;  /* `nil' has a unique fixed reference */
		  }
		  LinyeeRawGetI(L, t, FREELIST_REF);  /* get first free element */
		  ref_ = (int)LinyeeToInteger(L, -1);  /* ref = t[FREELIST_REF] */
		  LinyeePop(L, 1);  /* remove it from stack */
		  if (ref_ != 0) {  /* any free element? */
			LinyeeRawGetI(L, t, ref_);  /* remove it from list */
			LinyeeRawSetI(L, t, FREELIST_REF);  /* (t[FREELIST_REF] = t[ref]) */
		  }
		  else {  /* no free elements */
			ref_ = (int)LinyeeObjectLen(L, t);
			ref_++;  /* create new reference */
		  }
		  LinyeeRawSetI(L, t, ref_);
		  return ref_;
		}


		public static void LinyeeLUnref (LinyeeState L, int t, int ref_) {
		  if (ref_ >= 0) {
			t = AbsIndex(L, t);
			LinyeeRawGetI(L, t, FREELIST_REF);
			LinyeeRawSetI(L, t, ref_);  /* t[ref] = t[FREELIST_REF] */
			LinyeePushInteger(L, ref_);
			LinyeeRawSetI(L, t, FREELIST_REF);  /* t[FREELIST_REF] = ref */
		  }
		}



		/*
		** {======================================================
		** Load functions
		** =======================================================
		*/

		public class LoadF {
		  public int extraline;
		  public Stream f;
		  public CharPtr buff = new char[LUAL_BUFFERSIZE];
		};

		[CLSCompliantAttribute(false)]
		public static CharPtr GetF (LinyeeState L, object ud, out uint size) {
		  size = 0;
		  LoadF lf = (LoadF)ud;
		  //(void)L;
		  if (lf.extraline != 0) {
			lf.extraline = 0;
			size = 1;
			return "\n";
		  }
		  if (feof(lf.f) != 0) return null;
		  size = (uint)fread(lf.buff, 1, lf.buff.chars.Length, lf.f);
		  return (size > 0) ? new CharPtr(lf.buff) : null;
		}


		private static int ErrFile (LinyeeState L, CharPtr what, int fnameindex) {
		  CharPtr serr = strerror(errno());
		  CharPtr filename = LinyeeToString(L, fnameindex) + 1;
		  LinyeePushFString(L, "cannot %s %s: %s", what, filename, serr);
		  LinyeeRemove(L, fnameindex);
		  return LINYEE_ERRFILE;
		}


		public static int LinyeeLLoadFile (LinyeeState L, CharPtr filename) {
		  LoadF lf = new LoadF();
		  int status, readstatus;
		  int c;
		  int fnameindex = LinyeeGetTop(L) + 1;  /* index of filename on the stack */
		  lf.extraline = 0;
		  if (filename == null) {
			LinyeePushLiteral(L, "=stdin");
			lf.f = stdin;
		  }
		  else {
			LinyeePushFString(L, "@%s", filename);
			lf.f = fopen(filename, "r");
			if (lf.f == null) return ErrFile(L, "open", fnameindex);
		  }
		  c = getc(lf.f);
		  if (c == '#') {  /* Unix exec. file? */
			lf.extraline = 1;
			while ((c = getc(lf.f)) != EOF && c != '\n') ;  /* skip first line */
			if (c == '\n') c = getc(lf.f);
		  }
		  if (c == LINYEE_SIGNATURE[0] && (filename!=null)) {  /* binary file? */
			lf.f = freopen(filename, "rb", lf.f);  /* reopen in binary mode */
			if (lf.f == null) return ErrFile(L, "reopen", fnameindex);
			/* skip eventual `#!...' */
		   while ((c = getc(lf.f)) != EOF && c != LINYEE_SIGNATURE[0]) ;
			lf.extraline = 0;
		  }
		  if (c == UTF8_SIGNATURE[0] && (filename != null)) {
			int c1 = getc(lf.f);
			if (c1 != UTF8_SIGNATURE[1]) ungetc(c1, lf.f);
			else {
			  int c2 = getc(lf.f);
			  if (c2 != UTF8_SIGNATURE[2]) {
				ungetc(c2, lf.f);
				ungetc(c1, lf.f);
			  }
			  else
				c = getc(lf.f);
			}
		  }

		  ungetc(c, lf.f);
		  status = LinyeeLoad(L, GetF, lf, LinyeeToString(L, -1));
		  readstatus = ferror(lf.f);
		  if (filename != null) fclose(lf.f);  /* close file (even in case of errors) */
		  if (readstatus != 0) {
			LinyeeSetTop(L, fnameindex);  /* ignore results from `ly_load' */
			return ErrFile(L, "read", fnameindex);
		  }
		  LinyeeRemove(L, fnameindex);
		  return status;
		}


		public class LoadS {
		  public CharPtr s;
          [CLSCompliantAttribute(false)]
		  public uint size;
		};


		static CharPtr GetS (LinyeeState L, object ud, out uint size) {
		  LoadS ls = (LoadS)ud;
		  //(void)L;
		  //if (ls.size == 0) return null;
		  size = ls.size;
		  ls.size = 0;
		  return ls.s;
		}

		[CLSCompliantAttribute(false)]
		public static int LinyeeLLoadBuffer(LinyeeState L, CharPtr buff, uint size,
										CharPtr name) {
		  LoadS ls = new LoadS();
		  ls.s = new CharPtr(buff);
		  ls.size = size;
		  return LinyeeLoad(L, GetS, ls, name);
		}


		public static int LinyeeLLoadString(LinyeeState L, CharPtr s) {
		  return LinyeeLLoadBuffer(L, s, (uint)strlen(s), s);
		}



		/* }====================================================== */


        /// <summary>
        /// 分配一个新的类型对象
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
		private static object LinyeeAlloc (Type t) {
			return System.Activator.CreateInstance(t);
		}


		private static int Panic (LinyeeState L) {
		  //(void)L;  /* to avoid warnings */
		  fprintf(stderr, "PANIC: unprotected error in call to Linyee API (%s)\n",
						   LinyeeToString(L, -1));
		  return 0;
		}


        /// <summary>
        /// 打开一个新线程
        /// </summary>
        /// <returns></returns>
		public static LinyeeState LinyeeLNewState()
		{
			LinyeeState L = LinyeeNewState(LinyeeAlloc, null);
		  if (L != null) LinyeeAtPanic(L, Panic);
		  return L;
		}

	}
}
