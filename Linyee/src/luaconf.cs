/*
** $Id: luaconf.h,v 1.82.1.7 2008/02/11 16:25:08 roberto Exp $
** Configuration file for Linyee
** See Copyright Notice in Linyee.h
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Linyee
{
	using LINYEE_INTEGER	= System.Int32;
	using LINYEE_NUMBER	= System.Double;
	using LUAI_UACNUMBER	= System.Double;
	using LINYEE_INTFRM_T		= System.Int64;
	using TValue = Linyee.LinyeeTypeValue;
	using ly_Number = System.Double;
    using System.Globalization;
    using global::Linyee.AT;

    public partial class Linyee
	{
		/*
		** ==================================================================
		** Search for "@@" to find all configurable definitions.
		** ===================================================================
		*/


		/*
		@@ LINYEE_ANSI controls the use of non-ansi features.
		** CHANGE it (define it) if you want Linyee to avoid the use of any
		** non-ansi feature or library.
		*/
		//#if defined(__STRICT_ANSI__)
		//#define LINYEE_ANSI
		//#endif


		//#if !defined(LINYEE_ANSI) && _WIN32
		//#define LINYEE_WIN
		//#endif

		//#if defined(LINYEE_USE_LINUX)
		//#define LINYEE_USE_POSIX
		//#define LINYEE_USE_DLOPEN		/* needs an extra library: -ldl */
		//#define LINYEE_USE_READLINE	/* needs some extra libraries */
		//#endif

		//#if defined(LINYEE_USE_MACOSX)
		//#define LINYEE_USE_POSIX
		//#define LINYEE_DL_DYLD		/* does not need extra library */
		//#endif



		/*
		@@ LINYEE_USE_POSIX includes all functionallity listed as X/Open System
		@* Interfaces Extension (XSI).
		** CHANGE it (define it) if your system is XSI compatible.
		*/
		//#if defined(LINYEE_USE_POSIX)
		//#define LINYEE_USE_MKSTEMP
		//#define LINYEE_USE_ISATTY
		//#define LINYEE_USE_POPEN
		//#define LINYEE_USE_ULONGJMP
		//#endif


		/*
		@@ LINYEE_PATH and LINYEE_CPATH are the names of the environment variables that
		@* Linyee check to set its paths.
		@@ LINYEE_INIT is the name of the environment variable that Linyee
		@* checks for initialization code.
		** CHANGE them if you want different names.
		*/
		public const string LINYEE_PATH = "LINYEE_PATH";
		public const string LINYEE_CPATH = "LINYEE_CPATH";
		public const string LINYEE_INIT = "LINYEE_INIT";


		/*
		@@ LINYEE_PATH_DEFAULT is the default path that Linyee uses to look for
		@* Linyee libraries.
		@@ LINYEE_CPATH_DEFAULT is the default path that Linyee uses to look for
		@* C libraries.
		** CHANGE them if your machine has a non-conventional directory
		** hierarchy or if you want to install your libraries in
		** non-conventional directories.
		*/
		public static readonly string LINYEE_ROOT;
		public static readonly string LINYEE_LDIR;
		public static readonly string LINYEE_CDIR;
		public static readonly string LINYEE_PATH_DEFAULT;
		public static readonly string LINYEE_CPATH_DEFAULT;

		//#if _WIN32
		/*
		** In Windows, any exclamation mark ('!') in the path is replaced by the
		** path of the directory of the executable file of the current process.
		*/
		private const string WIN32_LINYEE_LDIR = "!\\Linyee\\";
		private const string WIN32_LINYEE_CDIR = "!\\";
		private const string WIN32_LINYEE_PATH_DEFAULT =
			".\\?.Linyee;"  + WIN32_LINYEE_LDIR + "?.Linyee;"  + WIN32_LINYEE_LDIR + "?\\init.Linyee;"
				+ WIN32_LINYEE_CDIR + "?.Linyee;"  + WIN32_LINYEE_CDIR + "?\\init.Linyee";
		private const string WIN32_LINYEE_CPATH_DEFAULT =
			".\\?.dll;"  + WIN32_LINYEE_CDIR + "?.dll;" + WIN32_LINYEE_CDIR + "loadall.dll";

		//#else
		private const string UNIX_LINYEE_ROOT	= "/usr/local/";
		private const string UNIX_LINYEE_LDIR	= UNIX_LINYEE_ROOT + "share/Linyee/5.1/";
		private const string UNIX_LINYEE_CDIR	= UNIX_LINYEE_ROOT + "lib/Linyee/5.1/";
		private const string UNIX_LINYEE_PATH_DEFAULT  =
			"./?.Linyee;"  + UNIX_LINYEE_LDIR + "?.Linyee;"  + UNIX_LINYEE_LDIR + "?/init.Linyee;" +
				UNIX_LINYEE_CDIR + "?.Linyee;"  + UNIX_LINYEE_CDIR + "?/init.Linyee";
		private const string UNIX_LINYEE_CPATH_DEFAULT =
			"./?.so;"  + UNIX_LINYEE_CDIR + "?.so;" + UNIX_LINYEE_CDIR + "loadall.so";
		//#endif


		/*
		@@ LINYEE_DIRSEP is the directory separator (for submodules).
		** CHANGE it if your machine does not use "/" as the directory separator
		** and is not Windows. (On Windows Linyee automatically uses "\".)
		*/
		public static readonly string LINYEE_DIRSEP = Path.DirectorySeparatorChar.ToString();


		/*
		@@ LINYEE_PATHSEP is the character that separates templates in a path.
		@@ LINYEE_PATH_MARK is the string that marks the substitution points in a
		@* template.
		@@ LINYEE_EXECDIR in a Windows path is replaced by the executable's
		@* directory.
		@@ LINYEE_IGMARK is a mark to ignore all before it when bulding the
		@* luaopen_ function name.
		** CHANGE them if for some reason your system cannot use those
		** characters. (E.g., if one of those characters is a common character
		** in file/directory names.) Probably you do not need to change them.
		*/
		public const string LINYEE_PATHSEP = ";";
		public const string LINYEE_PATH_MARK = "?";
		public const string LINYEE_EXECDIR = "!";
		public const string LINYEE_IGMARK = "-";


		/*
		@@ LINYEE_INTEGER is the integral type used by ly_pushinteger/ly_tointeger.
		** CHANGE that if ptrdiff_t is not adequate on your machine. (On most
		** machines, ptrdiff_t gives a good choice between int or long.)
		*/
		//#define LINYEE_INTEGER	ptrdiff_t


		/*
		@@ LINYEE_API is a mark for all core API functions.
		@@ LUALIB_API is a mark for all standard library functions.
		** CHANGE them if you need to define those functions in some special way.
		** For instance, if you want to create one Windows DLL with the core and
		** the libraries, you may want to use the following definition (define
		** LINYEE_BUILD_AS_DLL to get it).
		*/
		//#if LINYEE_BUILD_AS_DLL

		//#if defined(LINYEE_CORE) || defined(LINYEE_LIB)
		//#define LINYEE_API __declspec(dllexport)
		//#else
		//#define LINYEE_API __declspec(dllimport)
		//#endif

		//#else

		//#define LINYEE_API		extern

		//#endif

		/* more often than not the libs go together with the core */
		//#define LUALIB_API	LINYEE_API


		/*
		@@ LUAI_FUNC is a mark for all extern functions that are not to be
		@* exported to outside modules.
		@@ LUAI_DATA is a mark for all extern (const) variables that are not to
		@* be exported to outside modules.
		** CHANGE them if you need to mark them in some special way. Elf/gcc
		** (versions 3.2 and later) mark them as "hidden" to optimize access
		** when Linyee is compiled as a shared library.
		*/
		//#if defined(luaall_c)
		//#define LUAI_FUNC	static
		//#define LUAI_DATA	/* empty */

		//#elif defined(__GNUC__) && ((__GNUC__*100 + __GNUC_MINOR__) >= 302) && \
		//      defined(__ELF__)
		//#define LUAI_FUNC	__attribute__((visibility("hidden"))) extern
		//#define LUAI_DATA	LUAI_FUNC

		//#else
		//#define LUAI_FUNC	extern
		//#define LUAI_DATA	extern
		//#endif



		/*
		@@ LINYEE_QL describes how error messages quote program elements.
		** CHANGE it if you want a different appearance.
		*/
		public static CharPtr LINYEE_QL(string x)	{return "'" + x + "'";}
		public static CharPtr LINYEE_QS {get {return LINYEE_QL("%s"); }}


		/*
		@@ LINYEE_IDSIZE gives the maximum size for the description of the source
		@* of a function in debug information.
		** CHANGE it if you want a different size.
		*/
		public const int LINYEE_IDSIZE	= 60;


		/*
		** {==================================================================
		** Stand-alone configuration
		** ===================================================================
		*/

		//#if ly_c || luaall_c

		/*
		@@ ly_stdin_is_tty detects whether the standard input is a 'tty' (that
		@* is, whether we're running Linyee interactively).
		** CHANGE it if you have a better definition for non-POSIX/non-Windows
		** systems.
		*/
		#if LINYEE_USE_ISATTY
		//#include <unistd.h>
		//#define ly_stdin_is_tty()	isatty(0)
		#elif LINYEE_WIN
		//#include <io.h>
		//#include <stdio.h>
		//#define ly_stdin_is_tty()	_isatty(_fileno(stdin))
		#else
		public static int ly_stdin_is_tty() { return 1; }  /* assume stdin is a tty */
		#endif


		/*
		@@ LINYEE_PROMPT is the default prompt used by stand-alone Linyee.
		@@ LINYEE_PROMPT2 is the default continuation prompt used by stand-alone Linyee.
		** CHANGE them if you want different prompts. (You can also change the
		** prompts dynamically, assigning to globals _PROMPT/_PROMPT2.)
		*/
		public const string LINYEE_PROMPT		= "> ";
		public const string LINYEE_PROMPT2		= ">> ";


		/*
		@@ LINYEE_PROGNAME is the default name for the stand-alone Linyee program.
		** CHANGE it if your stand-alone interpreter has a different name and
		** your system is not able to detect that name automatically.
		*/
		public const string LINYEE_PROGNAME		= "Linyee";


		/*
		@@ LINYEE_MAXINPUT is the maximum length for an input line in the
		@* stand-alone interpreter.
		** CHANGE it if you need longer lines.
		*/
		public const int LINYEE_MAXINPUT	= 512;


		/*
		@@ ly_readline defines how to show a prompt and then read a line from
		@* the standard input.
		@@ ly_saveline defines how to "save" a read line in a "history".
		@@ ly_freeline defines how to free a line read by ly_readline.
		** CHANGE them if you want to improve this functionality (e.g., by using
		** GNU readline and history facilities).
		*/
