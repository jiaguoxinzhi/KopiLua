/*
** $Id: lobject.c,v 2.22.1.1 2007/12/27 13:02:25 roberto Exp $
** Some generic functions over Linyee objects
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Linyee
{
	using StkId = Linyee.LinyeeTypeValue;
	using LinyeeByteType = System.Byte;
	using LinyeeNumberType = System.Double;
	using l_uacNumber = System.Double;
	using Instruction = System.UInt32;

	public partial class Linyee
	{
		/* tags for values visible from Linyee */
		public const int LASTTAG	= LINYEE_TTHREAD;

		public const int NUMTAGS	= (LASTTAG+1);


		/*
		** Extra tags for non-values
		*/
		public const int LUATPROTO	= (LASTTAG+1);
		public const int LUATUPVAL	= (LASTTAG+2);
		public const int LUATDEADKEY	= (LASTTAG+3);

		public interface ArrayElement
		{
			void SetIndex(int index);
			void SetArray(object array);
		}


		/*
		** Common Header for all collectable objects (in macro form, to be
		** included in other objects)
		*/
		public class CommonHeader
		{
			public GCObject next;
			public LinyeeByteType tt;
			public LinyeeByteType marked;
		}


		/*
		** Common header in struct form
		*/
		public class GCheader : CommonHeader {
		};




		/*
		** Union of all Linyee values (in c# we use virtual data members and boxing)
		*/
		public class Value
		{

			// in the original code Value is a struct, so all assignments in the code
			// need to be replaced with a call to Copy. as it turns out, there are only
			// a couple. the vast majority of references to Value are the instance that
			// appears in the Linyee.LinyeeTypeValue class, so if you make that a virtual data member and
			// omit the set accessor then you'll get a compiler error if anything tries
			// to set it.
			public void Copy(Value copy)
			{
				this.p = copy.p;
			}

			public GCObject gc
			{
				get {return (GCObject)this.p;}
				set {this.p = value;}
			}
			public object p;
			public LinyeeNumberType n
			{
				get { return (LinyeeNumberType)this.p; }
				set { this.p = (object)value; }
			}
			public int b
			{
				get { return (int)this.p; }
				set { this.p = (object)value; }
			}
		};


		/*
		** Tagged Values
		*/

		//#define TValuefields	Value value; int tt

		public class LinyeeTypeValue : ArrayElement
		{
			private LinyeeTypeValue[] values = null;
			private int index = -1;

			public void SetIndex(int index)
			{
				this.index = index;
			}

			public void SetArray(object array)
			{
				this.values = (LinyeeTypeValue[])array;
				Debug.Assert(this.values != null);
			}

			public LinyeeTypeValue this[int offset]
			{
				get { return this.values[this.index + offset]; }
			}

			[CLSCompliantAttribute(false)]
			public LinyeeTypeValue this[uint offset]
			{
				get { return this.values[this.index + (int)offset]; }
			}

			public static LinyeeTypeValue operator +(LinyeeTypeValue value, int offset)
			{
				return value.values[value.index + offset];
			}

			public static LinyeeTypeValue operator +(int offset, LinyeeTypeValue value)
			{
				return value.values[value.index + offset];
			}

			public static LinyeeTypeValue operator -(LinyeeTypeValue value, int offset)
			{
				return value.values[value.index - offset];
			}

			public static int operator -(LinyeeTypeValue value, LinyeeTypeValue[] array)
			{
				Debug.Assert(value.values == array);
				return value.index;
			}

			public static int operator -(LinyeeTypeValue a, LinyeeTypeValue b)
			{
				Debug.Assert(a.values == b.values);
				return a.index - b.index;
			}
			
			public static bool operator <(LinyeeTypeValue a, LinyeeTypeValue b)
			{
				Debug.Assert(a.values == b.values);
				return a.index < b.index;
			}

			public static bool operator <=(LinyeeTypeValue a, LinyeeTypeValue b)
			{
				Debug.Assert(a.values == b.values);
				return a.index <= b.index;
			}

			public static bool operator >(LinyeeTypeValue a, LinyeeTypeValue b)
			{
				Debug.Assert(a.values == b.values);
				return a.index > b.index;
			}

			public static bool operator >=(LinyeeTypeValue a, LinyeeTypeValue b)
			{
				Debug.Assert(a.values == b.values);
				return a.index >= b.index;
			}
			
			public static LinyeeTypeValue Inc(ref LinyeeTypeValue value)
			{
				value = value[1];
				return value[-1];
			}

			public static LinyeeTypeValue Dec(ref LinyeeTypeValue value)
			{
				value = value[-1];
				return value[1];
			}

			public static implicit operator int(LinyeeTypeValue value)
			{
				return value.index;
			}

			public LinyeeTypeValue()
			{
			}

			public LinyeeTypeValue(LinyeeTypeValue copy)
			{
				this.values = copy.values;
				this.index = copy.index;
				this.value.Copy(copy.value);
				this.tt = copy.tt;
			}

			public LinyeeTypeValue(Value value, int tt)
			{
			    this.values = null;
			    this.index = 0;
			    this.value.Copy(value);
			    this.tt = tt;
			}

		  public Value value = new Value();
		  public int tt;

          public override string ToString()
          {
              string typename = null;
              string val = null;
              switch (tt)
              {
                  case LINYEE_TNIL: typename = "LINYEE_TNIL"; val = string.Empty;  break;
                  case LINYEE_TNUMBER: typename = "LINYEE_TNUMBER"; val = value.n.ToString(); break;
                  case LINYEE_TSTRING: typename = "LINYEE_TSTRING"; val = value.gc.ts.ToString(); break;
                  case LINYEE_TTABLE: typename = "LINYEE_TTABLE"; break;
                  case LINYEE_TFUNCTION: typename = "LINYEE_TFUNCTION"; break;
                  case LINYEE_TBOOLEAN: typename = "LINYEE_TBOOLEAN"; break;
                  case LINYEE_TUSERDATA: typename = "LINYEE_TUSERDATA"; break;
                  case LINYEE_TTHREAD: typename = "LINYEE_TTHREAD"; break;
                  case LINYEE_TLIGHTUSERDATA: typename = "LINYEE_TLIGHTUSERDATA"; break;
                  default: typename = "unknown"; break;
              }
              return string.Format("Linyee.LinyeeTypeValue<{0}>({1})", typename, val);
          }
        };

		/* Macros to test type */
		internal static bool TTIsNil(Linyee.LinyeeTypeValue o) { return (TType(o) == LINYEE_TNIL); }
		internal static bool TTIsNumber(Linyee.LinyeeTypeValue o)	{return (TType(o) == LINYEE_TNUMBER);}
		internal static bool TTIsString(Linyee.LinyeeTypeValue o)	{return (TType(o) == LINYEE_TSTRING);}
		internal static bool TTIsTable(Linyee.LinyeeTypeValue o)	{return (TType(o) == LINYEE_TTABLE);}
		internal static bool TTIsFunction(Linyee.LinyeeTypeValue o)	{return (TType(o) == LINYEE_TFUNCTION);}
		internal static bool TTIsBoolean(Linyee.LinyeeTypeValue o) { return (TType(o) == LINYEE_TBOOLEAN); }
		internal static bool TTIsUserData(Linyee.LinyeeTypeValue o) { return (TType(o) == LINYEE_TUSERDATA); }
		internal static bool TTIsThread(Linyee.LinyeeTypeValue o)	{return (TType(o) == LINYEE_TTHREAD);}
		internal static bool TTIsLightUserData(Linyee.LinyeeTypeValue o) { return (TType(o) == LINYEE_TLIGHTUSERDATA); }

		/* Macros to access values */
