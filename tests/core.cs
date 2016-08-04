
using System;
using NUnit.Framework;
using System.IO;
using KopiLua;
using System.Text;

namespace Tests.iOS
{
	[TestFixture]
	public class core
	{
		LuaState state;
		string GetTestPath(string name)
		{
			string filePath = Path.Combine (Path.Combine ("LuaTests", "core"), name + ".lua");
			return filePath;
		}

		void AssertFile (string path)
		{
			string error = string.Empty;

			int result = Lua.LuaLLoadFile(state, path);

			if (result != 0) {
				CharPtr pstring = Lua.LuaToString (state, 1);
				if (pstring != null)
					error = pstring.ToString();
			}

			Assert.True(result == 0, "Fail loading file: " + path + "ERROR:" + error);

			result = Lua.LuaPCall(state, 0, -1, 0);

			if (result != 0) {
				CharPtr pstring = Lua.LuaToString(state, 1);
				if (pstring != null)
					error = pstring.ToString();
			}


			Assert.True(result == 0, "Fail calling file: " + path + " ERROR: " + error);
		}

        void AssertString(string str)
        {
            if (Lua.LuaLLoadString(state, str) != 0)
            {
                var error = Lua.LuaToString(state, -1);
                Lua.LuaPop(state, 1);

                Assert.Fail("LoadString failed.");
            }

            if (Lua.LuaPCall(state, 0, 0, 0) != 0)
            {
                Assert.Fail("Function call failed: " + Lua.LuaToString(state, -1).ToString());
            }
        }

		void TestLuaFile (string name)
		{
			string path = GetTestPath (name);
			AssertFile (path);
		}


		[SetUp]
		public void Setup()
		{
			state = Lua.LuaLNewState ();
			Lua.LuaLOpenLibs (state);
		}

		[TearDown]
		public void TearDown ()
		{
			Lua.LuaClose (state);
			state = null;
		}

		[Test]
		public void Bisect ()
		{
			TestLuaFile ("bisect");
		}

		[Test]
		public void CF ()
		{
			TestLuaFile ("cf");
		}

		[Test]
		public void Env ()
		{
			TestLuaFile ("env");
		}

		[Test]
		public void Factorial ()
		{
			TestLuaFile ("factorial");
		}

		[Test]
		public void FibFor ()
		{
			TestLuaFile ("fibfor");
		}

		[Test]
		public void Life ()
		{
			TestLuaFile ("life");
		}

		[Test]
		public void Printf ()
		{
			TestLuaFile ("printf");
		}

		[Test]
		public void ReadOnly ()
		{
			TestLuaFile ("readonly");
		}

		[Test]
		public void Sieve ()
		{
			TestLuaFile ("sieve");
		}

		[Test]
		public void Sort ()
		{
			TestLuaFile ("sort");
		}

		[Test]
		public void TraceGlobals ()
		{
			TestLuaFile ("trace-globals");
		}
		
		[Test]
        public void StringLib()
        {
            // Test for string.gsub working with more than MAXCALLS characters.

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append('a', Lua.MAXCCALLS);
            string before = sb.ToString() + " RR " + "TRAIL";
            string after = sb.ToString() + " 123456789 " + "TRAIL";

            string str = "assert(string.gsub(\"" + before + "\", \"RR\", \"123456789\") == \"" + after +"\")";

            AssertString(str);

            // Test for string.format producing expected results.

            str = "local fmts = string.format(\"%02d:%02d:%02d - %02d.%02d.%04d\", 16, 23, 42, 4, 8, 2015)\nprint(\"+\" .. fmts .. \"+\")\nassert(fmts == \"16:23:42 - 04.08.2015\")";

            AssertString(str);
        }

        [Test]
        public void OSLib()
        {
            string sd1 = "local d1 = os.date(\"*t\", 63568252248)\nprint('d1.yday=' .. d1.yday)";
            string st1 = "local t1 = os.time({year=2015, month=5, day=28, hour=15, min=50, sec=48})\nprint('t1=' .. tostring(t1))";
            string st2 = "local t2 = os.time({year=2015, month=5, day=28, hour=15, min=51, sec=18})\nprint(t2)";

            // Test for os.date giving proper values for a specified time.
            AssertString(sd1 + "\nassert(d1.yday == 146 and d1.year == 2015)");

            // Test for os.time giving proper values for a full time.
            AssertString(st1 + "\nassert(t1 == 63568425048)");

            // Test for os.time difference giving result in seconds.
            AssertString(st1 + "\n" + st2 + "\nassert(t2 - t1 == 30)");

            // Test for os.difftime giving result in seconds.
            AssertString(st1 + "\n" + st2 + "\nassert(os.difftime(t2, t1) == 30)");
        }

        [Test]
        public void MathLib()
        {
            // Test for math.random() giving different values after init with math.randomseed(os.time()).
            AssertString("local s1 = os.time({year=2015, month=5, day=28, hour=15, min=50, sec=48})\nlocal s2 = s1 + 1\nmath.randomseed(s1)\nmath.random()\nmath.random()\nlocal n1 = math.random()\nmath.randomseed(s2)\nmath.random()\nmath.random()\nlocal n2 = math.random()\nprint(tostring(n1) .. ',' .. tostring(n2))\nassert(n1 ~= n2)");

            // Test for math.random() giving same values for math.randomseed(42).
            AssertString("math.randomseed(42)\nmath.random()\nmath.random()\nlocal n1 = math.random()\nmath.randomseed(42)\nmath.random()\nmath.random()\nlocal n2 = math.random()\nprint(tostring(n1) .. ',' .. tostring(n2))\nassert(n1 == n2)");
        }
	}
}
