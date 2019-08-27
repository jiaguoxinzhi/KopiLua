/*
** $Id: ltablib.c,v 1.38.1.3 2008/02/14 16:46:58 roberto Exp $
** Library for Table Manipulation
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Linyee
{
	using ly_Number = System.Double;

	public partial class Linyee
	{
		private static int aux_getn(LinyeeState L, int n)	{LinyeeLCheckType(L, n, LINYEE_TTABLE); return LinyeeLGetN(L, n);}

		private static int foreachi (LinyeeState L) {
		  int i;
		  int n = aux_getn(L, 1);
		  LinyeeLCheckType(L, 2, LINYEE_TFUNCTION);
		  for (i=1; i <= n; i++) {
			LinyeePushValue(L, 2);  /* function */
			LinyeePushInteger(L, i);  /* 1st argument */
			LinyeeRawGetI(L, 1, i);  /* 2nd argument */
			LinyeeCall(L, 2, 1);
			if (!LinyeeIsNil(L, -1))
			  return 1;
			LinyeePop(L, 1);  /* remove nil result */
		  }
		  return 0;
		}


		private static int _foreach (LinyeeState L) {
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		  LinyeeLCheckType(L, 2, LINYEE_TFUNCTION);
		  LinyeePushNil(L);  /* first key */
		  while (LinyeeNext(L, 1) != 0) {
			LinyeePushValue(L, 2);  /* function */
			LinyeePushValue(L, -3);  /* key */
			LinyeePushValue(L, -3);  /* value */
			LinyeeCall(L, 2, 1);
			if (!LinyeeIsNil(L, -1))
			  return 1;
			LinyeePop(L, 2);  /* remove value and result */
		  }
		  return 0;
		}


		private static int maxn (LinyeeState L) {
		  ly_Number max = 0;
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		  LinyeePushNil(L);  /* first key */
		  while (LinyeeNext(L, 1) != 0) {
			LinyeePop(L, 1);  /* remove value */
			if (LinyeeType(L, -1) == LINYEE_TNUMBER) {
			  ly_Number v = LinyeeToNumber(L, -1);
			  if (v > max) max = v;
			}
		  }
		  LinyeePushNumber(L, max);
		  return 1;
		}


		private static int getn (LinyeeState L) {
		  LinyeePushInteger(L, aux_getn(L, 1));
		  return 1;
		}


		private static int setn (LinyeeState L) {
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		//#ifndef luaL_setn
		  //luaL_setn(L, 1, luaL_checkint(L, 2));
		//#else
		  LinyeeLError(L, LINYEE_QL("setn") + " is obsolete");
		//#endif
		  LinyeePushValue(L, 1);
		  return 1;
		}


		private static int tinsert (LinyeeState L) {
		  int e = aux_getn(L, 1) + 1;  /* first empty element */
		  int pos;  /* where to insert new element */
		  switch (LinyeeGetTop(L)) {
			case 2: {  /* called with only 2 arguments */
			  pos = e;  /* insert new element at the end */
			  break;
			}
			case 3: {
			  int i;
			  pos = LinyeeLCheckInt(L, 2);  /* 2nd argument is the position */
			  if (pos > e) e = pos;  /* `grow' array if necessary */
			  for (i = e; i > pos; i--) {  /* move up elements */
				LinyeeRawGetI(L, 1, i-1);
				LinyeeRawSetI(L, 1, i);  /* t[i] = t[i-1] */
			  }
			  break;
			}
			default: {
			  return LinyeeLError(L, "wrong number of arguments to " + LINYEE_QL("insert"));
			}
		  }
		  LinyeeLSetN(L, 1, e);  /* new size */
		  LinyeeRawSetI(L, 1, pos);  /* t[pos] = v */
		  return 0;
		}


		private static int tremove (LinyeeState L) {
		  int e = aux_getn(L, 1);
		  int pos = LinyeeLOptInt(L, 2, e);
		  if (!(1 <= pos && pos <= e))  /* position is outside bounds? */
		   return 0;  /* nothing to remove */
		  LinyeeLSetN(L, 1, e - 1);  /* t.n = n-1 */
		  LinyeeRawGetI(L, 1, pos);  /* result = t[pos] */
		  for ( ;pos<e; pos++) {
			LinyeeRawGetI(L, 1, pos+1);
			LinyeeRawSetI(L, 1, pos);  /* t[pos] = t[pos+1] */
		  }
		  LinyeePushNil(L);
		  LinyeeRawSetI(L, 1, e);  /* t[e] = nil */
		  return 1;
		}


		private static void addfield (LinyeeState L, LinyeeLBuffer b, int i) {
		  LinyeeRawGetI(L, 1, i);
		  if (LinyeeIsString(L, -1)==0)
			LinyeeLError(L, "invalid value (%s) at index %d in table for " +
						  LINYEE_QL("concat"), LinyeeLTypeName(L, -1), i);
			LinyeeLAddValue(b);
		}


		private static int tconcat (LinyeeState L) {
		  LinyeeLBuffer b = new LinyeeLBuffer();
		  uint lsep;
		  int i, last;
		  CharPtr sep = LinyeeLOptLString(L, 2, "", out lsep);
		  LinyeeLCheckType(L, 1, LINYEE_TTABLE);
		  i = LinyeeLOptInt(L, 3, 1);
		  last = LinyeeLOptInteger(L, LinyeeLCheckInt, 4, LinyeeLGetN(L, 1));
		  LinyeeLBuffInit(L, b);
		  for (; i < last; i++) {
			addfield(L, b, i);
			LinyeeLAddLString(b, sep, lsep);
		  }
		  if (i == last)  /* add last value (if interval was not empty) */
			addfield(L, b, i);
		  LinyeeLPushResult(b);
		  return 1;
		}



		/*
		** {======================================================
		** Quicksort
		** (based on `Algorithms in MODULA-3', Robert Sedgewick;
		**  Addison-Wesley, 1993.)
		*/


		private static void set2 (LinyeeState L, int i, int j) {
		  LinyeeRawSetI(L, 1, i);
		  LinyeeRawSetI(L, 1, j);
		}

		private static int sort_comp (LinyeeState L, int a, int b) {
		  if (!LinyeeIsNil(L, 2)) {  /* function? */
			int res;
			LinyeePushValue(L, 2);
			LinyeePushValue(L, a-1);  /* -1 to compensate function */
			LinyeePushValue(L, b-2);  /* -2 to compensate function and `a' */
			LinyeeCall(L, 2, 1);
			res = LinyeeToBoolean(L, -1);
			LinyeePop(L, 1);
			return res;
		  }
		  else  /* a < b? */
			return LinyeeLessThan(L, a, b);
		}

		private static int auxsort_loop1(LinyeeState L, ref int i)
		{
			LinyeeRawGetI(L, 1, ++i);
			return sort_comp(L, -1, -2);
		}

		private static int auxsort_loop2(LinyeeState L, ref int j)
		{
			LinyeeRawGetI(L, 1, --j);
			return sort_comp(L, -3, -1);
		}

		private static void auxsort (LinyeeState L, int l, int u) {
		  while (l < u) {  /* for tail recursion */
			int i, j;
			/* sort elements a[l], a[(l+u)/2] and a[u] */
			LinyeeRawGetI(L, 1, l);
			LinyeeRawGetI(L, 1, u);
			if (sort_comp(L, -1, -2) != 0)  /* a[u] < a[l]? */
			  set2(L, l, u);  /* swap a[l] - a[u] */
			else
			  LinyeePop(L, 2);
			if (u-l == 1) break;  /* only 2 elements */
			i = (l+u)/2;
			LinyeeRawGetI(L, 1, i);
			LinyeeRawGetI(L, 1, l);
			if (sort_comp(L, -2, -1) != 0)  /* a[i]<a[l]? */
			  set2(L, i, l);
			else {
			  LinyeePop(L, 1);  /* remove a[l] */
			  LinyeeRawGetI(L, 1, u);
			  if (sort_comp(L, -1, -2) != 0)  /* a[u]<a[i]? */
				set2(L, i, u);
			  else
				LinyeePop(L, 2);
			}
			if (u-l == 2) break;  /* only 3 elements */
			LinyeeRawGetI(L, 1, i);  /* Pivot */
			LinyeePushValue(L, -1);
			LinyeeRawGetI(L, 1, u-1);
			set2(L, i, u-1);
			/* a[l] <= P == a[u-1] <= a[u], only need to sort from l+1 to u-2 */
			i = l; j = u-1;
			for (;;) {  /* invariant: a[l..i] <= P <= a[j..u] */
			  /* repeat ++i until a[i] >= P */
			  while (auxsort_loop1(L, ref i) != 0) {
				if (i>u) LinyeeLError(L, "invalid order function for sorting");
				LinyeePop(L, 1);  /* remove a[i] */
			  }
			  /* repeat --j until a[j] <= P */
			  while (auxsort_loop2(L, ref j) != 0) {
				if (j<l) LinyeeLError(L, "invalid order function for sorting");
				LinyeePop(L, 1);  /* remove a[j] */
			  }
			  if (j<i) {
				LinyeePop(L, 3);  /* pop pivot, a[i], a[j] */
				break;
			  }
			  set2(L, i, j);
			}
			LinyeeRawGetI(L, 1, u-1);
			LinyeeRawGetI(L, 1, i);
			set2(L, u-1, i);  /* swap pivot (a[u-1]) with a[i] */
			/* a[l..i-1] <= a[i] == P <= a[i+1..u] */
			/* adjust so that smaller half is in [j..i] and larger one in [l..u] */
			if (i-l < u-i) {
			  j=l; i=i-1; l=i+2;
			}
			else {
			  j=i+1; i=u; u=j-2;
			}
			auxsort(L, j, i);  /* call recursively the smaller one */
		  }  /* repeat the routine for the larger one */
		}

		private static int sort (LinyeeState L) {
		  int n = aux_getn(L, 1);
		  LinyeeLCheckStack(L, 40, "");  /* assume array is smaller than 2^40 */
		  if (!LinyeeIsNoneOrNil(L, 2))  /* is there a 2nd argument? */
			LinyeeLCheckType(L, 2, LINYEE_TFUNCTION);
		  LinyeeSetTop(L, 2);  /* make sure there is two arguments */
		  auxsort(L, 1, n);
		  return 0;
		}

		/* }====================================================== */


		private readonly static LinyeeLReg[] tab_funcs = {
		  new LinyeeLReg("concat", tconcat),
		  new LinyeeLReg("foreach", _foreach),
		  new LinyeeLReg("foreachi", foreachi),
		  new LinyeeLReg("getn", getn),
		  new LinyeeLReg("maxn", maxn),
		  new LinyeeLReg("insert", tinsert),
		  new LinyeeLReg("remove", tremove),
		  new LinyeeLReg("setn", setn),
		  new LinyeeLReg("sort", sort),
		  new LinyeeLReg(null, null)
		};


		public static int luaopen_table (LinyeeState L) {
		  LinyeeLRegister(L, LINYEE_TABLIBNAME, tab_funcs);
		  return 1;
		}

	}
}
