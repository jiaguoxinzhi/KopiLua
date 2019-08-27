/*
** $Id: lstate.c,v 2.36.1.2 2008/01/03 15:20:39 roberto Exp $
** Global State
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Linyee
{
	using ly_byte = System.Byte;
	using ly_int32 = System.Int32;
	using ly_mem = System.UInt32;
	using TValue = Linyee.LinyeeTypeValue;
	using StkId = Linyee.LinyeeTypeValue;
	using ptrdiff_t = System.Int32;
	using Instruction = System.UInt32;

	public partial class Linyee
	{
		/* table of globals */
		public static TValue Gt(LinyeeState L)	{return L.l_gt;}

		/* registry */
		public static TValue Registry(LinyeeState L)	{return G(L).l_registry;}


		/* extra stack space to handle TM calls and some other extras */
		public const int EXTRASTACK   = 5;


		public const int BASICCISIZE           = 8;

		public const int BASICSTACKSIZE        = (2*LINYEE_MINSTACK);



		public class stringtable {
			public GCObject[] hash;
			public ly_int32 nuse;  /* number of elements */
			public int size;
		};


		/*
		** informations about a call
		*/
		public class CallInfo : ArrayElement
		{
			private CallInfo[] values = null;
			private int index = -1;

			public void SetIndex(int index)
			{
				this.index = index;
			}

			public void SetArray(object array)
			{
				this.values = (CallInfo[])array;
				Debug.Assert(this.values != null);
			}

			public CallInfo this[int offset]
			{
				get { return values[index+offset]; }
			}

			public static CallInfo operator +(CallInfo value, int offset)
			{
				return value.values[value.index + offset];
			}

			public static CallInfo operator -(CallInfo value, int offset)
			{
				return value.values[value.index - offset];
			}

			public static int operator -(CallInfo ci, CallInfo[] values)
			{
				Debug.Assert(ci.values == values);
				return ci.index;
			}

			public static int operator -(CallInfo ci1, CallInfo ci2)
			{
				Debug.Assert(ci1.values == ci2.values);
				return ci1.index - ci2.index;
			}

			public static bool operator <(CallInfo ci1, CallInfo ci2)
			{
				Debug.Assert(ci1.values == ci2.values);
				return ci1.index < ci2.index;
			}

			public static bool operator <=(CallInfo ci1, CallInfo ci2)
			{
				Debug.Assert(ci1.values == ci2.values);
				return ci1.index <= ci2.index;
			}

			public static bool operator >(CallInfo ci1, CallInfo ci2)
			{
				Debug.Assert(ci1.values == ci2.values);
				return ci1.index > ci2.index;
			}

			public static bool operator >=(CallInfo ci1, CallInfo ci2)
			{
				Debug.Assert(ci1.values == ci2.values);
				return ci1.index >= ci2.index;
			}

			public static CallInfo Inc(ref CallInfo value)
			{
				value = value[1];
				return value[-1];
			}

			public static CallInfo Dec(ref CallInfo value)
			{
				value = value[-1];
				return value[1];
			}

			public StkId base_;  /* base for this function */
			public StkId func;  /* function index in the stack */
			public StkId top;  /* top for this function */
			public InstructionPtr savedpc;
			public int nresults;  /* expected number of results from this function */
			public int tailcalls;  /* number of tail calls lost under this entry */
		};



		public static Closure CurrFunc(LinyeeState L) { return (CLValue(L.ci.func)); }
		public static Closure CIFunc(CallInfo ci) { return (CLValue(ci.func)); }
		public static bool FIsLinyee(CallInfo ci)	{return CIFunc(ci).c.isC==0;}
		public static bool IsLinyee(CallInfo ci)	{return (TTIsFunction((ci).func) && FIsLinyee(ci));}


		/*
		** `global state', shared by all threads of this state
		*/
		public class GlobalState {
		  public stringtable strt = new stringtable(); /* hash table for strings */
		  public ly_Alloc frealloc;  /* function to reallocate memory */
		  public object ud;         /* auxiliary data to `frealloc' */
		  public ly_byte currentwhite;
		  public ly_byte gcstate;  /* state of garbage collector */
		  public int sweepstrgc;  /* position of sweep in `strt' */
		  public GCObject rootgc;  /* list of all collectable objects */
		  public GCObjectRef sweepgc;  /* position of sweep in `rootgc' */
		  public GCObject gray;  /* list of gray objects */
		  public GCObject grayagain;  /* list of objects to be traversed atomically */
		  public GCObject weak;  /* list of weak tables (to be cleared) */
		  public GCObject tmudata;  /* last element of list of userdata to be GC */
		  public Mbuffer buff = new Mbuffer();  /* temporary buffer for string concatentation */
          [CLSCompliantAttribute(false)]
		  public ly_mem GCthreshold;
          [CLSCompliantAttribute(false)]
		  public ly_mem totalbytes;  /* number of bytes currently allocated */
          [CLSCompliantAttribute(false)]
		  public ly_mem estimate;  /* an estimate of number of bytes actually in use */
          [CLSCompliantAttribute(false)]
		  public ly_mem gcdept;  /* how much GC is `behind schedule' */
		  public int gcpause;  /* size of pause between successive GCs */
		  public int gcstepmul;  /* GC `granularity' */
		  public LinyeeNativeFunction panic;  /* to be called in unprotected errors */
		  public TValue l_registry = new LinyeeTypeValue();
		  public LinyeeState mainthread;
		  public UpVal uvhead = new UpVal();  /* head of double-linked list of all open upvalues */
		  public Table[] mt = new Table[NUMTAGS];  /* metatables for basic types */
		  public TString[] tmname = new TString[(int)TMS.TM_N];  /* array with tag-method names */
		};





		public static GlobalState G(LinyeeState L)	{return L.l_G;}
		public static void G_set(LinyeeState L, GlobalState s) { L.l_G = s; }


		/*
		** Union of all collectable objects (not a union anymore in the C# port)
		*/
		public class GCObject : GCheader, ArrayElement
		{
			public void SetIndex(int index)
			{
				//this.index = index;
			}

			public void SetArray(object array)
			{
				//this.values = (GCObject[])array;
				//Debug.Assert(this.values != null);
			}

			public GCheader gch {get{return (GCheader)this;}}
			public TString ts {get{return (TString)this;}}
			public Udata u {get{return (Udata)this;}}
			public Closure cl {get{return (Closure)this;}}
			public Table h {get{return (Table)this;}}
			public Proto p {get{return (Proto)this;}}
			public UpVal uv {get{return (UpVal)this;}}
			public LinyeeState th {get{return (LinyeeState)this;}}
		};

		/*	this interface and is used for implementing GCObject references,
		    it's used to emulate the behaviour of a C-style GCObject **
		 */
		public interface GCObjectRef
		{
			void set(GCObject value);
			GCObject get();
		}

		public class ArrayRef : GCObjectRef, ArrayElement
		{
			public ArrayRef()
			{
				this.array_elements = null;
				this.array_index = 0;
			}
			public ArrayRef(GCObject[] array_elements, int array_index)
			{
				this.array_elements = array_elements;
				this.array_index = array_index;
			}
			public void set(GCObject value) { array_elements[array_index] = value; }
			public GCObject get() { return array_elements[array_index]; }

			public void SetIndex(int index)
			{

			}
			public void SetArray(object vals)
			{

			}

			// ArrayRef is used to reference GCObject objects in an array, the next two members
			// point to that array and the index of the GCObject element we are referencing
			GCObject[] array_elements;
			int array_index;
		}

		public class OpenValRef : GCObjectRef
		{
			public OpenValRef(LinyeeState L) { this.L = L; }
			public void set(GCObject value) { this.L.openupval = value; }
			public GCObject get() { return this.L.openupval; }
			LinyeeState L;
		}

		public class RootGCRef : GCObjectRef
		{
			public RootGCRef(GlobalState g) { this.g = g; }
			public void set(GCObject value) { this.g.rootgc = value; }
			public GCObject get() { return this.g.rootgc; }
			GlobalState g;
		}

		public class NextRef : GCObjectRef
		{
			public NextRef(GCheader header) { this.header = header; }
			public void set(GCObject value) { this.header.next = value; }
			public GCObject get() { return this.header.next; }
			GCheader header;
		}

		
		/* macros to convert a GCObject into a specific value */
		public static TString rawgco2ts(GCObject o) { return (TString)CheckExp(o.gch.tt == LINYEE_TSTRING, o.ts); }
		public static TString gco2ts(GCObject o) { return (TString)(rawgco2ts(o).tsv); }
		public static Udata rawgco2u(GCObject o) { return (Udata)CheckExp(o.gch.tt == LINYEE_TUSERDATA, o.u); }
		public static Udata gco2u(GCObject o) { return (Udata)(rawgco2u(o).uv); }
		public static Closure gco2cl(GCObject o) { return (Closure)CheckExp(o.gch.tt == LINYEE_TFUNCTION, o.cl); }
		public static Table gco2h(GCObject o) { return (Table)CheckExp(o.gch.tt == LINYEE_TTABLE, o.h); }
		public static Proto gco2p(GCObject o) { return (Proto)CheckExp(o.gch.tt == LUATPROTO, o.p); }
		public static UpVal gco2uv(GCObject o) { return (UpVal)CheckExp(o.gch.tt == LUATUPVAL, o.uv); }
		public static UpVal ngcotouv(GCObject o) {return (UpVal)CheckExp((o == null) || (o.gch.tt == LUATUPVAL), o.uv); }
		public static LinyeeState gco2th(GCObject o) { return (LinyeeState)CheckExp(o.gch.tt == LINYEE_TTHREAD, o.th); }

		/* macro to convert any Linyee object into a GCObject */
		public static GCObject obj2gco(object v)	{return (GCObject)v;}


		public static int state_size(object x) { return Marshal.SizeOf(x) + LUAI_EXTRASPACE; }
		/*
		public static ly_byte fromstate(object l)
		{
			return (ly_byte)(l - LUAI_EXTRASPACE);
		}
		*/
		public static LinyeeState tostate(object l)
		{
			Debug.Assert(LUAI_EXTRASPACE == 0, "LUAI_EXTRASPACE not supported");
			return (LinyeeState)l;
		}


        /*
		** Main thread combines a thread state and the global state
		*/

        /// <summary>
        /// 主线程 结合 单线程状态和全局状态
        /// </summary>
        public class LG : LinyeeState {
		  public LinyeeState l {get {return this;}}
		  public GlobalState g = new GlobalState();
		};
		  


		private static void stack_init (LinyeeState L1, LinyeeState L) {
		  /* initialize CallInfo array */
		  L1.base_ci = LinyeeMNewVector<CallInfo>(L, BASICCISIZE);
		  L1.ci = L1.base_ci[0];
		  L1.size_ci = BASICCISIZE;
		  L1.end_ci = L1.base_ci[L1.size_ci - 1];
		  /* initialize stack array */
		  L1.stack = LinyeeMNewVector<TValue>(L, BASICSTACKSIZE + EXTRASTACK);
		  L1.stacksize = BASICSTACKSIZE + EXTRASTACK;
		  L1.top = L1.stack[0];
		  L1.stack_last = L1.stack[L1.stacksize - EXTRASTACK - 1];
		  /* initialize first ci */
		  L1.ci.func = L1.top;
		  SetNilValue(StkId.Inc(ref L1.top));  /* `function' entry for this `ci' */
		  L1.base_ = L1.ci.base_ = L1.top;
		  L1.ci.top = L1.top + LINYEE_MINSTACK;
		}


		private static void freestack (LinyeeState L, LinyeeState L1) {
		  LinyeeMFreeArray(L, L1.base_ci);
		  LinyeeMFreeArray(L, L1.stack);
		}


		/*
		** open parts that may cause memory-allocation errors
		*/
		private static void f_luaopen (LinyeeState L, object ud) {
		  GlobalState g = G(L);
		  //UNUSED(ud);
		  stack_init(L, L);  /* init stack */
		  SetHValue(L, Gt(L), luaH_new(L, 0, 2));  /* table of globals */
		  SetHValue(L, Registry(L), luaH_new(L, 0, 2));  /* registry */
		  luaS_resize(L, MINSTRTABSIZE);  /* initial size of string table */
		  luaT_init(L);
		  LinyeeXInit(L);
		  luaS_fix(luaS_newliteral(L, MEMERRMSG));
		  g.GCthreshold = 4*g.totalbytes;
		}


		private static void preinit_state (LinyeeState L, GlobalState g) {
		  G_set(L, g);
		  L.stack = null;
		  L.stacksize = 0;
		  L.errorJmp = null;
		  L.hook = null;
		  L.hookmask = 0;
		  L.basehookcount = 0;
		  L.allowhook = 1;
		  ResetHookCount(L);
		  L.openupval = null;
		  L.size_ci = 0;
		  L.nCcalls = L.baseCcalls = 0;
		  L.status = 0;
		  L.base_ci = null;
		  L.ci = null;
		  L.savedpc = new InstructionPtr();
		  L.errfunc = 0;
		  SetNilValue(Gt(L));
		}


		private static void close_state (LinyeeState L) {
		  GlobalState g = G(L);
		  LinyeeFClose(L, L.stack[0]);  /* close all upvalues for this thread */
		  LinyeeCFreeAll(L);  /* collect all objects */
		  LinyeeAssert(g.rootgc == obj2gco(L));
		  LinyeeAssert(g.strt.nuse == 0);
		  LinyeeMFreeArray(L, G(L).strt.hash);
		  luaZ_freebuffer(L, g.buff);
		  freestack(L, L);
		  LinyeeAssert(g.totalbytes == GetUnmanagedSize(typeof(LG)));
		  //g.frealloc(g.ud, fromstate(L), (uint)state_size(typeof(LG)), 0);
		}


		private static LinyeeState luaE_newthread (LinyeeState L) {
		  //LinyeeState L1 = tostate(luaM_malloc(L, state_size(typeof(LinyeeState))));
		  LinyeeState L1 = LinyeeMNew<LinyeeState>(L);
		  LinyeeCLink(L, obj2gco(L1), LINYEE_TTHREAD);
		  preinit_state(L1, G(L));
		  stack_init(L1, L);  /* init stack */
		  SetObj2N(L, Gt(L1), Gt(L));  /* share table of globals */
		  L1.hookmask = L.hookmask;
		  L1.basehookcount = L.basehookcount;
		  L1.hook = L.hook;
		  ResetHookCount(L1);
		  LinyeeAssert(IsWhite(obj2gco(L1)));
		  return L1;
		}


		private static void luaE_freethread (LinyeeState L, LinyeeState L1) {
		  LinyeeFClose(L1, L1.stack[0]);  /* close all upvalues for this thread */
		  LinyeeAssert(L1.openupval == null);
		  luai_userstatefree(L1);
		  freestack(L, L1);
		  //luaM_freemem(L, fromstate(L1));
		}


        /// <summary>
        /// 打开一个新线程
        /// </summary>
        /// <returns></returns>
		public static LinyeeState LinyeeNewState(ly_Alloc f, object ud)
        {
            int i;
            LinyeeState L;
            GlobalState g;
            //object l = f(ud, null, 0, (uint)state_size(typeof(LG)));
            var lg = new LG();
            if (lg == null) return null;
            L = lg.l;// tostate(l);
            g = lg.g;// (L as LG).g;
            L.next = null;
            L.tt = LINYEE_TTHREAD;
            g.currentwhite = (ly_byte)Bit2Mask(WHITE0BIT, FIXEDBIT);
            L.marked = LinyeeCWhite(g);
            ly_byte marked = L.marked;  // can't pass properties in as ref
            Set2Bits(ref marked, FIXEDBIT, SFIXEDBIT);
            L.marked = marked;
            preinit_state(L, g);
            g.frealloc = f;
            g.ud = ud;
            g.mainthread = L;
            g.uvhead.u.l.prev = g.uvhead;
            g.uvhead.u.l.next = g.uvhead;
            g.GCthreshold = 0;  /* mark it as unfinished state */
            g.strt.size = 0;
            g.strt.nuse = 0;
            g.strt.hash = null;
            SetNilValue(Registry(L));
            luaZ_initbuffer(L, g.buff);
            g.panic = null;
            g.gcstate = GCSpause;
            g.rootgc = obj2gco(L);
            g.sweepstrgc = 0;
            g.sweepgc = new RootGCRef(g);
            g.gray = null;
            g.grayagain = null;
            g.weak = null;
            g.tmudata = null;
            g.totalbytes = (uint)GetUnmanagedSize(typeof(LG));
            g.gcpause = LUAI_GCPAUSE;
            g.gcstepmul = LUAI_GCMUL;
            g.gcdept = 0;
            for (i = 0; i < NUMTAGS; i++) g.mt[i] = null;
            if (LinyeeDRawRunProtected(L, f_luaopen, null) != 0)
            {
                /* memory allocation error: free partial state */
                close_state(L);
                L = null;
            }
            else
                luai_userstateopen(L);
            return L;
        }


		private static void callallgcTM (LinyeeState L, object ud) {
		  //UNUSED(ud);
		  LinyeeCCallGCTM(L);  /* call GC metamethods for all udata */
		}


		public static void LinyeeClose (LinyeeState L) {
		  L = G(L).mainthread;  /* only the main thread can be closed */
		  LinyeeLock(L);
		  LinyeeFClose(L, L.stack[0]);  /* close all upvalues for this thread */
		  LinyeeCSeparateUData(L, 1);  /* separate udata that have GC metamethods */
		  L.errfunc = 0;  /* no error function during GC metamethods */
		  do {  /* repeat until no more errors */
			L.ci = L.base_ci[0];
			L.base_ = L.top = L.ci.base_;
			L.nCcalls = L.baseCcalls = 0;
		  } while (LinyeeDRawRunProtected(L, callallgcTM, null) != 0);
		  LinyeeAssert(G(L).tmudata == null);
		  luai_userstateclose(L);
		  close_state(L);
		}

	}
}
