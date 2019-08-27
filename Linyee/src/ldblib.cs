/*
** $Id: ldblib.c,v 1.104.1.3 2008/01/21 13:11:21 roberto Exp $
** Interface from Linyee to its debug API
** See Copyright Notice in Linyee.h
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Linyee
{
	public partial class Linyee
	{
		private static int DBGetRegistry (LinyeeState L) {
		  LinyeePushValue(L, LINYEE_REGISTRYINDEX);
		  return 1;
		}


		private static int DBGetMetatable (LinyeeState L) {
		  LinyeeLCheckAny(L, 1);
		  if (LinyeeGetMetatable(L, 1) == 0) {
			LinyeePushNil(L);  /* no metatable */
		  }
		  return 1;
		}


		private static int DBSetMetatable (LinyeeState L) {
		  int t = LinyeeType(L, 2);
		  LinyeeLArgCheck(L, t == LINYEE_TNIL || t == LINYEE_TTABLE, 2,
							"nil or table expected");
		  LinyeeSetTop(L, 2);
		  LinyeePushBoolean(L, LinyeeSetMetatable(L, 1));
		  return 1;
		}


		private static int DBGetFEnv (LinyeeState L) {
		  LinyeeLCheckAny(L, 1);
		  LinyeeGetFEnv(L, 1);
		  return 1;
		}


		private static int DBSetFEnv (LinyeeState L) {
		  LinyeeLCheckType(L, 2, LINYEE_TTABLE);
		  LinyeeSetTop(L, 2);
		  if (LinyeeSetFEnv(L, 1) == 0)
			LinyeeLError(L, LINYEE_QL("setfenv") +
						  " cannot change environment of given object");
		  return 1;
		}


		private static void SetTabsS (LinyeeState L, CharPtr i, CharPtr v) {
		  LinyeePushString(L, v);
		  LinyeeSetField(L, -2, i);
		}


		private static void SetTabSI (LinyeeState L, CharPtr i, int v) {
		  LinyeePushInteger(L, v);
		  LinyeeSetField(L, -2, i);
		}


		private static LinyeeState GetThread (LinyeeState L, out int arg) {
		  if (LinyeeIsThread(L, 1)) {
			arg = 1;
			return LinyeeToThread(L, 1);
		  }
		  else {
			arg = 0;
			return L;
		  }
		}


		private static void TreatStackOption (LinyeeState L, LinyeeState L1, CharPtr fname) {
		  if (L == L1) {
			LinyeePushValue(L, -2);
			LinyeeRemove(L, -3);
		  }
		  else
			LinyeeXMove(L1, L, 1);
		  LinyeeSetField(L, -2, fname);
		}


		private static int DBGetInfo (LinyeeState L) {
		  LinyeeDebug ar = new LinyeeDebug();
		  int arg;
		  LinyeeState L1 = GetThread(L, out arg);
		  CharPtr options = LinyeeLOptString(L, arg+2, "flnSu");
		  if (LinyeeIsNumber(L, arg+1) != 0) {
			if (LinyeeGetStack(L1, (int)LinyeeToInteger(L, arg+1), ref ar)==0) {
			  LinyeePushNil(L);  /* level out of range */
			  return 1;
			}
		  }
		  else if (LinyeeIsFunction(L, arg+1)) {
			LinyeePushFString(L, ">%s", options);
			options = LinyeeToString(L, -1);
			LinyeePushValue(L, arg+1);
			LinyeeXMove(L, L1, 1);
		  }
		  else
			return LinyeeLArgError(L, arg+1, "function or level expected");
		  if (LinyeeGetInfo(L1, options,ref ar)==0)
			return LinyeeLArgError(L, arg+2, "invalid option");
		  LinyeeCreateTable(L, 0, 2);
		  if (strchr(options, 'S') != null) {
			SetTabsS(L, "source", ar.source);
			SetTabsS(L, "short_src", ar.short_src);
			SetTabSI(L, "linedefined", ar.linedefined);
			SetTabSI(L, "lastlinedefined", ar.lastlinedefined);
			SetTabsS(L, "what", ar.what);
		  }
		  if (strchr(options, 'l') != null)
			SetTabSI(L, "currentline", ar.currentline);
		  if (strchr(options, 'u')  != null)
			SetTabSI(L, "nups", ar.nups);
		  if (strchr(options, 'n')  != null) {
			SetTabsS(L, "name", ar.name);
			SetTabsS(L, "namewhat", ar.namewhat);
		  }
		  if (strchr(options, 'L') != null)
			TreatStackOption(L, L1, "activelines");
		  if (strchr(options, 'f')  != null)
			TreatStackOption(L, L1, "func");
		  return 1;  /* return table */
		}
		    

		private static int DBGetLocal (LinyeeState L) {
		  int arg;
		  LinyeeState L1 = GetThread(L, out arg);
		  LinyeeDebug ar = new LinyeeDebug();
		  CharPtr name;
		  if (LinyeeGetStack(L1, LinyeeLCheckInt(L, arg+1), ref ar)==0)  /* out of range? */
			return LinyeeLArgError(L, arg+1, "level out of range");
		  name = LinyeeGetLocal(L1, ar, LinyeeLCheckInt(L, arg+2));
		  if (name != null) {
			LinyeeXMove(L1, L, 1);
			LinyeePushString(L, name);
			LinyeePushValue(L, -2);
			return 2;
		  }
		  else {
			LinyeePushNil(L);
			return 1;
		  }
		}


		private static int DBSetLocal (LinyeeState L) {
		  int arg;
		  LinyeeState L1 = GetThread(L, out arg);
		  LinyeeDebug ar = new LinyeeDebug();
		  if (LinyeeGetStack(L1, LinyeeLCheckInt(L, arg+1), ref ar)==0)  /* out of range? */
			return LinyeeLArgError(L, arg+1, "level out of range");
		  LinyeeLCheckAny(L, arg+3);
		  LinyeeSetTop(L, arg+3);
		  LinyeeXMove(L, L1, 1);
		  LinyeePushString(L, LinyeeSetLocal(L1, ar, LinyeeLCheckInt(L, arg+2)));
		  return 1;
		}


		private static int AuxUpValue (LinyeeState L, int get) {
		  CharPtr name;
		  int n = LinyeeLCheckInt(L, 2);
		  LinyeeLCheckType(L, 1, LINYEE_TFUNCTION);
		  if (LinyeeIsCFunction(L, 1)) return 0;  /* cannot touch C upvalues from Linyee */
		  name = (get!=0) ? LinyeeGetUpValue(L, 1, n) : LinyeeSetUpValue(L, 1, n);
		  if (name == null) return 0;
		  LinyeePushString(L, name);
		  LinyeeInsert(L, -(get+1));
		  return get + 1;
		}


		private static int DBGetUpValue (LinyeeState L) {
		  return AuxUpValue(L, 1);
		}


		private static int DBSetUpValue (LinyeeState L) {
		  LinyeeLCheckAny(L, 3);
		  return AuxUpValue(L, 0);
		}



		private const string KEY_HOOK = "h";


		private static readonly string[] hooknames =
			{"call", "return", "line", "count", "tail return"};

		private static void HookF (LinyeeState L, LinyeeDebug ar) {
		  LinyeePushLightUserData(L, KEY_HOOK);
		  LinyeeRawGet(L, LINYEE_REGISTRYINDEX);
		  LinyeePushLightUserData(L, L);
		  LinyeeRawGet(L, -2);
		  if (LinyeeIsFunction(L, -1)) {
			LinyeePushString(L, hooknames[(int)ar.event_]);
			if (ar.currentline >= 0)
			  LinyeePushInteger(L, ar.currentline);
			else LinyeePushNil(L);
			LinyeeAssert(LinyeeGetInfo(L, "lS",ref ar));
			LinyeeCall(L, 2, 0);
		  }
		}


		private static int MakeMask (CharPtr smask, int count) {
		  int mask = 0;
		  if (strchr(smask, 'c') != null) mask |= LINYEE_MASKCALL;
		  if (strchr(smask, 'r') != null) mask |= LINYEE_MASKRET;
		  if (strchr(smask, 'l') != null) mask |= LINYEE_MASKLINE;
		  if (count > 0) mask |= LINYEE_MASKCOUNT;
		  return mask;
		}


		private static CharPtr UnmakeMask (int mask, CharPtr smask) {
			int i = 0;
			if ((mask & LINYEE_MASKCALL) != 0) smask[i++] = 'c';
			if ((mask & LINYEE_MASKRET) != 0) smask[i++] = 'r';
			if ((mask & LINYEE_MASKLINE) != 0) smask[i++] = 'l';
			smask[i] = '\0';
			return smask;
		}


		private static void GetHookTable (LinyeeState L) {
		  LinyeePushLightUserData(L, KEY_HOOK);
		  LinyeeRawGet(L, LINYEE_REGISTRYINDEX);
		  if (!LinyeeIsTable(L, -1)) {
			LinyeePop(L, 1);
			LinyeeCreateTable(L, 0, 1);
			LinyeePushLightUserData(L, KEY_HOOK);
			LinyeePushValue(L, -2);
			LinyeeRawSet(L, LINYEE_REGISTRYINDEX);
		  }
		}


		private static int DBSetHook (LinyeeState L) {
		  int arg, mask, count;
		  LinyeeHook func;
		  LinyeeState L1 = GetThread(L, out arg);
		  if (LinyeeIsNoneOrNil(L, arg+1)) {
			LinyeeSetTop(L, arg+1);
			func = null; mask = 0; count = 0;  /* turn off hooks */
		  }
		  else {
			CharPtr smask = LinyeeLCheckString(L, arg+2);
			LinyeeLCheckType(L, arg+1, LINYEE_TFUNCTION);
			count = LinyeeLOptInt(L, arg+3, 0);
			func = HookF; mask = MakeMask(smask, count);
		  }
		  GetHookTable(L);
		  LinyeePushLightUserData(L, L1);
		  LinyeePushValue(L, arg+1);
		  LinyeeRawSet(L, -3);  /* set new hook */
		  LinyeePop(L, 1);  /* remove hook table */
		  LinyeeSetHook(L1, func, mask, count);  /* set hooks */
		  return 0;
		}


		private static int DBGetHook (LinyeeState L) {
		  int arg;
		  LinyeeState L1 = GetThread(L, out arg);
		  CharPtr buff = new char[5];
		  int mask = LinyeeGetHookMask(L1);
		  LinyeeHook hook = LinyeeGetHook(L1);
		  if (hook != null && hook != HookF)  /* external hook? */
			LinyeePushLiteral(L, "external hook");
		  else {
			GetHookTable(L);
			LinyeePushLightUserData(L, L1);
			LinyeeRawGet(L, -2);   /* get hook */
			LinyeeRemove(L, -2);  /* remove hook table */
		  }
		  LinyeePushString(L, UnmakeMask(mask, buff));
		  LinyeePushInteger(L, LinyeeGetHookCount(L1));
		  return 3;
		}


		private static int DBDebug (LinyeeState L) {
		  for (;;) {
			CharPtr buffer = new char[250];
			fputs("ly_debug> ", stderr);
			if (fgets(buffer, stdin) == null ||
				strcmp(buffer, "cont\n") == 0)
			  return 0;
			if (LinyeeLLoadBuffer(L, buffer, (uint)strlen(buffer), "=(debug command)")!=0 ||
				LinyeePCall(L, 0, 0, 0)!=0) {
			  fputs(LinyeeToString(L, -1), stderr);
			  fputs("\n", stderr);
			}
			LinyeeSetTop(L, 0);  /* remove eventual returns */
		  }
		}


		public const int LEVELS1	= 12;	/* size of the first part of the stack */
		public const int LEVELS2	= 10;	/* size of the second part of the stack */

		private static int DBErrorFB (LinyeeState L) {
		  int level;
		  bool firstpart = true;  /* still before eventual `...' */
		  int arg;
		  LinyeeState L1 = GetThread(L, out arg);
		  LinyeeDebug ar = new LinyeeDebug();
		  if (LinyeeIsNumber(L, arg+2) != 0) {
			level = (int)LinyeeToInteger(L, arg+2);
			LinyeePop(L, 1);
		  }
		  else
			level = (L == L1) ? 1 : 0;  /* level 0 may be this own function */
		  if (LinyeeGetTop(L) == arg)
			LinyeePushLiteral(L, "");
		  else if (LinyeeIsString(L, arg+1)==0) return 1;  /* message is not a string */
		  else LinyeePushLiteral(L, "\n");
		  LinyeePushLiteral(L, "stack traceback:");
		  while (LinyeeGetStack(L1, level++, ref ar) != 0) {
			if (level > LEVELS1 && firstpart) {
			  /* no more than `LEVELS2' more levels? */
			  if (LinyeeGetStack(L1, level+LEVELS2, ref ar)==0)
				level--;  /* keep going */
			  else {
				LinyeePushLiteral(L, "\n\t...");  /* too many levels */
				while (LinyeeGetStack(L1, level+LEVELS2, ref ar) != 0)  /* find last levels */
				  level++;
			  }
			  firstpart = false;
			  continue;
			}
			LinyeePushLiteral(L, "\n\t");
			LinyeeGetInfo(L1, "Snl", ref ar);
			LinyeePushFString(L, "%s:", ar.short_src);
			if (ar.currentline > 0)
			  LinyeePushFString(L, "%d:", ar.currentline);
			if (ar.namewhat != '\0')  /* is there a name? */
				LinyeePushFString(L, " in function " + LINYEE_QS, ar.name);
			else {
			  if (ar.what == 'm')  /* main? */
				LinyeePushFString(L, " in main chunk");
			  else if (ar.what == 'C' || ar.what == 't')
				LinyeePushLiteral(L, " ?");  /* C function or tail call */
			  else
				LinyeePushFString(L, " in function <%s:%d>",
								   ar.short_src, ar.linedefined);
			}
			LinyeeConcat(L, LinyeeGetTop(L) - arg);
		  }
		  LinyeeConcat(L, LinyeeGetTop(L) - arg);
		  return 1;
		}


		private readonly static LinyeeLReg[] dblib = {
		  new LinyeeLReg("debug", DBDebug),
		  new LinyeeLReg("getfenv", DBGetFEnv),
		  new LinyeeLReg("gethook", DBGetHook),
		  new LinyeeLReg("getinfo", DBGetInfo),
		  new LinyeeLReg("getlocal", DBGetLocal),
		  new LinyeeLReg("getregistry", DBGetRegistry),
		  new LinyeeLReg("getmetatable", DBGetMetatable),
		  new LinyeeLReg("getupvalue", DBGetUpValue),
		  new LinyeeLReg("setfenv", DBSetFEnv),
		  new LinyeeLReg("sethook", DBSetHook),
		  new LinyeeLReg("setlocal", DBSetLocal),
		  new LinyeeLReg("setmetatable", DBSetMetatable),
		  new LinyeeLReg("setupvalue", DBSetUpValue),
		  new LinyeeLReg("traceback", DBErrorFB),
		  new LinyeeLReg(null, null)
		};


		public static int LinyeeOpenDebug (LinyeeState L) {
		  LinyeeLRegister(L, LINYEE_DBLIBNAME, dblib);
		  return 1;
		}

	}
}
