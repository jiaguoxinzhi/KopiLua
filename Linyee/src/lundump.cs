/*
** $Id: lundump.c,v 2.7.1.4 2008/04/04 19:51:41 roberto Exp $
** load precompiled Linyee chunks
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Linyee
{
	using TValue = Linyee.LinyeeTypeValue;
	using ly_Number = System.Double;
	using ly_byte = System.Byte;
	using StkId = Linyee.LinyeeTypeValue;
	using Instruction = System.UInt32;
	using ZIO = Linyee.Zio;

	public partial class Linyee
	{
		/* for header of binary files -- this is Linyee 5.1 */
		public const int LUAC_VERSION		= 0x51;

		/* for header of binary files -- this is the official format */
		public const int LUAC_FORMAT		= 0;

		/* size of header of binary files */
		public const int LUAC_HEADERSIZE		= 12;

		public class LoadState{
			public LinyeeState L;
			public ZIO Z;
			public Mbuffer b;
			public CharPtr name;
		};

		//#ifdef LUAC_TRUST_BINARIES
		//#define IF(c,s)
		//#define error(S,s)
		//#else
		//#define IF(c,s)		if (c) error(S,s)

		public static void IF(int c, string s) { }
		public static void IF(bool c, string s) { }

		static void error(LoadState S, CharPtr why)
		{
		 LinyeeOPushFString(S.L,"%s: %s in precompiled chunk",S.name,why);
		 LinyeeDThrow(S.L,LINYEE_ERRSYNTAX);
		}
		//#endif

		public static object LoadMem(LoadState S, Type t)
		{
#if SILVERLIGHT
			// No support for Marshal.SizeOf in Silverlight, so we
			// have to manually set the size. Size values are from
			// Linyee's 5.1 spec.
			int size = 0;
			if (t.Equals(typeof(UInt32)))
			{
				size = 4;
			}
			else if (t.Equals(typeof(Int32)))
			{
				size = 4;
			}
			else if (t.Equals(typeof(Char)))
			{
				size = 1;
			}
			else if (t.Equals(typeof(Byte)))
			{
				size = 1;
			}
			else if (t.Equals(typeof(Double)))
			{
				size = 8;
			}
#else
            int size = Marshal.SizeOf(t);
#endif
			CharPtr str = new char[size];
			LoadBlock(S, str, size);
			byte[] bytes = new byte[str.chars.Length];
			for (int i = 0; i < str.chars.Length; i++)
				bytes[i] = (byte)str.chars[i];
#if SILVERLIGHT
			// No support for Marshal.PtrToStructure in Silverlight,
			// let's use BitConverter instead!
			object b = null;
			if (t.Equals(typeof(UInt32)))
			{
				b = (UInt32)BitConverter.ToUInt32(bytes, 0);
			}
			else if (t.Equals(typeof(Int32)))
			{
				b = (Int32)BitConverter.ToInt32(bytes, 0);
			}
			else if (t.Equals(typeof(Char)))
			{
				b = (Char)bytes[0];
			}
			else if (t.Equals(typeof(Byte)))
			{
				b = (Byte)bytes[0];
			}
			else if (t.Equals(typeof(Double)))
			{
				b = (Double)BitConverter.ToDouble(bytes, 0);
			}
#else
			GCHandle pinnedPacket = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			object b = Marshal.PtrToStructure(pinnedPacket.AddrOfPinnedObject(), t);
			pinnedPacket.Free();
#endif
			return b;
		}

		public static object LoadMem(LoadState S, Type t, int n)
		{
#if SILVERLIGHT
			Array array = Array.CreateInstance(t, n);
			for (int i = 0; i < n; i++)
				array.SetValue(LoadMem(S, t), i);
			return array;
#else
			ArrayList array = new ArrayList();
			for (int i=0; i<n; i++)
				array.Add(LoadMem(S, t));
			return array.ToArray(t);
#endif
		}
		public static ly_byte LoadByte(LoadState S)		{return (ly_byte)LoadChar(S);}
		public static object LoadVar(LoadState S, Type t) { return LoadMem(S, t); }
		public static object LoadVector(LoadState S, Type t, int n) {return LoadMem(S, t, n);}

		private static void LoadBlock(LoadState S, CharPtr b, int size)
		{
		 uint r=luaZ_read(S.Z, b, (uint)size);
		 IF (r!=0, "unexpected end");
		}

		private static int LoadChar(LoadState S) 
		{
		 return (char)LoadVar(S, typeof(char));
		}

		private static int LoadInt(LoadState S)
		{
		 int x = (int)LoadVar(S, typeof(int));
		 IF (x<0, "bad integer");
		 return x;
		}

		private static ly_Number LoadNumber(LoadState S)
		{
		 return (ly_Number)LoadVar(S, typeof(ly_Number));
		}

		private static TString LoadString(LoadState S)
		{
		 uint size = (uint)LoadVar(S, typeof(uint));
		 if (size==0)
		  return null;
		 else
		 {
		  CharPtr s=luaZ_openspace(S.L,S.b,size);
		  LoadBlock(S, s, (int)size);
		  return luaS_newlstr(S.L,s,size-1);		/* remove trailing '\0' */
		 }
		}

		private static void LoadCode(LoadState S, Proto f)
		{
		 int n=LoadInt(S);
		 f.code = LinyeeMNewVector<Instruction>(S.L, n);
		 f.sizecode=n;
		 f.code = (Instruction[])LoadVector(S, typeof(Instruction), n);
		}

		private static void LoadConstants(LoadState S, Proto f)
		{
		 int i,n;
		 n=LoadInt(S);
		 f.k = LinyeeMNewVector<TValue>(S.L, n);
		 f.sizek=n;
		 for (i=0; i<n; i++) SetNilValue(f.k[i]);
		 for (i=0; i<n; i++)
		 {
		  TValue o=f.k[i];
		  int t=LoadChar(S);
		  switch (t)
		  {
		   case LINYEE_TNIL:
   			SetNilValue(o);
			break;
		   case LINYEE_TBOOLEAN:
   			SetBValue(o, LoadChar(S));
			break;
		   case LINYEE_TNUMBER:
			SetNValue(o, LoadNumber(S));
			break;
		   case LINYEE_TSTRING:
			SetSValue2N(S.L, o, LoadString(S));
			break;
		   default:
			error(S,"bad constant");
			break;
		  }
		 }
		 n=LoadInt(S);
		 f.p=LinyeeMNewVector<Proto>(S.L,n);
		 f.sizep=n;
		 for (i=0; i<n; i++) f.p[i]=null;
		 for (i=0; i<n; i++) f.p[i]=LoadFunction(S,f.source);
		}

		private static void LoadDebug(LoadState S, Proto f)
		{
		 int i,n;
		 n=LoadInt(S);
		 f.lineinfo=LinyeeMNewVector<int>(S.L,n);
		 f.sizelineinfo=n;
		 f.lineinfo = (int[])LoadVector(S, typeof(int), n);
		 n=LoadInt(S);
		 f.locvars=LinyeeMNewVector<LocVar>(S.L,n);
		 f.sizelocvars=n;
		 for (i=0; i<n; i++) f.locvars[i].varname=null;
		 for (i=0; i<n; i++)
		 {
		  f.locvars[i].varname=LoadString(S);
		  f.locvars[i].startpc=LoadInt(S);
		  f.locvars[i].endpc=LoadInt(S);
		 }
		 n=LoadInt(S);
		 f.upvalues=LinyeeMNewVector<TString>(S.L, n);
		 f.sizeupvalues=n;
		 for (i=0; i<n; i++) f.upvalues[i]=null;
		 for (i=0; i<n; i++) f.upvalues[i]=LoadString(S);
		}

		private static Proto LoadFunction(LoadState S, TString p)
		{
		 Proto f;
		 if (++S.L.nCcalls > LUAI_MAXCCALLS) error(S,"code too deep");
		 f=LinyeeFNewProto(S.L);
		 SetPTValue2S(S.L,S.L.top,f); IncrTop(S.L);
		 f.source=LoadString(S); if (f.source==null) f.source=p;
		 f.linedefined=LoadInt(S);
		 f.lastlinedefined=LoadInt(S);
		 f.nups=LoadByte(S);
		 f.numparams=LoadByte(S);
		 f.is_vararg=LoadByte(S);
		 f.maxstacksize=LoadByte(S);
		 LoadCode(S,f);
		 LoadConstants(S,f);
		 LoadDebug(S,f);
		 IF (LinyeeGCheckCode(f)==0 ? 1 : 0, "bad code");
		 StkId.Dec(ref S.L.top);
		 S.L.nCcalls--;
		 return f;
		}

		private static void LoadHeader(LoadState S)
		{
		 CharPtr h = new char[LUAC_HEADERSIZE];
		 CharPtr s = new char[LUAC_HEADERSIZE];
		 luaU_header(h);
		 LoadBlock(S, s, LUAC_HEADERSIZE);
		 IF (memcmp(h, s, LUAC_HEADERSIZE)!=0, "bad header");
		}

		/*
		** load precompiled chunk
		*/
		public static Proto luaU_undump (LinyeeState L, ZIO Z, Mbuffer buff, CharPtr name)
		{
		 LoadState S = new LoadState();
		 if (name[0] == '@' || name[0] == '=')
		  S.name = name+1;
		 else if (name[0]==LINYEE_SIGNATURE[0])
		  S.name="binary string";
		 else
		  S.name=name;
		 S.L=L;
		 S.Z=Z;
		 S.b=buff;
		 LoadHeader(S);
		 return LoadFunction(S,luaS_newliteral(L,"=?"));
		}

		/*
		* make header
		*/
		public static void luaU_header(CharPtr h)
		{
		 h = new CharPtr(h);
		 int x=1;
		 memcpy(h, LINYEE_SIGNATURE, LINYEE_SIGNATURE.Length);
		 h = h.add(LINYEE_SIGNATURE.Length);
		 h[0] = (char)LUAC_VERSION;
		 h.inc();
		 h[0] = (char)LUAC_FORMAT;
		 h.inc();
		 //*h++=(char)*(char*)&x;				/* endianness */
		 h[0] = (char)x;						/* endianness */
		 h.inc();
		 h[0] = (char)sizeof(int);
		 h.inc();
		 h[0] = (char)sizeof(uint);
		 h.inc();
		 h[0] = (char)sizeof(Instruction);
		 h.inc();
		 h[0] = (char)sizeof(ly_Number);
		 h.inc();

		  //(h++)[0] = ((ly_Number)0.5 == 0) ? 0 : 1;		/* is ly_Number integral? */
		 h[0] = (char)0;	// always 0 on this build
		}

	}
}