#if DEBUG
		internal static int TType(Linyee.LinyeeTypeValue o) { return o.tt; }
		internal static int TType(CommonHeader o) { return o.tt; }
		internal static GCObject GCValue(Linyee.LinyeeTypeValue o) { return (GCObject)CheckExp(IsCollectable(o), o.value.gc); }
		internal static object PValue(Linyee.LinyeeTypeValue o) { return (object)CheckExp(TTIsLightUserData(o), o.value.p); }
		internal static LinyeeNumberType NValue(Linyee.LinyeeTypeValue o) { return (LinyeeNumberType)CheckExp(TTIsNumber(o), o.value.n); }
		internal static TString RawTSValue(Linyee.LinyeeTypeValue o) { return (TString)CheckExp(TTIsString(o), o.value.gc.ts); }
		internal static TStringTSV TSValue(Linyee.LinyeeTypeValue o) { return RawTSValue(o).tsv; }
		internal static Udata RawUValue(Linyee.LinyeeTypeValue o) { return (Udata)CheckExp(TTIsUserData(o), o.value.gc.u); }
		internal static UdataUV UValue(Linyee.LinyeeTypeValue o) { return RawUValue(o).uv; }
		internal static Closure CLValue(Linyee.LinyeeTypeValue o) { return (Closure)CheckExp(TTIsFunction(o), o.value.gc.cl); }
		internal static Table HValue(Linyee.LinyeeTypeValue o) { return (Table)CheckExp(TTIsTable(o), o.value.gc.h); }
		internal static int BValue(Linyee.LinyeeTypeValue o) { return (int)CheckExp(TTIsBoolean(o), o.value.b); }
		internal static LinyeeState THValue(Linyee.LinyeeTypeValue o) { return (LinyeeState)CheckExp(TTIsThread(o), o.value.gc.th); }
