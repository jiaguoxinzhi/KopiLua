using Linyee.Lib.Resource;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Linyee
{
    public class Smain
    {
        public int argc;
        public string[] argv;
        public int status;
    };


    class Program
    {
        static LinyeeState globalL = null;

        static CharPtr progname = Linyee.LINYEE_PROGNAME;



        static void lstop(LinyeeState L, LinyeeDebug ar)
        {
            Linyee.LinyeeSetHook(L, null, 0, 0);
            Linyee.LinyeeLError(L, $"{Resource.interrupted}{Resource.Keyboard21}");
        }


        static void laction(int i)
        {
            //signal(i, SIG_DFL); /* if another SIGINT happens before lstop,
            //						  terminate process (default action) */
            Linyee.LinyeeSetHook(globalL, lstop, Linyee.LINYEE_MASKCALL | Linyee.LINYEE_MASKRET | Linyee.LINYEE_MASKCOUNT, 1);
        }


        static void print_usage()
        {
            Console.Error.Write(
            "usage: {0} [options] [script [args]].\n" +
            "Available options are:\n" +
            "  -e stat  execute string " + Linyee.LINYEE_QL("stat").ToString() + "\n" +
            "  -l name  require library " + Linyee.LINYEE_QL("name").ToString() + "\n" +
            "  -i       enter interactive mode after executing " + Linyee.LINYEE_QL("script").ToString() + "\n" +
            "  -v       show version information\n" +
            "  --       stop handling options\n" +
            "  -        execute stdin and stop handling options\n"
            ,
            progname);
            Console.Error.Flush();
        }


        static void l_message(CharPtr pname, CharPtr msg)
        {
            if (pname != null) Linyee.fprintf(Linyee.stderr, "%s: ", pname);
            Linyee.fprintf(Linyee.stderr, "%s\n", msg);
            Linyee.fflush(Linyee.stderr);
        }


        static int report(LinyeeState L, int status)
        {
            if ((status != 0) && !Linyee.LinyeeIsNil(L, -1))
            {
                CharPtr msg = Linyee.LinyeeToString(L, -1);
                if (msg == null) msg = "(error object is not a string)";
                l_message(progname, msg);
                Linyee.LinyeePop(L, 1);
            }
            return status;
        }


        static int traceback(LinyeeState L)
        {
            if (Linyee.LinyeeIsString(L, 1) == 0)  /* 'message' not a string? */
                return 1;  /* keep it intact */
            Linyee.LinyeeGetField(L, Linyee.LINYEE_GLOBALSINDEX, "debug");
            if (!Linyee.LinyeeIsTable(L, -1))
            {
                Linyee.LinyeePop(L, 1);
                return 1;
            }
            Linyee.LinyeeGetField(L, -1, "traceback");
            if (!Linyee.LinyeeIsFunction(L, -1))
            {
                Linyee.LinyeePop(L, 2);
                return 1;
            }
            Linyee.LinyeePushValue(L, 1);  /* pass error message */
            Linyee.LinyeePushInteger(L, 2);  /* skip this function and traceback */
            Linyee.LinyeeCall(L, 2, 1);  /* call debug.traceback */
            return 1;
        }


        static int docall(LinyeeState L, int narg, int clear)
        {
            int status;
            int base_ = Linyee.LinyeeGetTop(L) - narg;  /* function index */
            Linyee.LinyeePushCFunction(L, traceback);  /* push traceback function */
            Linyee.LinyeeInsert(L, base_);  /* put it under chunk and args */
                                      //signal(SIGINT, laction);
            status = Linyee.LinyeePCall(L, narg, ((clear != 0) ? 0 : Linyee.LINYEE_MULTRET), base_);
            //signal(SIGINT, SIG_DFL);
            Linyee.LinyeeRemove(L, base_);  /* remove traceback function */
                                      /* force a complete garbage collection in case of errors */
            if (status != 0) Linyee.LinyeeGC(L, Linyee.LINYEE_GCCOLLECT, 0);
            return status;
        }

        /// <summary>
        /// 输出版本号
        /// </summary>
        static void print_version()
        {
            l_message(null, Linyee.LINYEE_RELEASE + "  " + Linyee.LINYEE_COPYRIGHT);
        }


        static int getargs(LinyeeState L, string[] argv, int n)
        {
            int narg;
            int i;
            int argc = argv.Length; /* count total number of arguments */
            narg = argc - (n + 1);  /* number of arguments to the script */
            Linyee.LinyeeLCheckStack(L, narg + 3, "too many arguments to script");
            for (i = n + 1; i < argc; i++)
                Linyee.LinyeePushString(L, argv[i]);
            Linyee.LinyeeCreateTable(L, narg, n + 1);
            for (i = 0; i < argc; i++)
            {
                Linyee.LinyeePushString(L, argv[i]);
                Linyee.LinyeeRawSetI(L, -2, i - n);
            }
            return narg;
        }

        /// <summary>
        /// 文件模式
        /// </summary>
        /// <param name="L"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        static int dofile(LinyeeState L, CharPtr name)
        {
            int status = (Linyee.LinyeeLLoadFile(L, name) != 0) || (docall(L, 0, 1) != 0) ? 1 : 0;
            return report(L, status);
        }


        static int dostring(LinyeeState L, CharPtr s, CharPtr name)
        {
            int status = (Linyee.LinyeeLLoadBuffer(L, s, (uint)Linyee.strlen(s), name) != 0) || (docall(L, 0, 1) != 0) ? 1 : 0;
            return report(L, status);
        }


        static int dolibrary(LinyeeState L, CharPtr name)
        {
            Linyee.LinyeeGetGlobal(L, "require");
            Linyee.LinyeePushString(L, name);
            return report(L, docall(L, 1, 1));
        }


        static CharPtr get_prompt(LinyeeState L, int firstline)
        {
            CharPtr p;
            Linyee.LinyeeGetField(L, Linyee.LINYEE_GLOBALSINDEX, (firstline != 0) ? "_PROMPT" : "_PROMPT2");
            p = Linyee.LinyeeToString(L, -1);
            if (p == null) p = ((firstline != 0) ? Linyee.LINYEE_PROMPT : Linyee.LINYEE_PROMPT2);
            Linyee.LinyeePop(L, 1);  /* remove global */
            return p;
        }


        static int incomplete(LinyeeState L, int status)
        {
            if (status == Linyee.LINYEE_ERRSYNTAX)
            {
                uint lmsg;
                CharPtr msg = Linyee.LinyeeToLString(L, -1, out lmsg);
                CharPtr tp = msg + lmsg - (Linyee.strlen(Linyee.LINYEE_QL("<eof>")));
                if (Linyee.strstr(msg, Linyee.LINYEE_QL("<eof>")) == tp)
                {
                    Linyee.LinyeePop(L, 1);
                    return 1;
                }
            }
            return 0;  /* else... */
        }


        static int pushline(LinyeeState L, int firstline)
        {
            CharPtr buffer = new char[Linyee.LINYEE_MAXINPUT];
            CharPtr b = new CharPtr(buffer);
            int l;
            CharPtr prmt = get_prompt(L, firstline);
            if (!Linyee.ly_readline(L, b, prmt))
                return 0;  /* no input */
            l = Linyee.strlen(b);
            if (l > 0 && b[l - 1] == '\n')  /* line ends with newline? */
                b[l - 1] = '\0';  /* remove it */
            if ((firstline != 0) && (b[0] == '='))  /* first line starts with `=' ? */
                Linyee.LinyeePushFString(L, "return %s", b + 1);  /* change it to `return' */
            else
                Linyee.LinyeePushString(L, b);
            Linyee.ly_freeline(L, b);
            return 1;
        }


        static int loadline(LinyeeState L)
        {
            int status;
            Linyee.LinyeeSetTop(L, 0);
            if (pushline(L, 1) == 0)
                return -1;  /* no input */
            for (; ; )
            {  /* repeat until gets a complete line */
                status = Linyee.LinyeeLLoadBuffer(L, Linyee.LinyeeToString(L, 1), Linyee.LinyeeStrLen(L, 1), "=stdin");
                if (incomplete(L, status) == 0) break;  /* cannot try to add lines? */
                if (pushline(L, 0) == 0)  /* no more input? */
                    return -1;
                Linyee.LinyeePushLiteral(L, "\n");  /* add a new line... */
                Linyee.LinyeeInsert(L, -2);  /* ...between the two lines */
                Linyee.LinyeeConcat(L, 3);  /* join them */
            }
            Linyee.ly_saveline(L, 1);
            Linyee.LinyeeRemove(L, 1);  /* remove line */
            return status;
        }

        /// <summary>
        /// 控制台模式
        /// </summary>
        /// <param name="L"></param>
        static void dotty(LinyeeState L)
        {
            int status;
            CharPtr oldprogname = progname;
            progname = null;
            while ((status = loadline(L)) != -1)
            {
                if (status == 0) status = docall(L, 0, 0);
                report(L, status);
                if (status == 0 && Linyee.LinyeeGetTop(L) > 0)
                {  /* any result to print? */
                    Linyee.LinyeeGetGlobal(L, "print");
                    Linyee.LinyeeInsert(L, 1);
                    if (Linyee.LinyeePCall(L, Linyee.LinyeeGetTop(L) - 1, 0, 0) != 0)
                        l_message(progname, Linyee.LinyeePushFString(L,
                                               "error calling " + Linyee.LINYEE_QL("print").ToString() + " (%s)",
                                               Linyee.LinyeeToString(L, -1)));
                }
            }
            Linyee.LinyeeSetTop(L, 0);  /* clear stack */
            Linyee.fputs("\n", Linyee.stdout);
            Linyee.fflush(Linyee.stdout);
            progname = oldprogname;
        }


        static int handle_script(LinyeeState L, string[] argv, int n)
        {
            int status;
            CharPtr fname;
            int narg = getargs(L, argv, n);  /* collect arguments */
            Linyee.LinyeeSetGlobal(L, "arg");
            fname = argv[n];
            if (Linyee.strcmp(fname, "-") == 0 && Linyee.strcmp(argv[n - 1], "--") != 0)
                fname = null;  /* stdin */
            status = Linyee.LinyeeLLoadFile(L, fname);
            Linyee.LinyeeInsert(L, -(narg + 1));
            if (status == 0)
                status = docall(L, narg, 0);
            else
                Linyee.LinyeePop(L, narg);
            return report(L, status);
        }


        /* check that argument has no extra characters at the end */
        //#define notail(x)	{if ((x)[2] != '\0') return -1;}


        static int collectargs(string[] argv, ref int pi, ref int pv, ref int pe)
        {
            int i;
            for (i = 1; i < argv.Length; i++)
            {
                if (argv[i][0] != '-')  /* not an option? */
                    return i;
                switch (argv[i][1])
                {  /* option */
                    case '-':
                        if (argv[i].Length != 2) return -1;
                        return (i + 1) >= argv.Length ? i + 1 : 0;

                    case '\0':
                        return i;

                    case 'i':
                        if (argv[i].Length != 2) return -1;
                        pi = 1;
                        if (argv[i].Length != 2) return -1;
                        pv = 1;
                        break;

                    case 'v':
                        if (argv[i].Length != 2) return -1;
                        pv = 1;
                        break;

                    case 'e':
                        pe = 1;
                        if (argv[i].Length == 2)
                        {
                            i++;
                            if (argv[i] == null) return -1;
                        }
                        break;

                    case 'l':
                        if (argv[i].Length == 2)
                        {
                            i++;
                            if (i >= argv.Length) return -1;
                        }
                        break;
                    default: return -1;  /* invalid option */
                }
            }
            return 0;
        }


        static int runargs(LinyeeState L, string[] argv, int n)
        {
            int i;
            for (i = 1; i < n; i++)
            {
                if (argv[i] == null) continue;
                Linyee.LinyeeAssert(argv[i][0] == '-');
                switch (argv[i][1])
                {  /* option */
                    case 'e':
                        {
                            string chunk = argv[i].Substring(2);
                            if (chunk == "") chunk = argv[++i];
                            Linyee.LinyeeAssert(chunk != null);
                            if (dostring(L, chunk, "=(command line)") != 0)
                                return 1;
                            break;
                        }
                    case 'l':
                        {
                            string filename = argv[i].Substring(2);
                            if (filename == "") filename = argv[++i];
                            Linyee.LinyeeAssert(filename != null);
                            if (dolibrary(L, filename) != 0)
                                return 1;  /* stop if file fails */
                            break;
                        }
                    default: break;
                }
            }
            return 0;
        }


        static int handle_luainit(LinyeeState L)
        {
            CharPtr init = Linyee.getenv(Linyee.LINYEE_INIT);
            if (init == null) return 0;  /* status OK */
            else if (init[0] == '@')
                return dofile(L, init + 1);
            else
                return dostring(L, init, "=" + Linyee.LINYEE_INIT);
        }




        static int pmain(LinyeeState L)
        {
            Smain s = (Smain)Linyee.LinyeeToUserData(L, 1);
            string[] argv = s.argv;
            int script;
            int has_i = 0, has_v = 0, has_e = 0;
            globalL = L;
            if ((argv.Length > 0) && (argv[0] != "")) progname = argv[0];
            Linyee.LinyeeGC(L, Linyee.LINYEE_GCSTOP, 0);  /* stop collector during initialization */
            Linyee.LinyeeLOpenLibs(L);  /* open libraries */
            Linyee.LinyeeGC(L, Linyee.LINYEE_GCRESTART, 0);
            s.status = handle_luainit(L);
            if (s.status != 0) return 0;
            script = collectargs(argv, ref has_i, ref has_v, ref has_e);
            if (script < 0)
            {  /* invalid args? */
                print_usage();
                s.status = 1;
                return 0;
            }
            if (has_v != 0) print_version();
            s.status = runargs(L, argv, (script > 0) ? script : s.argc);
            if (s.status != 0) return 0;
            if (script != 0)
                s.status = handle_script(L, argv, script);
            if (s.status != 0) return 0;
            if (has_i != 0)
                dotty(L);
            else if ((script == 0) && (has_e == 0) && (has_v == 0))
            {
                if (Linyee.ly_stdin_is_tty() != 0)
                {
                    print_version();
                    dotty(L);
                }
                else dofile(L, null);  /* executes stdin as a file */
            }
            return 0;
        }

        static int Main(string[] args)
        {
            //services.AddLocalization(options => options.ResourcesPath = "Resources");
            //Console.OutputEncoding = System.Text.Encoding.UTF8;//第一种方式：指定编码
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//第二种方式

            Thread.CurrentThread.CurrentCulture = new CultureInfo("zh-CN");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("zh-CN");

            Console.WriteLine($"{Resource.Hello_World}{Resource.Keyboard21}");

            List<string> newargs = new List<string>(args);
            newargs.Insert(0, Assembly.GetExecutingAssembly().Location);
            args = (string[])newargs.ToArray();

            int status;
            Smain s = new Smain();
            LinyeeState L = Linyee.LinyeeOpen();  /* create state */
            if (L == null)
            {
                l_message(args[0], "cannot create state: not enough memory");
                return Linyee.EXIT_FAILURE;
            }
            s.argc = args.Length;
            s.argv = args;
            status = Linyee.LinyeeCPCall(L, pmain, s);
            report(L, status);
            Linyee.LinyeeClose(L);
            return (status != 0) || (s.status != 0) ? Linyee.EXIT_FAILURE : Linyee.EXIT_SUCCESS;
            //return Linyee.EXIT_SUCCESS;

            //lua.LoadCLRPackage();
            //lua.DoString(@" import ('NLinyeeSample') ");
            //lua["gValue"] = "This is a global value"; // You can set a global value.
            //lua.DoString(@"print(gValue)");
            //lua.DoString(" require 'rubygems' ");
            //lua.DoString(" local restyredis = require 'resty.redis'; ");
            //lua.DoString(" local redis = redis:new() ");
            //var res = lua.DoString("return redis.call('set','a')");
            //Console.WriteLine($"{res}");
            //Console.Read();

        }
    }
}