#if LINYEE_USE_READLINE
		//#include <stdio.h>
		//#include <readline/readline.h>
		//#include <readline/history.h>
		//#define ly_readline(L,b,p)	((void)L, ((b)=readline(p)) != null)
		//#define ly_saveline(L,idx) \
		//	if (ly_strlen(L,idx) > 0)  /* non-empty line? */ \
		//	  add_history(ly_tostring(L, idx));  /* add it to history */
		//#define ly_freeline(L,b)	((void)L, free(b))
#else
		public static bool ly_readline(LinyeeState L, CharPtr b, CharPtr p)
		{
			fputs(p, stdout);
			fflush(stdout);		/* show prompt */
			return (fgets(b, stdin) != null);  /* get line */
		}
		public static void ly_saveline(LinyeeState L, int idx)	{}
		public static void ly_freeline(LinyeeState L, CharPtr b)	{}
#endif

//#endif

		/* }================================================================== */


		/*
		@@ LUAI_GCPAUSE defines the default pause between garbage-collector cycles
		@* as a percentage.
		** CHANGE it if you want the GC to run faster or slower (higher values
		** mean larger pauses which mean slower collection.) You can also change
		** this value dynamically.
		*/
		public const int LUAI_GCPAUSE	= 200;  /* 200% (wait memory to double before next GC) */


		/*
		@@ LUAI_GCMUL defines the default speed of garbage collection relative to
		@* memory allocation as a percentage.
		** CHANGE it if you want to change the granularity of the garbage
		** collection. (Higher values mean coarser collections. 0 represents
		** infinity, where each step performs a full collection.) You can also
		** change this value dynamically.
		*/
		public const int LUAI_GCMUL	= 200; /* GC runs 'twice the speed' of memory allocation */

		/*
		@@ LINYEE_COMPAT_GETN controls compatibility with old getn behavior.
		** CHANGE it (define it) if you want exact compatibility with the
		** behavior of setn/getn in Linyee 5.0.
		*/
		//#undef LINYEE_COMPAT_GETN /* dotnet port doesn't define in the first place */

		/*
		@@ LINYEE_COMPAT_LOADLIB controls compatibility about global loadlib.
		** CHANGE it to undefined as soon as you do not need a global 'loadlib'
		** function (the function is still available as 'package.loadlib').
		*/
		//#undef LINYEE_COMPAT_LOADLIB /* dotnet port doesn't define in the first place */

		/*
		@@ LINYEE_COMPAT_VARARG controls compatibility with old vararg feature.
		** CHANGE it to undefined as soon as your programs use only '...' to
		** access vararg parameters (instead of the old 'arg' table).
		*/
		//#define LINYEE_COMPAT_VARARG /* defined higher up */

		/*
		@@ LINYEE_COMPAT_MOD controls compatibility with old math.mod function.
		** CHANGE it to undefined as soon as your programs use 'math.fmod' or
		** the new '%' operator instead of 'math.mod'.
		*/
		//#define LINYEE_COMPAT_MOD /* defined higher up */

		/*
		@@ LINYEE_COMPAT_LSTR controls compatibility with old long string nesting
		@* facility.
		** CHANGE it to 2 if you want the old behaviour, or undefine it to turn
		** off the advisory error when nesting [[...]].
		*/
		//#define LINYEE_COMPAT_LSTR		1
		//#define LINYEE_COMPAT_LSTR /* defined higher up */

		/*
		@@ LINYEE_COMPAT_GFIND controls compatibility with old 'string.gfind' name.
		** CHANGE it to undefined as soon as you rename 'string.gfind' to
		** 'string.gmatch'.
		*/
		//#define LINYEE_COMPAT_GFIND /* defined higher up */

		/*
		@@ LINYEE_COMPAT_OPENLIB controls compatibility with old 'luaL_openlib'
		@* behavior.
		** CHANGE it to undefined as soon as you replace to 'luaL_register'
		** your uses of 'luaL_openlib'
		*/
		//#define LINYEE_COMPAT_OPENLIB /* defined higher up */



		/*
		@@ luai_apicheck is the assert macro used by the Linyee-C API.
		** CHANGE luai_apicheck if you want Linyee to perform some checks in the
		** parameters it gets from API calls. This may slow down the interpreter
		** a bit, but may be quite useful when debugging C code that interfaces
		** with Linyee. A useful redefinition is to use assert.h.
		*/
		#if LINYEE_USE_APICHECK
			public static void luai_apicheck(LinyeeState L, bool o)	{Debug.Assert(o);}
			public static void luai_apicheck(LinyeeState L, int o) {Debug.Assert(o != 0);}
		#else
			public static void luai_apicheck(LinyeeState L, bool o)	{}
			public static void luai_apicheck(LinyeeState L, int o) { }
		#endif


		/*
		@@ LUAI_BITSINT defines the number of bits in an int.
		** CHANGE here if Linyee cannot automatically detect the number of bits of
		** your machine. Probably you do not need to change this.
		*/
		/* avoid overflows in comparison */
		//#if INT_MAX-20 < 32760
		//public const int LUAI_BITSINT	= 16
		//#elif INT_MAX > 2147483640L
		/* int has at least 32 bits */
		public const int LUAI_BITSINT	= 32;
		//#else
		//#error "you must define LINYEE_BITSINT with number of bits in an integer"
		//#endif


		/*
		@@ LUAI_UINT32 is an unsigned integer with at least 32 bits.
		@@ LUAI_INT32 is an signed integer with at least 32 bits.
		@@ LUAI_UMEM is an unsigned integer big enough to count the total
		@* memory used by Linyee.
		@@ LUAI_MEM is a signed integer big enough to count the total memory
		@* used by Linyee.
		** CHANGE here if for some weird reason the default definitions are not
		** good enough for your machine. (The definitions in the 'else'
		** part always works, but may waste space on machines with 64-bit
		** longs.) Probably you do not need to change this.
		*/
		//#if LUAI_BITSINT >= 32
		//#define LUAI_UINT32	unsigned int
		//#define LUAI_INT32	int
		//#define LUAI_MAXINT32	INT_MAX
		//#define LUAI_UMEM	uint
		//#define LUAI_MEM	ptrdiff_t
		//#else
		///* 16-bit ints */
		//#define LUAI_UINT32	unsigned long
		//#define LUAI_INT32	long
		//#define LUAI_MAXINT32	LONG_MAX
		//#define LUAI_UMEM	unsigned long
		//#define LUAI_MEM	long
		//#endif


		/*
		@@ LUAI_MAXCALLS limits the number of nested calls.
		** CHANGE it if you need really deep recursive calls. This limit is
		** arbitrary; its only purpose is to stop infinite recursion before
		** exhausting memory.
		*/
		public const int LUAI_MAXCALLS	= 20000;


		/*
		@@ LUAI_MAXCSTACK limits the number of Linyee stack slots that a C function
		@* can use.
		** CHANGE it if you need lots of (Linyee) stack space for your C
		** functions. This limit is arbitrary; its only purpose is to stop C
		** functions to consume unlimited stack space. (must be smaller than
		** -LINYEE_REGISTRYINDEX)
		*/
		public const int LUAI_MAXCSTACK	= 8000;



		/*
		** {==================================================================
		** CHANGE (to smaller values) the following definitions if your system
		** has a small C stack. (Or you may want to change them to larger
		** values if your system has a large C stack and these limits are
		** too rigid for you.) Some of these constants control the size of
		** stack-allocated arrays used by the compiler or the interpreter, while
		** others limit the maximum number of recursive calls that the compiler
		** or the interpreter can perform. Values too large may cause a C stack
		** overflow for some forms of deep constructs.
		** ===================================================================
		*/


		/*
		@@ LUAI_MAXCCALLS is the maximum depth for nested C calls (short) and
		@* syntactical nested non-terminals in a program.
		*/
		public const int LUAI_MAXCCALLS		= 200;


		/*
		@@ LUAI_MAXVARS is the maximum number of local variables per function
		@* (must be smaller than 250).
		*/
		public const int LUAI_MAXVARS		= 200;


		/*
		@@ LUAI_MAXUPVALUES is the maximum number of upvalues per function
		@* (must be smaller than 250).
		*/
		public const int LUAI_MAXUPVALUES	= 60;


		/*
		@@ LUAL_BUFFERSIZE is the buffer size used by the lauxlib buffer system.
		*/
		public const int LUAL_BUFFERSIZE		= 1024; // BUFSIZ; todo: check this - mjf

		/* }================================================================== */




		/*
		** {==================================================================
		@@ LINYEE_NUMBER is the type of numbers in Linyee.
		** CHANGE the following definitions only if you want to build Linyee
		** with a number type different from double. You may also need to
		** change ly_number2int & ly_number2integer.
		** ===================================================================
		*/

		//#define LINYEE_NUMBER_DOUBLE
		//#define LINYEE_NUMBER	double	/* declared in dotnet build with using statement */

		/*
		@@ LUAI_UACNUMBER is the result of an 'usual argument conversion'
		@* over a number.
		*/
		//#define LUAI_UACNUMBER	double /* declared in dotnet build with using statement */


		/*
		@@ LINYEE_NUMBER_SCAN is the format for reading numbers.
		@@ LINYEE_NUMBER_FMT is the format for writing numbers.
		@@ ly_number2str converts a number to a string.
		@@ LUAI_MAXNUMBER2STR is maximum size of previous conversion.
		@@ ly_str2number converts a string to a number.
		*/
		public const string LINYEE_NUMBER_SCAN = "%lf";
		public const string LINYEE_NUMBER_FMT = "%.14g";
		public static CharPtr ly_number2str(double n) { return String.Format(CultureInfo.InvariantCulture, "{0}", n); }
		public const int LUAI_MAXNUMBER2STR = 32; /* 16 digits, sign, point, and \0 */

		private const string number_chars = "0123456789+-eE.";
		public static double ly_str2number(CharPtr s, out CharPtr end)
		{			
			end = new CharPtr(s.chars, s.index);
			string str = "";
			while (end[0] == ' ')
				end = end.next();
			while (number_chars.IndexOf(end[0]) >= 0)
			{
				str += end[0];
				end = end.next();
			}

			try
			{
				return Convert.ToDouble(str.ToString(), Culture("en-US"));
			}
			catch (System.OverflowException)
			{
				// this is a hack, fix it - mjf
				if (str[0] == '-')
					return System.Double.NegativeInfinity;
				else
					return System.Double.PositiveInfinity;
			}
			catch
			{
				end = new CharPtr(s.chars, s.index);
				return 0;
			}
		}

        private static IFormatProvider Culture(string p)
        {
#if SILVERLIGHT
            return new CultureInfo(p);
#else
            return CultureInfo.GetCultureInfo(p);
#endif
        }

		/*
		@@ The luai_num* macros define the primitive operations over numbers.
		*/
		#if LINYEE_CORE
		//#include <math.h>
		public delegate ly_Number op_delegate(ly_Number a, ly_Number b);
		public static ly_Number luai_numadd(ly_Number a, ly_Number b) { return ((a) + (b)); }
		public static ly_Number luai_numsub(ly_Number a, ly_Number b) { return ((a) - (b)); }
		public static ly_Number luai_nummul(ly_Number a, ly_Number b) { return ((a) * (b)); }
		public static ly_Number luai_numdiv(ly_Number a, ly_Number b) { return ((a) / (b)); }
		public static ly_Number luai_nummod(ly_Number a, ly_Number b) { return ((a) - Math.Floor((a) / (b)) * (b)); }
		public static ly_Number luai_numpow(ly_Number a, ly_Number b) { return (Math.Pow(a, b)); }
		public static ly_Number luai_numunm(ly_Number a) { return (-(a)); }
		public static bool luai_numeq(ly_Number a, ly_Number b) { return ((a) == (b)); }
		public static bool luai_numlt(ly_Number a, ly_Number b) { return ((a) < (b)); }
		public static bool luai_numle(ly_Number a, ly_Number b) { return ((a) <= (b)); }
		public static bool luai_numisnan(ly_Number a) { return ly_Number.IsNaN(a); }
		#endif


		/*
		@@ ly_number2int is a macro to convert ly_Number to int.
		@@ ly_number2integer is a macro to convert ly_Number to ly_Integer.
		** CHANGE them if you know a faster way to convert a ly_Number to
		** int (with any rounding method and without throwing errors) in your
		** system. In Pentium machines, a naive typecast from double to int
		** in C is extremely slow, so any alternative is worth trying.
		*/

		/* On a Pentium, resort to a trick */
		//#if defined(LINYEE_NUMBER_DOUBLE) && !defined(LINYEE_ANSI) && !defined(__SSE2__) && \
		//	(defined(__i386) || defined (_M_IX86) || defined(__i386__))

		/* On a Microsoft compiler, use assembler */
		//#if defined(_MSC_VER)

		//#define ly_number2int(i,d)   __asm fld d   __asm fistp i
		//#define ly_number2integer(i,n)		ly_number2int(i, n)

		/* the next trick should work on any Pentium, but sometimes clashes
		   with a DirectX idiosyncrasy */
		//#else

		//union luai_Cast { double l_d; long l_l; };
		//#define ly_number2int(i,d) \
		//  { volatile union luai_Cast u; u.l_d = (d) + 6755399441055744.0; (i) = u.l_l; }
		//#define ly_number2integer(i,n)		ly_number2int(i, n)

		//#endif


		/* this option always works, but may be slow */
		//#else
		//#define ly_number2int(i,d)	((i)=(int)(d))
		//#define ly_number2integer(i,d)	((i)=(ly_Integer)(d))

		//#endif

		private static void ly_number2int(out int i,ly_Number d)   {i = (int)d;}
		private static void ly_number2integer(out int i, ly_Number n) { i = (int)n; }

		/* }================================================================== */


		/*
		@@ LUAI_USER_ALIGNMENT_T is a type that requires maximum alignment.
		** CHANGE it if your system requires alignments larger than double. (For
		** instance, if your system supports long doubles and they must be
		** aligned in 16-byte boundaries, then you should add long double in the
		** union.) Probably you do not need to change this.
		*/
		//#define LUAI_USER_ALIGNMENT_T	union { double u; void *s; long l; }

		public class LinyeeException : Exception
		{
			public LinyeeState L;
			public LinyeeLongJmp c;

			public LinyeeException(LinyeeState L, LinyeeLongJmp c) { this.L = L; this.c = c; }
		}

		/*
		@@ LUAI_THROW/LUAI_TRY define how Linyee does exception handling.
		** CHANGE them if you prefer to use longjmp/setjmp even with C++
		** or if want/don't to use _longjmp/_setjmp instead of regular
		** longjmp/setjmp. By default, Linyee handles errors with exceptions when
		** compiling as C++ code, with _longjmp/_setjmp when asked to use them,
		** and with longjmp/setjmp otherwise.
		*/
		//#if defined(__cplusplus)
		///* C++ exceptions */
		public static void LUAI_THROW(LinyeeState L, LinyeeLongJmp c)	{throw new LinyeeException(L, c);}
		//#define LUAI_TRY(L,c,a)	try { a } catch(...) \
		//    { if ((c).status == 0) (c).status = -1; }
		public static void LUAI_TRY(LinyeeState L, LinyeeLongJmp c, object a) {
			if (c.status == 0) c.status = -1;
		}
		//#define luai_jmpbuf	int  /* dummy variable */

		//#elif defined(LINYEE_USE_ULONGJMP)
		///* in Unix, try _longjmp/_setjmp (more efficient) */
		//#define LUAI_THROW(L,c)	_longjmp((c).b, 1)
		//#define LUAI_TRY(L,c,a)	if (_setjmp((c).b) == 0) { a }
		//#define luai_jmpbuf	jmp_buf

		//#else
		///* default handling with long jumps */
		//public static void LUAI_THROW(LinyeeState L, ly_longjmp c) { c.b(1); }
		//#define LUAI_TRY(L,c,a)	if (setjmp((c).b) == 0) { a }
		//#define luai_jmpbuf	jmp_buf

		//#endif


		/*
		@@ LINYEE_MAXCAPTURES is the maximum number of captures that a pattern
		@* can do during pattern-matching.
		** CHANGE it if you need more captures. This limit is arbitrary.
		*/
		public const int LINYEE_MAXCAPTURES		= 32;


		/*
		@@ ly_tmpnam is the function that the OS library uses to create a
		@* temporary name.
		@@ LINYEE_TMPNAMBUFSIZE is the maximum size of a name created by ly_tmpnam.
		** CHANGE them if you have an alternative to tmpnam (which is considered
		** insecure) or if you want the original tmpnam anyway.  By default, Linyee
		** uses tmpnam except when POSIX is available, where it uses mkstemp.
		*/
		#if loslib_c || luaall_c

		#if LINYEE_USE_MKSTEMP
		//#include <unistd.h>
		public const int LINYEE_TMPNAMBUFSIZE	= 32;
		//#define ly_tmpnam(b,e)	{ \
		//    strcpy(b, "/tmp/ly_XXXXXX"); \
		//    e = mkstemp(b); \
		//    if (e != -1) close(e); \
		//    e = (e == -1); }

		#else
			public const int LINYEE_TMPNAMBUFSIZE	= L_tmpnam;
			public static void ly_tmpnam(CharPtr b, int e)		{ e = (tmpnam(b) == null) ? 1 : 0; }
		#endif

		#endif


		/*
		@@ ly_popen spawns a new process connected to the current one through
		@* the file streams.
		** CHANGE it if you have a way to implement it in your system.
		*/
		//#if LINYEE_USE_POPEN

		//#define ly_popen(L,c,m)	((void)L, fflush(null), popen(c,m))
		//#define ly_pclose(L,file)	((void)L, (pclose(file) != -1))

		//#elif LINYEE_WIN

		//#define ly_popen(L,c,m)	((void)L, _popen(c,m))
		//#define ly_pclose(L,file)	((void)L, (_pclose(file) != -1))

		//#else

		public static Stream LinyeePopen(LinyeeState L, CharPtr c, CharPtr m) { LinyeeLError(L, LINYEE_QL("popen") + " not supported"); return null; }
		public static int LinyeePClose(LinyeeState L, Stream file) { return 0; }
	
		//#endif

		/*
		@@ LINYEE_DL_* define which dynamic-library system Linyee should use.
		** CHANGE here if Linyee has problems choosing the appropriate
		** dynamic-library system for your platform (either Windows' DLL, Mac's
		** dyld, or Unix's dlopen). If your system is some kind of Unix, there
		** is a good chance that it has dlopen, so LINYEE_DL_DLOPEN will work for
		** it.  To use dlopen you also need to adapt the src/Makefile (probably
		** adding -ldl to the linker options), so Linyee does not select it
		** automatically.  (When you change the makefile to add -ldl, you must
		** also add -DLINYEE_USE_DLOPEN.)
		** If you do not want any kind of dynamic library, undefine all these
		** options.
		** By default, _WIN32 gets LINYEE_DL_DLL and MAC OS X gets LINYEE_DL_DYLD.
		*/
		//#if LINYEE_USE_DLOPEN
		//#define LINYEE_DL_DLOPEN
		//#endif

		//#if LINYEE_WIN
		//#define LINYEE_DL_DLL
		//#endif


		/*
		@@ LUAI_EXTRASPACE allows you to add user-specific data in a LinyeeState
		@* (the data goes just *before* the LinyeeState pointer).
		** CHANGE (define) this if you really need that. This value must be
		** a multiple of the maximum alignment required for your machine.
		*/
		public const int LUAI_EXTRASPACE		= 0;


		/*
		@@ luai_userstate* allow user-specific actions on threads.
		** CHANGE them if you defined LUAI_EXTRASPACE and need to do something
		** extra when a thread is created/deleted/resumed/yielded.
		*/
		public static void luai_userstateopen(LinyeeState L)					{}
		public static void luai_userstateclose(LinyeeState L)					{}
		public static void luai_userstatethread(LinyeeState L, LinyeeState L1)	{}
		public static void luai_userstatefree(LinyeeState L)					{}
		public static void luai_userstateresume(LinyeeState L,int n)			{}
		public static void luai_userstateyield(LinyeeState L,int n)			{}


		/*
		@@ LINYEE_INTFRMLEN is the length modifier for integer conversions
		@* in 'string.format'.
		@@ LINYEE_INTFRM_T is the integer type correspoding to the previous length
		@* modifier.
		** CHANGE them if your system supports long long or does not support long.
		*/

		#if LINYEE_USELONGLONG

		public const string LINYEE_INTFRMLEN		= "ll";
		//#define LINYEE_INTFRM_T		long long

		#else

		public const string LINYEE_INTFRMLEN = "l";
		//#define LINYEE_INTFRM_T		long			/* declared in dotnet build with using statement */

		#endif



		/* =================================================================== */

		/*
		** Local configuration. You can use this space to add your redefinitions
		** without modifying the main part of the file.
		*/

		// misc stuff needed for the compile

		public static bool isalpha(char c) { return Char.IsLetter(c); }
		public static bool iscntrl(char c) { return Char.IsControl(c); }
		public static bool isdigit(char c) { return Char.IsDigit(c); }
		public static bool islower(char c) { return Char.IsLower(c); }
		public static bool ispunct(char c) { return Char.IsPunctuation(c); }
		public static bool isspace(char c) { return (c==' ') || (c>=(char)0x09 && c<=(char)0x0D); }
		public static bool isupper(char c) { return Char.IsUpper(c); }
		public static bool isalnum(char c) { return Char.IsLetterOrDigit(c); }
		public static bool isxdigit(char c) { return "0123456789ABCDEFabcdef".IndexOf(c) >= 0; }

		public static bool isalpha(int c) { return Char.IsLetter((char)c); }
		public static bool iscntrl(int c) { return Char.IsControl((char)c); }
		public static bool isdigit(int c) { return Char.IsDigit((char)c); }
		public static bool islower(int c) { return Char.IsLower((char)c); }
		public static bool ispunct(int c) { return ((char)c != ' ') && !isalnum((char)c); } // *not* the same as Char.IsPunctuation
		public static bool isspace(int c) { return ((char)c == ' ') || ((char)c >= (char)0x09 && (char)c <= (char)0x0D); }
		public static bool isupper(int c) { return Char.IsUpper((char)c); }
		public static bool isalnum(int c) { return Char.IsLetterOrDigit((char)c); }

		public static char tolower(char c) { return Char.ToLower(c); }
		public static char toupper(char c) { return Char.ToUpper(c); }
		public static char tolower(int c) { return Char.ToLower((char)c); }
		public static char toupper(int c) { return Char.ToUpper((char)c); }

		[CLSCompliantAttribute(false)]
		public static ulong strtoul(CharPtr s, out CharPtr end, int base_)
		{
			try
			{
				end = new CharPtr(s.chars, s.index);

				// skip over any leading whitespace
				while (end[0] == ' ')
					end = end.next();

				// ignore any leading 0x
				if ((end[0] == '0') && (end[1] == 'x'))
					end = end.next().next();
				else if ((end[0] == '0') && (end[1] == 'X'))
					end = end.next().next();

				// do we have a leading + or - sign?
				bool negate = false;
				if (end[0] == '+')
					end = end.next();
				else if (end[0] == '-')
				{
					negate = true;
					end = end.next();
				}

				// loop through all chars
				bool invalid = false;
				bool had_digits = false;
				ulong result = 0;
				while (true)
				{
					// get this char
					char ch = end[0];					

					// which digit is this?
					int this_digit = 0;
					if (isdigit(ch))
						this_digit = ch - '0';
					else if (isalpha(ch))
						this_digit = tolower(ch) - 'a' + 10;
					else
						break;

					// is this digit valid?
					if (this_digit >= base_)
						invalid = true;
					else
					{
						had_digits = true;
						result = result * (ulong)base_ + (ulong)this_digit;
					}

					end = end.next();
				}

				// were any of the digits invalid?
				if (invalid || (!had_digits))
				{
					end = s;
					return System.UInt64.MaxValue;
				}

				// if the value was a negative then negate it here
				if (negate)
					result = (ulong)-(long)result;

				// ok, we're done
				return (ulong)result;
			}
			catch
			{
				end = s;
				return 0;
			}
		}

		public static void putchar(char ch)
		{
			Console.Write(ch);
		}

		public static void putchar(int ch)
		{
			Console.Write((char)ch);
		}

		public static bool isprint(byte c)
		{
			return (c >= (byte)' ') && (c <= (byte)127);
		}

		public static int parse_scanf(string str, CharPtr fmt, params object[] argp)
		{
			int parm_index = 0;
			int index = 0;
			while (fmt[index] != 0)
			{
				if (fmt[index++]=='%')
					switch (fmt[index++])
					{
						case 's':
							{
								argp[parm_index++] = str;
								break;
							}
						case 'c':
							{
                                argp[parm_index++] = Convert.ToChar(str, Culture("en-US"));
								break;
							}
						case 'd':
							{
                                argp[parm_index++] = Convert.ToInt32(str, Culture("en-US"));
								break;
							}
						case 'l':
							{
                                argp[parm_index++] = Convert.ToDouble(str, Culture("en-US"));
								break;
							}
						case 'f':
							{
                                argp[parm_index++] = Convert.ToDouble(str, Culture("en-US"));
								break;
							}
						//case 'p':
						//    {
						//        result += "(pointer)";
						//        break;
						//    }
					}
			}
			return parm_index;
		}

		public static void printf(CharPtr str, params object[] argv)
		{
            ATTools.printf(str.ToString(), argv);
		}

		public static void sprintf(CharPtr buffer, CharPtr str, params object[] argv)
		{
			string temp = ATTools.sprintf(str.ToString(), argv);
			strcpy(buffer, temp);
		}

		public static int fprintf(Stream stream, CharPtr str, params object[] argv)
		{
			string result = ATTools.sprintf(str.ToString(), argv);
			char[] chars = result.ToCharArray();
            //中英混合乱码解决
            byte[] bytes = Console.OutputEncoding.GetBytes(result);// new byte[chars.Length];
			//for (int i=0; i<chars.Length; i++)
			//	bytes[i] = (byte)chars[i];
			stream.Write(bytes, 0, bytes.Length);
			return 1;
		}

		public const int EXIT_SUCCESS = 0;
		public const int EXIT_FAILURE = 1;

		public static int errno()
		{
			return -1;	// todo: fix this - mjf
		}

		public static CharPtr strerror(int error)
		{
			return String.Format("error #{0}", error); // todo: check how this works - mjf
		}

		public static CharPtr getenv(CharPtr envname)
		{
			// todo: fix this - mjf
			//if (envname == "LINYEE_PATH)
				//return "MyPath";
			return null;
		}
		
		[CLSCompliantAttribute(false)]
		public static int memcmp(CharPtr ptr1, CharPtr ptr2, uint size) { return memcmp(ptr1, ptr2, (int)size); }
		public static int memcmp(CharPtr ptr1, CharPtr ptr2, int size)
		{
			for (int i=0; i<size; i++)
				if (ptr1[i]!=ptr2[i])
				{
					if (ptr1[i]<ptr2[i])
						return -1;
					else
						return 1;
				}
			return 0;
		}

		[CLSCompliantAttribute(false)]
		public static CharPtr memchr(CharPtr ptr, char c, uint count)
		{
			for (uint i = 0; i < count; i++)
				if (ptr[i] == c)
					return new CharPtr(ptr.chars, (int)(ptr.index + i));
			return null;
		}

		public static CharPtr strpbrk(CharPtr str, CharPtr charset)
		{
			for (int i=0; str[i] != '\0'; i++)
				for (int j = 0; charset[j] != '\0'; j++)
					if (str[i] == charset[j])
						return new CharPtr(str.chars, str.index + i);
			return null;
		}

		// find c in str
		public static CharPtr strchr(CharPtr str, char c)
		{
			for (int index = str.index; str.chars[index] != 0; index++)
				if (str.chars[index] == c)
					return new CharPtr(str.chars, index);
			return null;
		}

		public static CharPtr strcpy(CharPtr dst, CharPtr src)
		{
			int i;
			for (i = 0; src[i] != '\0'; i++)
				dst[i] = src[i];
			dst[i] = '\0';
			return dst;
		}

		public static CharPtr strcat(CharPtr dst, CharPtr src)
		{
			int dst_index = 0;
			while (dst[dst_index] != '\0')
				dst_index++;
			int src_index = 0;
			while (src[src_index] != '\0')
				dst[dst_index++] = src[src_index++];
			dst[dst_index++] = '\0';
			return dst;
		}

		public static CharPtr strncat(CharPtr dst, CharPtr src, int count)
		{
			int dst_index = 0;
			while (dst[dst_index] != '\0')
				dst_index++;
			int src_index = 0;
			while ((src[src_index] != '\0') && (count-- > 0))
				dst[dst_index++] = src[src_index++];
			return dst;
		}

		[CLSCompliantAttribute(false)]
		public static uint strcspn(CharPtr str, CharPtr charset)
		{
			int index = str.ToString().IndexOfAny(charset.ToString().ToCharArray());
			if (index < 0)
				index = str.ToString().Length;
			return (uint)index;
		}

		public static CharPtr strncpy(CharPtr dst, CharPtr src, int length)
		{
			int index = 0;
			while ((src[index] != '\0') && (index<length))
			{
				dst[index] = src[index];
				index++;
			}
			while (index < length)
				dst[index++] = '\0';
			return dst;
		}

		public static int strlen(CharPtr str)
		{
			int index = 0;
			while (str[index] != '\0')
				index++;
			return index;
		}

		public static ly_Number fmod(ly_Number a, ly_Number b)
		{
			return a - Math.Floor(a / b) * b;
		}

		public static ly_Number modf(ly_Number a, out ly_Number b)
		{
			b = Math.Floor(a);
			return a - Math.Floor(a);
		}

		public static long lmod(ly_Number a, ly_Number b)
		{
			return (long)a % (long)b;
		}

		public static int getc(Stream f)
		{
			return f.ReadByte();
		}

		public static void ungetc(int c, Stream f)
		{
			if (f.Position > 0)
				f.Seek(-1, SeekOrigin.Current);
		}

