/*
** $Id: ldo.c,v 2.38.1.3 2008/01/18 22:31:22 roberto Exp $
** Stack and Call structure of Linyee
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;

#if XBOX
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
#endif

namespace Linyee
{
	using LinyeeIntegerType = System.Int32;
	using ptrdiff_t = System.Int32;
	using TValue = Linyee.LinyeeTypeValue;
	using StkId = Linyee.LinyeeTypeValue;
	using LinyeeByteType = System.Byte;
	using ZIO = Linyee.Zio;

	public partial class Linyee
	{
		public static void LinyeeDCheckStack(LinyeeState L, int n) {
			if ((L.stack_last - L.top) <= n)
				LinyeeDGrowStack(L, n);
			else
			{
				#if HARDSTACKTESTS
				luaD_reallocstack(L, L.stacksize - EXTRA_STACK - 1);
				#endif
			}
		}

		public static void IncrTop(LinyeeState L)
		{
			LinyeeDCheckStack(L, 1);
			StkId.Inc(ref L.top);
		}

		// in the original C code these values save and restore the stack by number of bytes. marshalling sizeof
		// isn't that straightforward in managed languages, so i implement these by index instead.
		public static int SaveStack(LinyeeState L, StkId p)		{return p;}
		public static StkId RestoreStack(LinyeeState L, int n)	{return L.stack[n];}
		public static int SaveCI(LinyeeState L, CallInfo p)		{return p - L.base_ci;}
		public static CallInfo RestoreCI(LinyeeState L, int n)	{ return L.base_ci[n]; }


		/* results from luaD_precall */
		public const int PCRLUA		= 0;	/* initiated a call to a Linyee function */
		public const int PCRC		= 1;	/* did a call to a C function */
		public const int PCRYIELD	= 2;	/* C funtion yielded */


		/* type of protected functions, to be ran by `runprotected' */
		public delegate void Pfunc(LinyeeState L, object ud);


		/*
		** {======================================================
		** Error-recovery functions
		** =======================================================
		*/
		
		public delegate void LinyeeIJmpBuf(LinyeeIntegerType b);

		/* chain list of long jump buffers */
		public class LinyeeLongJmp {
		  public LinyeeLongJmp previous;
		  public LinyeeIJmpBuf b;
          [CLSCompliantAttribute(false)]
		  public volatile int status;  /* error code */
		};


		public static void LinyeeDSetErrorObj (LinyeeState L, int errcode, StkId oldtop) {
		  switch (errcode) {
			case LINYEE_ERRMEM: {
			  SetSValue2S(L, oldtop, luaS_newliteral(L, MEMERRMSG));
			  break;
			}
			case LINYEE_ERRERR: {
			  SetSValue2S(L, oldtop, luaS_newliteral(L, "error in error handling"));
			  break;
			}
			case LINYEE_ERRSYNTAX:
			case LINYEE_ERRRUN: {
			  SetObj2S(L, oldtop, L.top-1);  /* error message on current top */
			  break;
			}
		  }
		  L.top = oldtop + 1;
		}


		private static void RestoreStackLimit (LinyeeState L) {
			LinyeeAssert(L.stack_last == L.stacksize - EXTRASTACK - 1);
		  if (L.size_ci > LUAI_MAXCALLS) {  /* there was an overflow? */
			int inuse = L.ci - L.base_ci;
			if (inuse + 1 < LUAI_MAXCALLS)  /* can `undo' overflow? */
			  LinyeeDReallocCI(L, LUAI_MAXCALLS);
		  }
		}


		private static void ResetStack (LinyeeState L, int status) {
		  L.ci = L.base_ci[0];
		  L.base_ = L.ci.base_;
		  LinyeeFClose(L, L.base_);  /* close eventual pending closures */
		  LinyeeDSetErrorObj(L, status, L.base_);
		  L.nCcalls = L.baseCcalls;
		  L.allowhook = 1;
		  RestoreStackLimit(L);
		  L.errfunc = 0;
		  L.errorJmp = null;
		}


		public static void LinyeeDThrow (LinyeeState L, int errcode) {
		  if (L.errorJmp != null) {
			L.errorJmp.status = errcode;
			LUAI_THROW(L, L.errorJmp);
		  }
		  else {
			L.status = CastByte(errcode);
			if (G(L).panic != null) {
			  ResetStack(L, errcode);
			  LinyeeUnlock(L);
			  G(L).panic(L);
			}
#if XBOX
			throw new ApplicationException();
#else
#if SILVERLIGHT
            throw new SystemException();
#else
			Environment.Exit(EXIT_FAILURE);
#endif
#endif

          }
		}


		public static int LinyeeDRawRunProtected (LinyeeState L, Pfunc f, object ud) {
		  LinyeeLongJmp lj = new LinyeeLongJmp();
		  lj.status = 0;
		  lj.previous = L.errorJmp;  /* chain new error handler */
		  L.errorJmp = lj;
			/*
		  LUAI_TRY(L, lj,
			f(L, ud)
		  );
			 * */
#if CATCH_EXCEPTIONS
		  try
#endif
		  {
			  f(L, ud);
		  }
#if CATCH_EXCEPTIONS
		  catch
		  {
			  if (lj.status == 0)
				  lj.status = -1;
		  }
#endif
		  L.errorJmp = lj.previous;  /* restore old error handler */
		  return lj.status;
		}

		/* }====================================================== */


		private static void CorrectStack (LinyeeState L, TValue[] oldstack) {
			/* don't need to do this
		  CallInfo ci;
		  GCObject up;
		  L.top = L.stack[L.top - oldstack];
		  for (up = L.openupval; up != null; up = up.gch.next)
			gco2uv(up).v = L.stack[gco2uv(up).v - oldstack];
		  for (ci = L.base_ci[0]; ci <= L.ci; CallInfo.inc(ref ci)) {
			  ci.top = L.stack[ci.top - oldstack];
			ci.base_ = L.stack[ci.base_ - oldstack];
			ci.func = L.stack[ci.func - oldstack];
		  }
		  L.base_ = L.stack[L.base_ - oldstack];
			 * */
		}

		public static void LinyeeDRealAllocStack (LinyeeState L, int newsize) {
		  TValue[] oldstack = L.stack;
		  int realsize = newsize + 1 + EXTRASTACK;
		  LinyeeAssert(L.stack_last == L.stacksize - EXTRASTACK - 1);
		  LinyeeMReallocVector(L, ref L.stack, L.stacksize, realsize/*, TValue*/);
		  L.stacksize = realsize;
		  L.stack_last = L.stack[newsize];
		  CorrectStack(L, oldstack);
		}

		public static void LinyeeDReallocCI (LinyeeState L, int newsize) {
		  CallInfo oldci = L.base_ci[0];
		  LinyeeMReallocVector(L, ref L.base_ci, L.size_ci, newsize/*, CallInfo*/);
		  L.size_ci = newsize;
		  L.ci = L.base_ci[L.ci - oldci];
		  L.end_ci = L.base_ci[L.size_ci - 1];
		}

		public static void LinyeeDGrowStack (LinyeeState L, int n) {
		  if (n <= L.stacksize)  /* double size is enough? */
			LinyeeDRealAllocStack(L, 2*L.stacksize);
		  else
			LinyeeDRealAllocStack(L, L.stacksize + n);
		}

		private static CallInfo GrowCI (LinyeeState L) {
		  if (L.size_ci > LUAI_MAXCALLS)  /* overflow while handling overflow? */
			LinyeeDThrow(L, LINYEE_ERRERR);
		  else {
			LinyeeDReallocCI(L, 2*L.size_ci);
			if (L.size_ci > LUAI_MAXCALLS)
			  LinyeeGRunError(L, "stack overflow");
		  }
		  CallInfo.Inc(ref L.ci);
		  return L.ci;
		}


		public static void LinyeeDCallHook (LinyeeState L, int event_, int line) {
		  LinyeeHook hook = L.hook;
		  if ((hook!=null) && (L.allowhook!=0)) {
			ptrdiff_t top = SaveStack(L, L.top);
			ptrdiff_t ci_top = SaveStack(L, L.ci.top);
			LinyeeDebug ar = new LinyeeDebug();
			ar.event_ = event_;
			ar.currentline = line;
			if (event_ == LINYEE_HOOKTAILRET)
			  ar.i_ci = 0;  /* tail call; no debug information about it */
			else
			  ar.i_ci = L.ci - L.base_ci;
			LinyeeDCheckStack(L, LINYEE_MINSTACK);  /* ensure minimum stack size */
			L.ci.top = L.top + LINYEE_MINSTACK;
			LinyeeAssert(L.ci.top <= L.stack_last);
			L.allowhook = 0;  /* cannot call hooks inside a hook */
			LinyeeUnlock(L);
			hook(L, ar);
			LinyeeLock(L);
			LinyeeAssert(L.allowhook==0);
			L.allowhook = 1;
			L.ci.top = RestoreStack(L, ci_top);
			L.top = RestoreStack(L, top);
		  }
		}


		private static StkId AdjustVarArgs (LinyeeState L, Proto p, int actual) {
		  int i;
		  int nfixargs = p.numparams;
		  Table htab = null;
		  StkId base_, fixed_;
		  for (; actual < nfixargs; ++actual)
			  SetNilValue(StkId.Inc(ref L.top));
		#if LINYEE_COMPAT_VARARG
		  if ((p.is_vararg & VARARG_NEEDSARG) != 0) { /* compat. with old-style vararg? */
			int nvar = actual - nfixargs;  /* number of extra arguments */
			ly_assert(p.is_vararg & VARARG_HASARG);
			luaC_checkGC(L);
			luaD_checkstack(L, p.maxstacksize);
			htab = luaH_new(L, nvar, 1);  /* create `arg' table */
			for (i=0; i<nvar; i++)  /* put extra arguments into `arg' table */
			  setobj2n(L, luaH_setnum(L, htab, i+1), L.top - nvar + i);
			/* store counter in field `n' */
			setnvalue(luaH_setstr(L, htab, luaS_newliteral(L, "n")), cast_num(nvar));
		  }
		#endif
		  /* move fixed parameters to final position */
		  fixed_ = L.top - actual;  /* first fixed argument */
		  base_ = L.top;  /* final position of first argument */
		  for (i=0; i<nfixargs; i++) {
			SetObj2S(L, StkId.Inc(ref L.top), fixed_ + i);
			SetNilValue(fixed_ + i);
		  }
		  /* add `arg' parameter */
		  if (htab!=null) {
			StkId top = L.top;
			StkId.Inc(ref L.top);
			SetHValue(L, top, htab);
			LinyeeAssert(IsWhite(obj2gco(htab)));
		  }
		  return base_;
		}


		static StkId TryFuncTM (LinyeeState L, StkId func) {
		  /*const*/ TValue tm = luaT_gettmbyobj(L, func, TMS.TM_CALL);
		  StkId p;
		  ptrdiff_t funcr = SaveStack(L, func);
		  if (!TTIsFunction(tm))
			LinyeeGTypeError(L, func, "call");
		  /* Open a hole inside the stack at `func' */
		  for (p = L.top; p > func; StkId.Dec(ref p)) SetObj2S(L, p, p - 1);
		  IncrTop(L);
		  func = RestoreStack(L, funcr);  /* previous call may change stack */
		  SetObj2S(L, func, tm);  /* tag method is the new function to be called */
		  return func;
		}



		public static CallInfo IncCI(LinyeeState L)
		{
			if (L.ci == L.end_ci) return GrowCI(L);
			//   (condhardstacktests(luaD_reallocCI(L, L.size_ci)), ++L.ci))
			CallInfo.Inc(ref L.ci);
			return L.ci;
		}


		public static int LinyeeDPreCall (LinyeeState L, StkId func, int nresults) {
		  LClosure cl;
		  ptrdiff_t funcr;
		  if (!TTIsFunction(func)) /* `func' is not a function? */
			func = TryFuncTM(L, func);  /* check the `function' tag method */

		  funcr = SaveStack(L, func);
		  cl = CLValue(func).l;
		  L.ci.savedpc = InstructionPtr.Assign(L.savedpc);

		  if (cl.isC==0) {  /* Linyee function? prepare its call */
			CallInfo ci;
			StkId st, base_;
			Proto p = cl.p;
			LinyeeDCheckStack(L, p.maxstacksize);
			func = RestoreStack(L, funcr);
			if (p.is_vararg == 0) {  /* no varargs? */
			  base_ = L.stack[func + 1];
			  if (L.top > base_ + p.numparams)
				  L.top = base_ + p.numparams;
			}
			else {  /* vararg function */
				int nargs = L.top - func - 1;
				base_ = AdjustVarArgs(L, p, nargs);
				func = RestoreStack(L, funcr);  /* previous call may change the stack */
			}
			ci = IncCI(L);  /* now `enter' new function */
			ci.func = func;
			L.base_ = ci.base_ = base_;
			ci.top = L.base_ + p.maxstacksize;
			LinyeeAssert(ci.top <= L.stack_last);
			L.savedpc = new InstructionPtr(p.code, 0);  /* starting point */
			ci.tailcalls = 0;
			ci.nresults = nresults;
			for (st = L.top; st < ci.top; StkId.Inc(ref st))
				SetNilValue(st);
			L.top = ci.top;
			if ((L.hookmask & LINYEE_MASKCALL) != 0) {
			  InstructionPtr.inc(ref L.savedpc);  /* hooks assume 'pc' is already incremented */
			  LinyeeDCallHook(L, LINYEE_HOOKCALL, -1);
			  InstructionPtr.dec(ref L.savedpc);  /* correct 'pc' */
			}
			return PCRLUA;
		  }
		  else {  /* if is a C function, call it */
			CallInfo ci;
			int n;
			LinyeeDCheckStack(L, LINYEE_MINSTACK);  /* ensure minimum stack size */
			ci = IncCI(L);  /* now `enter' new function */
			ci.func = RestoreStack(L, funcr);
			L.base_ = ci.base_ = ci.func + 1;
			ci.top = L.top + LINYEE_MINSTACK;
			LinyeeAssert(ci.top <= L.stack_last);
			ci.nresults = nresults;
			if ((L.hookmask & LINYEE_MASKCALL) != 0)
			  LinyeeDCallHook(L, LINYEE_HOOKCALL, -1);
			LinyeeUnlock(L);
			n = CurrFunc(L).c.f(L);  /* do the actual call */
			LinyeeLock(L);
			if (n < 0)  /* yielding? */
			  return PCRYIELD;
			else {
			  LinyeeDPosCall(L, L.top - n);
			  return PCRC;
			}
		  }
		}


		private static StkId CallRetHooks (LinyeeState L, StkId firstResult) {
		  ptrdiff_t fr = SaveStack(L, firstResult);  /* next call may change stack */
		  LinyeeDCallHook(L, LINYEE_HOOKRET, -1);
		  if (FIsLinyee(L.ci)) {  /* Linyee function? */
			while ( ((L.hookmask & LINYEE_MASKRET)!=0) && (L.ci.tailcalls-- != 0)) /* tail calls */
			  LinyeeDCallHook(L, LINYEE_HOOKTAILRET, -1);
		  }
		  return RestoreStack(L, fr);
		}


		public static int LinyeeDPosCall (LinyeeState L, StkId firstResult) {
		  StkId res;
		  int wanted, i;
		  CallInfo ci;
		  if ((L.hookmask & LINYEE_MASKRET) != 0)
			firstResult = CallRetHooks(L, firstResult);
		  ci = CallInfo.Dec(ref L.ci);
		  res = ci.func;  /* res == final position of 1st result */
		  wanted = ci.nresults;
		  L.base_ = (ci - 1).base_;  /* restore base */
		  L.savedpc = InstructionPtr.Assign((ci - 1).savedpc);  /* restore savedpc */
		  /* move results to correct place */
		  for (i = wanted; i != 0 && firstResult < L.top; i--)
		  {
			  SetObj2S(L, res, firstResult);
			  res = res + 1;
			  firstResult = firstResult + 1;
		  }
		  while (i-- > 0)
			  SetNilValue(StkId.Inc(ref res));
		  L.top = res;
		  return (wanted - LINYEE_MULTRET);  /* 0 iff wanted == LINYEE_MULTRET */
		}


		/*
		** Call a function (C or Linyee). The function to be called is at *func.
		** The arguments are on the stack, right after the function.
		** When returns, all the results are on the stack, starting at the original
		** function position.
		*/ 
		private static void LinyeeDCall (LinyeeState L, StkId func, int nResults) {
		  if (++L.nCcalls >= LUAI_MAXCCALLS) {
			if (L.nCcalls == LUAI_MAXCCALLS)
			  LinyeeGRunError(L, "C stack overflow");
			else if (L.nCcalls >= (LUAI_MAXCCALLS + (LUAI_MAXCCALLS>>3)))
			  LinyeeDThrow(L, LINYEE_ERRERR);  /* error while handing stack error */
		  }

		  if (LinyeeDPreCall(L, func, nResults) == PCRLUA)  /* is a Linyee function? */
			luaV_execute(L, 1);  /* call it */

		  L.nCcalls--;
		  LinyeeCCheckGC(L);
		}


		private static void Resume (LinyeeState L, object ud) {
		  StkId firstArg = (StkId)ud;
		  CallInfo ci = L.ci;
		  if (L.status == 0) {  /* start coroutine? */
			LinyeeAssert(ci == L.base_ci[0] && firstArg > L.base_);
			if (LinyeeDPreCall(L, firstArg - 1, LINYEE_MULTRET) != PCRLUA)
			  return;
		  }
		  else {  /* resuming from previous yield */
			LinyeeAssert(L.status == LINYEE_YIELD);
			L.status = 0;
			if (!FIsLinyee(ci)) {  /* `common' yield? */
			  /* finish interrupted execution of `OP_CALL' */
			  LinyeeAssert(GET_OPCODE((ci-1).savedpc[-1]) == OpCode.OP_CALL ||
						 GET_OPCODE((ci-1).savedpc[-1]) == OpCode.OP_TAILCALL);
				if (LinyeeDPosCall(L, firstArg) != 0)  /* complete it... */
				L.top = L.ci.top;  /* and correct top if not multiple results */
			}
			else  /* yielded inside a hook: just continue its execution */
			  L.base_ = L.ci.base_;
		  }
		  luaV_execute(L, L.ci - L.base_ci);
		}


		private static int ResumeError (LinyeeState L, CharPtr msg) {
		  L.top = L.ci.base_;
		  SetSValue2S(L, L.top, luaS_new(L, msg));
		  IncrTop(L);
		  LinyeeUnlock(L);
		  return LINYEE_ERRRUN;
		}


		public static int LinyeeResume (LinyeeState L, int nargs) {
		  int status;
		  LinyeeLock(L);
		  if (L.status != LINYEE_YIELD && (L.status != 0 || (L.ci != L.base_ci[0])))
			  return ResumeError(L, "cannot resume non-suspended coroutine");
		  if (L.nCcalls >= LUAI_MAXCCALLS)
			return ResumeError(L, "C stack overflow");
		  luai_userstateresume(L, nargs);
		  LinyeeAssert(L.errfunc == 0);
		  L.baseCcalls = ++L.nCcalls;
		  status = LinyeeDRawRunProtected(L, Resume, L.top - nargs);
		  if (status != 0) {  /* error? */
			L.status = CastByte(status);  /* mark thread as `dead' */
			LinyeeDSetErrorObj(L, status, L.top);
			L.ci.top = L.top;
		  }
		  else {
			LinyeeAssert(L.nCcalls == L.baseCcalls);
			status = L.status;
		  }
		  --L.nCcalls;
		  LinyeeUnlock(L);
		  return status;
		}

		[CLSCompliantAttribute(false)]
		public static int LinyeeYield (LinyeeState L, int nresults) {
		  luai_userstateyield(L, nresults);
		  LinyeeLock(L);
		  if (L.nCcalls > L.baseCcalls)
			LinyeeGRunError(L, "attempt to yield across metamethod/C-call boundary");
		  L.base_ = L.top - nresults;  /* protect stack slots below */
		  L.status = LINYEE_YIELD;
		  LinyeeUnlock(L);
		  return -1;
		}


		public static int LinyeeDPCall (LinyeeState L, Pfunc func, object u,
						ptrdiff_t old_top, ptrdiff_t ef) {
		  int status;
		  ushort oldnCcalls = L.nCcalls;
		  ptrdiff_t old_ci = SaveCI(L, L.ci);
		  LinyeeByteType old_allowhooks = L.allowhook;
		  ptrdiff_t old_errfunc = L.errfunc;
		  L.errfunc = ef;
		  status = LinyeeDRawRunProtected(L, func, u);
		  if (status != 0) {  /* an error occurred? */
			StkId oldtop = RestoreStack(L, old_top);
			LinyeeFClose(L, oldtop);  /* close eventual pending closures */
			LinyeeDSetErrorObj(L, status, oldtop);
			L.nCcalls = oldnCcalls;
			L.ci = RestoreCI(L, old_ci);
			L.base_ = L.ci.base_;
			L.savedpc = InstructionPtr.Assign(L.ci.savedpc);
			L.allowhook = old_allowhooks;
			RestoreStackLimit(L);
		  }
		  L.errfunc = old_errfunc;
		  return status;
		}



		/*
		** Execute a protected parser.
		*/
		public class SParser {  /* data to `f_parser' */
		  public ZIO z;
		  public Mbuffer buff = new Mbuffer();  /* buffer to be used by the scanner */
		  public CharPtr name;
		};

		private static void FParser (LinyeeState L, object ud) {
		  int i;
		  Proto tf;
		  Closure cl;
		  SParser p = (SParser)ud;
		  int c = luaZ_lookahead(p.z);
		  LinyeeCCheckGC(L);
		  tf = (c == LINYEE_SIGNATURE[0]) ?
			luaU_undump(L, p.z, p.buff, p.name) :
			luaY_parser(L, p.z, p.buff, p.name);
		  cl = LinyeeFNewLClosure(L, tf.nups, HValue(Gt(L)));
		  cl.l.p = tf;
		  for (i = 0; i < tf.nups; i++)  /* initialize eventual upvalues */
			cl.l.upvals[i] = LinyeeFNewUpVal(L);
		  SetCLValue(L, L.top, cl);
		  IncrTop(L);
		}


		public static int LinyeeDProtectedParser (LinyeeState L, ZIO z, CharPtr name) {
		  SParser p = new SParser();
		  int status;
		  p.z = z; p.name = new CharPtr(name);
		  luaZ_initbuffer(L, p.buff);
		  status = LinyeeDPCall(L, FParser, p, SaveStack(L, L.top), L.errfunc);
		  luaZ_freebuffer(L, p.buff);
		  return status;
		}
	}
}