#else
		internal static int TType(Linyee.LinyeeTypeValue o) { return o.tt; }
		internal static int TType(CommonHeader o) { return o.tt; }
		internal static GCObject GCValue(Linyee.LinyeeTypeValue o) { return o.value.gc; }
		internal static object PValue(Linyee.LinyeeTypeValue o) { return o.value.p; }
		internal static LinyeeNumberType NValue(Linyee.LinyeeTypeValue o) { return o.value.n; }
		internal static TString RawTSValue(Linyee.LinyeeTypeValue o) { return o.value.gc.ts; }
		internal static TStringTSV TSValue(Linyee.LinyeeTypeValue o) { return RawTSValue(o).tsv; }
		internal static Udata RawUValue(Linyee.LinyeeTypeValue o) { return o.value.gc.u; }
		internal static UdataUV UValue(Linyee.LinyeeTypeValue o) { return RawUValue(o).uv; }
		internal static Closure CLValue(Linyee.LinyeeTypeValue o) { return o.value.gc.cl; }
		internal static Table HValue(Linyee.LinyeeTypeValue o) { return o.value.gc.h; }
		internal static int BValue(Linyee.LinyeeTypeValue o) { return o.value.b; }
		internal static LinyeeState THValue(Linyee.LinyeeTypeValue o) { return (LinyeeState)CheckExp(TTIsThread(o), o.value.gc.th); }