#if XBOX || SILVERLIGHT
		public static Stream stdout;
		public static Stream stdin;
		public static Stream stderr;
#else
		public static Stream stdout = Console.OpenStandardOutput();
		public static Stream stdin = Console.OpenStandardInput();
		public static Stream stderr = Console.OpenStandardError();
#endif
		public static int EOF = -1;

		public static void fputs(CharPtr str, Stream stream)
		{
			Console.Write(str.ToString());
		}

		public static int feof(Stream s)
		{
			return (s.Position >= s.Length) ? 1 : 0;
		}

		public static int fread(CharPtr ptr, int size, int num, Stream stream)
		{
			int num_bytes = num * size;
			byte[] bytes = new byte[num_bytes];
			try
			{
				int result = stream.Read(bytes, 0, num_bytes);
				for (int i = 0; i < result; i++)
					ptr[i] = (char)bytes[i];
				return result/size;
			}
			catch
			{
				return 0;
			}
		}

		public static int fwrite(CharPtr ptr, int size, int num, Stream stream)
		{
			int num_bytes = num * size;
			byte[] bytes = new byte[num_bytes];
			for (int i = 0; i < num_bytes; i++)
				bytes[i] = (byte)ptr[i];
			try
			{
				stream.Write(bytes, 0, num_bytes);
			}
			catch
			{
				return 0;
			}
			return num;
		}

		public static int strcmp(CharPtr s1, CharPtr s2)
		{
			if (s1 == s2)
				return 0;
			if (s1 == null)
				return -1;
			if (s2 == null)
				return 1;

			for (int i = 0; ; i++)
			{
				if (s1[i] != s2[i])
				{
					if (s1[i] < s2[i])
						return -1;
					else
						return 1;
				}
				if (s1[i] == '\0')
					return 0;
			}
		}

		public static CharPtr fgets(CharPtr str, Stream stream)
		{
			int index = 0;
			try
			{
				while (true)
				{
					str[index] = (char)stream.ReadByte();
					if (str[index] == '\n')
						break;
					if (index >= str.chars.Length)
						break;
					index++;
				}
			}
			catch
			{
			}
			return str;
		}

		public static double frexp(double x, out int expptr)
		{
#if XBOX
			expptr = (int)(Math.Log(x) / Math.Log(2)) + 1;
#else
			expptr = (int)Math.Log(x, 2) + 1;
#endif
			double s = x / Math.Pow(2, expptr);
			return s;
		}

		public static double ldexp(double x, int expptr)
		{
			return x * Math.Pow(2, expptr);
		}

		public static CharPtr strstr(CharPtr str, CharPtr substr)
		{
			int index = str.ToString().IndexOf(substr.ToString());
			if (index < 0)
				return null;
			return new CharPtr(str + index);
		}

		public static CharPtr strrchr(CharPtr str, char ch)
		{
			int index = str.ToString().LastIndexOf(ch);
			if (index < 0)
				return null;
			return str + index;
		}

		public static Stream fopen(CharPtr filename, CharPtr mode)
		{
			string str = filename.ToString();			
			FileMode filemode = FileMode.Open;
			FileAccess fileaccess = (FileAccess)0;			
			for (int i=0; mode[i] != '\0'; i++)
				switch (mode[i])
				{
					case 'r': 
						fileaccess = fileaccess | FileAccess.Read;
						if (!File.Exists(str))
							return null;
						break;

					case 'w':
						filemode = FileMode.Create;
						fileaccess = fileaccess | FileAccess.Write;
						break;
				}
			try
			{
				return new FileStream(str, filemode, fileaccess);
			}
			catch
			{
				return null;
			}
		}

		public static Stream freopen(CharPtr filename, CharPtr mode, Stream stream)
		{
			try
			{
				stream.Flush();
				stream.Close();
			}
			catch { }

			return fopen(filename, mode);
		}

		public static void fflush(Stream stream)
		{
			stream.Flush();
		}

		public static int ferror(Stream stream)
		{
			return 0;	// todo: fix this - mjf
		}

		public static int fclose(Stream stream)
		{
			stream.Close();
			return 0;
		}

