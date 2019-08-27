using System;

namespace Linyee
{
	using ly_byte = System.Byte;
	using ly_int32 = System.Int32;
	using ly_mem = System.UInt32;
	using TValue = Linyee.LinyeeTypeValue;
	using StkId = Linyee.LinyeeTypeValue;
	using ptrdiff_t = System.Int32;
	using Instruction = System.UInt32;

    /// <summary>
    /// 'per thread' state
    /// 每个线程的状态
    /// </summary>
    public class LinyeeState : Linyee.GCObject {

		public ly_byte status;
		public StkId top;  /* first free slot in the stack */
		public StkId base_;  /* base of current function */
		public Linyee.GlobalState l_G;
		public Linyee.CallInfo ci;  /* call info for current function */
		public InstructionPtr savedpc = new InstructionPtr();  /* `savedpc' of current function */
		public StkId stack_last;  /* last free slot in the stack */
		public StkId[] stack;  /* stack base */
		public Linyee.CallInfo end_ci;  /* points after end of ci array*/
		public Linyee.CallInfo[] base_ci;  /* array of CallInfo's */
		public int stacksize;
		public int size_ci;  /* size of array `base_ci' */
		[CLSCompliantAttribute(false)]
		public ushort nCcalls;  /* number of nested C calls */
		[CLSCompliantAttribute(false)]
		public ushort baseCcalls;  /* nested C calls when resuming coroutine */
		public ly_byte hookmask;
		public ly_byte allowhook;
		public int basehookcount;
		public int hookcount;
		public LinyeeHook hook;
		public TValue l_gt = new Linyee.LinyeeTypeValue();  /* table of globals */
		public TValue env = new Linyee.LinyeeTypeValue();  /* temporary place for environments */
		public Linyee.GCObject openupval;  /* list of open upvalues in this stack */
		public Linyee.GCObject gclist;
		public Linyee.LinyeeLongJmp errorJmp;  /* current error recover point */
		public ptrdiff_t errfunc;  /* current error handling function (stack index) */
	}
}
