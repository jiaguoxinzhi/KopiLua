/*
** $Id: lfunc.c,v 2.12.1.2 2007/12/28 14:58:43 roberto Exp $
** Auxiliary functions to manipulate prototypes and closures
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Linyee
{
	using TValue = Linyee.LinyeeTypeValue;
	using StkId = Linyee.LinyeeTypeValue;
	using Instruction = System.UInt32;

	public partial class Linyee
	{

		public static int SizeCclosure(int n) {
			return GetUnmanagedSize(typeof(CClosure)) + GetUnmanagedSize(typeof(TValue)) * (n - 1);
		}

		public static int SizeLclosure(int n) {
			return GetUnmanagedSize(typeof(LClosure)) + GetUnmanagedSize(typeof(TValue)) * (n - 1);
		}

		public static Closure LinyeeFNewCclosure (LinyeeState L, int nelems, Table e) {
		  //Closure c = (Closure)luaM_malloc(L, sizeCclosure(nelems));	
		  Closure c = LinyeeMNew<Closure>(L);
		  AddTotalBytes(L, SizeCclosure(nelems));
		  LinyeeCLink(L, obj2gco(c), LINYEE_TFUNCTION);
		  c.c.isC = 1;
		  c.c.env = e;
		  c.c.nupvalues = CastByte(nelems);
		  c.c.upvalue = new TValue[nelems];
		  for (int i = 0; i < nelems; i++)
			  c.c.upvalue[i] = new LinyeeTypeValue();
		  return c;
		}


		public static Closure LinyeeFNewLClosure (LinyeeState L, int nelems, Table e) {
		  //Closure c = (Closure)luaM_malloc(L, sizeLclosure(nelems));
		  Closure c = LinyeeMNew<Closure>(L);
		  AddTotalBytes(L, SizeLclosure(nelems));
		  LinyeeCLink(L, obj2gco(c), LINYEE_TFUNCTION);
		  c.l.isC = 0;
		  c.l.env = e;
		  c.l.nupvalues = CastByte(nelems);
		  c.l.upvals = new UpVal[nelems];
		  for (int i = 0; i < nelems; i++)
			  c.l.upvals[i] = new UpVal();
		  while (nelems-- > 0) c.l.upvals[nelems] = null;
		  return c;
		}


		public static UpVal LinyeeFNewUpVal (LinyeeState L) {
		  UpVal uv = LinyeeMNew<UpVal>(L);
		  LinyeeCLink(L, obj2gco(uv), LUATUPVAL);
		  uv.v = uv.u.value;
		  SetNilValue(uv.v);
		  return uv;
		}

		public static UpVal LinyeeFindUpVal (LinyeeState L, StkId level) {
		  GlobalState g = G(L);
		  GCObjectRef pp = new OpenValRef(L);
		  UpVal p;
		  UpVal uv;
		  while (pp.get() != null && (p = ngcotouv(pp.get())).v >= level) {
			LinyeeAssert(p.v != p.u.value);
			if (p.v == level) {  /* found a corresponding upvalue? */
			  if (IsDead(g, obj2gco(p)))  /* is it dead? */
				ChangeWhite(obj2gco(p));  /* ressurect it */
			  return p;
			}
			pp = new NextRef(p);
		  }
		  uv = LinyeeMNew<UpVal>(L);  /* not found: create a new one */
		  uv.tt = LUATUPVAL;
		  uv.marked = LinyeeCWhite(g);
		  uv.v = level;  /* current value lives in the stack */
		  uv.next = pp.get();  /* chain it in the proper position */
		  pp.set( obj2gco(uv) );
		  uv.u.l.prev = g.uvhead;  /* double link it in `uvhead' list */
		  uv.u.l.next = g.uvhead.u.l.next;
		  uv.u.l.next.u.l.prev = uv;
		  g.uvhead.u.l.next = uv;
		  LinyeeAssert(uv.u.l.next.u.l.prev == uv && uv.u.l.prev.u.l.next == uv);
		  return uv;
		}


		private static void UnlinkUpVal (UpVal uv) {
		  LinyeeAssert(uv.u.l.next.u.l.prev == uv && uv.u.l.prev.u.l.next == uv);
		  uv.u.l.next.u.l.prev = uv.u.l.prev;  /* remove from `uvhead' list */
		  uv.u.l.prev.u.l.next = uv.u.l.next;
		}


		public static void LinyeeFreeUpVal (LinyeeState L, UpVal uv) {
		  if (uv.v != uv.u.value)  /* is it open? */
			UnlinkUpVal(uv);  /* remove from open list */
		  LinyeeMFree(L, uv);  /* free upvalue */
		}


		public static void LinyeeFClose (LinyeeState L, StkId level) {
		  UpVal uv;
		  GlobalState g = G(L);
		  while (L.openupval != null && (uv = ngcotouv(L.openupval)).v >= level) {
			GCObject o = obj2gco(uv);
			LinyeeAssert(!IsBlack(o) && uv.v != uv.u.value);
			L.openupval = uv.next;  /* remove from `open' list */
			if (IsDead(g, o))
			  LinyeeFreeUpVal(L, uv);  /* free upvalue */
			else {
			  UnlinkUpVal(uv);
			  SetObj(L, uv.u.value, uv.v);
			  uv.v = uv.u.value;  /* now current value lives here */
			  LinyeeCLinkUpVal(L, uv);  /* link upvalue into `gcroot' list */
			}
		  }
		}


		public static Proto LinyeeFNewProto (LinyeeState L) {
		  Proto f = LinyeeMNew<Proto>(L);
		  LinyeeCLink(L, obj2gco(f), LUATPROTO);
		  f.k = null;
		  f.sizek = 0;
		  f.p = null;
		  f.sizep = 0;
		  f.code = null;
		  f.sizecode = 0;
		  f.sizelineinfo = 0;
		  f.sizeupvalues = 0;
		  f.nups = 0;
		  f.upvalues = null;
		  f.numparams = 0;
		  f.is_vararg = 0;
		  f.maxstacksize = 0;
		  f.lineinfo = null;
		  f.sizelocvars = 0;
		  f.locvars = null;
		  f.linedefined = 0;
		  f.lastlinedefined = 0;
		  f.source = null;
		  return f;
		}

		public static void LinyeeFFreeProto (LinyeeState L, Proto f) {
		  LinyeeMFreeArray<Instruction>(L, f.code);
		  LinyeeMFreeArray<Proto>(L, f.p);
		  LinyeeMFreeArray<TValue>(L, f.k);
		  LinyeeMFreeArray<Int32>(L, f.lineinfo);
		  LinyeeMFreeArray<LocVar>(L, f.locvars);
		  LinyeeMFreeArray<TString>(L, f.upvalues);
		  LinyeeMFree(L, f);
		}

		// we have a gc, so nothing to do
		public static void LinyeeFFreeClosure (LinyeeState L, Closure c) {
		  int size = (c.c.isC != 0) ? SizeCclosure(c.c.nupvalues) :
								  SizeLclosure(c.l.nupvalues);
		  //luaM_freemem(L, c, size);
		  SubtractTotalBytes(L, size);
		}


		/*
		** Look for n-th local variable at line `line' in function `func'.
		** Returns null if not found.
		*/
		public static CharPtr LinyeeFGetLocalName (Proto f, int local_number, int pc) {
		  int i;
		  for (i = 0; i<f.sizelocvars && f.locvars[i].startpc <= pc; i++) {
			if (pc < f.locvars[i].endpc) {  /* is variable active? */
			  local_number--;
			  if (local_number == 0)
				return GetStr(f.locvars[i].varname);
			}
		  }
		  return null;  /* not found */
		}

	}
}