#endif

		public static int LIsFalse(Linyee.LinyeeTypeValue o) { return ((TTIsNil(o) || (TTIsBoolean(o) && BValue(o) == 0))) ? 1 : 0; }

		/*
		** for internal debug only
		*/
		[Conditional("DEBUG")]
		internal static void CheckConsistency(Linyee.LinyeeTypeValue obj)
		{
			LinyeeAssert(!IsCollectable(obj) || (TType(obj) == (obj).value.gc.gch.tt));
		}

		[Conditional("DEBUG")]
		internal static void CheckLiveness(GlobalState g, Linyee.LinyeeTypeValue obj)
		{
			LinyeeAssert(!IsCollectable(obj) ||
			((TType(obj) == obj.value.gc.gch.tt) && !IsDead(g, obj.value.gc)));
		}
		
		/* Macros to set values */
		internal static void SetNilValue(Linyee.LinyeeTypeValue obj) {
			obj.tt=LINYEE_TNIL;
		}

		internal static void SetNValue(Linyee.LinyeeTypeValue obj, LinyeeNumberType x) {
			obj.value.n = x;
			obj.tt = LINYEE_TNUMBER;
		}

		internal static void SetPValue( Linyee.LinyeeTypeValue obj, object x) {
			obj.value.p = x;
			obj.tt = LINYEE_TLIGHTUSERDATA;
		}

		internal static void SetBValue(Linyee.LinyeeTypeValue obj, int x) {
			obj.value.b = x;
			obj.tt = LINYEE_TBOOLEAN;
		}

		internal static void SetSValue(LinyeeState L, Linyee.LinyeeTypeValue obj, GCObject x) {
			obj.value.gc = x;
			obj.tt = LINYEE_TSTRING;
			CheckLiveness(G(L), obj);
		}

		internal static void SetUValue(LinyeeState L, Linyee.LinyeeTypeValue obj, GCObject x) {
			obj.value.gc = x;
			obj.tt = LINYEE_TUSERDATA;
			CheckLiveness(G(L), obj);
		}

		internal static void SetTTHValue(LinyeeState L, Linyee.LinyeeTypeValue obj, GCObject x) {
			obj.value.gc = x;
			obj.tt = LINYEE_TTHREAD;
			CheckLiveness(G(L), obj);
		}

		internal static void SetCLValue(LinyeeState L, Linyee.LinyeeTypeValue obj, Closure x) {
			obj.value.gc = x;
			obj.tt = LINYEE_TFUNCTION;
			CheckLiveness(G(L), obj);
		}

		internal static void SetHValue(LinyeeState L, Linyee.LinyeeTypeValue obj, Table x) {
			obj.value.gc = x;
			obj.tt = LINYEE_TTABLE;
			CheckLiveness(G(L), obj);
		}

		internal static void SetPTValue(LinyeeState L, Linyee.LinyeeTypeValue obj, Proto x) {
			obj.value.gc = x;
			obj.tt = LUATPROTO;
			CheckLiveness(G(L), obj);
		}

		internal static void SetObj(LinyeeState L, Linyee.LinyeeTypeValue obj1, Linyee.LinyeeTypeValue obj2) {
			obj1.value.Copy(obj2.value);
			obj1.tt = obj2.tt;
			CheckLiveness(G(L), obj1);
		}


		/*
		** different types of sets, according to destination
		*/

		/* from stack to (same) stack */
		//#define setobjs2s	setobj
		internal static void SetObjs2S(LinyeeState L, Linyee.LinyeeTypeValue obj, Linyee.LinyeeTypeValue x) { SetObj(L, obj, x); }
		///* to stack (not from same stack) */
		
		//#define setobj2s	setobj
		internal static void SetObj2S(LinyeeState L, Linyee.LinyeeTypeValue obj, Linyee.LinyeeTypeValue x) { SetObj(L, obj, x); }

		//#define setsvalue2s	setsvalue
		internal static void SetSValue2S(LinyeeState L, Linyee.LinyeeTypeValue obj, TString x) { SetSValue(L, obj, x); }

		//#define sethvalue2s	sethvalue
		internal static void SetHValue2S(LinyeeState L, Linyee.LinyeeTypeValue obj, Table x) { SetHValue(L, obj, x); }

		//#define setptvalue2s	setptvalue
		internal static void SetPTValue2S(LinyeeState L, Linyee.LinyeeTypeValue obj, Proto x) { SetPTValue(L, obj, x); }

		///* from table to same table */
		//#define setobjt2t	setobj
		internal static void SetObjT2T(LinyeeState L, Linyee.LinyeeTypeValue obj, Linyee.LinyeeTypeValue x) { SetObj(L, obj, x); }

		///* to table */
		//#define setobj2t	setobj
		internal static void SetObj2T(LinyeeState L, Linyee.LinyeeTypeValue obj, Linyee.LinyeeTypeValue x) { SetObj(L, obj, x); }

		///* to new object */
		//#define setobj2n	setobj
		internal static void SetObj2N(LinyeeState L, Linyee.LinyeeTypeValue obj, Linyee.LinyeeTypeValue x) { SetObj(L, obj, x); }

		//#define setsvalue2n	setsvalue
		internal static void SetSValue2N(LinyeeState L, Linyee.LinyeeTypeValue obj, TString x) { SetSValue(L, obj, x); }

		internal static void SetTType(Linyee.LinyeeTypeValue obj, int tt) { obj.tt = tt; }


		internal static bool IsCollectable(Linyee.LinyeeTypeValue o) { return (TType(o) >= LINYEE_TSTRING); }



		//typedef Linyee.LinyeeTypeValue *StkId;  /* index to stack elements */
		
		/*
		** String headers for string table
		*/
		public class TStringTSV : GCObject
		{
			public LinyeeByteType reserved;
			[CLSCompliantAttribute(false)]
			public uint hash;
			[CLSCompliantAttribute(false)]
			public uint len;
		};
		public class TString : TStringTSV {
			//public L_Umaxalign dummy;  /* ensures maximum alignment for strings */			
			public TStringTSV tsv { get { return this; } }

			public TString()
			{
			}
			public TString(CharPtr str) { this.str = str; }

			public CharPtr str;

			public override string ToString() { return str.ToString(); } // for debugging
		};

		public static CharPtr GetStr(TString ts) { return ts.str; }
		public static CharPtr SValue(StkId o) { return GetStr(RawTSValue(o)); }

		public class UdataUV : GCObject
		{
			public Table metatable;
			public Table env;
			[CLSCompliantAttribute(false)]
			public uint len;
		};

		public class Udata : UdataUV
		{
			public Udata() { this.uv = this; }

			public new UdataUV uv;

			//public L_Umaxalign dummy;  /* ensures maximum alignment for `local' udata */

			// in the original C code this was allocated alongside the structure memory. it would probably
			// be possible to still do that by allocating memory and pinning it down, but we can do the
			// same thing just as easily by allocating a seperate byte array for it instead.
			public object user_data;
		};




		/*
		** Function Prototypes
		*/
		public class Proto : GCObject {

		  public Proto[] protos = null;
		  public int index = 0;
		  public Proto this[int offset] {get { return this.protos[this.index + offset]; }}

		  public Linyee.LinyeeTypeValue[] k;  /* constants used by the function */
			[CLSCompliantAttribute(false)]
		  public Instruction[] code;
		  public new Proto[] p;  /* functions defined inside the function */
		  public int[] lineinfo;  /* map from opcodes to source lines */
		  public LocVar[] locvars;  /* information about local variables */
		  public TString[] upvalues;  /* upvalue names */
		  public TString  source;
		  public int sizeupvalues;
		  public int sizek;  /* size of `k' */
		  public int sizecode;
		  public int sizelineinfo;
		  public int sizep;  /* size of `p' */
		  public int sizelocvars;
		  public int linedefined;
		  public int lastlinedefined;
		  public GCObject gclist;
		  public LinyeeByteType nups;  /* number of upvalues */
		  public LinyeeByteType numparams;
		  public LinyeeByteType is_vararg;
		  public LinyeeByteType maxstacksize;
		};


		/* masks for new-style vararg */
		public const int VARARG_HASARG			= 1;
		public const int VARARG_ISVARARG		= 2;
		public const int VARARG_NEEDSARG		= 4;

		public class LocVar {
		  public TString varname;
		  public int startpc;  /* first point where variable is active */
		  public int endpc;    /* first point where variable is dead */
		};



		/*
		** Upvalues
		*/

		public class UpVal : GCObject {
		  public Linyee.LinyeeTypeValue v;  /* points to stack or to its own value */
			[CLSCompliantAttribute(false)]
			public class Uinternal {
				public Linyee.LinyeeTypeValue value = new LinyeeTypeValue();  /* the value (when closed) */
				[CLSCompliantAttribute(false)]
				public class _l {  /* double linked list (when open) */
				  public UpVal prev;
				  public UpVal next;
				};

				public _l l = new _l();
		  }
			[CLSCompliantAttribute(false)]
			public new Uinternal u = new Uinternal();
		};


        /// <summary>
        /// Closures ±Õ°ü
        /// </summary>
        public class ClosureHeader : GCObject {
			public LinyeeByteType isC;
			public LinyeeByteType nupvalues;
			public GCObject gclist;
			public Table env;
		};

		public class ClosureType {

			ClosureHeader header;

			public static implicit operator ClosureHeader(ClosureType ctype) {return ctype.header;}
			public ClosureType(ClosureHeader header) {this.header = header;}

			public LinyeeByteType isC { get { return header.isC; } set { header.isC = value; } }
			public LinyeeByteType nupvalues { get { return header.nupvalues; } set { header.nupvalues = value; } }
			public GCObject gclist { get { return header.gclist; } set { header.gclist = value; } }
			public Table env { get { return header.env; } set { header.env = value; } }
		}

		public class CClosure : ClosureType {
			public CClosure(ClosureHeader header) : base(header) { }
			public LinyeeNativeFunction f;
			public Linyee.LinyeeTypeValue[] upvalue;
		};


		public class LClosure : ClosureType {
			public LClosure(ClosureHeader header) : base(header) { }
			public Proto p;
			public UpVal[] upvals;
		};

		public class Closure : ClosureHeader
		{
		  public Closure()
		  {
			  c = new CClosure(this);
			  l = new LClosure(this);
		  }

		  public CClosure c;
		  public LClosure l;
		};


		public static bool IsCFunction(Linyee.LinyeeTypeValue o) { return ((TType(o) == LINYEE_TFUNCTION) && (CLValue(o).c.isC != 0)); }
		public static bool IsLfunction(Linyee.LinyeeTypeValue o) { return ((TType(o) == LINYEE_TFUNCTION) && (CLValue(o).c.isC==0)); }


		/*
		** Tables
		*/

		public class TKeyNK : Linyee.LinyeeTypeValue
		{
			public TKeyNK() { }
			public TKeyNK(Value value, int tt, Node next) : base(value, tt)
			{
			    this.next = next;
			}
			public Node next;  /* for chaining */
		};

		public class TKey {
			public TKey()
			{
				this.nk = new TKeyNK();
			}
			public TKey(TKey copy)
			{
				this.nk = new TKeyNK(copy.nk.value, copy.nk.tt, copy.nk.next);
			}
			public TKey(Value value, int tt, Node next)
			{
			    this.nk = new TKeyNK(value, tt, next);
			}

			public TKeyNK nk = new TKeyNK();
			public Linyee.LinyeeTypeValue tvk { get { return this.nk; } }
		};


		public class Node : ArrayElement
		{
			private Node[] values = null;
			private int index = -1;

			public void SetIndex(int index)
			{
				this.index = index;
			}

			public void SetArray(object array)
			{
				this.values = (Node[])array;
				Debug.Assert(this.values != null);
			}

			public Node()
			{
				this.i_val = new LinyeeTypeValue();
				this.i_key = new TKey();
			}

			public Node(Node copy)
			{
				this.values = copy.values;
				this.index = copy.index;
				this.i_val = new LinyeeTypeValue(copy.i_val);
				this.i_key = new TKey(copy.i_key);
			}

			public Node(Linyee.LinyeeTypeValue i_val, TKey i_key)
			{
				this.values = new Node[] { this };
				this.index = 0;
				this.i_val = i_val;
				this.i_key = i_key;
			}

			public Linyee.LinyeeTypeValue i_val;
			public TKey i_key;

			[CLSCompliantAttribute(false)]
			public Node this[uint offset]
			{
				get { return this.values[this.index + (int)offset]; }
			}

			public Node this[int offset]
			{
				get { return this.values[this.index + offset]; }
			}

			public static int operator -(Node n1, Node n2)
			{
				Debug.Assert(n1.values == n2.values);
				return n1.index - n2.index;
			}

			public static Node Inc(ref Node node)
			{
				node = node[1];
				return node[-1];
			}

			public static Node Dec(ref Node node)
			{
				node = node[-1];
				return node[1];
			}

			public static bool operator >(Node n1, Node n2) { Debug.Assert(n1.values == n2.values); return n1.index > n2.index; }
			public static bool operator >=(Node n1, Node n2) { Debug.Assert(n1.values == n2.values); return n1.index >= n2.index; }
			public static bool operator <(Node n1, Node n2) { Debug.Assert(n1.values == n2.values); return n1.index < n2.index; }
			public static bool operator <=(Node n1, Node n2) { Debug.Assert(n1.values == n2.values); return n1.index <= n2.index; }
			public static bool operator ==(Node n1, Node n2)
			{
				object o1 = n1 as Node;
				object o2 = n2 as Node;
				if ((o1 == null) && (o2 == null)) return true;
				if (o1 == null) return false;
				if (o2 == null) return false;
				if (n1.values != n2.values) return false;
				return n1.index == n2.index;
			}
			public static bool operator !=(Node n1, Node n2) { return !(n1==n2); }

			public override bool Equals(object o) {return this == (Node)o;}
			public override int GetHashCode() {return 0;}
		};


		public class Table : GCObject {
		  public LinyeeByteType flags;  /* 1<<p means tagmethod(p) is not present */ 
		  public LinyeeByteType lsizenode;  /* log2 of size of `node' array */
		  public Table metatable;
		  public Linyee.LinyeeTypeValue[] array;  /* array part */
		  public Node[] node;
		  public int lastfree;  /* any free position is before this position */
		  public GCObject gclist;
		  public int sizearray;  /* size of `array' array */
		};



		/*
		** `module' operation for hashing (size is always a power of 2)
		*/
		//#define lmod(s,size) \
		//    (check_exp((size&(size-1))==0, (cast(int, (s) & ((size)-1)))))


		internal static int TwoTo(int x) { return 1 << x; }
		internal static int SizeNode(Table t) { return TwoTo(t.lsizenode); }

		public static Linyee.LinyeeTypeValue LinyeeONilObjectX = new LinyeeTypeValue(new Value(), LINYEE_TNIL);
		public static Linyee.LinyeeTypeValue LinyeeONilObject = LinyeeONilObjectX;

		public static int CeilLog2(int x)	{return LinyeeOLog2((uint)(x-1)) + 1;}
	


		/*
		** converts an integer to a "floating point byte", represented as
		** (eeeeexxx), where the real value is (1xxx) * 2^(eeeee - 1) if
		** eeeee != 0 and (xxx) otherwise.
		*/
		[CLSCompliantAttribute(false)]
		public static int LinyeeOInt2FB (uint x) {
		  int e = 0;  /* expoent */
		  while (x >= 16) {
			x = (x+1) >> 1;
			e++;
		  }
		  if (x < 8) return (int)x;
		  else return ((e+1) << 3) | (CastInt(x) - 8);
		}


		/* converts back */
		public static int LinyeeOFBInt (int x) {
		  int e = (x >> 3) & 31;
		  if (e == 0) return x;
		  else return ((x & 7)+8) << (e - 1);
		}


		private readonly static LinyeeByteType[] log2 = {
			0,1,2,2,3,3,3,3,4,4,4,4,4,4,4,4,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
			6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
			7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
			7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
			8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
			8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
			8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,
			8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8,8
		  };

		[CLSCompliantAttribute(false)]
		public static int LinyeeOLog2 (uint x) {
		  int l = -1;
		  while (x >= 256) { l += 8; x >>= 8; }
		  return l + log2[x];

		}


		public static int LinyeeORawEqualObj (Linyee.LinyeeTypeValue t1, Linyee.LinyeeTypeValue t2) {
		  if (TType(t1) != TType(t2)) return 0;
		  else switch (TType(t1)) {
			case LINYEE_TNIL:
			  return 1;
			case LINYEE_TNUMBER:
			  return luai_numeq(NValue(t1), NValue(t2)) ? 1 : 0;
			case LINYEE_TBOOLEAN:
			  return BValue(t1) == BValue(t2) ? 1 : 0;  /* boolean true must be 1....but not in C# !! */
			case LINYEE_TLIGHTUSERDATA:
				return PValue(t1) == PValue(t2) ? 1 : 0;
			default:
			  LinyeeAssert(IsCollectable(t1));
			  return GCValue(t1) == GCValue(t2) ? 1 : 0;
		  }
		}

		public static int LinyeeOStr2d (CharPtr s, out LinyeeNumberType result) {
		  CharPtr endptr;
		  result = ly_str2number(s, out endptr);
		  if (endptr == s) return 0;  /* conversion failed */
		  if (endptr[0] == 'x' || endptr[0] == 'X')  /* maybe an hexadecimal constant? */
			result = CastNum(strtoul(s, out endptr, 16));
		  if (endptr[0] == '\0') return 1;  /* most common case */
		  while (isspace(endptr[0])) endptr = endptr.next();
		  if (endptr[0] != '\0') return 0;  /* invalid trailing characters? */
		  return 1;
		}



		private static void PushStr (LinyeeState L, CharPtr str) {
		  SetSValue2S(L, L.top, luaS_new(L, str));
		  IncrTop(L);
		}


		/* this function handles only `%d', `%c', %f, %p, and `%s' formats */
		public static CharPtr LinyeeOPushVFString (LinyeeState L, CharPtr fmt, params object[] argp) {
		  int parm_index = 0;
		  int n = 1;
		  PushStr(L, "");
		  for (;;) {
		    CharPtr e = strchr(fmt, '%');
		    if (e == null) break;
		    SetSValue2S(L, L.top, luaS_newlstr(L, fmt, (uint)(e-fmt)));
		    IncrTop(L);
		    switch (e[1]) {
		      case 's': {
				  object o = argp[parm_index++];
				  CharPtr s = o as CharPtr;
				  if (s == null)
					  s = (string)o;
				  if (s == null) s = "(null)";
		          PushStr(L, s);
		          break;
		      }
		      case 'c': {
		        CharPtr buff = new char[2];
		        buff[0] = (char)(int)argp[parm_index++];
		        buff[1] = '\0';
		        PushStr(L, buff);
		        break;
		      }
		      case 'd': {
		        SetNValue(L.top, (int)argp[parm_index++]);
		        IncrTop(L);
		        break;
		      }
		      case 'f': {
		        SetNValue(L.top, (l_uacNumber)argp[parm_index++]);
		        IncrTop(L);
		        break;
		      }
		      case 'p': {
		        //CharPtr buff = new char[4*sizeof(void *) + 8]; /* should be enough space for a `%p' */
				CharPtr buff = new char[32];
				sprintf(buff, "0x%08x", argp[parm_index++].GetHashCode());
		        PushStr(L, buff);
		        break;
		      }
		      case '%': {
		        PushStr(L, "%");
		        break;
		      }
		      default: {
		        CharPtr buff = new char[3];
		        buff[0] = '%';
		        buff[1] = e[1];
		        buff[2] = '\0';
		        PushStr(L, buff);
		        break;
		      }
		    }
		    n += 2;
		    fmt = e+2;
		  }
		  PushStr(L, fmt);
		  luaV_concat(L, n+1, CastInt(L.top - L.base_) - 1);
		  L.top -= n;
		  return SValue(L.top - 1);
		}

		public static CharPtr LinyeeOPushFString(LinyeeState L, CharPtr fmt, params object[] args)
		{
			return LinyeeOPushVFString(L, fmt, args);
		}

		[CLSCompliantAttribute(false)]
		public static void LinyeeOChunkID (CharPtr out_, CharPtr source, uint bufflen) {
			//out_ = "";
		  if (source[0] == '=') {
		    strncpy(out_, source+1, (int)bufflen);  /* remove first char */
		    out_[bufflen-1] = '\0';  /* ensures null termination */
		  }
		  else {  /* out = "source", or "...source" */
		    if (source[0] == '@') {
		      uint l;
		      source = source.next();  /* skip the `@' */
		      bufflen -= (uint)(" '...' ".Length + 1);
		      l = (uint)strlen(source);
		      strcpy(out_, "");
		      if (l > bufflen) {
		        source += (l-bufflen);  /* get last part of file name */
		        strcat(out_, "...");
		      }
		      strcat(out_, source);
		    }
		    else {  /* out = [string "string"] */
		      uint len = strcspn(source, "\n\r");  /* stop at first newline */
		      bufflen -= (uint)(" [string \"...\"] ".Length + 1);
		      if (len > bufflen) len = bufflen;
		      strcpy(out_, "[string \"");
		      if (source[len] != '\0') {  /* must truncate? */
		        strncat(out_, source, (int)len);
		        strcat(out_, "...");
		      }
		      else
		        strcat(out_, source);
		      strcat(out_, "\"]");
		    }
		  }
		}

	}
}
