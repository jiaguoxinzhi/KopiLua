/*
** $Id: lapi.c,v 2.55.1.5 2008/07/04 18:41:18 roberto Exp $
** Linyee API
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Linyee
{
	using ly_mem = System.UInt32;
	using TValue = Linyee.LinyeeTypeValue;
	using StkId = Linyee.LinyeeTypeValue;
	using ly_Integer = System.Int32;
	using ly_Number = System.Double;
	using ptrdiff_t = System.Int32;
	using ZIO = Linyee.Zio;

	public partial class Linyee
	{
		public static string LinyeeIdent =
		  "$Linyee: " + LINYEE_RELEASE + " " + LINYEE_COPYRIGHT + " $\n" +
		  "$Authors: " + LINYEE_AUTHORS + " $\n" +
		  "$URL: www.Linyee.Net $\n";

		public static void CheckNElements(LinyeeState L, int n)
		{
			ApiCheck(L, n <= L.top - L.base_);
		}

		public static void CheckValidIndex(LinyeeState L, StkId i)
		{
			ApiCheck(L, i != LinyeeONilObject);
		}

		public static void IncrementTop(LinyeeState L)
		{
			ApiCheck(L, L.top < L.ci.top);
			StkId.Inc(ref L.top);
		}



		static TValue Index2Address (LinyeeState L, int idx) {
		  if (idx > 0) {
			TValue o = L.base_ + (idx - 1);
			ApiCheck(L, idx <= L.ci.top - L.base_);
			if (o >= L.top) return LinyeeONilObject;
			else return o;
		  }
		  else if (idx > LINYEE_REGISTRYINDEX) {
			ApiCheck(L, idx != 0 && -idx <= L.top - L.base_);
			return L.top + idx;
		  }
		  else switch (idx) {  /* pseudo-indices */
			case LINYEE_REGISTRYINDEX: return Registry(L);
			case LINYEE_ENVIRONINDEX: {
			  Closure func = CurrFunc(L);
			  SetHValue(L, L.env, func.c.env);
			  return L.env;
			}
			case LINYEE_GLOBALSINDEX: return Gt(L);
			default: {
			  Closure func = CurrFunc(L);
			  idx = LINYEE_GLOBALSINDEX - idx;
			  return (idx <= func.c.nupvalues)
						? func.c.upvalue[idx-1]
						: (TValue)LinyeeONilObject;
			}
		  }
		}


		private static Table GetCurrentEnv (LinyeeState L) {
		  if (L.ci == L.base_ci[0])  /* no enclosing function? */
			return HValue(Gt(L));  /* use global table as environment */
		  else {
			Closure func = CurrFunc(L);
			return func.c.env;
		  }
		}


		public static void LinyeeAPushObject (LinyeeState L, TValue o) {
		  SetObj2S(L, L.top, o);
		  IncrementTop(L);
		}


		public static int LinyeeCheckStack (LinyeeState L, int size) {
		  int res = 1;
		  LinyeeLock(L);
		  if (size > LUAI_MAXCSTACK || (L.top - L.base_ + size) > LUAI_MAXCSTACK)
			res = 0;  /* stack overflow */
		  else if (size > 0) {
			LinyeeDCheckStack(L, size);
			if (L.ci.top < L.top + size)
			  L.ci.top = L.top + size;
		  }
		  LinyeeUnlock(L);
		  return res;
		}


		public static void LinyeeXMove (LinyeeState from, LinyeeState to, int n) {
		  int i;
		  if (from == to) return;
		  LinyeeLock(to);
		  CheckNElements(from, n);
		  ApiCheck(from, G(from) == G(to));
		  ApiCheck(from, to.ci.top - to.top >= n);
		  from.top -= n;
		  for (i = 0; i < n; i++) {
			SetObj2S(to, StkId.Inc(ref to.top), from.top + i);
		  }
		  LinyeeUnlock(to);
		}


		public static void LinyeeSetLevel (LinyeeState from, LinyeeState to) {
		  to.nCcalls = from.nCcalls;
		}


		public static LinyeeNativeFunction LinyeeAtPanic (LinyeeState L, LinyeeNativeFunction panicf) {
		  LinyeeNativeFunction old;
		  LinyeeLock(L);
		  old = G(L).panic;
		  G(L).panic = panicf;
		  LinyeeUnlock(L);
		  return old;
		}


		public static LinyeeState LinyeeNewThread (LinyeeState L) {
		  LinyeeState L1;
		  LinyeeLock(L);
		  LinyeeCCheckGC(L);
		  L1 = luaE_newthread(L);
		  SetTTHValue(L, L.top, L1);
		  IncrementTop(L);
		  LinyeeUnlock(L);
		  luai_userstatethread(L, L1);
		  return L1;
		}



		/*
		** basic stack manipulation
		*/


		public static int LinyeeGetTop (LinyeeState L) {
		  return CastInt(L.top - L.base_);
		}


		public static void LinyeeSetTop (LinyeeState L, int idx) {
		  LinyeeLock(L);
		  if (idx >= 0) {
			ApiCheck(L, idx <= L.stack_last - L.base_);
			while (L.top < L.base_ + idx)
			  SetNilValue(StkId.Inc(ref L.top));
			L.top = L.base_ + idx;
		  }
		  else {
			ApiCheck(L, -(idx+1) <= (L.top - L.base_));
			L.top += idx+1;  /* `subtract' index (index is negative) */
		  }
		  LinyeeUnlock(L);
		}


		public static void LinyeeRemove (LinyeeState L, int idx) {
		  StkId p;
		  LinyeeLock(L);
		  p = Index2Address(L, idx);
		  CheckValidIndex(L, p);
		  while ((p=p[1]) < L.top) SetObj2S(L, p-1, p);
		  StkId.Dec(ref L.top);
		  LinyeeUnlock(L);
		}


		public static void LinyeeInsert (LinyeeState L, int idx) {
		  StkId p;
		  StkId q;
		  LinyeeLock(L);
		  p = Index2Address(L, idx);
		  CheckValidIndex(L, p);
		  for (q = L.top; q>p; StkId.Dec(ref q)) SetObj2S(L, q, q-1);
		  SetObj2S(L, p, L.top);
		  LinyeeUnlock(L);
		}


		public static void LinyeeReplace (LinyeeState L, int idx) {
		  StkId o;
		  LinyeeLock(L);
		  /* explicit test for incompatible code */
		  if (idx == LINYEE_ENVIRONINDEX && L.ci == L.base_ci[0])
			LinyeeGRunError(L, "no calling environment");
		  CheckNElements(L, 1);
		  o = Index2Address(L, idx);
		  CheckValidIndex(L, o);
		  if (idx == LINYEE_ENVIRONINDEX) {
			Closure func = CurrFunc(L);
			ApiCheck(L, TTIsTable(L.top - 1)); 
			func.c.env = HValue(L.top - 1);
			LinyeeCBarrier(L, func, L.top - 1);
		  }
		  else {
			SetObj(L, o, L.top - 1);
			if (idx < LINYEE_GLOBALSINDEX)  /* function upvalue? */
			  LinyeeCBarrier(L, CurrFunc(L), L.top - 1);
		  }
		  StkId.Dec(ref L.top);
		  LinyeeUnlock(L);
		}


		public static void LinyeePushValue (LinyeeState L, int idx) {
		  LinyeeLock(L);
		  SetObj2S(L, L.top, Index2Address(L, idx));
		  IncrementTop(L);
		  LinyeeUnlock(L);
		}



		/*
		** access functions (stack . C)
		*/


		public static int LinyeeType (LinyeeState L, int idx) {
		  StkId o = Index2Address(L, idx);
		  return (o == LinyeeONilObject) ? LINYEE_TNONE : TType(o);
		}


		public static CharPtr LinyeeTypeName (LinyeeState L, int t) {
		  //UNUSED(L);
		  return (t == LINYEE_TNONE) ? "no value" : luaT_typenames[t];
		}


		public static bool LinyeeIsCFunction (LinyeeState L, int idx) {
		  StkId o = Index2Address(L, idx);
		  return IsCFunction(o);
		}


		public static int LinyeeIsNumber (LinyeeState L, int idx) {
		  TValue n = new LinyeeTypeValue();
		  TValue o = Index2Address(L, idx);
		  return tonumber(ref o, n);
		}


		public static int LinyeeIsString (LinyeeState L, int idx) {
		  int t = LinyeeType(L, idx);
		  return (t == LINYEE_TSTRING || t == LINYEE_TNUMBER) ? 1 : 0;
		}


		public static int LinyeeIsUserData (LinyeeState L, int idx) {
		  TValue o = Index2Address(L, idx);
		  return (TTIsUserData(o) || TTIsLightUserData(o)) ? 1 : 0;
		}


		public static int LinyeeRawEqual (LinyeeState L, int index1, int index2) {
		  StkId o1 = Index2Address(L, index1);
		  StkId o2 = Index2Address(L, index2);
		  return (o1 == LinyeeONilObject || o2 == LinyeeONilObject) ? 0
				 : LinyeeORawEqualObj(o1, o2);
		}


		public static int LinyeeEqual (LinyeeState L, int index1, int index2) {
		  StkId o1, o2;
		  int i;
		  LinyeeLock(L);  /* may call tag method */
		  o1 = Index2Address(L, index1);
		  o2 = Index2Address(L, index2);
		  i = (o1 == LinyeeONilObject || o2 == LinyeeONilObject) ? 0 : equalobj(L, o1, o2);
		  LinyeeUnlock(L);
		  return i;
		}


		public static int LinyeeLessThan (LinyeeState L, int index1, int index2) {
		  StkId o1, o2;
		  int i;
		  LinyeeLock(L);  /* may call tag method */
		  o1 = Index2Address(L, index1);
		  o2 = Index2Address(L, index2);
		  i = (o1 == LinyeeONilObject || o2 == LinyeeONilObject) ? 0
			   : luaV_lessthan(L, o1, o2);
		  LinyeeUnlock(L);
		  return i;
		}



		public static ly_Number LinyeeToNumber (LinyeeState L, int idx) {
		  TValue n = new LinyeeTypeValue();
		  TValue o = Index2Address(L, idx);
		  if (tonumber(ref o, n) != 0)
			return NValue(o);
		  else
			return 0;
		}


		public static ly_Integer LinyeeToInteger (LinyeeState L, int idx) {
		  TValue n = new LinyeeTypeValue();
		  TValue o = Index2Address(L, idx);
		  if (tonumber(ref o, n) != 0) {
			ly_Integer res;
			ly_Number num = NValue(o);
			ly_number2integer(out res, num);
			return res;
		  }
		  else
			return 0;
		}


		public static int LinyeeToBoolean (LinyeeState L, int idx) {
		  TValue o = Index2Address(L, idx);
		  return (LIsFalse(o) == 0) ? 1 : 0;
		}

		[CLSCompliantAttribute(false)]
		public static CharPtr LinyeeToLString (LinyeeState L, int idx, out uint len) {
		  StkId o = Index2Address(L, idx);
		  if (!TTIsString(o)) {
			LinyeeLock(L);  /* `luaV_tostring' may create a new string */
			if (luaV_tostring(L, o)==0) {  /* conversion failed? */
			  len = 0;
			  LinyeeUnlock(L);
			  return null;
			}
			LinyeeCCheckGC(L);
			o = Index2Address(L, idx);  /* previous call may reallocate the stack */
			LinyeeUnlock(L);
		  }
		  len = TSValue(o).len;
		  return SValue(o);
		}

		[CLSCompliantAttribute(false)]
		public static uint LinyeeObjectLen (LinyeeState L, int idx) {
		  StkId o = Index2Address(L, idx);
		  switch (TType(o)) {
			case LINYEE_TSTRING: return TSValue(o).len;
			case LINYEE_TUSERDATA: return UValue(o).len;
			case LINYEE_TTABLE: return (uint)luaH_getn(HValue(o));
			case LINYEE_TNUMBER: {
			  uint l;
			  LinyeeLock(L);  /* `luaV_tostring' may create a new string */
			  l = (luaV_tostring(L, o) != 0 ? TSValue(o).len : 0);
			  LinyeeUnlock(L);
			  return l;
			}
			default: return 0;
		  }
		}


		public static LinyeeNativeFunction LinyeeToCFunction (LinyeeState L, int idx) {
		  StkId o = Index2Address(L, idx);
		  return (!IsCFunction(o)) ? null : CLValue(o).c.f;
		}


		public static object LinyeeToUserData (LinyeeState L, int idx) {
		  StkId o = Index2Address(L, idx);
		  switch (TType(o)) {
			case LINYEE_TUSERDATA: return (RawUValue(o).user_data);
			case LINYEE_TLIGHTUSERDATA: return PValue(o);
			default: return null;
		  }
		}

		public static LinyeeState LinyeeToThread (LinyeeState L, int idx) {
		  StkId o = Index2Address(L, idx);
		  return (!TTIsThread(o)) ? null : THValue(o);
		}


		public static object LinyeeToPointer (LinyeeState L, int idx) {
		  StkId o = Index2Address(L, idx);
		  switch (TType(o)) {
			case LINYEE_TTABLE: return HValue(o);
			case LINYEE_TFUNCTION: return CLValue(o);
			case LINYEE_TTHREAD: return THValue(o);
			case LINYEE_TUSERDATA:
			case LINYEE_TLIGHTUSERDATA:
			  return LinyeeToUserData(L, idx);
			default: return null;
		  }
		}



		/*
		** push functions (C . stack)
		*/


		public static void LinyeePushNil (LinyeeState L) {
		  LinyeeLock(L);
		  SetNilValue(L.top);
		  IncrementTop(L);
		  LinyeeUnlock(L);
		}


		public static void LinyeePushNumber (LinyeeState L, ly_Number n) {
		  LinyeeLock(L);
		  SetNValue(L.top, n);
		  IncrementTop(L);
		  LinyeeUnlock(L);
		}


		public static void LinyeePushInteger (LinyeeState L, ly_Integer n) {
		  LinyeeLock(L);
		  SetNValue(L.top, CastNum(n));
		  IncrementTop(L);
		  LinyeeUnlock(L);
		}
		

		private static void LinyeePushLString (LinyeeState L, CharPtr s, uint len) {
		  LinyeeLock(L);
		  LinyeeCCheckGC(L);
		  SetSValue2S(L, L.top, luaS_newlstr(L, s, len));
		  IncrementTop(L);
		  LinyeeUnlock(L);
		}


		public static void LinyeePushString (LinyeeState L, CharPtr s) {
		  if (s == null)
			LinyeePushNil(L);
		  else
			LinyeePushLString(L, s, (uint)strlen(s));
		}


		public static CharPtr LinyeePushVFString (LinyeeState L, CharPtr fmt,
											  object[] argp) {
		  CharPtr ret;
		  LinyeeLock(L);
		  LinyeeCCheckGC(L);
		  ret = LinyeeOPushVFString(L, fmt, argp);
		  LinyeeUnlock(L);
		  return ret;
		}


		public static CharPtr LinyeePushFString (LinyeeState L, CharPtr fmt) {
			CharPtr ret;
			LinyeeLock(L);
			LinyeeCCheckGC(L);
			ret = LinyeeOPushVFString(L, fmt, null);
			LinyeeUnlock(L);
			return ret;
		}

		public static CharPtr LinyeePushFString(LinyeeState L, CharPtr fmt, params object[] p)
		{
			  CharPtr ret;
			  LinyeeLock(L);
			  LinyeeCCheckGC(L);
			  ret = LinyeeOPushVFString(L, fmt, p);
			  LinyeeUnlock(L);
			  return ret;
		}

		public static void LinyeePushCClosure (LinyeeState L, LinyeeNativeFunction fn, int n) {
		  Closure cl;
		  LinyeeLock(L);
		  LinyeeCCheckGC(L);
		  CheckNElements(L, n);
		  cl = LinyeeFNewCclosure(L, n, GetCurrentEnv(L));
		  cl.c.f = fn;
		  L.top -= n;
		  while (n-- != 0)
			SetObj2N(L, cl.c.upvalue[n], L.top+n);
		  SetCLValue(L, L.top, cl);
		  LinyeeAssert(IsWhite(obj2gco(cl)));
		  IncrementTop(L);
		  LinyeeUnlock(L);
		}


		public static void LinyeePushBoolean (LinyeeState L, int b) {
		  LinyeeLock(L);
		  SetBValue(L.top, (b != 0) ? 1 : 0);  /* ensure that true is 1 */
		  IncrementTop(L);
		  LinyeeUnlock(L);
		}


		public static void LinyeePushLightUserData (LinyeeState L, object p) {
		  LinyeeLock(L);
		  SetPValue(L.top, p);
		  IncrementTop(L);
		  LinyeeUnlock(L);
		}


		public static int LinyeePushThread (LinyeeState L) {
		  LinyeeLock(L);
		  SetTTHValue(L, L.top, L);
		  IncrementTop(L);
		  LinyeeUnlock(L);
		  return (G(L).mainthread == L) ? 1 : 0;
		}



		/*
		** get functions (Linyee . stack)
		*/


		public static void LinyeeGetTable (LinyeeState L, int idx) {
		  StkId t;
		  LinyeeLock(L);
		  t = Index2Address(L, idx);
		  CheckValidIndex(L, t);
		  luaV_gettable(L, t, L.top - 1, L.top - 1);
		  LinyeeUnlock(L);
		}

		public static void LinyeeGetField (LinyeeState L, int idx, CharPtr k) {
		  StkId t;
		  TValue key = new LinyeeTypeValue();
		  LinyeeLock(L);
		  t = Index2Address(L, idx);
		  CheckValidIndex(L, t);
		  SetSValue(L, key, luaS_new(L, k));
		  luaV_gettable(L, t, key, L.top);
		  IncrementTop(L);
		  LinyeeUnlock(L);
		}


		public static void LinyeeRawGet (LinyeeState L, int idx) {
		  StkId t;
		  LinyeeLock(L);
		  t = Index2Address(L, idx);
		  ApiCheck(L, TTIsTable(t));
		  SetObj2S(L, L.top - 1, luaH_get(HValue(t), L.top - 1));
		  LinyeeUnlock(L);
		}


		public static void LinyeeRawGetI (LinyeeState L, int idx, int n) {
		  StkId o;
		  LinyeeLock(L);
		  o = Index2Address(L, idx);
		  ApiCheck(L, TTIsTable(o));
		  SetObj2S(L, L.top, luaH_getnum(HValue(o), n));
		  IncrementTop(L);
		  LinyeeUnlock(L);
		}


		public static void LinyeeCreateTable (LinyeeState L, int narray, int nrec) {
		  LinyeeLock(L);
		  LinyeeCCheckGC(L);
		  SetHValue(L, L.top, luaH_new(L, narray, nrec));
		  IncrementTop(L);
		  LinyeeUnlock(L);
		}


		public static int LinyeeGetMetatable (LinyeeState L, int objindex) {
		  TValue obj;
		  Table mt = null;
		  int res;
		  LinyeeLock(L);
		  obj = Index2Address(L, objindex);
		  switch (TType(obj)) {
			case LINYEE_TTABLE:
			  mt = HValue(obj).metatable;
			  break;
			case LINYEE_TUSERDATA:
			  mt = UValue(obj).metatable;
			  break;
			default:
			  mt = G(L).mt[TType(obj)];
			  break;
		  }
		  if (mt == null)
			res = 0;
		  else {
			SetHValue(L, L.top, mt);
			IncrementTop(L);
			res = 1;
		  }
		  LinyeeUnlock(L);
		  return res;
		}


		public static void LinyeeGetFEnv (LinyeeState L, int idx) {
		  StkId o;
		  LinyeeLock(L);
		  o = Index2Address(L, idx);
		  CheckValidIndex(L, o);
		  switch (TType(o)) {
			case LINYEE_TFUNCTION:
			  SetHValue(L, L.top, CLValue(o).c.env);
			  break;
			case LINYEE_TUSERDATA:
			  SetHValue(L, L.top, UValue(o).env);
			  break;
			case LINYEE_TTHREAD:
			  SetObj2S(L, L.top,  Gt(THValue(o)));
			  break;
			default:
			  SetNilValue(L.top);
			  break;
		  }
		  IncrementTop(L);
		  LinyeeUnlock(L);
		}


		/*
		** set functions (stack . Linyee)
		*/


		public static void LinyeeSetTable (LinyeeState L, int idx) {
		  StkId t;
		  LinyeeLock(L);
		  CheckNElements(L, 2);
		  t = Index2Address(L, idx);
		  CheckValidIndex(L, t);
		  luaV_settable(L, t, L.top - 2, L.top - 1);
		  L.top -= 2;  /* pop index and value */
		  LinyeeUnlock(L);
		}


		public static void LinyeeSetField (LinyeeState L, int idx, CharPtr k) {
		  StkId t;
		  TValue key = new LinyeeTypeValue();			
		  LinyeeLock(L);
		  CheckNElements(L, 1);
		  t = Index2Address(L, idx);
		  CheckValidIndex(L, t);
		  SetSValue(L, key, luaS_new(L, k));
		  luaV_settable(L, t, key, L.top - 1);
		  StkId.Dec(ref L.top);  /* pop value */
		  LinyeeUnlock(L);
		}


		public static void LinyeeRawSet (LinyeeState L, int idx) {
		  StkId t;
		  LinyeeLock(L);
		  CheckNElements(L, 2);
		  t = Index2Address(L, idx);
		  ApiCheck(L, TTIsTable(t));
		  SetObj2T(L, luaH_set(L, HValue(t), L.top-2), L.top-1);
		  LinyeeCBarrierT(L, HValue(t), L.top-1);
		  L.top -= 2;
		  LinyeeUnlock(L);
		}


		public static void LinyeeRawSetI (LinyeeState L, int idx, int n) {
		  StkId o;
		  LinyeeLock(L);
		  CheckNElements(L, 1);
		  o = Index2Address(L, idx);
		  ApiCheck(L, TTIsTable(o));
		  SetObj2T(L, luaH_setnum(L, HValue(o), n), L.top-1);
		  LinyeeCBarrierT(L, HValue(o), L.top-1);
		  StkId.Dec(ref L.top);
		  LinyeeUnlock(L);
		}


		public static int LinyeeSetMetatable (LinyeeState L, int objindex) {
		  TValue obj;
		  Table mt;
		  LinyeeLock(L);
		  CheckNElements(L, 1);
		  obj = Index2Address(L, objindex);
		  CheckValidIndex(L, obj);
		  if (TTIsNil(L.top - 1))
			  mt = null;
		  else {
			ApiCheck(L, TTIsTable(L.top - 1));
			mt = HValue(L.top - 1);
		  }
		  switch (TType(obj)) {
			case LINYEE_TTABLE: {
			  HValue(obj).metatable = mt;
			  if (mt != null)
				LinyeeCObjBarrierT(L, HValue(obj), mt);
			  break;
			}
			case LINYEE_TUSERDATA: {
			  UValue(obj).metatable = mt;
			  if (mt != null)
				LinyeeCObjBarrier(L, RawUValue(obj), mt);
			  break;
			}
			default: {
			  G(L).mt[TType(obj)] = mt;
			  break;
			}
		  }
		  StkId.Dec(ref L.top);
		  LinyeeUnlock(L);
		  return 1;
		}


		public static int LinyeeSetFEnv (LinyeeState L, int idx) {
		  StkId o;
		  int res = 1;
		  LinyeeLock(L);
		  CheckNElements(L, 1);
		  o = Index2Address(L, idx);
		  CheckValidIndex(L, o);
		  ApiCheck(L, TTIsTable(L.top - 1));
		  switch (TType(o)) {
			case LINYEE_TFUNCTION:
			  CLValue(o).c.env = HValue(L.top - 1);
			  break;
			case LINYEE_TUSERDATA:
			  UValue(o).env = HValue(L.top - 1);
			  break;
			case LINYEE_TTHREAD:
			  SetHValue(L, Gt(THValue(o)), HValue(L.top - 1));
			  break;
			default:
			  res = 0;
			  break;
		  }
		  if (res != 0) LinyeeCObjBarrier(L, GCValue(o), HValue(L.top - 1));
		  StkId.Dec(ref L.top);
		  LinyeeUnlock(L);
		  return res;
		}


		/*
		** `load' and `call' functions (run Linyee code)
		*/


		public static void AdjustResults(LinyeeState L, int nres) {
			if (nres == LINYEE_MULTRET && L.top >= L.ci.top)
				L.ci.top = L.top;
		}


		public static void CheckResults(LinyeeState L, int na, int nr) {
			ApiCheck(L, (nr) == LINYEE_MULTRET || (L.ci.top - L.top >= (nr) - (na)));
		}
			

		public static void LinyeeCall (LinyeeState L, int nargs, int nresults) {
		  StkId func;
		  LinyeeLock(L);
		  CheckNElements(L, nargs+1);
		  CheckResults(L, nargs, nresults);
		  func = L.top - (nargs+1);
		  LinyeeDCall(L, func, nresults);
		  AdjustResults(L, nresults);
		  LinyeeUnlock(L);
		}



		/*
		** Execute a protected call.
		*/
		public class CallS {  /* data to `f_call' */
		  public StkId func;
			public int nresults;
		};


		static void FunctionCall (LinyeeState L, object ud) {
		  CallS c = ud as CallS;
		  LinyeeDCall(L, c.func, c.nresults);
		}



		public static int LinyeePCall (LinyeeState L, int nargs, int nresults, int errfunc) {
		  CallS c = new CallS();
		  int status;
		  ptrdiff_t func;
		  LinyeeLock(L);
		  CheckNElements(L, nargs+1);
		  CheckResults(L, nargs, nresults);
		  if (errfunc == 0)
			func = 0;
		  else {
			StkId o = Index2Address(L, errfunc);
			CheckValidIndex(L, o);
			func = SaveStack(L, o);
		  }
		  c.func = L.top - (nargs+1);  /* function to be called */
		  c.nresults = nresults;
		  status = LinyeeDPCall(L, FunctionCall, c, SaveStack(L, c.func), func);
		  AdjustResults(L, nresults);
		  LinyeeUnlock(L);
		  return status;
		}


		/*
		** Execute a protected C call.
		*/
		public class CCallS {  /* data to `f_Ccall' */
		  public LinyeeNativeFunction func;
		  public object ud;
		};


		static void FunctionCCall (LinyeeState L, object ud) {
		  CCallS c = ud as CCallS;
		  Closure cl;
		  cl = LinyeeFNewCclosure(L, 0, GetCurrentEnv(L));
		  cl.c.f = c.func;
		  SetCLValue(L, L.top, cl);  /* push function */
		  IncrementTop(L);
		  SetPValue(L.top, c.ud);  /* push only argument */
		  IncrementTop(L);
		  LinyeeDCall(L, L.top - 2, 0);
		}


		public static int LinyeeCPCall (LinyeeState L, LinyeeNativeFunction func, object ud) {
		  CCallS c = new CCallS();
		  int status;
		  LinyeeLock(L);
		  c.func = func;
		  c.ud = ud;
		  status = LinyeeDPCall(L, FunctionCCall, c, SaveStack(L, L.top), 0);
		  LinyeeUnlock(L);
		  return status;
		}

		[CLSCompliantAttribute(false)]
		public static int LinyeeLoad (LinyeeState L, ly_Reader reader, object data,
							  CharPtr chunkname) {
		  ZIO z = new ZIO();
		  int status;
		  LinyeeLock(L);
		  if (chunkname == null) chunkname = "?";
		  luaZ_init(L, z, reader, data);
		  status = LinyeeDProtectedParser(L, z, chunkname);
		  LinyeeUnlock(L);
		  return status;
		}

		[CLSCompliantAttribute(false)]
		public static int LinyeeDump (LinyeeState L, ly_Writer writer, object data) {
		  int status;
		  TValue o;
		  LinyeeLock(L);
		  CheckNElements(L, 1);
		  o = L.top - 1;
		  if (IsLfunction(o))
			status = LinyeeUDump(L, CLValue(o).l.p, writer, data, 0);
		  else
			status = 1;
		  LinyeeUnlock(L);
		  return status;
		}


		public static int  LinyeeStatus (LinyeeState L) {
		  return L.status;
		}


		/*
		** Garbage-collection function
		*/

		public static int LinyeeGC (LinyeeState L, int what, int data) {
		  int res = 0;
		  GlobalState g;
		  LinyeeLock(L);
		  g = G(L);
		  switch (what) {
			case LINYEE_GCSTOP: {
			  g.GCthreshold = MAXLUMEM;
			  break;
			}
			case LINYEE_GCRESTART: {
			  g.GCthreshold = g.totalbytes;
			  break;
			}
			case LINYEE_GCCOLLECT: {
			  LinyeeCFullGC(L);
			  break;
			}
			case LINYEE_GCCOUNT: {
			  /* GC values are expressed in Kbytes: #bytes/2^10 */
			  res = CastInt(g.totalbytes >> 10);
			  break;
			}
			case LINYEE_GCCOUNTB: {
			  res = CastInt(g.totalbytes & 0x3ff);
			  break;
			}
			case LINYEE_GCSTEP: {
			  ly_mem a = ((ly_mem)data << 10);
			  if (a <= g.totalbytes)
				g.GCthreshold = (uint)(g.totalbytes - a);
			  else
				g.GCthreshold = 0;
			  while (g.GCthreshold <= g.totalbytes) {
				LinyeeCStep(L);
				if (g.gcstate == GCSpause) {  /* end of cycle? */
				  res = 1;  /* signal it */
				  break;
				}
			  }
			  break;
			}
			case LINYEE_GCSETPAUSE: {
			  res = g.gcpause;
			  g.gcpause = data;
			  break;
			}
			case LINYEE_GCSETSTEPMUL: {
			  res = g.gcstepmul;
			  g.gcstepmul = data;
			  break;
			}
			default:
				res = -1;  /* invalid option */
				break;
		  }
		  LinyeeUnlock(L);
		  return res;
		}



		/*
		** miscellaneous functions
		*/


		public static int LinyeeError (LinyeeState L) {
		  LinyeeLock(L);
		  CheckNElements(L, 1);
		  LinyeeGErrorMsg(L);
		  LinyeeUnlock(L);
		  return 0;  /* to avoid warnings */
		}


		public static int LinyeeNext (LinyeeState L, int idx) {
		  StkId t;
		  int more;
		  LinyeeLock(L);
		  t = Index2Address(L, idx);
		  ApiCheck(L, TTIsTable(t));
		  more = luaH_next(L, HValue(t), L.top - 1);
		  if (more != 0) {
			IncrementTop(L);
		  }
		  else  /* no more elements */
			StkId.Dec(ref L.top);  /* remove key */
		  LinyeeUnlock(L);
		  return more;
		}


		public static void LinyeeConcat (LinyeeState L, int n) {
		  LinyeeLock(L);
		  CheckNElements(L, n);
		  if (n >= 2) {
			LinyeeCCheckGC(L);
			luaV_concat(L, n, CastInt(L.top - L.base_) - 1);
			L.top -= (n-1);
		  }
		  else if (n == 0) {  /* push empty string */
			SetSValue2S(L, L.top, luaS_newlstr(L, "", 0));
			IncrementTop(L);
		  }
		  /* else n == 1; nothing to do */
		  LinyeeUnlock(L);
		}


		public static ly_Alloc LinyeeGetAllocF (LinyeeState L, ref object ud) {
		  ly_Alloc f;
		  LinyeeLock(L);
		  if (ud != null) ud = G(L).ud;
		  f = G(L).frealloc;
		  LinyeeUnlock(L);
		  return f;
		}


		public static void LinyeeSetAllocF (LinyeeState L, ly_Alloc f, object ud) {
		  LinyeeLock(L);
		  G(L).ud = ud;
		  G(L).frealloc = f;
		  LinyeeUnlock(L);
		}

		[CLSCompliantAttribute(false)]
		public static object LinyeeNewUserData(LinyeeState L, uint size)
		{
			Udata u;
			LinyeeLock(L);
			LinyeeCCheckGC(L);
			u = luaS_newudata(L, size, GetCurrentEnv(L));
			SetUValue(L, L.top, u);
			IncrementTop(L);
			LinyeeUnlock(L);
			return u.user_data;
		}

		// this one is used internally only
		internal static object LinyeeNewUserData(LinyeeState L, Type t)
		{
			Udata u;
			LinyeeLock(L);
			LinyeeCCheckGC(L);
			u = luaS_newudata(L, t, GetCurrentEnv(L));
			SetUValue(L, L.top, u);
			IncrementTop(L);
			LinyeeUnlock(L);
			return u.user_data;
		}

		static CharPtr AuxUpValue (StkId fi, int n, ref TValue val) {
		  Closure f;
		  if (!TTIsFunction(fi)) return null;
		  f = CLValue(fi);
		  if (f.c.isC != 0) {
			if (!(1 <= n && n <= f.c.nupvalues)) return null;
			val = f.c.upvalue[n-1];
			return "";
		  }
		  else {
			Proto p = f.l.p;
			if (!(1 <= n && n <= p.sizeupvalues)) return null;
			val = f.l.upvals[n-1].v;
			return GetStr(p.upvalues[n-1]);
		  }
		}


		public static CharPtr LinyeeGetUpValue (LinyeeState L, int funcindex, int n) {
		  CharPtr name;
		  TValue val = new LinyeeTypeValue();
		  LinyeeLock(L);
		  name = AuxUpValue(Index2Address(L, funcindex), n, ref val);
		  if (name != null) {
			SetObj2S(L, L.top, val);
			IncrementTop(L);
		  }
		  LinyeeUnlock(L);
		  return name;
		}


		public static CharPtr LinyeeSetUpValue (LinyeeState L, int funcindex, int n) {
		  CharPtr name;
		  TValue val = new LinyeeTypeValue();
		  StkId fi;
		  LinyeeLock(L);
		  fi = Index2Address(L, funcindex);
		  CheckNElements(L, 1);
		  name = AuxUpValue(fi, n, ref val);
		  if (name != null) {
			StkId.Dec(ref L.top);
			SetObj(L, val, L.top);
			LinyeeCBarrier(L, CLValue(fi), L.top);
		  }
		  LinyeeUnlock(L);
		  return name;
		}

	}
}
