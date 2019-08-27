/*
** $Id: lbaselib.c,v 1.191.1.6 2008/02/14 16:46:22 roberto Exp $
** Basic library
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Linyee
{
	using LinyeeNumberType = System.Double;

	public partial class Linyee
	{
		/*
		** If your system does not support `stdout', you can just remove this function.
		** If you need, you can define your own `print' function, following this
		** model but changing `fputs' to put the strings at a proper place
		** (a console window or a log file, for instance).
		*/
		private static int LinyeeBPrint (LinyeeState L) {
		  int n = LinyeeGetTop(L);  /* number of arguments */
		  int i;
		  LinyeeGetGlobal(L, "tostring");
		  for (i=1; i<=n; i++) {
			CharPtr s;
			LinyeePushValue(L, -1);  /* function to be called */
			LinyeePushValue(L, i);   /* value to print */
			LinyeeCall(L, 1, 1);
			s = LinyeeToString(L, -1);  /* get result */
			if (s == null)
			  return LinyeeLError(L, LINYEE_QL("tostring") + " must return a string to " +
								   LINYEE_QL("print"));
			if (i > 1) fputs("\t", stdout);
			fputs(s, stdout);
			LinyeePop(L, 1);  /* pop result */
		  }
		  Console.Write("\n", stdout);
		  return 0;
		}


		private static int LinyeeBToNumber (LinyeeState L) {
		  int base_ = LinyeeLOptInt(L, 2, 10);
		  if (base_ == 10) {  /* standard conversion */
			LinyeeLCheckAny(L, 1);
			if (LinyeeIsNumber(L, 1) != 0) {
			  LinyeePushNumber(L, LinyeeToNumber(L, 1));
			  return 1;
			}
		  }
		  else {
			CharPtr s1 = LinyeeLCheckString(L, 1);
			CharPtr s2;
			ulong n;
			LinyeeLArgCheck(L, 2 <= base_ && base_ <= 36, 2, "base out of range");
			n = strtoul(s1, out s2, base_);
			if (s1 != s2) {  /* at least one valid digit? */
			  while (isspace((byte)(s2[0]))) s2 = s2.next();  /* skip trailing spaces */
			  if (s2[0] == '\0') {  /* no invalid trailing characters? */
				LinyeePushNumber(L, (LinyeeNumberType)n);
				return 1;
			  }
			}
		  }
		  LinyeePushNil(L);  /* else not a number */
		  return 1;
		}


		private static int LinyeeBError (LinyeeState L) {
		  int level = LinyeeLOptInt(L, 2, 1);
		  LinyeeSetTop(L, 1);
		  if ((LinyeeIsString(L, 1)!=0) && (level > 0)) {  /* add extra information? */
			LinyeeLWhere(L, level);
			LinyeePushValue(L, 1);
			LinyeeConcat(L, 2);
		  }
		  return LinyeeError(L);
		}


		private static int LinyeeBGetMetatable (LinyeeState L) {
		  LinyeeLCheckAny(L, 1);
		  if (LinyeeGetMetatable(L, 1)==0) {
			LinyeePushNil(L);
			return 1;  /* no metatable */
		  }
		  LinyeeLGetMetafield(L, 1, "__metatable");
		  return 1;  /* returns either __metatable field (if present) or metatable */
		}


		private static int LinyeeBSetMetatable (LinyeeState L) {
		  int t = LinyeeType(L, 2);
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		  LinyeeLArgCheck(L, t == LINYEE_TNIL || t == LINYEE_TTABLE, 2,
							"nil or table expected");
		  if (LinyeeLGetMetafield(L, 1, "__metatable") != 0)
			LinyeeLError(L, "cannot change a protected metatable");
		  LinyeeSetTop(L, 2);
		  LinyeeSetMetatable(L, 1);
		  return 1;
		}


		private static void GetFunc (LinyeeState L, int opt) {
		  if (LinyeeIsFunction(L, 1)) LinyeePushValue(L, 1);
		  else {
			LinyeeDebug ar = new LinyeeDebug();
			int level = (opt != 0) ? LinyeeLOptInt(L, 1, 1) : LinyeeLCheckInt(L, 1);
			LinyeeLArgCheck(L, level >= 0, 1, "level must be non-negative");
			if (LinyeeGetStack(L, level, ref ar) == 0)
			  LinyeeLArgError(L, 1, "invalid level");
			LinyeeGetInfo(L, "f", ref ar);
			if (LinyeeIsNil(L, -1))
			  LinyeeLError(L, "no function environment for tail call at level %d",
							level);
		  }
		}


		private static int LinyeeBGetFEnv (LinyeeState L) {
		  GetFunc(L, 1);
		  if (LinyeeIsCFunction(L, -1))  /* is a C function? */
			LinyeePushValue(L, LINYEE_GLOBALSINDEX);  /* return the thread's global env. */
		  else
			LinyeeGetFEnv(L, -1);
		  return 1;
		}


		private static int LinyeeBSetFEnv (LinyeeState L) {
		  LinyeeLCheckType(L, 2, LINYEE_TTABLE);
		  GetFunc(L, 0);
		  LinyeePushValue(L, 2);
		  if ((LinyeeIsNumber(L, 1)!=0) && (LinyeeToNumber(L, 1) == 0)) {
			/* change environment of current thread */
			LinyeePushThread(L);
			LinyeeInsert(L, -2);
			LinyeeSetFEnv(L, -2);
			return 0;
		  }
		  else if (LinyeeIsCFunction(L, -2) || LinyeeSetFEnv(L, -2) == 0)
			LinyeeLError(L,
				  LINYEE_QL("setfenv") + " cannot change environment of given object");
		  return 1;
		}


		private static int LinyeeBRawEqual (LinyeeState L) {
		  LinyeeLCheckAny(L, 1);
		  LinyeeLCheckAny(L, 2);
		  LinyeePushBoolean(L, LinyeeRawEqual(L, 1, 2));
		  return 1;
		}


		private static int LinyeeBRawGet (LinyeeState L) {
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		  LinyeeLCheckAny(L, 2);
		  LinyeeSetTop(L, 2);
		  LinyeeRawGet(L, 1);
		  return 1;
		}

		private static int LinyeeBRawSet (LinyeeState L) {
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		  LinyeeLCheckAny(L, 2);
		  LinyeeLCheckAny(L, 3);
		  LinyeeSetTop(L, 3);
		  LinyeeRawSet(L, 1);
		  return 1;
		}


		private static int LinyeeBGGInfo (LinyeeState L) {
		  LinyeePushInteger(L, LinyeeGetGCCount(L));
		  return 1;
		}

		public static readonly CharPtr[] opts = {"stop", "restart", "collect",
			"count", "step", "setpause", "setstepmul", null};
		public readonly static int[] optsnum = {LINYEE_GCSTOP, LINYEE_GCRESTART, LINYEE_GCCOLLECT,
			LINYEE_GCCOUNT, LINYEE_GCSTEP, LINYEE_GCSETPAUSE, LINYEE_GCSETSTEPMUL};

		private static int LinyeeBCollectGarbage (LinyeeState L) {		  
		  int o = LinyeeLCheckOption(L, 1, "collect", opts);
		  int ex = LinyeeLOptInt(L, 2, 0);
		  int res = LinyeeGC(L, optsnum[o], ex);
		  switch (optsnum[o]) {
			case LINYEE_GCCOUNT: {
			  int b = LinyeeGC(L, LINYEE_GCCOUNTB, 0);
			  LinyeePushNumber(L, res + ((LinyeeNumberType)b/1024));
			  return 1;
			}
			case LINYEE_GCSTEP: {
			  LinyeePushBoolean(L, res);
			  return 1;
			}
			default: {
			  LinyeePushNumber(L, res);
			  return 1;
			}
		  }
		}


		private static int LinyeeBType (LinyeeState L) {
		  LinyeeLCheckAny(L, 1);
		  LinyeePushString(L, LinyeeLTypeName(L, 1));
		  return 1;
		}


		private static int LinyeeBNext (LinyeeState L) {
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		  LinyeeSetTop(L, 2);  /* create a 2nd argument if there isn't one */
		  if (LinyeeNext(L, 1) != 0)
			return 2;
		  else {
			LinyeePushNil(L);
			return 1;
		  }
		}


		private static int LinyeeBPairs (LinyeeState L) {
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		  LinyeePushValue(L, LinyeeUpValueIndex(1));  /* return generator, */
		  LinyeePushValue(L, 1);  /* state, */
		  LinyeePushNil(L);  /* and initial value */
		  return 3;
		}


		private static int CheckPairsAux (LinyeeState L) {
		  int i = LinyeeLCheckInt(L, 2);
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		  i++;  /* next value */
		  LinyeePushInteger(L, i);
		  LinyeeRawGetI(L, 1, i);
		  return (LinyeeIsNil(L, -1)) ? 0 : 2;
		}


		private static int LinyeeBIPairs (LinyeeState L) {
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		  LinyeePushValue(L, LinyeeUpValueIndex(1));  /* return generator, */
		  LinyeePushValue(L, 1);  /* state, */
		  LinyeePushInteger(L, 0);  /* and initial value */
		  return 3;
		}


		private static int LoadAux (LinyeeState L, int status) {
		  if (status == 0)  /* OK? */
			return 1;
		  else {
			LinyeePushNil(L);
			LinyeeInsert(L, -2);  /* put before error message */
			return 2;  /* return nil plus error message */
		  }
		}


		private static int LinyeeBLoadString (LinyeeState L) {
		  uint l;
		  CharPtr s = LinyeeLCheckLString(L, 1, out l);
		  CharPtr chunkname = LinyeeLOptString(L, 2, s);
		  return LoadAux(L, LinyeeLLoadBuffer(L, s, l, chunkname));
		}


		private static int LinyeeBLoadFile (LinyeeState L) {
		  CharPtr fname = LinyeeLOptString(L, 1, null);
		  return LoadAux(L, LinyeeLLoadFile(L, fname));
		}


		/*
		** Reader for generic `load' function: `ly_load' uses the
		** stack for internal stuff, so the reader cannot change the
		** stack top. Instead, it keeps its resulting string in a
		** reserved slot inside the stack.
		*/
		private static CharPtr GenericReader (LinyeeState L, object ud, out uint size) {
		  //(void)ud;  /* to avoid warnings */
		  LinyeeLCheckStack(L, 2, "too many nested functions");
		  LinyeePushValue(L, 1);  /* get function */
		  LinyeeCall(L, 0, 1);  /* call it */
		  if (LinyeeIsNil(L, -1)) {
			size = 0;
			return null;
		  }
		  else if (LinyeeIsString(L, -1) != 0)
		  {
			  LinyeeReplace(L, 3);  /* save string in a reserved stack slot */
			  return LinyeeToLString(L, 3, out size);
		  }
		  else
		  {
			  size = 0;
			  LinyeeLError(L, "reader function must return a string");
		  }
		  return null;  /* to avoid warnings */
		}


		private static int LinyeeBLoad (LinyeeState L) {
		  int status;
		  CharPtr cname = LinyeeLOptString(L, 2, "=(load)");
		  LinyeeLCheckType(L, 1, LINYEE_TFUNCTION);
		  LinyeeSetTop(L, 3);  /* function, eventual name, plus one reserved slot */
		  status = LinyeeLoad(L, GenericReader, null, cname);
		  return LoadAux(L, status);
		}


		private static int LinyeeBDoFile (LinyeeState L) {
		  CharPtr fname = LinyeeLOptString(L, 1, null);
		  int n = LinyeeGetTop(L);
		  if (LinyeeLLoadFile(L, fname) != 0) LinyeeError(L);
		  LinyeeCall(L, 0, LINYEE_MULTRET);
		  return LinyeeGetTop(L) - n;
		}


		private static int LinyeeBAssert (LinyeeState L) {
		  LinyeeLCheckAny(L, 1);
		  if (LinyeeToBoolean(L, 1)==0)
			return LinyeeLError(L, "%s", LinyeeLOptString(L, 2, "assertion failed!"));
		  return LinyeeGetTop(L);
		}


		private static int LinyeeBUnpack (LinyeeState L) {
		  int i, e, n;
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		  i = LinyeeLOptInt(L, 2, 1);
		  e = LinyeeLOptInteger(L, LinyeeLCheckInt, 3, LinyeeLGetN(L, 1));
		  if (i > e) return 0;  /* empty range */
		  n = e - i + 1;  /* number of elements */
		  if (n <= 0 || (LinyeeCheckStack(L, n)==0))  /* n <= 0 means arith. overflow */
			return LinyeeLError(L, "too many results to unpack");
		  LinyeeRawGetI(L, 1, i);  /* push arg[i] (avoiding overflow problems) */
		  while (i++ < e)  /* push arg[i + 1...e] */
			LinyeeRawGetI(L, 1, i);
		  return n;
		}


		private static int LinyeeBSelect (LinyeeState L) {
		  int n = LinyeeGetTop(L);
		  if (LinyeeType(L, 1) == LINYEE_TSTRING && LinyeeToString(L, 1)[0] == '#') {
			LinyeePushInteger(L, n-1);
			return 1;
		  }
		  else {
			int i = LinyeeLCheckInt(L, 1);
			if (i < 0) i = n + i;
			else if (i > n) i = n;
			LinyeeLArgCheck(L, 1 <= i, 1, "index out of range");
			return n - i;
		  }
		}


		private static int LinyeeBPCall (LinyeeState L) {
		  int status;
		  LinyeeLCheckAny(L, 1);
		  status = LinyeePCall(L, LinyeeGetTop(L) - 1, LINYEE_MULTRET, 0);
		  LinyeePushBoolean(L, (status == 0) ? 1 : 0);
		  LinyeeInsert(L, 1);
		  return LinyeeGetTop(L);  /* return status + all results */
		}


		private static int LinyeeBXPCall (LinyeeState L) {
		  int status;
		  LinyeeLCheckAny(L, 2);
		  LinyeeSetTop(L, 2);
		  LinyeeInsert(L, 1);  /* put error function under function to be called */
		  status = LinyeePCall(L, 0, LINYEE_MULTRET, 1);
		  LinyeePushBoolean(L, (status == 0) ? 1 : 0);
		  LinyeeReplace(L, 1);
		  return LinyeeGetTop(L);  /* return status + all results */
		}


		private static int LinyeeBToString (LinyeeState L) {
		  LinyeeLCheckAny(L, 1);
		  if (LinyeeLCallMeta(L, 1, "__tostring") != 0)  /* is there a metafield? */
			return 1;  /* use its value */
		  switch (LinyeeType(L, 1)) {
			case LINYEE_TNUMBER:
			  LinyeePushString(L, LinyeeToString(L, 1));
			  break;
			case LINYEE_TSTRING:
			  LinyeePushValue(L, 1);
			  break;
			case LINYEE_TBOOLEAN:
			  LinyeePushString(L, (LinyeeToBoolean(L, 1) != 0 ? "true" : "false"));
			  break;
			case LINYEE_TNIL:
			  LinyeePushLiteral(L, "nil");
			  break;
			default:
			  LinyeePushFString(L, "%s: %p", LinyeeLTypeName(L, 1), LinyeeToPointer(L, 1));
			  break;
		  }
		  return 1;
		}


		private static int LinyeeBNewProxy (LinyeeState L) {
		  LinyeeSetTop(L, 1);
		  LinyeeNewUserData(L, 0);  /* create proxy */
		  if (LinyeeToBoolean(L, 1) == 0)
			return 1;  /* no metatable */
		  else if (LinyeeIsBoolean(L, 1)) {
			LinyeeNewTable(L);  /* create a new metatable `m' ... */
			LinyeePushValue(L, -1);  /* ... and mark `m' as a valid metatable */
			LinyeePushBoolean(L, 1);
			LinyeeRawSet(L, LinyeeUpValueIndex(1));  /* weaktable[m] = true */
		  }
		  else {
			int validproxy = 0;  /* to check if weaktable[metatable(u)] == true */
			if (LinyeeGetMetatable(L, 1) != 0) {
			  LinyeeRawGet(L, LinyeeUpValueIndex(1));
			  validproxy = LinyeeToBoolean(L, -1);
			  LinyeePop(L, 1);  /* remove value */
			}
			LinyeeLArgCheck(L, validproxy!=0, 1, "boolean or proxy expected");
			LinyeeGetMetatable(L, 1);  /* metatable is valid; get it */
		  }
		  LinyeeSetMetatable(L, 2);
		  return 1;
		}


		private readonly static LinyeeLReg[] base_funcs = {
		  new LinyeeLReg("assert", LinyeeBAssert),
		  new LinyeeLReg("collectgarbage", LinyeeBCollectGarbage),
		  new LinyeeLReg("dofile", LinyeeBDoFile),
		  new LinyeeLReg("error", LinyeeBError),
		  new LinyeeLReg("gcinfo", LinyeeBGGInfo),
		  new LinyeeLReg("getfenv", LinyeeBGetFEnv),
		  new LinyeeLReg("getmetatable", LinyeeBGetMetatable),
		  new LinyeeLReg("loadfile", LinyeeBLoadFile),
		  new LinyeeLReg("load", LinyeeBLoad),
		  new LinyeeLReg("loadstring", LinyeeBLoadString),
		  new LinyeeLReg("next", LinyeeBNext),
		  new LinyeeLReg("pcall", LinyeeBPCall),
		  new LinyeeLReg("print", LinyeeBPrint),
		  new LinyeeLReg("rawequal", LinyeeBRawEqual),
		  new LinyeeLReg("rawget", LinyeeBRawGet),
		  new LinyeeLReg("rawset", LinyeeBRawSet),
		  new LinyeeLReg("select", LinyeeBSelect),
		  new LinyeeLReg("setfenv", LinyeeBSetFEnv),
		  new LinyeeLReg("setmetatable", LinyeeBSetMetatable),
		  new LinyeeLReg("tonumber", LinyeeBToNumber),
		  new LinyeeLReg("tostring", LinyeeBToString),
		  new LinyeeLReg("type", LinyeeBType),
		  new LinyeeLReg("unpack", LinyeeBUnpack),
		  new LinyeeLReg("xpcall", LinyeeBXPCall),
		  new LinyeeLReg(null, null)
		};


		/*
		** {======================================================
		** Coroutine library
		** =======================================================
		*/

		public const int CO_RUN		= 0;	/* running */
		public const int CO_SUS		= 1;	/* suspended */
		public const int CO_NOR		= 2;	/* 'normal' (it resumed another coroutine) */
		public const int CO_DEAD	= 3;

		private static readonly string[] statnames =
			{"running", "suspended", "normal", "dead"};

		private static int costatus (LinyeeState L, LinyeeState co) {
		  if (L == co) return CO_RUN;
		  switch (LinyeeStatus(co)) {
			case LINYEE_YIELD:
			  return CO_SUS;
			case 0: {
			  LinyeeDebug ar = new LinyeeDebug();
			  if (LinyeeGetStack(co, 0,ref ar) > 0)  /* does it have frames? */
				return CO_NOR;  /* it is running */
			  else if (LinyeeGetTop(co) == 0)
				  return CO_DEAD;
			  else
				return CO_SUS;  /* initial state */
			}
			default:  /* some error occured */
			  return CO_DEAD;
		  }
		}


		private static int LinyeeBCosStatus (LinyeeState L) {
		  LinyeeState co = LinyeeToThread(L, 1);
		  LinyeeLArgCheck(L, co!=null, 1, "coroutine expected");
		  LinyeePushString(L, statnames[costatus(L, co)]);
		  return 1;
		}


		private static int AuxResume (LinyeeState L, LinyeeState co, int narg) {
		  int status = costatus(L, co);
		  if (LinyeeCheckStack(co, narg)==0)
			LinyeeLError(L, "too many arguments to resume");
		  if (status != CO_SUS) {
			LinyeePushFString(L, "cannot resume %s coroutine", statnames[status]);
			return -1;  /* error flag */
		  }
		  LinyeeXMove(L, co, narg);
		  LinyeeSetLevel(L, co);
		  status = LinyeeResume(co, narg);
		  if (status == 0 || status == LINYEE_YIELD) {
			int nres = LinyeeGetTop(co);
			if (LinyeeCheckStack(L, nres + 1)==0)
			  LinyeeLError(L, "too many results to resume");
			LinyeeXMove(co, L, nres);  /* move yielded values */
			return nres;
		  }
		  else {
			LinyeeXMove(co, L, 1);  /* move error message */
			return -1;  /* error flag */
		  }
		}


		private static int LinyeeBCorResume (LinyeeState L) {
		  LinyeeState co = LinyeeToThread(L, 1);
		  int r;
		  LinyeeLArgCheck(L, co!=null, 1, "coroutine expected");
		  r = AuxResume(L, co, LinyeeGetTop(L) - 1);
		  if (r < 0) {
			LinyeePushBoolean(L, 0);
			LinyeeInsert(L, -2);
			return 2;  /* return false + error message */
		  }
		  else {
			LinyeePushBoolean(L, 1);
			LinyeeInsert(L, -(r + 1));
			return r + 1;  /* return true + `resume' returns */
		  }
		}


		private static int LinyeeBAuxWrap (LinyeeState L) {
		  LinyeeState co = LinyeeToThread(L, LinyeeUpValueIndex(1));
		  int r = AuxResume(L, co, LinyeeGetTop(L));
		  if (r < 0) {
			if (LinyeeIsString(L, -1) != 0) {  /* error object is a string? */
			  LinyeeLWhere(L, 1);  /* add extra info */
			  LinyeeInsert(L, -2);
			  LinyeeConcat(L, 2);
			}
			LinyeeError(L);  /* propagate error */
		  }
		  return r;
		}


		private static int LinyeeBCoCreate (LinyeeState L) {
		  LinyeeState NL = LinyeeNewThread(L);
		  LinyeeLArgCheck(L, LinyeeIsFunction(L, 1) && !LinyeeIsCFunction(L, 1), 1,
			"Linyee function expected");
		  LinyeePushValue(L, 1);  /* move function to top */
		  LinyeeXMove(L, NL, 1);  /* move function from L to NL */
		  return 1;
		}


		private static int LinyeeBCoWrap (LinyeeState L) {
		  LinyeeBCoCreate(L);
		  LinyeePushCClosure(L, LinyeeBAuxWrap, 1);
		  return 1;
		}


		private static int LinyeeBYield (LinyeeState L) {
		  return LinyeeYield(L, LinyeeGetTop(L));
		}


		private static int LinyeeBCoRunning (LinyeeState L) {
		  if (LinyeePushThread(L) != 0)
			LinyeePushNil(L);  /* main thread is not a coroutine */
		  return 1;
		}


		private readonly static LinyeeLReg[] co_funcs = {
		  new LinyeeLReg("create", LinyeeBCoCreate),
		  new LinyeeLReg("resume", LinyeeBCorResume),
		  new LinyeeLReg("running", LinyeeBCoRunning),
		  new LinyeeLReg("status", LinyeeBCosStatus),
		  new LinyeeLReg("wrap", LinyeeBCoWrap),
		  new LinyeeLReg("yield", LinyeeBYield),
		  new LinyeeLReg(null, null)
		};

		/* }====================================================== */


		private static void AuxOpen (LinyeeState L, CharPtr name,
							 LinyeeNativeFunction f, LinyeeNativeFunction u) {
		  LinyeePushCFunction(L, u);
		  LinyeePushCClosure(L, f, 1);
		  LinyeeSetField(L, -2, name);
		}


		private static void BaseOpen (LinyeeState L) {
		  /* set global _G */
		  LinyeePushValue(L, LINYEE_GLOBALSINDEX);
		  LinyeeSetGlobal(L, "_G");
		  /* open lib into global table */
		  LinyeeLRegister(L, "_G", base_funcs);
		  LinyeePushLiteral(L, LINYEE_VERSION);
		  LinyeeSetGlobal(L, "_VERSION");  /* set global _VERSION */
		  /* `ipairs' and `pairs' need auxiliary functions as upvalues */
		  AuxOpen(L, "ipairs", LinyeeBIPairs, CheckPairsAux);
		  AuxOpen(L, "pairs", LinyeeBPairs, LinyeeBNext);
		  /* `newproxy' needs a weaktable as upvalue */
		  LinyeeCreateTable(L, 0, 1);  /* new table `w' */
		  LinyeePushValue(L, -1);  /* `w' will be its own metatable */
		  LinyeeSetMetatable(L, -2);
		  LinyeePushLiteral(L, "kv");
		  LinyeeSetField(L, -2, "__mode");  /* metatable(w).__mode = "kv" */
		  LinyeePushCClosure(L, LinyeeBNewProxy, 1);
		  LinyeeSetGlobal(L, "newproxy");  /* set global `newproxy' */
		  /** Linyee Hack - Add L state to registry to get inside coroutine ***/
		  LinyeePushThread (L);
		  LinyeeSetField (L, LINYEE_REGISTRYINDEX, "main_state");
		}


		public static int LinyeeOpenBase (LinyeeState L) {
		  BaseOpen(L);
		  LinyeeLRegister(L, LINYEE_COLIBNAME, co_funcs);
		  return 2;
		}

	}
}
