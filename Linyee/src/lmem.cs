/*
** $Id: lmem.c,v 1.70.1.1 2007/12/27 13:02:25 roberto Exp $
** Interface to Memory Manager
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Linyee
{
	public partial class Linyee
	{
		public const string MEMERRMSG	= "not enough memory";

		public static T[] LinyeeMReallocV<T>(LinyeeState L, T[] block, int new_size)
		{
			return (T[])LinyeeMRealloc(L, block, new_size);
		}
			
		//#define luaM_freemem(L, b, s)	luaM_realloc_(L, (b), (s), 0)
		//#define luaM_free(L, b)		luaM_realloc_(L, (b), sizeof(*(b)), 0)
		//public static void luaM_freearray(LinyeeState L, object b, int n, Type t) { luaM_reallocv(L, b, n, 0, Marshal.SizeOf(b)); }

		// C# has it's own gc, so nothing to do here...in theory...
		public static void LinyeeMFreeMem<T>(LinyeeState L, T b) { LinyeeMRealloc<T>(L, new T[] {b}, 0); }
		public static void LinyeeMFree<T>(LinyeeState L, T b) { LinyeeMRealloc<T>(L, new T[] {b}, 0); }
		public static void LinyeeMFreeArray<T>(LinyeeState L, T[] b) { LinyeeMReallocV(L, b, 0); }

		public static T LinyeeMMalloc<T>(LinyeeState L) { return (T)LinyeeMRealloc<T>(L); }
		public static T LinyeeMNew<T>(LinyeeState L) { return (T)LinyeeMRealloc<T>(L); }
		public static T[] LinyeeMNewVector<T>(LinyeeState L, int n)
		{
			return LinyeeMReallocV<T>(L, null, n);
		}

		public static void LinyeeMGrowVector<T>(LinyeeState L, ref T[] v, int nelems, ref int size, int limit, CharPtr e)
		{
			if (nelems + 1 > size)
				v = (T[])LinyeeMGrowAux(L, ref v, ref size, limit, e);
		}

		public static T[] LinyeeMReallocVector<T>(LinyeeState L, ref T[] v, int oldn, int n)
		{
			Debug.Assert((v == null && oldn == 0) || (v.Length == oldn));
			v = LinyeeMReallocV<T>(L, v, n);
			return v;
		}


		/*
		** About the realloc function:
		** void * frealloc (void *ud, void *ptr, uint osize, uint nsize);
		** (`osize' is the old size, `nsize' is the new size)
		**
		** Linyee ensures that (ptr == null) iff (osize == 0).
		**
		** * frealloc(ud, null, 0, x) creates a new block of size `x'
		**
		** * frealloc(ud, p, x, 0) frees the block `p'
		** (in this specific case, frealloc must return null).
		** particularly, frealloc(ud, null, 0, 0) does nothing
		** (which is equivalent to free(null) in ANSI C)
		**
		** frealloc returns null if it cannot create or reallocate the area
		** (any reallocation to an equal or smaller size cannot fail!)
		*/



		public const int MINSIZEARRAY	= 4;


		public static T[] LinyeeMGrowAux<T>(LinyeeState L, ref T[] block, ref int size,
							 int limit, CharPtr errormsg)
		{
			T[] newblock;
			int newsize;
			if (size >= limit / 2)
			{  /* cannot double it? */
				if (size >= limit)  /* cannot grow even a little? */
					LinyeeGRunError(L, errormsg);
				newsize = limit;  /* still have at least one free place */
			}
			else
			{
				newsize = size * 2;
				if (newsize < MINSIZEARRAY)
					newsize = MINSIZEARRAY;  /* minimum size */
			}
			newblock = LinyeeMReallocV<T>(L, block, newsize);
			size = newsize;  /* update only when everything else is OK */
			return newblock;
		}


		public static object LinyeeMTooBig (LinyeeState L) {
		  LinyeeGRunError(L, "memory allocation error: block too big");
		  return null;  /* to avoid warnings */
		}



		/*
		** generic allocation routine.
		*/

		public static object LinyeeMRealloc(LinyeeState L, Type t)
		{
			int unmanaged_size = (int)GetUnmanagedSize(t);
			int nsize = unmanaged_size;
			object new_obj = System.Activator.CreateInstance(t);
			AddTotalBytes(L, nsize);
			return new_obj;
		}

		public static object LinyeeMRealloc<T>(LinyeeState L)
		{
			int unmanaged_size = (int)GetUnmanagedSize(typeof(T));
			int nsize = unmanaged_size;
			T new_obj = (T)System.Activator.CreateInstance(typeof(T));
			AddTotalBytes(L, nsize);
			return new_obj;
		}

		public static object LinyeeMRealloc<T>(LinyeeState L, T obj)
		{
			int unmanaged_size = (int)GetUnmanagedSize(typeof(T));
			int old_size = (obj == null) ? 0 : unmanaged_size;
			int osize = old_size * unmanaged_size;
			int nsize = unmanaged_size;
			T new_obj = (T)System.Activator.CreateInstance(typeof(T));
			SubtractTotalBytes(L, osize);
			AddTotalBytes(L, nsize);
			return new_obj;
		}

		public static object LinyeeMRealloc<T>(LinyeeState L, T[] old_block, int new_size)
		{
			int unmanaged_size = (int)GetUnmanagedSize(typeof(T));
			int old_size = (old_block == null) ? 0 : old_block.Length;
			int osize = old_size * unmanaged_size;
			int nsize = new_size * unmanaged_size;
			T[] new_block = new T[new_size];
			for (int i = 0; i < Math.Min(old_size, new_size); i++)
				new_block[i] = old_block[i];
			for (int i = old_size; i < new_size; i++)
				new_block[i] = (T)System.Activator.CreateInstance(typeof(T));
			if (CanIndex(typeof(T)))
				for (int i = 0; i < new_size; i++)
				{
					ArrayElement elem = new_block[i] as ArrayElement;
					Debug.Assert(elem != null, String.Format("Need to derive type {0} from ArrayElement", typeof(T).ToString()));
					elem.SetIndex(i);
					elem.SetArray(new_block);
				}
			SubtractTotalBytes(L, osize);
			AddTotalBytes(L, nsize);
			return new_block;
		}

		public static bool CanIndex(Type t)
		{
			if (t == typeof(char))
				return false;
			if (t == typeof(byte))
				return false;
			if (t == typeof(int))
				return false;
			if (t == typeof(uint))
				return false;
			if (t == typeof(LocVar))
				return false;
			return true;
		}

		static void AddTotalBytes(LinyeeState L, int num_bytes) { G(L).totalbytes += (uint)num_bytes; }
		static void SubtractTotalBytes(LinyeeState L, int num_bytes) { G(L).totalbytes -= (uint)num_bytes; }

		static void AddTotalBytes(LinyeeState L, uint num_bytes) {G(L).totalbytes += num_bytes;}
		static void SubtractTotalBytes(LinyeeState L, uint num_bytes) {G(L).totalbytes -= num_bytes;}
	}
}
