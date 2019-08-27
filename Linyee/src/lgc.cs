/*
** $Id: lgc.c,v 2.38.1.1 2007/12/27 13:02:25 roberto Exp $
** Garbage Collector
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Linyee
{
	using LinyeeIntegerType = System.UInt32;
	using l_mem = System.Int32;
	using ly_mem = System.UInt32;
	using TValue = Linyee.LinyeeTypeValue;
	using StkId = Linyee.LinyeeTypeValue;
	using LinyeeByteType = System.Byte;
	using Instruction = System.UInt32;

	public partial class Linyee
	{
		/*
		** Possible states of the Garbage Collector
		*/
		public const int GCSpause		= 0;
		public const int GCSpropagate	= 1;
		public const int GCSsweepstring	= 2;
		public const int GCSsweep		= 3;
		public const int GCSfinalize	= 4;


		/*
		** some userful bit tricks
		*/
		public static int ResetBits(ref LinyeeByteType x, int m) { x &= (LinyeeByteType)~m; return x; }
		public static int SetBits(ref LinyeeByteType x, int m) { x |= (LinyeeByteType)m; return x; }
		public static bool TestBits(LinyeeByteType x, int m) { return (x & (LinyeeByteType)m) != 0; }
        /// <summary>
        /// 单掩码值
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
		public static int BitMask(int b)	{return 1<<b;}
        /// <summary>
        /// 位置转掩码值
        /// </summary>
        /// <param name="b1">位置</param>
        /// <param name="b2">位置</param>
        /// <returns></returns>
		public static int Bit2Mask(int b1, int b2)	{return (BitMask(b1) | BitMask(b2));}
		public static int LSetBit(ref LinyeeByteType x, int b) { return SetBits(ref x, BitMask(b)); }
		public static int ResetBit(ref LinyeeByteType x, int b) { return ResetBits(ref x, BitMask(b)); }
		public static bool TestBit(LinyeeByteType x, int b) { return TestBits(x, BitMask(b)); }
		public static int Set2Bits(ref LinyeeByteType x, int b1, int b2) { return SetBits(ref x, (Bit2Mask(b1, b2))); }
		public static int Reset2Bits(ref LinyeeByteType x, int b1, int b2) { return ResetBits(ref x, (Bit2Mask(b1, b2))); }
		public static bool Test2Bits(LinyeeByteType x, int b1, int b2) { return TestBits(x, (Bit2Mask(b1, b2))); }



		/*
		** Layout for bit use in `marked' field:
		** bit 0 - object is white (type 0)
		** bit 1 - object is white (type 1)
		** bit 2 - object is black
		** bit 3 - for userdata: has been finalized
		** bit 3 - for tables: has weak keys
		** bit 4 - for tables: has weak values
		** bit 5 - object is fixed (should not be collected)
		** bit 6 - object is "super" fixed (only the main thread)
		*/


		public const int WHITE0BIT		= 0;
		public const int WHITE1BIT		= 1;
		public const int BLACKBIT		= 2;
		public const int FINALIZEDBIT	= 3;
		public const int KEYWEAKBIT		= 3;
		public const int VALUEWEAKBIT	= 4;
		public const int FIXEDBIT		= 5;
		public const int SFIXEDBIT		= 6;
		public readonly static int WHITEBITS		= Bit2Mask(WHITE0BIT, WHITE1BIT);


		public static bool IsWhite(GCObject x) { return Test2Bits(x.gch.marked, WHITE0BIT, WHITE1BIT); }
		public static bool IsBlack(GCObject x) { return TestBit(x.gch.marked, BLACKBIT); }
		public static bool IsGray(GCObject x) { return (!IsBlack(x) && !IsWhite(x)); }

		public static int OtherWhite(GlobalState g) { return g.currentwhite ^ WHITEBITS; }
		public static bool IsDead(GlobalState g, GCObject v) { return (v.gch.marked & OtherWhite(g) & WHITEBITS) != 0; }

		public static void ChangeWhite(GCObject x) { x.gch.marked ^= (byte)WHITEBITS; }
		public static void Gray2Black(GCObject x) { LSetBit(ref x.gch.marked, BLACKBIT); }

		public static bool ValIsWhite(TValue x) { return (IsCollectable(x) && IsWhite(GCValue(x))); }

		public static byte LinyeeCWhite(GlobalState g) { return (byte)(g.currentwhite & WHITEBITS); }


		public static void LinyeeCCheckGC(LinyeeState L)
		{
			//condhardstacktests(luaD_reallocstack(L, L.stacksize - EXTRA_STACK - 1));
			//luaD_reallocstack(L, L.stacksize - EXTRA_STACK - 1);
			if (G(L).totalbytes >= G(L).GCthreshold)
				LinyeeCStep(L);
		}


		public static void LinyeeCBarrier(LinyeeState L, object p, TValue v) { if (ValIsWhite(v) && IsBlack(obj2gco(p)))
			LinyeeCBarrierF(L,obj2gco(p),GCValue(v)); }

		public static void LinyeeCBarrierT(LinyeeState L, Table t, TValue v) { if (ValIsWhite(v) && IsBlack(obj2gco(t)))
		    LinyeeCBarrierBack(L,t); }

		public static void LinyeeCObjBarrier(LinyeeState L, object p, object o)
			{ if (IsWhite(obj2gco(o)) && IsBlack(obj2gco(p)))
				LinyeeCBarrierF(L,obj2gco(p),obj2gco(o)); }

		public static void LinyeeCObjBarrierT(LinyeeState L, Table t, object o)
			{ if (IsWhite(obj2gco(o)) && IsBlack(obj2gco(t))) LinyeeCBarrierBack(L,t); }

		[CLSCompliantAttribute(false)]
		public const uint GCSTEPSIZE	= 1024;
		public const int GCSWEEPMAX		= 40;
		public const int GCSWEEPCOST	= 10;
		public const int GCFINALIZECOST	= 100;


		public static byte maskmarks	= (byte)(~(BitMask(BLACKBIT)|WHITEBITS));

		public static void MakeWhite(GlobalState g, GCObject x)
		{
		   x.gch.marked = (byte)(x.gch.marked & maskmarks | LinyeeCWhite(g));
		}

		public static void White2Gray(GCObject x) { Reset2Bits(ref x.gch.marked, WHITE0BIT, WHITE1BIT); }
		public static void Black2Gray(GCObject x) { ResetBit(ref x.gch.marked, BLACKBIT); }

		public static void StringMark(TString s) {Reset2Bits(ref s.tsv.marked, WHITE0BIT, WHITE1BIT);}

		public static bool IsFinalized(UdataUV u) { return TestBit(u.marked, FINALIZEDBIT); }
		public static void MarkFinalized(UdataUV u)
		{
			LinyeeByteType marked = u.marked;	// can't pass properties in as ref
			LSetBit(ref marked, FINALIZEDBIT);
			u.marked = marked;
		}


		public static int KEYWEAK		= BitMask(KEYWEAKBIT);
		public static int VALUEWEAK		= BitMask(VALUEWEAKBIT);

		public static void MarkValue(GlobalState g, TValue o) 
		{
			CheckConsistency(o);
			if (IsCollectable(o) && IsWhite(GCValue(o)))
				ReallyMarkObject(g,GCValue(o));
		}

		public static void MarkObject(GlobalState g, object t)
		{
			if (IsWhite(obj2gco(t)))
				ReallyMarkObject(g, obj2gco(t));
		}

		public static void SetThreshold(GlobalState g)
		{
			g.GCthreshold = (uint)((g.estimate / 100) * g.gcpause);
		}

		private static void RemoveEntry (Node n) {
		  LinyeeAssert(TTIsNil(gval(n)));
		  if (IsCollectable(gkey(n)))
			SetTType(gkey(n), LUATDEADKEY);  /* dead key; remove it */
		}


		private static void ReallyMarkObject (GlobalState g, GCObject o) {
		  LinyeeAssert(IsWhite(o) && !IsDead(g, o));
		  White2Gray(o);
		  switch (o.gch.tt) {
			case LINYEE_TSTRING: {
			  return;
			}
			case LINYEE_TUSERDATA: {
			  Table mt = gco2u(o).metatable;
			  Gray2Black(o);  /* udata are never gray */
			  if (mt != null) MarkObject(g, mt);
			  MarkObject(g, gco2u(o).env);
			  return;
			}
			case LUATUPVAL: {
			  UpVal uv = gco2uv(o);
			  MarkValue(g, uv.v);
			  if (uv.v == uv.u.value)  /* closed? */
				Gray2Black(o);  /* open upvalues are never black */
			  return;
			}
			case LINYEE_TFUNCTION: {
			  gco2cl(o).c.gclist = g.gray;
			  g.gray = o;
			  break;
			}
			case LINYEE_TTABLE: {
			  gco2h(o).gclist = g.gray;
			  g.gray = o;
			  break;
			}
			case LINYEE_TTHREAD: {
			  gco2th(o).gclist = g.gray;
			  g.gray = o;
			  break;
			}
			case LUATPROTO: {
			  gco2p(o).gclist = g.gray;
			  g.gray = o;
			  break;
			}
			default: LinyeeAssert(0); break;
		  }
		}


		private static void MarkTMU (GlobalState g) {
		  GCObject u = g.tmudata;
		  if (u != null) {
			do {
			  u = u.gch.next;
			  MakeWhite(g, u);  /* may be marked, if left from previous GC */
			  ReallyMarkObject(g, u);
			} while (u != g.tmudata);
		  }
		}


		/* move `dead' udata that need finalization to list `tmudata' */
		[CLSCompliantAttribute(false)]
		public static uint LinyeeCSeparateUData (LinyeeState L, int all) {
		  GlobalState g = G(L);
		  uint deadmem = 0;
		  GCObjectRef p = new NextRef(g.mainthread);
		  GCObject curr;
		  while ((curr = p.get()) != null) {
			if (!(IsWhite(curr) || (all!=0)) || IsFinalized(gco2u(curr)))
			  p = new NextRef(curr.gch);  /* don't bother with them */
			else if (fasttm(L, gco2u(curr).metatable, TMS.TM_GC) == null) {
			  MarkFinalized(gco2u(curr));  /* don't need finalization */
			  p = new NextRef(curr.gch);
			}
			else {  /* must call its gc method */
			  deadmem += (uint)sizeudata(gco2u(curr));
			  MarkFinalized(gco2u(curr));
			  p.set( curr.gch.next );
			  /* link `curr' at the end of `tmudata' list */
			  if (g.tmudata == null)  /* list is empty? */
				g.tmudata = curr.gch.next = curr;  /* creates a circular list */
			  else {
				curr.gch.next = g.tmudata.gch.next;
				g.tmudata.gch.next = curr;
				g.tmudata = curr;
			  }
			}
		  }
		  return deadmem;
		}


		private static int TraverseTable (GlobalState g, Table h) {
		  int i;
		  int weakkey = 0;
		  int weakvalue = 0;
		  /*const*/ TValue mode;
		  if (h.metatable != null)
			MarkObject(g, h.metatable);
		  mode = gfasttm(g, h.metatable, TMS.TM_MODE);
		  if ((mode != null) && TTIsString(mode)) {  /* is there a weak mode? */
			  weakkey = (strchr(SValue(mode), 'k') != null) ? 1 : 0 ;
			  weakvalue = (strchr(SValue(mode), 'v') != null) ? 1 : 0;
			if ((weakkey!=0) || (weakvalue!=0)) {  /* is really weak? */
			  h.marked &= (byte)~(KEYWEAK | VALUEWEAK);  /* clear bits */
			  h.marked |= CastByte((weakkey << KEYWEAKBIT) |
									 (weakvalue << VALUEWEAKBIT));
			  h.gclist = g.weak;  /* must be cleared after GC, ... */
			  g.weak = obj2gco(h);  /* ... so put in the appropriate list */
			}
		  }
		  if ((weakkey!=0) && (weakvalue!=0)) return 1;
		  if (weakvalue==0) {
			i = h.sizearray;
			while ((i--) != 0)
			  MarkValue(g, h.array[i]);
		  }
		  i = SizeNode(h);
		  while ((i--) != 0) {
			Node n = gnode(h, i);
			LinyeeAssert(TType(gkey(n)) != LUATDEADKEY || TTIsNil(gval(n)));
			if (TTIsNil(gval(n)))
			  RemoveEntry(n);  /* remove empty entries */
			else {
			  LinyeeAssert(!TTIsNil(gkey(n)));
			  if (weakkey==0) MarkValue(g, gkey(n));
			  if (weakvalue==0) MarkValue(g, gval(n));
			}
		  }
		  return ((weakkey != 0) || (weakvalue != 0)) ? 1 : 0;
		}


		/*
		** All marks are conditional because a GC may happen while the
		** prototype is still being created
		*/
		private static void TraverseProto (GlobalState g, Proto f) {
		  int i;
		  if (f.source != null) StringMark(f.source);
		  for (i=0; i<f.sizek; i++)  /* mark literals */
			MarkValue(g, f.k[i]);
		  for (i=0; i<f.sizeupvalues; i++) {  /* mark upvalue names */
			if (f.upvalues[i] != null)
			  StringMark(f.upvalues[i]);
		  }
		  for (i=0; i<f.sizep; i++) {  /* mark nested protos */
			if (f.p[i] != null)
			  MarkObject(g, f.p[i]);
		  }
		  for (i=0; i<f.sizelocvars; i++) {  /* mark local-variable names */
			if (f.locvars[i].varname != null)
			  StringMark(f.locvars[i].varname);
		  }
		}



		private static void TraverseClosure (GlobalState g, Closure cl) {
		  MarkObject(g, cl.c.env);
		  if (cl.c.isC != 0) {
			int i;
			for (i=0; i<cl.c.nupvalues; i++)  /* mark its upvalues */
			  MarkValue(g, cl.c.upvalue[i]);
		  }
		  else {
			int i;
			LinyeeAssert(cl.l.nupvalues == cl.l.p.nups);
			MarkObject(g, cl.l.p);
			for (i=0; i<cl.l.nupvalues; i++)  /* mark its upvalues */
			  MarkObject(g, cl.l.upvals[i]);
		  }
		}


		private static void CheckStackSizes (LinyeeState L, StkId max) {
		  int ci_used = CastInt(L.ci - L.base_ci[0]);  /* number of `ci' in use */
		  int s_used = CastInt(max - L.stack);  /* part of stack in use */
		  if (L.size_ci > LUAI_MAXCALLS)  /* handling overflow? */
			return;  /* do not touch the stacks */
		  if (4*ci_used < L.size_ci && 2*BASICCISIZE < L.size_ci)
			LinyeeDReallocCI(L, L.size_ci/2);  /* still big enough... */
		  //condhardstacktests(luaD_reallocCI(L, ci_used + 1));
		  if (4*s_used < L.stacksize &&
			  2*(BASICSTACKSIZE+EXTRASTACK) < L.stacksize)
			LinyeeDRealAllocStack(L, L.stacksize/2);  /* still big enough... */
		  //condhardstacktests(luaD_reallocstack(L, s_used));
		}


		private static void TraverseStack (GlobalState g, LinyeeState l) {
		  StkId o, lim;
		  CallInfo ci;
		  MarkValue(g, Gt(l));
		  lim = l.top;
		  for (ci = l.base_ci[0]; ci < l.ci; CallInfo.Inc(ref ci)) {
			LinyeeAssert(ci.top <= l.stack_last);
			if (lim < ci.top) lim = ci.top;
		  }
		  for (o = l.stack[0]; o < l.top; StkId.Inc(ref o))
			MarkValue(g, o);
		  for (; o <= lim; StkId.Inc(ref o))
			SetNilValue(o);
		  CheckStackSizes(l, lim);
		}


		/*
		** traverse one gray object, turning it to black.
		** Returns `quantity' traversed.
		*/
		private static l_mem PropagateMark (GlobalState g) {
		  GCObject o = g.gray;
		  LinyeeAssert(IsGray(o));
		  Gray2Black(o);
		  switch (o.gch.tt) {
			case LINYEE_TTABLE: {
			  Table h = gco2h(o);
			  g.gray = h.gclist;
			  if (TraverseTable(g, h) != 0)  /* table is weak? */
				Black2Gray(o);  /* keep it gray */
			    return	GetUnmanagedSize(typeof(Table)) +
						GetUnmanagedSize(typeof(TValue)) * h.sizearray +
						GetUnmanagedSize(typeof(Node)) * SizeNode(h);
			}
			case LINYEE_TFUNCTION: {
			  Closure cl = gco2cl(o);
			  g.gray = cl.c.gclist;
			  TraverseClosure(g, cl);
			  return (cl.c.isC != 0) ? SizeCclosure(cl.c.nupvalues) :
								   SizeLclosure(cl.l.nupvalues);
			}
			case LINYEE_TTHREAD: {
			  LinyeeState th = gco2th(o);
			  g.gray = th.gclist;
			  th.gclist = g.grayagain;
			  g.grayagain = o;
			  Black2Gray(o);
			  TraverseStack(g, th);
			  return	GetUnmanagedSize(typeof(LinyeeState)) +
						GetUnmanagedSize(typeof(TValue)) * th.stacksize +
						GetUnmanagedSize(typeof(CallInfo)) * th.size_ci;
			}
			case LUATPROTO: {
			  Proto p = gco2p(o);
			  g.gray = p.gclist;
			  TraverseProto(g, p);
			  return	GetUnmanagedSize(typeof(Proto)) +
						GetUnmanagedSize(typeof(Instruction)) * p.sizecode +
						GetUnmanagedSize(typeof(Proto)) * p.sizep +
						GetUnmanagedSize(typeof(TValue)) * p.sizek + 
						GetUnmanagedSize(typeof(int)) * p.sizelineinfo +
						GetUnmanagedSize(typeof(LocVar)) * p.sizelocvars +
						GetUnmanagedSize(typeof(TString)) * p.sizeupvalues;
			}
			default: LinyeeAssert(0); return 0;
		  }
		}


		private static uint PropagateAll (GlobalState g) {
		  uint m = 0;
		  while (g.gray != null) m += (uint)PropagateMark(g);
		  return m;
		}


		/*
		** The next function tells whether a key or value can be cleared from
		** a weak table. Non-collectable objects are never removed from weak
		** tables. Strings behave as `values', so are never removed too. for
		** other objects: if really collected, cannot keep them; for userdata
		** being finalized, keep them in keys, but not in values
		*/
		private static bool IsCleared (TValue o, bool iskey) {
		  if (!IsCollectable(o)) return false;
		  if (TTIsString(o)) {
			StringMark(RawTSValue(o));  /* strings are `values', so are never weak */
			return false;
		  }
		  return IsWhite(GCValue(o)) ||
			(TTIsUserData(o) && (!iskey && IsFinalized(UValue(o))));
		}


		/*
		** clear collected entries from weaktables
		*/
		private static void ClearTable (GCObject l) {
		  while (l != null) {
			Table h = gco2h(l);
			int i = h.sizearray;
			LinyeeAssert(TestBit(h.marked, VALUEWEAKBIT) ||
					   TestBit(h.marked, KEYWEAKBIT));
			if (TestBit(h.marked, VALUEWEAKBIT)) {
			  while (i--!= 0) {
				TValue o = h.array[i];
				if (IsCleared(o, false))  /* value was collected? */
				  SetNilValue(o);  /* remove value */
			  }
			}
			i = SizeNode(h);
			while (i-- != 0) {
			  Node n = gnode(h, i);
			  if (!TTIsNil(gval(n)) &&  /* non-empty entry? */
				  (IsCleared(key2tval(n), true) || IsCleared(gval(n), false))) {
				SetNilValue(gval(n));  /* remove value ... */
				RemoveEntry(n);  /* remove entry from Table */
			  }
			}
			l = h.gclist;
		  }
		}


		private static void FreeObj (LinyeeState L, GCObject o) {
		  switch (o.gch.tt) {
			case LUATPROTO: LinyeeFFreeProto(L, gco2p(o)); break;
			case LINYEE_TFUNCTION: LinyeeFFreeClosure(L, gco2cl(o)); break;
			case LUATUPVAL: LinyeeFreeUpVal(L, gco2uv(o)); break;
			case LINYEE_TTABLE: luaH_free(L, gco2h(o)); break;
			case LINYEE_TTHREAD: {
			  LinyeeAssert(gco2th(o) != L && gco2th(o) != G(L).mainthread);
			  luaE_freethread(L, gco2th(o));
			  break;
			}
			case LINYEE_TSTRING: {
			  G(L).strt.nuse--;
			  SubtractTotalBytes(L, sizestring(gco2ts(o)));
			  LinyeeMFreeMem(L, gco2ts(o));
			  break;
			}
			case LINYEE_TUSERDATA: {
			  SubtractTotalBytes(L, sizeudata(gco2u(o)));
			  LinyeeMFreeMem(L, gco2u(o));
			  break;
			}
			default: LinyeeAssert(0); break;
		  }
		}



		public static void SweepWholeList(LinyeeState L, GCObjectRef p) { SweepList(L, p, MAXLUMEM); }


		private static GCObjectRef SweepList (LinyeeState L, GCObjectRef p, ly_mem count) {
		  GCObject curr;
		  GlobalState g = G(L);
		  int deadmask = OtherWhite(g);
		  while ((curr = p.get()) != null && count-- > 0) {
			if (curr.gch.tt == LINYEE_TTHREAD)  /* sweep open upvalues of each thread */
			  SweepWholeList(L, new OpenValRef( gco2th(curr) ));
			if (((curr.gch.marked ^ WHITEBITS) & deadmask) != 0) {  /* not dead? */
			  LinyeeAssert(!IsDead(g, curr) || TestBit(curr.gch.marked, FIXEDBIT));
			  MakeWhite(g, curr);  /* make it white (for next cycle) */
			  p = new NextRef(curr.gch);
			}
			else {  /* must erase `curr' */
			  LinyeeAssert(IsDead(g, curr) || deadmask == BitMask(SFIXEDBIT));
			  p.set( curr.gch.next );
			  if (curr == g.rootgc)  /* is the first element of the list? */
				g.rootgc = curr.gch.next;  /* adjust first */
			  FreeObj(L, curr);
			}
		  }
		  return p;
		}


		private static void CheckSizes (LinyeeState L) {
		  GlobalState g = G(L);
		  /* check size of string hash */
		  if (g.strt.nuse < (LinyeeIntegerType)(g.strt.size/4) &&
			  g.strt.size > MINSTRTABSIZE*2)
			luaS_resize(L, g.strt.size/2);  /* table is too big */
		  /* check size of buffer */
		  if (luaZ_sizebuffer(g.buff) > LUAMINBUFFER*2) {  /* buffer too big? */
			uint newsize = luaZ_sizebuffer(g.buff) / 2;
			luaZ_resizebuffer(L, g.buff, (int)newsize);
		  }
		}


		private static void GCTM (LinyeeState L) {
		  GlobalState g = G(L);
		  GCObject o = g.tmudata.gch.next;  /* get first element */
		  Udata udata = rawgco2u(o);
		  TValue tm;
		  /* remove udata from `tmudata' */
		  if (o == g.tmudata)  /* last element? */
			g.tmudata = null;
		  else
			g.tmudata.gch.next = udata.uv.next;
		  udata.uv.next = g.mainthread.next;  /* return it to `root' list */
		  g.mainthread.next = o;
		  MakeWhite(g, o);
		  tm = fasttm(L, udata.uv.metatable, TMS.TM_GC);
		  if (tm != null) {
			LinyeeByteType oldah = L.allowhook;
			ly_mem oldt = (ly_mem)g.GCthreshold;
			L.allowhook = 0;  /* stop debug hooks during GC tag method */
			g.GCthreshold = 2*g.totalbytes;  /* avoid GC steps */
			SetObj2S(L, L.top, tm);
			SetUValue(L, L.top+1, udata);
			L.top += 2;
			LinyeeDCall(L, L.top - 2, 0);
			L.allowhook = oldah;  /* restore hooks */
			g.GCthreshold = (uint)oldt;  /* restore threshold */
		  }
		}


		/*
		** Call all GC tag methods
		*/
		public static void LinyeeCCallGCTM (LinyeeState L) {
		  while (G(L).tmudata != null)
			GCTM(L);
		}


		public static void LinyeeCFreeAll (LinyeeState L) {
		  GlobalState g = G(L);
		  int i;
		  g.currentwhite = (byte)(WHITEBITS | BitMask(SFIXEDBIT));  /* mask to collect all elements */
		  SweepWholeList(L, new RootGCRef(g));
		  for (i = 0; i < g.strt.size; i++)  /* free all string lists */
			SweepWholeList(L, new ArrayRef(g.strt.hash, i));
		}


		private static void MarkMT (GlobalState g) {
		  int i;
		  for (i=0; i<NUMTAGS; i++)
			if (g.mt[i] != null) MarkObject(g, g.mt[i]);
		}


		/* mark root set */
		private static void MarkRoot (LinyeeState L) {
		  GlobalState g = G(L);
		  g.gray = null;
		  g.grayagain = null;
		  g.weak = null;
		  MarkObject(g, g.mainthread);
		  /* make global table be traversed before main stack */
		  MarkValue(g, Gt(g.mainthread));
		  MarkValue(g, Registry(L));
		  MarkMT(g);
		  g.gcstate = GCSpropagate;
		}


		private static void RemarkUpVals (GlobalState g) {
		  UpVal uv;
		  for (uv = g.uvhead.u.l.next; uv != g.uvhead; uv = uv.u.l.next) {
			LinyeeAssert(uv.u.l.next.u.l.prev == uv && uv.u.l.prev.u.l.next == uv);
			if (IsGray(obj2gco(uv)))
			  MarkValue(g, uv.v);
		  }
		}


		private static void Atomic (LinyeeState L) {
		  GlobalState g = G(L);
		  uint udsize;  /* total size of userdata to be finalized */
		  /* remark occasional upvalues of (maybe) dead threads */
		  RemarkUpVals(g);
		  /* traverse objects cautch by write barrier and by 'remarkupvals' */
		  PropagateAll(g);
		  /* remark weak tables */
		  g.gray = g.weak;
		  g.weak = null;
		  LinyeeAssert(!IsWhite(obj2gco(g.mainthread)));
		  MarkObject(g, L);  /* mark running thread */
		  MarkMT(g);  /* mark basic metatables (again) */
		  PropagateAll(g);
		  /* remark gray again */
		  g.gray = g.grayagain;
		  g.grayagain = null;
		  PropagateAll(g);
		  udsize = LinyeeCSeparateUData(L, 0);  /* separate userdata to be finalized */
		  MarkTMU(g);  /* mark `preserved' userdata */
		  udsize += PropagateAll(g);  /* remark, to propagate `preserveness' */
		  ClearTable(g.weak);  /* remove collected objects from weak tables */
		  /* flip current white */
		  g.currentwhite = CastByte(OtherWhite(g));
		  g.sweepstrgc = 0;
		  g.sweepgc = new RootGCRef(g);
		  g.gcstate = GCSsweepstring;
		  g.estimate = g.totalbytes - udsize;  /* first estimate */
		}


		private static l_mem SingleStep (LinyeeState L) {
		  GlobalState g = G(L);
		  /*ly_checkmemory(L);*/
		  switch (g.gcstate) {
			case GCSpause: {
			  MarkRoot(L);  /* start a new collection */
			  return 0;
			}
			case GCSpropagate: {
			  if (g.gray != null)
				return PropagateMark(g);
			  else {  /* no more `gray' objects */
				Atomic(L);  /* finish mark phase */
				return 0;
			  }
			}
			case GCSsweepstring: {
			  ly_mem old = (ly_mem)g.totalbytes;
			  SweepWholeList(L, new ArrayRef(g.strt.hash, g.sweepstrgc++));
			  if (g.sweepstrgc >= g.strt.size)  /* nothing more to sweep? */
				g.gcstate = GCSsweep;  /* end sweep-string phase */
			  LinyeeAssert(old >= g.totalbytes);
			  g.estimate -= (uint)(old - g.totalbytes);
			  return GCSWEEPCOST;
			}
			case GCSsweep: {
			  ly_mem old = (ly_mem)g.totalbytes;
			  g.sweepgc = SweepList(L, g.sweepgc, GCSWEEPMAX);
			  if (g.sweepgc.get() == null) {  /* nothing more to sweep? */
				CheckSizes(L);
				g.gcstate = GCSfinalize;  /* end sweep phase */
			  }
			  LinyeeAssert(old >= g.totalbytes);
			  g.estimate -= (uint)(old - g.totalbytes);
			  return GCSWEEPMAX*GCSWEEPCOST;
			}
			case GCSfinalize: {
			  if (g.tmudata != null) {
				GCTM(L);
				if (g.estimate > GCFINALIZECOST)
				  g.estimate -= GCFINALIZECOST;
				return GCFINALIZECOST;
			  }
			  else {
				g.gcstate = GCSpause;  /* end collection */
				g.gcdept = 0;
				return 0;
			  }
			}
			default: LinyeeAssert(0); return 0;
		  }
		}

		public static void LinyeeCStep (LinyeeState L) {
		  GlobalState g = G(L);
		  l_mem lim = (l_mem)((GCSTEPSIZE / 100) * g.gcstepmul);
		  if (lim == 0)
			lim = (l_mem)((MAXLUMEM-1)/2);  /* no limit */
		  g.gcdept += g.totalbytes - g.GCthreshold;
		  do {
			lim -= SingleStep(L);
			if (g.gcstate == GCSpause)
			  break;
		  } while (lim > 0);
		  if (g.gcstate != GCSpause) {
			if (g.gcdept < GCSTEPSIZE)
			  g.GCthreshold = g.totalbytes + GCSTEPSIZE;  /* - lim/g.gcstepmul;*/
			else {
			  g.gcdept -= GCSTEPSIZE;
			  g.GCthreshold = g.totalbytes;
			}
		  }
		  else {
			SetThreshold(g);
		  }
		}


		public static void LinyeeCFullGC (LinyeeState L) {
		  GlobalState g = G(L);
		  if (g.gcstate <= GCSpropagate) {
			/* reset sweep marks to sweep all elements (returning them to white) */
			g.sweepstrgc = 0;
			g.sweepgc = new RootGCRef(g);
			/* reset other collector lists */
			g.gray = null;
			g.grayagain = null;
			g.weak = null;
			g.gcstate = GCSsweepstring;
		  }
		  LinyeeAssert(g.gcstate != GCSpause && g.gcstate != GCSpropagate);
		  /* finish any pending sweep phase */
		  while (g.gcstate != GCSfinalize) {
			LinyeeAssert(g.gcstate == GCSsweepstring || g.gcstate == GCSsweep);
			SingleStep(L);
		  }
		  MarkRoot(L);
		  while (g.gcstate != GCSpause) {
			SingleStep(L);
		  }
		  SetThreshold(g);
		}


		public static void LinyeeCBarrierF (LinyeeState L, GCObject o, GCObject v) {
		  GlobalState g = G(L);
		  LinyeeAssert(IsBlack(o) && IsWhite(v) && !IsDead(g, v) && !IsDead(g, o));
		  LinyeeAssert(g.gcstate != GCSfinalize && g.gcstate != GCSpause);
		  LinyeeAssert(TType(o.gch) != LINYEE_TTABLE);
		  /* must keep invariant? */
		  if (g.gcstate == GCSpropagate)
			ReallyMarkObject(g, v);  /* restore invariant */
		  else  /* don't mind */
			MakeWhite(g, o);  /* mark as white just to avoid other barriers */
		}


		public static void LinyeeCBarrierBack(LinyeeState L, Table t)
		{
		  GlobalState g = G(L);
		  GCObject o = obj2gco(t);
		  LinyeeAssert(IsBlack(o) && !IsDead(g, o));
		  LinyeeAssert(g.gcstate != GCSfinalize && g.gcstate != GCSpause);
		  Black2Gray(o);  /* make table gray (again) */
		  t.gclist = g.grayagain;
		  g.grayagain = o;
		}


		public static void LinyeeCLink (LinyeeState L, GCObject o, LinyeeByteType tt) {
		  GlobalState g = G(L);
		  o.gch.next = g.rootgc;
		  g.rootgc = o;
		  o.gch.marked = LinyeeCWhite(g);
		  o.gch.tt = tt;
		}


		public static void LinyeeCLinkUpVal (LinyeeState L, UpVal uv) {
		  GlobalState g = G(L);
		  GCObject o = obj2gco(uv);
		  o.gch.next = g.rootgc;  /* link upvalue into `rootgc' list */
		  g.rootgc = o;
		  if (IsGray(o)) { 
			if (g.gcstate == GCSpropagate) {
			  Gray2Black(o);  /* closed upvalues need barrier */
			  LinyeeCBarrier(L, uv, uv.v);
			}
			else {  /* sweep phase: sweep it (turning it into white) */
			  MakeWhite(g, o);
			  LinyeeAssert(g.gcstate != GCSfinalize && g.gcstate != GCSpause);
			}
		  }
		}

	}
}
