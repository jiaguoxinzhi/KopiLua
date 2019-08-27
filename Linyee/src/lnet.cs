using System;

namespace Linyee
{
	public partial class Linyee
	{
		private static object tag = 0;

		public static void LinyeePushStdCallCFunction (LinyeeState luaState, LinyeeNativeFunction function)
		{
			LinyeePushCFunction (luaState, function);
		}

		public static bool LinyeeLCheckMetatable (LinyeeState luaState, int index)
		{
			bool retVal = false;
			
			if (LinyeeGetMetatable (luaState, index) != 0) {
				LinyeePushLightUserData (luaState, tag);
				LinyeeRawGet (luaState, -2);
				retVal = !LinyeeIsNil (luaState, -1);
				LinyeeSetTop (luaState, -3);
			}
			
			return retVal;
		}

		public static LinyeeTag LinyeeNetGetTag ()
		{
			return new LinyeeTag (tag);
		}

		public static void LinyeePushLightUserData (LinyeeState L, LinyeeTag p)
		{
			LinyeePushLightUserData (L, p.Tag);
		}

		// Starting with 5.1 the auxlib version of checkudata throws an exception if the type isn't right
		// Instead, we want to run our own version that checks the type and just returns null for failure
		private static object CheckUserDataRaw (LinyeeState L, int ud, string tname)
		{
			object p = LinyeeToUserData (L, ud);
			
			if (p != null) {
				/* value is a userdata? */
				if (LinyeeGetMetatable (L, ud) != 0) { 
					bool isEqual;
					
					/* does it have a metatable? */
					LinyeeGetField (L, LINYEE_REGISTRYINDEX, tname);  /* get correct metatable */
					
					isEqual = LinyeeRawEqual (L, -1, -2) != 0;
					
					// NASTY - we need our own version of the ly_pop macro
					// ly_pop(L, 2);  /* remove both metatables */
					LinyeeSetTop (L, -(2) - 1);
					
					if (isEqual)	/* does it have the correct mt? */
						return p;
				}
			}
			
			return null;
		}


		public static int LinyeeNetCheckUData (LinyeeState luaState, int ud, string tname)
		{
			object udata = CheckUserDataRaw (luaState, ud, tname);
			return udata != null ? FourBytesToInt (udata as byte[]) : -1;
		}

		public static int LinyeeNetToNetObject (LinyeeState luaState, int index)
		{
			byte[] udata;
			
			if (LinyeeType (luaState, index) == LINYEE_TUSERDATA) {
				if (LinyeeLCheckMetatable (luaState, index)) {
					udata = LinyeeToUserData (luaState, index) as byte[];
					if (udata != null)
						return FourBytesToInt (udata);
				}
				
				udata = CheckUserDataRaw (luaState, index, "luaNet_class") as byte[];
				if (udata != null)
					return FourBytesToInt (udata);
				
				udata = CheckUserDataRaw (luaState, index, "luaNet_searchbase") as byte[];
				if (udata != null)
					return FourBytesToInt (udata);
				
				udata = CheckUserDataRaw (luaState, index, "luaNet_function") as byte[];
				if (udata != null)
					return FourBytesToInt (udata);
			}
			
			return -1;
		}

		public static void LinyeeNetNewUData (LinyeeState luaState, int val)
		{
			var userdata = LinyeeNewUserData (luaState, sizeof(int)) as byte[];
			IntToFourBytes (val, userdata);
		}

		public static int LinyeeNetRawNetObj (LinyeeState luaState, int obj)
		{
			byte[] bytes = LinyeeToUserData (luaState, obj) as byte[];
			if (bytes == null)
				return -1;
			return FourBytesToInt (bytes);
		}

		private static int FourBytesToInt (byte [] bytes)
		{
			return bytes [0] + (bytes [1] << 8) + (bytes [2] << 16) + (bytes [3] << 24);
		}

		private static void IntToFourBytes (int val, byte [] bytes)
		{
			// gfoot: is this really a good idea?
			bytes [0] = (byte)val;
			bytes [1] = (byte)(val >> 8);
			bytes [2] = (byte)(val >> 16);
			bytes [3] = (byte)(val >> 24);
		}

		/* Compatibility functions to allow NLinyee work with Linyee 5.1.5 and Linyee 5.2.2 with the same dll interface.
		 * Linyee methods to match KeraLinyee API */ 

		public static int LinyeeNetRegistryIndex ()
		{
			return LINYEE_REGISTRYINDEX;
		}

		public static void LinyeeNetPushGlobalTable (LinyeeState L) 
		{
			LinyeePushValue (L, LINYEE_GLOBALSINDEX);
		}

		public static void LinyeeNetPopGlobalTable (LinyeeState L)
		{
			LinyeeReplace (L, LINYEE_GLOBALSINDEX);
		}

		public static void LinyeeNetSetGlobal (LinyeeState L, string name)
		{
			LinyeeSetGlobal (L, name);
		}

		public static void LinyeeNetGetGlobal (LinyeeState L, string name)
		{
			LinyeeGetGlobal (L, name);
		}
		
		public static int LinyeeNetPCall (LinyeeState L, int nargs, int nresults, int errfunc)
		{
			return LinyeePCall (L, nargs, nresults, errfunc);
		}

		[CLSCompliantAttribute (false)]
		public static int LinyeeNetLoadBuffer (LinyeeState L, string buff, uint sz, string name)
		{
			if (sz == 0)
				sz = (uint) strlen (buff);
			return LinyeeLLoadBuffer (L, buff, sz, name);
		}

		[CLSCompliantAttribute (false)]
		public static int LinyeeNetLoadBuffer (LinyeeState L, byte [] buff, uint sz, string name)
		{
			return LinyeeLLoadBuffer (L, buff, sz, name);
		}

		public static int LinyeeNetLoadFile (LinyeeState L, string file)
		{
			return LinyeeLLoadFile (L, file);
		}

		public static double LinyeeNetToNumber (LinyeeState L, int idx)
		{
			return LinyeeToNumber (L, idx);
		}

		public static int LinyeeNetEqual (LinyeeState L, int idx1, int idx2)
		{
			return LinyeeEqual (L, idx1, idx2);
		}

		[CLSCompliantAttribute (false)]
		public static void LinyeeNetPushLString (LinyeeState L, string s, uint len)
		{
			LinyeePushLString (L, s, len);
		}

		public static int LinyeeNetIsStringStrict (LinyeeState L, int idx)
		{
			int t = LinyeeType (L, idx);
			return (t == LINYEE_TSTRING) ? 1 : 0;
		}

		public static LinyeeState LinyeeNetGetMainState (LinyeeState L1)
		{
			LinyeeGetField (L1, LINYEE_REGISTRYINDEX, "main_state");
			LinyeeState main = LinyeeToThread (L1, -1);
			LinyeePop (L1, 1);
			return main;
		}
	}
}

