/*
** $Id: liolib.c,v 2.73.1.3 2008/01/18 17:47:43 roberto Exp $
** Standard I/O (and system) library
** See Copyright Notice in Linyee.h
*/

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Linyee
{
	using LinyeeNumberType = System.Double;
	using LinyeeIntegerType = System.Int32;

	public class FilePtr
	{
		public Stream file;
	}

	public partial class Linyee
	{
		public const int IOINPUT	= 1;
		public const int IOOUTPUT	= 2;

		private static readonly string[] fnames = { "input", "output" };


		private static int PushResult (LinyeeState L, int i, CharPtr filename) {
		  int en = errno();  /* calls to Linyee API may change this value */
		  if (i != 0) {
			LinyeePushBoolean(L, 1);
			return 1;
		  }
		  else {
			LinyeePushNil(L);
			if (filename != null)
				LinyeePushFString(L, "%s: %s", filename, strerror(en));
			else
				LinyeePushFString(L, "%s", strerror(en));
			LinyeePushInteger(L, en);
			return 3;
		  }
		}


		private static void FileError (LinyeeState L, int arg, CharPtr filename) {
		  LinyeePushFString(L, "%s: %s", filename, strerror(errno()));
		  LinyeeLArgError(L, arg, LinyeeToString(L, -1));
		}


		public static FilePtr ToFilePointer(LinyeeState L) { return (FilePtr)LinyeeLCheckUData(L, 1, LINYEE_FILEHANDLE); }


		private static int GetIOType (LinyeeState L) {
		  object ud;
		  LinyeeLCheckAny(L, 1);
		  ud = LinyeeToUserData(L, 1);
		  LinyeeGetField(L, LINYEE_REGISTRYINDEX, LINYEE_FILEHANDLE);
		  if (ud == null || (LinyeeGetMetatable(L, 1)==0) || (LinyeeRawEqual(L, -2, -1)==0))
			LinyeePushNil(L);  /* not a file */
		  else if ( (ud as FilePtr).file == null)
			LinyeePushLiteral(L, "closed file");
		  else
			LinyeePushLiteral(L, "file");
		  return 1;
		}


		private static Stream ToFile (LinyeeState L) {
		  FilePtr f = ToFilePointer(L);
		  if (f.file == null)
			LinyeeLError(L, "attempt to use a closed file");
		  return f.file;
		}



		/*
		** When creating file files, always creates a `closed' file file
		** before opening the actual file; so, if there is a memory error, the
		** file is not left opened.
		*/
		private static FilePtr NewFile (LinyeeState L) {

		  FilePtr pf = (FilePtr)LinyeeNewUserData(L, typeof(FilePtr));
		  pf.file = null;  /* file file is currently `closed' */
		  LinyeeLGetMetatable(L, LINYEE_FILEHANDLE);
		  LinyeeSetMetatable(L, -2);
		  return pf;
		}


		/*
		** function to (not) close the standard files stdin, stdout, and stderr
		*/
		private static int IoNoClose (LinyeeState L) {
		  LinyeePushNil(L);
		  LinyeePushLiteral(L, "cannot close standard file");
		  return 2;
		}


		/*
		** function to close 'popen' files
		*/
		private static int IoPClose (LinyeeState L) {
		  FilePtr p = ToFilePointer(L);
		  int ok = (LinyeePClose(L, p.file) == 0) ? 1 : 0;
		  p.file = null;
		  return PushResult(L, ok, null);
		}


		/*
		** function to close regular files
		*/
		private static int IoFClose (LinyeeState L) {
		  FilePtr p = ToFilePointer(L);
		  int ok = (fclose(p.file) == 0) ? 1 : 0;
		  p.file = null;
		  return PushResult(L, ok, null);
		}


		private static int AuxClose (LinyeeState L) {
		  LinyeeGetFEnv(L, 1);
		  LinyeeGetField(L, -1, "__close");
		  return (LinyeeToCFunction(L, -1))(L);
		}


		private static int IoClose (LinyeeState L) {
		  if (LinyeeIsNone(L, 1))
			LinyeeRawGetI(L, LINYEE_ENVIRONINDEX, IOOUTPUT);
		  ToFile(L);  /* make sure argument is a file */
		  return AuxClose(L);
		}


		private static int IoGC (LinyeeState L) {
		  Stream f = ToFilePointer(L).file;
		  /* ignore closed files */
		  if (f != null)
			AuxClose(L);
		  return 0;
		}


		private static int IoToString (LinyeeState L) {
		  Stream f = ToFilePointer(L).file;
		  if (f == null)
			LinyeePushLiteral(L, "file (closed)");
		  else
			LinyeePushFString(L, "file (%p)", f);
		  return 1;
		}


		private static int IoOpen (LinyeeState L) {
		  CharPtr filename = LinyeeLCheckString(L, 1);
		  CharPtr mode = LinyeeLOptString(L, 2, "r");
		  FilePtr pf = NewFile(L);
		  pf.file = fopen(filename, mode);
		  return (pf.file == null) ? PushResult(L, 0, filename) : 1;
		}


		/*
		** this function has a separated environment, which defines the
		** correct __close for 'popen' files
		*/
		private static int IoPopen (LinyeeState L) {
		  CharPtr filename = LinyeeLCheckString(L, 1);
		  CharPtr mode = LinyeeLOptString(L, 2, "r");
		  FilePtr pf = NewFile(L);
		  pf.file = LinyeePopen(L, filename, mode);
		  return (pf.file == null) ? PushResult(L, 0, filename) : 1;
		}


		private static int IoTmpFile (LinyeeState L) {
		  FilePtr pf = NewFile(L);
#if XBOX
			LinyeeLError(L, "io_tmpfile not supported on Xbox360");
#else
		  pf.file = tmpfile();
#endif
		  return (pf.file == null) ? PushResult(L, 0, null) : 1;
		}


		private static Stream GetIOFile (LinyeeState L, int findex) {
		  Stream f;
		  LinyeeRawGetI(L, LINYEE_ENVIRONINDEX, findex);
		  f = (LinyeeToUserData(L, -1) as FilePtr).file;
		  if (f == null)
			LinyeeLError(L, "standard %s file is closed", fnames[findex - 1]);
		  return f;
		}


		private static int GIOFile (LinyeeState L, int f, CharPtr mode) {
		  if (!LinyeeIsNoneOrNil(L, 1)) {
			CharPtr filename = LinyeeToString(L, 1);
			if (filename != null) {
			  FilePtr pf = NewFile(L);
			  pf.file = fopen(filename, mode);
			  if (pf.file == null)
				FileError(L, 1, filename);
			}
			else {
			  ToFile(L);  /* check that it's a valid file file */
			  LinyeePushValue(L, 1);
			}
			LinyeeRawSetI(L, LINYEE_ENVIRONINDEX, f);
		  }
		  /* return current value */
		  LinyeeRawGetI(L, LINYEE_ENVIRONINDEX, f);
		  return 1;
		}


		private static int IoInput (LinyeeState L) {
		  return GIOFile(L, IOINPUT, "r");
		}


		private static int IoOutput (LinyeeState L) {
		  return GIOFile(L, IOOUTPUT, "w");
		}

		private static void AuxLines (LinyeeState L, int idx, int toclose) {
		  LinyeePushValue(L, idx);
		  LinyeePushBoolean(L, toclose);  /* close/not close file when finished */
		  LinyeePushCClosure(L, IoReadLine, 2);
		}


		private static int FLines (LinyeeState L) {
		  ToFile(L);  /* check that it's a valid file file */
		  AuxLines(L, 1, 0);
		  return 1;
		}


		private static int IoLines (LinyeeState L) {
		  if (LinyeeIsNoneOrNil(L, 1)) {  /* no arguments? */
			/* will iterate over default input */
			LinyeeRawGetI(L, LINYEE_ENVIRONINDEX, IOINPUT);
			return FLines(L);
		  }
		  else {
			CharPtr filename = LinyeeLCheckString(L, 1);
			FilePtr pf = NewFile(L);
			pf.file = fopen(filename, "r");
			if (pf.file == null)
			  FileError(L, 1, filename);
			AuxLines(L, LinyeeGetTop(L), 1);
			return 1;
		  }
		}


		/*
		** {======================================================
		** READ
		** =======================================================
		*/


		private static int ReadNumber (LinyeeState L, Stream f) {
		  //LinyeeNumberType d;
			object[] parms = { (object)(double)0.0 };
			if (fscanf (f, LINYEE_NUMBER_SCAN, parms) == 1) {
				LinyeePushNumber (L, (double)parms [0]);
				return 1;
			} 
			else {
				LinyeePushNil(L);  /* "result" to be removed */
				return 0;  /* read fails */
			}
		}


		private static int TestEof (LinyeeState L, Stream f) {
		  int c = getc(f);
		  ungetc(c, f);
		  LinyeePushLString(L, null, 0);
		  return (c != EOF) ? 1 : 0;
		}


		private static int ReadLine (LinyeeState L, Stream f) {
		  LinyeeLBuffer b = new LinyeeLBuffer();
		  LinyeeLBuffInit(L, b);
		  for (;;) {
			uint l;
			CharPtr p = LinyeeLPrepBuffer(b);
			if (fgets(p, f) == null) {  /* eof? */
			  LinyeeLPushResult(b);  /* close buffer */
				return (LinyeeObjectLen(L, -1) > 0) ? 1 : 0;  /* check whether read something */
			}
			l = (uint)strlen(p);
			if (l == 0 || p[l-1] != '\n')
			  LinyeeLAddSize(b, (int)l);
			else {
			  LinyeeLAddSize(b, (int)(l - 1));  /* do not include `eol' */
			  LinyeeLPushResult(b);  /* close buffer */
			  return 1;  /* read at least an `eol' */
			}
		  }
		}


		private static int ReadChars (LinyeeState L, Stream f, uint n) {
		  uint rlen;  /* how much to read */
		  uint nr;  /* number of chars actually read */
		  LinyeeLBuffer b = new LinyeeLBuffer();
		  LinyeeLBuffInit(L, b);
		  rlen = LUAL_BUFFERSIZE;  /* try to read that much each time */
		  do {
			CharPtr p = LinyeeLPrepBuffer(b);
			if (rlen > n) rlen = n;  /* cannot read more than asked */
			nr = (uint)fread(p, GetUnmanagedSize(typeof(char)), (int)rlen, f);
			LinyeeLAddSize(b, (int)nr);
			n -= nr;  /* still have to read `n' chars */
		  } while (n > 0 && nr == rlen);  /* until end of count or eof */
		  LinyeeLPushResult(b);  /* close buffer */
		  return (n == 0 || LinyeeObjectLen(L, -1) > 0) ? 1 : 0;
		}


		private static int GRead (LinyeeState L, Stream f, int first) {
		  int nargs = LinyeeGetTop(L) - 1;
		  int success;
		  int n;
		  clearerr(f);
		  if (nargs == 0) {  /* no arguments? */
			success = ReadLine(L, f);
			n = first+1;  /* to return 1 result */
		  }
		  else {  /* ensure stack space for all results and for auxlib's buffer */
			LinyeeLCheckStack(L, nargs+LINYEE_MINSTACK, "too many arguments");
			success = 1;
			for (n = first; (nargs-- != 0) && (success!=0); n++) {
			  if (LinyeeType(L, n) == LINYEE_TNUMBER) {
				uint l = (uint)LinyeeToInteger(L, n);
				success = (l == 0) ? TestEof(L, f) : ReadChars(L, f, l);
			  }
			  else {
				CharPtr p = LinyeeToString(L, n);
				LinyeeLArgCheck(L, (p!=null) && (p[0] == '*'), n, "invalid option");
				switch (p[1]) {
				  case 'n':  /* number */
					success = ReadNumber(L, f);
					break;
				  case 'l':  /* line */
					success = ReadLine(L, f);
					break;
				  case 'a':  /* file */
					ReadChars(L, f, ~((uint)0));  /* read MAX_uint chars */
					success = 1; /* always success */
					break;
				  default:
					return LinyeeLArgError(L, n, "invalid format");
				}
			  }
			}
		  }
		  if (ferror(f)!=0)
			return PushResult(L, 0, null);
		  if (success==0) {
			LinyeePop(L, 1);  /* remove last result */
			LinyeePushNil(L);  /* push nil instead */
		  }
		  return n - first;
		}


		private static int IoRead (LinyeeState L) {
		  return GRead(L, GetIOFile(L, IOINPUT), 1);
		}


		private static int FRead (LinyeeState L) {
		  return GRead(L, ToFile(L), 2);
		}


		private static int IoReadLine (LinyeeState L) {
		  Stream f = (LinyeeToUserData(L, LinyeeUpValueIndex(1)) as FilePtr).file;
		  int sucess;
		  if (f == null)  /* file is already closed? */
			LinyeeLError(L, "file is already closed");
		  sucess = ReadLine(L, f);
		  if (ferror(f)!=0)
			return LinyeeLError(L, "%s", strerror(errno()));
		  if (sucess != 0) return 1;
		  else {  /* EOF */
			if (LinyeeToBoolean(L, LinyeeUpValueIndex(2)) != 0) {  /* generator created file? */
			  LinyeeSetTop(L, 0);
			  LinyeePushValue(L, LinyeeUpValueIndex(1));
			  AuxClose(L);  /* close it */
			}
			return 0;
		  }
		}

		/* }====================================================== */


		private static int GWrite (LinyeeState L, Stream f, int arg) {
		  int nargs = LinyeeGetTop(L) - 1;
		  int status = 1;
		  for (; (nargs--) != 0; arg++) {
			if (LinyeeType(L, arg) == LINYEE_TNUMBER) {
			  /* optimization: could be done exactly as for strings */
			  status = ((status!=0) &&
				  (fprintf(f, LINYEE_NUMBER_FMT, LinyeeToNumber(L, arg)) > 0)) ? 1 : 0;
			}
			else {
			  uint l;
			  CharPtr s = LinyeeLCheckLString(L, arg, out l);
			  status = ((status!=0) && (fwrite(s, GetUnmanagedSize(typeof(char)), (int)l, f) == l)) ? 1 : 0;
			}
		  }
		  return PushResult(L, status, null);
		}


		private static int IoWrite (LinyeeState L) {
		  return GWrite(L, GetIOFile(L, IOOUTPUT), 1);
		}


		private static int FWrite (LinyeeState L) {
		  return GWrite(L, ToFile(L), 2);
		}

		

		private static int FSeek (LinyeeState L) {
		  int[] mode = { SEEK_SET, SEEK_CUR, SEEK_END };
		  CharPtr[] modenames = { "set", "cur", "end", null };
		  Stream f = ToFile(L);
		  int op = LinyeeLCheckOption(L, 2, "cur", modenames);
		  long offset = LinyeeLOptLong(L, 3, 0);
		  op = fseek(f, offset, mode[op]);
		  if (op != 0)
			return PushResult(L, 0, null);  /* error */
		  else {
			LinyeePushInteger(L, ftell(f));
			return 1;
		  }
		}

		private static int FSetVBuf (LinyeeState L) {
		  CharPtr[] modenames = { "no", "full", "line", null };
		  int[] mode = { _IONBF, _IOFBF, _IOLBF };
		  Stream f = ToFile(L);
		  int op = LinyeeLCheckOption(L, 2, null, modenames);
		  LinyeeIntegerType sz = LinyeeLOptInteger(L, 3, LUAL_BUFFERSIZE);
		  int res = setvbuf(f, null, mode[op], (uint)sz);
		  return PushResult(L, (res == 0) ? 1 : 0, null);
		}



		private static int IoFlush (LinyeeState L) {
			int result = 1;
			try {GetIOFile(L, IOOUTPUT).Flush();} catch {result = 0;}
		  return PushResult(L, result, null);
		}


		private static int FFlush (LinyeeState L) {
			int result = 1;
			try {ToFile(L).Flush();} catch {result = 0;}
			return PushResult(L, result, null);
		}


		private readonly static LinyeeLReg[] iolib = {
		  new LinyeeLReg("close", IoClose),
		  new LinyeeLReg("flush", IoFlush),
		  new LinyeeLReg("input", IoInput),
		  new LinyeeLReg("lines", IoLines),
		  new LinyeeLReg("open", IoOpen),
		  new LinyeeLReg("output", IoOutput),
		  new LinyeeLReg("popen", IoPopen),
		  new LinyeeLReg("read", IoRead),
		  new LinyeeLReg("tmpfile", IoTmpFile),
		  new LinyeeLReg("type", GetIOType),
		  new LinyeeLReg("write", IoWrite),
		  new LinyeeLReg(null, null)
		};


		private readonly static LinyeeLReg[] flib = {
		  new LinyeeLReg("close", IoClose),
		  new LinyeeLReg("flush", FFlush),
		  new LinyeeLReg("lines", FLines),
		  new LinyeeLReg("read", FRead),
		  new LinyeeLReg("seek", FSeek),
		  new LinyeeLReg("setvbuf", FSetVBuf),
		  new LinyeeLReg("write", FWrite),
		  new LinyeeLReg("__gc", IoGC),
		  new LinyeeLReg("__tostring", IoToString),
		  new LinyeeLReg(null, null)
		};


		private static void CreateMeta (LinyeeState L) {
		  LinyeeLNewMetatable(L, LINYEE_FILEHANDLE);  /* create metatable for file files */
		  LinyeePushValue(L, -1);  /* push metatable */
		  LinyeeSetField(L, -2, "__index");  /* metatable.__index = metatable */
		  LinyeeLRegister(L, null, flib);  /* file methods */
		}


		private static void CreateStdFile (LinyeeState L, Stream f, int k, CharPtr fname) {
		  NewFile(L).file = f;
		  if (k > 0) {
			LinyeePushValue(L, -1);
			LinyeeRawSetI(L, LINYEE_ENVIRONINDEX, k);
		  }
		  LinyeePushValue(L, -2);  /* copy environment */
		  LinyeeSetFEnv(L, -2);  /* set it */
		  LinyeeSetField(L, -3, fname);
		}


		private static void NewFEnv (LinyeeState L, LinyeeNativeFunction cls) {
		  LinyeeCreateTable(L, 0, 1);
		  LinyeePushCFunction(L, cls);
		  LinyeeSetField(L, -2, "__close");
		}


		public static int LinyeeOpenIo (LinyeeState L) {
		  CreateMeta(L);
		  /* create (private) environment (with fields IO_INPUT, IO_OUTPUT, __close) */
		  NewFEnv(L, IoFClose);
		  LinyeeReplace(L, LINYEE_ENVIRONINDEX);
		  /* open library */
		  LinyeeLRegister(L, LINYEE_IOLIBNAME, iolib);
		  /* create (and set) default files */
		  NewFEnv(L, IoNoClose);  /* close function for default files */
		  CreateStdFile(L, stdin, IOINPUT, "stdin");
		  CreateStdFile(L, stdout, IOOUTPUT, "stdout");
		  CreateStdFile(L, stderr, 0, "stderr");
		  LinyeePop(L, 1);  /* pop environment for default files */
		  LinyeeGetField(L, -1, "popen");
		  NewFEnv(L, IoPClose);  /* create environment for 'popen' */
		  LinyeeSetFEnv(L, -2);  /* set fenv for 'popen' */
		  LinyeePop(L, 1);  /* pop 'popen' */
		  return 1;
		}

	}
}