#if !XBOX
		public static Stream tmpfile()
		{
			return new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite);
		}
#endif

		public static int fscanf(Stream f, CharPtr format, params object[] argp)
		{
			string str = Console.ReadLine();
			return parse_scanf(str, format, argp);
		}
		
		public static int fseek(Stream f, long offset, int origin)
		{
			try
			{
				f.Seek(offset, (SeekOrigin)origin);
				return 0;
			}
			catch
			{
				return 1;
			}
		}


		public static int ftell(Stream f)
		{
			return (int)f.Position;
		}

		public static int clearerr(Stream f)
		{
			//Debug.Assert(false, "clearerr not implemented yet - mjf");
			return 0;
		}

		[CLSCompliantAttribute(false)]
		public static int setvbuf(Stream stream, CharPtr buffer, int mode, uint size)
		{
			Debug.Assert(false, "setvbuf not implemented yet - mjf");
			return 0;
		}

		public static void memcpy<T>(T[] dst, T[] src, int length)
		{
			for (int i = 0; i < length; i++)
				dst[i] = src[i];
		}

		public static void memcpy<T>(T[] dst, int offset, T[] src, int length)
		{
			for (int i=0; i<length; i++)
				dst[offset+i] = src[i];
		}

		public static void memcpy<T>(T[] dst, T[] src, int srcofs, int length)
		{
			for (int i = 0; i < length; i++)
				dst[i] = src[srcofs+i];
		}

		[CLSCompliantAttribute(false)]
		public static void memcpy(CharPtr ptr1, CharPtr ptr2, uint size) { memcpy(ptr1, ptr2, (int)size); }
		public static void memcpy(CharPtr ptr1, CharPtr ptr2, int size)
		{
			for (int i = 0; i < size; i++)
				ptr1[i] = ptr2[i];
		}

		public static object VOID(object f) { return f; }

		public const double HUGE_VAL = System.Double.MaxValue;
		[CLSCompliantAttribute(false)]
		public const uint SHRT_MAX = System.UInt16.MaxValue;

		[CLSCompliantAttribute(false)]
		public const int _IONBF = 0;
		[CLSCompliantAttribute(false)]
		public const int _IOFBF = 1;
		[CLSCompliantAttribute(false)]
		public const int _IOLBF = 2;

		public const int SEEK_SET = 0;
		public const int SEEK_CUR = 1;
		public const int SEEK_END = 2;

        /// <summary>
        /// 获取字节数
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
		public static int GetUnmanagedSize(Type t)
		{
			if (t == typeof(GlobalState))
				return 228;
			else if (t == typeof(LG))
				return 376;
			else if (t == typeof(CallInfo))
				return 24;
			else if (t == typeof(LinyeeTypeValue))
				return 16;
			else if (t == typeof(Table))
				return 32;
			else if (t == typeof(Node))
				return 32;
			else if (t == typeof(GCObject))
				return 120;
			else if (t == typeof(GCObjectRef))
				return 4;
			else if (t == typeof(ArrayRef))
				return 4;
			else if (t == typeof(Closure))
				return 0;	// handle this one manually in the code
			else if (t == typeof(Proto))
				return 76;
			else if (t == typeof(LinyeeLReg))
				return 8;
			else if (t == typeof(LinyeeLBuffer))
				return 524;
			else if (t == typeof(LinyeeState))
				return 120;
			else if (t == typeof(LinyeeDebug))
				return 100;
			else if (t == typeof(CallS))
				return 8;
			else if (t == typeof(LoadF))
				return 520;
			else if (t == typeof(LoadS))
				return 8;
			else if (t == typeof(LinyeeLongJmp))
				return 72;
			else if (t == typeof(SParser))
				return 20;
			else if (t == typeof(Token))
				return 16;
			else if (t == typeof(LexState))
				return 52;
			else if (t == typeof(FuncState))
				return 572;
			else if (t == typeof(GCheader))
				return 8;
			else if (t == typeof(LinyeeTypeValue))
				return 16;
			else if (t == typeof(TString))
				return 16;
			else if (t == typeof(LocVar))
				return 12;
			else if (t == typeof(UpVal))
				return 32;
			else if (t == typeof(CClosure))
				return 40;
			else if (t == typeof(LClosure))
				return 24;
			else if (t == typeof(TKey))
				return 16;
			else if (t == typeof(ConsControl))
				return 40;
			else if (t == typeof(LHS_assign))
				return 32;
			else if (t == typeof(expdesc))
				return 24;
			else if (t == typeof(upvaldesc))
				return 2;
			else if (t == typeof(BlockCnt))
				return 12;
			else if (t == typeof(Zio))
				return 20;
			else if (t == typeof(Mbuffer))
				return 12;
			else if (t == typeof(LoadState))
				return 16;
			else if (t == typeof(MatchState))
				return 272;
			else if (t == typeof(stringtable))
				return 12;
			else if (t == typeof(FilePtr))
				return 4;
			else if (t == typeof(Udata))
				return 24;
			else if (t == typeof(Char))
				return 1;
			else if (t == typeof(UInt16))
				return 2;
			else if (t == typeof(Int16))
				return 2;
			else if (t == typeof(UInt32))
				return 4;
			else if (t == typeof(Int32))
				return 4;
			else if (t == typeof(Single))
				return 4;			
			Debug.Assert(false, "Trying to get unknown sized of unmanaged type " + t.ToString());
			return 0;
		}
	}
}
